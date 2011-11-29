﻿/*
 * User: aleksey.nakoryakov
 * Date: 29.11.2011
 * Time: 15:11
 */
using System;
using System.Linq;

using acad = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.DatabaseServices;

namespace Bargool.Acad.Extensions
{
	/// <summary>
	/// Класс расширений для DBObject
	/// </summary>
	public static class DBObjectExtensions
	{
		
		/// <summary>
		/// Добавляет Xrecord к объекту
		/// </summary>
		/// <param name="o">Объект для добавления, куда добавляется Xrecord</param>
		/// <param name="xrecordName">Имя записи</param>
		/// <param name="buffer">Данные для добавления</param>
		/// <param name="rewrite">Перезаписывать Xrecord? Если false - будем добавлять в конец</param>
		/// <param name="addDuplicates">Добавлять дубликаты? Если false - дубликаты не добавлять</param>
		public static void WriteXrecord(this DBObject o, string xrecordName, ResultBuffer buffer, bool rewrite, bool addDuplicates)
		{
			if (xrecordName == null)
				throw new ArgumentNullException("xrecordName is null");
			if (buffer == null)
				throw new ArgumentNullException("buffer is null");
			if (o.ExtensionDictionary==ObjectId.Null || o.ExtensionDictionary.IsErased) {
				o.CreateExtensionDictionary();
			}
			using (DBDictionary dict = (DBDictionary)o.ExtensionDictionary.GetObject(OpenMode.ForWrite))
			{
				Xrecord xrecord = new Xrecord();
				if (dict.Contains(xrecordName)) {
					xrecord = (Xrecord)dict.GetAt(xrecordName).GetObject(OpenMode.ForWrite);
				}
				else {
					dict.SetAt(xrecordName, xrecord);
					o.Database.TransactionManager.AddNewlyCreatedDBObject(o, true);
				}
				if (rewrite) {
					xrecord.Data = buffer;
				}
				else {
					TypedValue[] tempBuffer = xrecord.Data.AsArray();
					if (addDuplicates) {
						xrecord.Data = new ResultBuffer(tempBuffer
						                                .Concat(buffer.AsArray())
						                                .ToArray());
					}
					else {
						xrecord.Data = new ResultBuffer(tempBuffer
						                                .Union(buffer.AsArray())
						                                .ToArray());
					}
				}
			}
		}
		
		/// <summary>
		/// Возвращает содержимое Xrecord
		/// </summary>
		/// <param name="o">Объект, из которого извлекается содержимое Xrecord</param>
		/// <param name="xrecordName">Имя записи</param>
		/// <returns>Содержимое Xrecord, либо null, если такого Xrecord нет</returns>
		public static ResultBuffer GetXrecord(this DBObject o, string xrecordName)
		{
			if (xrecordName == null)
				throw new ArgumentNullException("xrecordName is null");
			if (o.ExtensionDictionary != ObjectId.Null &&
			   !o.ExtensionDictionary.IsErased)
			{
				using (DBDictionary dict = (DBDictionary)o.ExtensionDictionary.GetObject(OpenMode.ForRead))
				{
					if (dict.Contains(xrecordName))
					{
						Xrecord xrecord = dict.GetAt(xrecordName).GetObject(OpenMode.ForRead) as Xrecord;
						if (xrecord != null)
							return xrecord.Data;
					}
				}
			}
			return null;
		}
		
		/// <summary>
		/// Удаляет Xrecord с заданным именем, если после удаления остается пустой словарь - он также удаляется
		/// </summary>
		/// <param name="o">Объект, у которого удаляется Xrecord</param>
		/// <param name="xrecordName">Имя записи</param>
		public static void DeleteXrecord(this DBObject o, string xrecordName)
		{
			if (xrecordName == null)
				throw new ArgumentNullException("xrecordName is null");
			if (o.ExtensionDictionary != ObjectId.Null &&
			   !o.ExtensionDictionary.IsErased)
			{
				using (DBDictionary dict = (DBDictionary)o.ExtensionDictionary.GetObject(OpenMode.ForRead))
				{
					if (dict.Contains(xrecordName))
					{
					    dict.Remove(xrecordName);
					    if (dict.Count==0)
					    {
					    	dict.Erase();
					    }
					}
				}
			}
		}
	}
}
