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

                        var settingsIcon = EditorGUIUtility.IconContent("settings");
                        settingsIcon.tooltip = "Select a CSV file to use as the source data for this deck";
                        if (GUILayout.Button(settingsIcon, GUILayout.ExpandWidth(false)))
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
                                RefreshTable();
                            }
                        }

                        using (new EditorGUI.DisabledScope(!hasFile))
                        {
                            var refreshIcon = EditorGUIUtility.IconContent("refresh");
                            refreshIcon.tooltip = "Refresh the deck asset with the latest data from the CSV file";
                            if (GUILayout.Button(refreshIcon, GUILayout.ExpandWidth(false)))
                            {
                                Undo.RecordObject(Target, "Refresh CSV source");
                                Target.RefreshCsvSheet();
                                EditorUtility.SetDirty(Target);
                                RefreshTable();
                            }

                            var folderIcon = EditorGUIUtility.IconContent("d_project");
                            folderIcon.tooltip = "Reveal the CSV file in the file explorer";
                            if (GUILayout.Button(folderIcon, GUILayout.ExpandWidth(false)))
                            {
                                EditorUtility.RevealInFinder(Target.CsvAbsolutePath);
                            }

                            var editIcon = EditorGUIUtility.IconContent("d_editicon.sml");
                            editIcon.tooltip = "Open your CSV in a text editor";
                            if (GUILayout.Button(editIcon, GUILayout.ExpandWidth(false)))
                            {
                                if (Target.TryGetCsvProjectPath(out var path))
                                {
                                    var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                                    if (csv != null)
                                    {
                                        AssetDatabase.OpenAsset(csv);    
                                    }    
                                }
                                else
                                {
                                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                                        Target.CsvAbsolutePath, 0);
                                }
                            }
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cardPrefab"), new GUIContent("Card Prefab", "The prefab to use as the base for cards in this deck"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("backSprite"), new GUIContent("Card Back", "The sprite to use as the card backs when exporting using options that include backs"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dpi"), new GUIContent("DPI", "The resolution of your exported card files in dots per inch / pixels per inch"));
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(new GUIContent("Card Export", "Settings for exporting cards as individual image files"), EditorStyles.boldLabel);

                    var nameKeyProperty = serializedObject.FindProperty("cardNameKey");
                    var nameKey2Property = serializedObject.FindProperty("cardNameKey2");

                    var columnOptionsList = Target.CsvSheet.Headers.ToList();
                    columnOptionsList.Insert(0, "<None>");
                    var columnOptions = columnOptionsList.ToArray();

                    var nameOptionIndex = Array.IndexOf(columnOptions, nameKeyProperty.stringValue);
                    nameOptionIndex = EditorGUILayout.Popup(
                        new GUIContent("Name Column", "The sheet column to use as the prefix of the each card's filename"),
                        nameOptionIndex,
                        columnOptions);

                    if (nameOptionIndex > 0)
                    {
                        nameKeyProperty.stringValue = columnOptions[nameOptionIndex];

                        var nameOption2Index = Array.IndexOf(columnOptions, nameKey2Property.stringValue);
                        nameOption2Index = EditorGUILayout.Popup(
                            new GUIContent("Name Column 2", "The sheet column to use as the suffix of the each card's filename (if any)"),
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

                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("prependCardCounts"),
                        new GUIContent(
                            "Include Counts",
                            "Whether to prefix the file names with the card counts. Makes it easier to import the exported files into Tabletop Simulator"));


                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("includeBleeds"),
                        new GUIContent(
                            "Render Bleeds",
                            "Whether to render the bleed area of the card. Important for uploading the cards for print, such as to The Game Crafter"));

                    using (new EditorGUI.DisabledScope(!Target.ReadyToExport))
                    {
                        if (GUILayout.Button(new GUIContent("Export card images",
                                "Export individual images for every card in the deck")))
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
                    }
                }
                
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(new GUIContent("Atlas Export", "Settings for exporting multiple cards in each image file, known as a sprite atlas"), EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("atlasDimensions"),
                        new GUIContent("Atlas Dimensions", "How many columns and rows to include in each file of the atlas"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("atlasBackBehavior"),
                        new GUIContent("Card Backs", "How to treat card backs in the exported files"));
                    
                    using (new EditorGUI.DisabledScope(!Target.ReadyToExport))
                    {
                        if (GUILayout.Button(new GUIContent("Export atlas images", "Export sprite atlases")))
                        {
                            var path = EditorUtility.OpenFolderPanel("Select export folder", Target.LastExportPath, "");
                            if (!string.IsNullOrEmpty(path))
                            {
                                using (new EmptySceneScope())
                                {
                                    Target.ExportAtlasImages(path);
                                }
                            }
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(new GUIContent("Print Export", "Settings for exporting multiple cards on each printable sheet"), EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetSizeInches"),
                        new GUIContent("Page Size (inches)", "The size of each printable sheet"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetBleedInches"),
                        new GUIContent("Bleed (inches)", "How much space to leave empty on each edge of the sheet to aid in printing"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetSpacingInches"),
                        new GUIContent("Card Spacing (inches)", "How much space to leave between each card"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetShowCutMarkers"),
                        new GUIContent("Cut Markers", "Whether to render guides for where to cut out the cards on the sheet images"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("oneSheetBackBehavior"),    
                        new GUIContent("Card Backs", "How to treat card backs in the sheet images"));

                    using (new EditorGUI.DisabledScope(!Target.ReadyToExport))
                    {
                        if (GUILayout.Button(new GUIContent("Export sheet images", "Export printable sheets")))
                        {
                            var path = EditorUtility.OpenFolderPanel("Select export folder", Target.LastExportPath, "");
                            if (!string.IsNullOrEmpty(path))
                            {
                                using (new EmptySceneScope())
                                {
                                    Target.ExportPrintImages(path);
                                }
                            }
                        }
                    }
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
            if (Target == null || 
                Target.CsvSheet == null || 
                Target.CsvSheet.Headers.Count == 0)
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
