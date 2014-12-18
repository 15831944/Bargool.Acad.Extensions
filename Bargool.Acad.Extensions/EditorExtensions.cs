﻿/*
 * User: aleksey
 * Date: 01.02.2014
 * Time: 9:35
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace Bargool.Acad.Extensions
{
    /// <summary>
    /// Description of EditorExtensions.
    /// </summary>
    public static class EditorExtensions
    {
        public static void WriteLine(this Editor ed, string message, params object[] parameters)
        {
            string s = string.Format(message, parameters);
            ed.WriteMessage(s + "\n");
        }

        /// <summary>
        /// Borrowed from http://www.theswamp.org/index.php?topic=46442.msg514605#msg514605
        /// </summary>
        public static void Zoom(this Editor ed, Extents3d ext)
        {
            if (ed == null)
                throw new ArgumentNullException("ed");

            using (ViewTableRecord view = ed.GetCurrentView())
            {
                ext.TransformBy(view.WorldToEye());
                view.Width = ext.MaxPoint.X - ext.MinPoint.X;
                view.Height = ext.MaxPoint.Y - ext.MinPoint.Y;
                view.CenterPoint = new Point2d(
                    (ext.MaxPoint.X + ext.MinPoint.X) / 2.0,
                    (ext.MaxPoint.Y + ext.MinPoint.Y) / 2.0);
                ed.SetCurrentView(view);
            }
        }

        /// <summary>
        /// Borrowed from http://www.theswamp.org/index.php?topic=46442.msg514605#msg514605
        /// </summary>
        /// <param name="ed"></param>
        public static void ZoomExtents(this Editor ed)
        {
            if (ed == null)
                throw new ArgumentNullException("ed");

            Database db = ed.Document.Database;
            Extents3d ext = (short)Application.GetSystemVariable("cvport") == 1 ?
                new Extents3d(db.Pextmin, db.Pextmax) :
                new Extents3d(db.Extmin, db.Extmax);
            ed.Zoom(ext);
        }

        /// <summary>
        /// Returns the transformation matrix from the ViewportTableRecord DCS to WCS.
        /// </summary>
        /// <remarks>Borrowed from http://www.acadnetwork.com/index.php?topic=232.msg406#msg406</remarks>
        /// <param name="view">The ViewportTableRecord instance this method applies to.</param>
        /// <returns>The DCS to WCS transformation matrix.</returns>
        public static Matrix3d EyeToWorld(this AbstractViewTableRecord view)
        {
            return
                Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) *
                Matrix3d.Displacement(view.Target - Point3d.Origin) *
                Matrix3d.PlaneToWorld(view.ViewDirection);
        }

        /// <summary>
        /// Returns the transformation matrix from the ViewportTableRecord WCS to DCS.
        /// </summary>
        /// <remarks>Borrowed from http://www.acadnetwork.com/index.php?topic=232.msg406#msg406</remarks>
        /// <param name="view">The ViewportTableRecord instance this method applies to.</param>
        /// <returns>The WCS to DCS transformation matrix.</returns>
        public static Matrix3d WorldToEye(this AbstractViewTableRecord view)
        {
            return view.EyeToWorld().Inverse();
        }

        // поле для использования в обработчике собятия
        private static Func<ObjectId, bool> _filterObjectMethod;

        /// <summary>
        /// Получение выделенных объектов. При этом учитывается возможность предварительного выбора объектов
        /// </summary>
        /// <param name="ed"></param>
        /// <param name="pso"></param>
        /// <returns></returns>
        public static PromptSelectionResult GetFilteredSelection(this Editor ed, PromptSelectionOptions pso)
        {
            return GetFilteredSelection(ed, pso, id => true);
        }
        
        /// <summary>
        /// Получение выделенных объектов с учетом заданного фильтра. При этом учитывается возможность предварительного выбора объектов. 
        /// </summary>
        /// <param name="ed"></param>
        /// <param name="pso"></param>
        /// <param name="filterObjectMethod">Метод, по которому происходит фильтрация объектов</param>
        /// <returns></returns>
        public static PromptSelectionResult GetFilteredSelection(this Editor ed, PromptSelectionOptions pso, Func<ObjectId, bool> filterObjectMethod)
        {
            _filterObjectMethod = filterObjectMethod;
            PromptSelectionResult res = ed.SelectImplied();

            //IEnumerable<ObjectId> filteredObjects = null;

            if (res.Status == PromptStatus.OK)
            {
                var filteredObjects = res.GetSelectedObjectIds().Where(id => _filterObjectMethod(id));
                ed.SetImpliedSelection(filteredObjects.ToArray());
                res = ed.SelectImplied();
                //ed.SetImpliedSelection(new ObjectId[0]);
            }
            else
            {
                SelectionAddedEventHandler selHandler = new SelectionAddedEventHandler(ed_SelectionAdded);
                ed.SelectionAdded += selHandler;
                res = ed.GetSelection(pso);
                ed.SelectionAdded -= selHandler;
            }

            _filterObjectMethod = null;
            return res;
        }

        /// <summary>
        /// Обработчик события для применения фильтрации при выборе объектов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ed_SelectionAdded(object sender, SelectionAddedEventArgs e)
        {
            ObjectId[] ids = e.AddedObjects.GetObjectIds();

            for (int i = 0; i < ids.Length; i++)
            {
                if (!_filterObjectMethod(ids[i]))
                    e.Remove(i);
            }
        }
    }
}
