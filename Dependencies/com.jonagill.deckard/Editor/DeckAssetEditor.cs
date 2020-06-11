using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Deckard.Editor
{
    [CustomEditor(typeof(DeckAsset))]
    public class DeckAssetEditor : UnityEditor.Editor
    {
        private CsvTable csvTable;

        private DeckAsset Target => (DeckAsset) target;

        public override void OnInspectorGUI()
        {
            if (csvTable == null)
            {
                RefreshTable();
            }
            
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

                        EditorGUILayout.SelectableLabel(displayPath,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
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
                                serializedObject.Update();
                                EditorUtility.SetDirty(Target);
                            }
                        }

                        GUI.enabled = hasFile;
                        if (GUILayout.Button(EditorGUIUtility.IconContent("refresh"), GUILayout.ExpandWidth(false)))
                        {
                            Undo.RecordObject(Target, "Refresh CSV source");
                            Target.RefreshCsvSheet();
                            EditorUtility.SetDirty(Target);
                            RefreshTable();
                        }

                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_project"), GUILayout.ExpandWidth(false)))
                        {
                            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(Target.CsvDirectoryPath,
                                0);
                        }


                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"),
                            GUILayout.ExpandWidth(false)))
                        {
                            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(Target.CsvAbsolutePath, 0);
                        }

                        GUI.enabled = true;
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cardPrefab"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dpi"));
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Card Export", EditorStyles.boldLabel);

                    var nameKeyProperty = serializedObject.FindProperty("cardNameKey");
                    var nameKey2Property = serializedObject.FindProperty("cardNameKey2");

                    var columnOptionsList = Target.CsvSheet.Headers.ToList();
                    columnOptionsList.Insert(0, "<None>");
                    var columnOptions = columnOptionsList.ToArray();

                    var nameOptionIndex = Array.IndexOf(columnOptions, nameKeyProperty.stringValue);
                    nameOptionIndex = EditorGUILayout.Popup(
                        "Name Column",
                        nameOptionIndex,
                        columnOptions);

                    if (nameOptionIndex > 0)
                    {
                        nameKeyProperty.stringValue = columnOptions[nameOptionIndex];

                        var nameOption2Index = Array.IndexOf(columnOptions, nameKey2Property.stringValue);
                        nameOption2Index = EditorGUILayout.Popup(
                            "Name Column 2",
                            nameOption2Index,
                            columnOptions);

                        if (nameOption2Index > 0)
                        {
                            nameKey2Property.stringValue = columnOptions[nameOption2Index];
                        }
                        else
                        {
                            nameKey2Property.stringValue = string.Empty;
                        }
                    }
                    else
                    {
                        nameKeyProperty.stringValue = string.Empty;
                        nameKey2Property.stringValue = string.Empty;
                    }

                    var prependCardCountsProperty = serializedObject.FindProperty("prependCardCounts");
                    prependCardCountsProperty.boolValue = EditorGUILayout.Toggle(
                        new GUIContent(
                            "Include Counts",
                            "Whether to prefix the file names with the card counts. Makes it easier to import the exported files into Tabletop Simulator."),
                        prependCardCountsProperty.boolValue);

                    GUI.enabled = Target.ReadyToExport;
                    if (GUILayout.Button("Export card files"))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select export folder", Target.LastExportPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            using (new EmptySceneScope())
                            {
                                Target.ExportCardImages(path);
                            }
                        }
                    }

                    GUI.enabled = true;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Sheet Export", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetSizeInches"),
                        new GUIContent("Page Size (inches)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetBleedInches"),
                        new GUIContent("Bleed"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetSpacingInches"),
                        new GUIContent("Card Spacing"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetShowCutMarkers"),
                        new GUIContent("Cut Markers"));

                    GUI.enabled = Target.ReadyToExport;
                    if (GUILayout.Button("Export sheet images"))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select export folder", Target.LastExportPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            using (new EmptySceneScope())
                            {
                                Target.ExportSheetImages(path);
                            }
                        }
                    }

                    GUI.enabled = true;
                }

                EditorGUILayout.Space();

                if (Target.CsvSheet.RecordCount > 0)
                {
                    DrawTable();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshTable()
        {
            if (Target == null || Target.CsvSheet == null)
            {
                return;
            }
            
            csvTable = new CsvTable(Target.CsvSheet);
        }

        private void DrawTable()
        {
            if (csvTable == null)
            {
                return;
            }
            
            csvTable.DrawLayout(260);
        }
    }
}
