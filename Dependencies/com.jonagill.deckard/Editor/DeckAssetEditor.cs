using System;
using System.IO;
using System.Linq;
using Deckard.Data;
using EditorGUITable;
using UnityEditor;
using UnityEditor.SceneManagement;
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
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    var absolutePath = Target.CsvAbsolutePath;

                    var hasPath = !string.IsNullOrEmpty(absolutePath);
                    var hasFile = hasPath && File.Exists(absolutePath);

                    var displayPath = Target.CsvDisplayPath;
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("CSV:", GUILayout.ExpandWidth(false), GUILayout.Width(50));
                        
                        if (!hasPath)
                        {
                            displayPath = "<UNSET>";
                        }
                        else if (!hasFile)
                        {
                            displayPath = "<MISSING>";
                        }

                        EditorGUILayout.SelectableLabel(displayPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
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
                                EditorUtility.SetDirty(Target);
                            }
                        }

                        GUI.enabled = hasFile;
                        if (GUILayout.Button(EditorGUIUtility.IconContent("refresh"), GUILayout.ExpandWidth(false)))
                        {
                            Undo.RecordObject(Target, "Refresh CSV source");
                            Target.RefreshCsvSheet();
                            EditorUtility.SetDirty(Target);
                        }

                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_project"), GUILayout.ExpandWidth(false)))
                        {
                            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(Target.CsvDirectoryPath, 0);
                        }

                        
                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), GUILayout.ExpandWidth(false)))
                        {
                            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(Target.CsvAbsolutePath, 0);
                        }

                        GUI.enabled = true;
                    }
                }
                
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cardPrefab"));
                }
                
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    var nameKeyProperty = serializedObject.FindProperty("cardNameKey");
                    var columnOptions = Target.CsvSheet.Headers.ToArray();
                    var optionIndex = Array.IndexOf(columnOptions, nameKeyProperty.stringValue);
                    optionIndex = EditorGUILayout.Popup(
                        "Name Column",
                        optionIndex,
                        columnOptions);
                    if (optionIndex >= 0)
                    {
                        nameKeyProperty.stringValue = columnOptions[optionIndex];
                    } 
                    
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dpi"));
                    
                    GUI.enabled = Target.ReadyToExport;
                    if (GUILayout.Button("Export"))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select export folder", Target.LastExportPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                            
                            // Cache out the current scene path because the scene gets torn down when we load a new scene
                            var prevScene = EditorSceneManager.GetSceneAt(0);
                            var prevScenePath = prevScene.path;

                            // Load into an empty scene to prevent any possible rendering conflicts
                            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                            
                            Target.Export(path);
                            
                            EditorSceneManager.OpenScene(prevScenePath);
                        }
                    }
                }

                EditorGUILayout.Space();

                if (Target.CsvSheet.RecordCount > 0)
                {
                    DrawSheetTable(Target.CsvSheet);
                }
            }

            serializedObject.ApplyModifiedProperties();
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
                        .Select(f => (TableCell) new DeckCell(f))
                        .ToList();
                })
                .ToList();

            guiTableState = GUITableLayout.DrawTable(
                guiTableState,
                columns,
                rows,
                GUITableOption.AllowScrollView(true));
        }

        private class DeckCell : TableCell
        {
            private readonly string value;

            public override void DrawCell (Rect rect)
            {
                EditorGUI.SelectableLabel(rect, value);
            }

            public override string comparingValue => value;

            public override int CompareTo(object other)
            {
                var selectableOther = other as DeckCell;
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

            public DeckCell (string value)
            {
                this.value = value;
            }
        }
    }
}
