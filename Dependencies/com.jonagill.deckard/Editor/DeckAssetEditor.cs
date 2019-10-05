using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Deckard.Parsing;
using EditorGUITable;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Deckard.Editor
{
    [CustomEditor(typeof(DeckAsset))]
    public class DeckAssetEditor : UnityEditor.Editor
    {
        private GUITableState guiTableState;

        private DeckAsset Target => (DeckAsset) target;

        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    EditorGUILayout.LabelField("CSV:", GUILayout.ExpandWidth(false), GUILayout.Width(50));

                    var absolutePath = Target.CsvAbsolutePath;

                    var hasPath = !string.IsNullOrEmpty(absolutePath);
                    var hasFile = hasPath && File.Exists(absolutePath);

                    var displayPath = Target.CsvDisplayPath;
                    if (!hasPath)
                    {
                        displayPath = "<UNSET>";
                    }
                    else if (!hasFile)
                    {
                        displayPath = "<MISSING>";
                    }

                    EditorGUILayout.SelectableLabel(displayPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorGUIUtility.IconContent("settings"), GUILayout.ExpandWidth(false)))
                    {
                        var directory = hasPath
                            ? Path.GetDirectoryName(absolutePath)
                            : Application.dataPath;

                        var path = EditorUtility.OpenFilePanel("Select CSV file", directory, "csv");
                        if (!string.IsNullOrEmpty(path))
                        {
                            Undo.RecordObject(Target, "Set CSV source");
                            Target.SetCsvPath(path);
                        }
                    }

                    GUI.enabled = hasFile;
                    if (GUILayout.Button(EditorGUIUtility.IconContent("refresh"), GUILayout.ExpandWidth(false)))
                    {
                        Undo.RecordObject(Target, "Refresh CSV source");
                        Target.RefreshCsvSheet();
                    }

                    GUI.enabled = true;
                }

                EditorGUILayout.Space();

                if (Target.CsvSheet.RecordCount > 0)
                {
                    DrawSheetTable(Target.CsvSheet);
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawSheetTable(CsvSheet sheet)
        {
            var viewWidth = EditorGUIUtility.currentViewWidth;
            var columnWidth = viewWidth / sheet.Headers.Count;
            var columns = sheet.Headers
                .Select(header => new TableColumn(
                    TableColumn.Title(header), 
                    TableColumn.Sortable(true),
                    TableColumn.Width(columnWidth)))
                .ToList();

            var rows = sheet.Records
                .Select(r =>
                {
                    return r.Fields
                        .Select(f => (TableCell) new SelectableLabelCell(f))
                        .ToList();
                })
                .ToList();

            guiTableState = GUITableLayout.DrawTable(
                guiTableState,
                columns,
                rows,
                GUITableOption.AllowScrollView(true));
        }

        private class SelectableLabelCell : TableCell
        {
            private readonly string value;

            public override void DrawCell (Rect rect)
            {
                EditorGUI.SelectableLabel(rect, value);
            }

            public override string comparingValue => value;

            public override int CompareTo(object other)
            {
                var selectableOther = other as SelectableLabelCell;
                if (selectableOther != null)
                {
                    if (int.TryParse(value, out var myInt) && int.TryParse(selectableOther.value, out var otherInt))
                    {
                        return myInt.CompareTo(otherInt);
                    }
                    
                    if (float.TryParse(value, out var myFloat) && float.TryParse(selectableOther.value, out var otherFloat))
                    {
                        return myFloat.CompareTo(otherFloat);
                    }
                }

                return base.CompareTo(other);
            }

            public SelectableLabelCell (string value)
            {
                this.value = value;
            }
        }
    }
}
