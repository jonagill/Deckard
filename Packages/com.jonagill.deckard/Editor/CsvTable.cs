using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using Deckard.Data;

namespace Deckard.Editor
{
    public class CsvTable
    {
        public CsvSheet Sheet { get; }

        private const int COLUMN_WIDTH = 64;

        private CsvTreeView _treeView;

        public float DesiredWidth => Sheet.Headers.Count * COLUMN_WIDTH;
        public float DesiredHeight => (Sheet.RecordCount+1.5f) * EditorGUIUtility.singleLineHeight;

        public CsvTable(CsvSheet sheet)
        {
            Sheet = sheet;
            
            TreeViewState state = new TreeViewState();

            //add name
            var columnCount = sheet.Headers.Count;
            MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                columns[i] = new MultiColumnHeaderState.Column();
                columns[i].allowToggleVisibility = false;
                columns[i].headerContent = new GUIContent(sheet.Headers[i]);
                columns[i].minWidth = 64;
                columns[i].width = columns[i].minWidth;
                columns[i].canSort = true;
            }

            MultiColumnHeaderState headerstate = new MultiColumnHeaderState(columns);
            MultiColumnHeader header = new MultiColumnHeader(headerstate);

            _treeView = new CsvTreeView(state, header, sheet);
            _treeView.Reload();
        }

        public void DrawLayout(int maxHeight = -1)
        {
            float width, height;
            width = DesiredWidth;

            if (maxHeight > 0)
            {
                height = Mathf.Min(maxHeight, DesiredHeight);
            }
            else
            {
                height = DesiredHeight;
            }
            var rect = GUILayoutUtility.GetRect(width, height);
            Draw(rect);
        }

        public void Draw(Rect r)
        {
            _treeView.OnGUI(r);
        }
    }

    public class CsvTreeView : TreeView
    {
        public CsvSheet Sheet { get; private set; }
        
        public CsvTreeView(TreeViewState state, MultiColumnHeader header, CsvSheet sheet) : base(state, header)
        {
            Sheet = sheet;
            
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            cellMargin = 5;

            multiColumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.ResizeToFit();
        }

        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            Sort(GetRows());
            Repaint();
        }

        public void AddRecord(CsvSheet.Record record)
        {
            AddRecordInternal(record);

            var rows = GetRows();
            Sort(rows);

            Repaint();
        }

        private void AddRecordInternal(CsvSheet.Record record)
        {
            var rows = GetRows();
            var newItem = new CsvTreeViewItem(record, this);

            rootItem.AddChild(newItem);
            rows.Add(newItem);
        }

        void Sort(IList<TreeViewItem> rows)
        {
            if (multiColumnHeader.sortedColumnIndex == -1)
                return;

            if (rows.Count == 0)
                return;

            int sortedColumn = multiColumnHeader.sortedColumnIndex;
            var children = rootItem.children.Cast<CsvTreeViewItem>();

            var comparer = new CsvColumnComparer(Sheet, sortedColumn);
            var ordered = multiColumnHeader.IsSortedAscending(sortedColumn)
                ? children.OrderBy(item => item.Record, comparer)
                : children.OrderByDescending(item => item.Record, comparer);

            rows.Clear();
            foreach (var v in ordered)
            {
                rows.Add(v);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (CsvTreeViewItem) args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                Rect r = args.GetCellRect(i);
                int column = args.GetColumn(i);

                item.Record.TryGetField(column, out var label);
                EditorGUI.SelectableLabel(r, label);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem();

            root.depth = -1;
            root.id = -1;
            root.parent = null;
            root.children = new List<TreeViewItem>();

            foreach (var record in Sheet.Records)
            {
                var child = new CsvTreeViewItem(record, this);
                root.AddChild(child);
            }

            return root;
        }


    }

    public sealed class CsvTreeViewItem : TreeViewItem
    {
        private static int nextId;

        public CsvSheet.Record Record { get; }

        public CsvTreeViewItem(CsvSheet.Record record, CsvTreeView treeView)
        {
            Record = record;
            children = new List<TreeViewItem>();
            depth = 0;
            id = nextId++;
        }
    }

    public class CsvColumnComparer : IComparer<CsvSheet.Record>
    {
        private enum CompareType
        {
            String,
            Int,
            Float
        }

        private CompareType compareType;
        private int sortedColumnIndex;
        
        public CsvColumnComparer(CsvSheet sheet, int sortedColumnIndex)
        {
            this.sortedColumnIndex = sortedColumnIndex;
            
            var columnFields = sheet.Records.Select(r =>
                {
                    r.TryGetField(sortedColumnIndex, out var field);
                    return field;
                })
                .Where(f => !string.IsNullOrEmpty(f));

            if (columnFields.All(f => int.TryParse(f, out _)))
            {
                compareType = CompareType.Int;
            }
            else if (columnFields.All(f => float.TryParse(f, out _)))
            {
                compareType = CompareType.Float;
            }
            else
            {
                compareType = CompareType.String;
            }
        }
        
        public int Compare(CsvSheet.Record x, CsvSheet.Record y)
        {
            x.TryGetField(sortedColumnIndex, out var xField);
            y.TryGetField(sortedColumnIndex, out var yField);

            switch (compareType)
            {
                case CompareType.Int:
                    return CompareIntField(xField, yField);
                case CompareType.Float:
                    return CompareFloatField(xField, yField);
                case CompareType.String:
                default:
                    return CompareStringField(xField, yField);
            }

        }

        private int CompareIntField(string x, string y)
        {
            int.TryParse(x, out var xInt);
            int.TryParse(y, out var yInt);
            return xInt.CompareTo(yInt);
        }
        
        private int CompareFloatField(string x, string y)
        {
            float.TryParse(x, out var xFloat);
            float.TryParse(y, out var yFloat);
            return xFloat.CompareTo(yFloat);
        }

        private int CompareStringField(string x, string y)
        {
            return string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}