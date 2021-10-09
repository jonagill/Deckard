﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deckard.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

namespace Deckard
{
    [CreateAssetMenu(fileName = "New Deck", menuName = "Deckard/New Deck")]
    public class DeckAsset : ScriptableObject
    {
        private class ApplyCardBehaviorsScope : IDisposable
        {
            private IReadOnlyList<CsvDataBehaviour> csvBehaviours;
            private IReadOnlyList<ExportBehaviour> exportBehaviours;
            
            public ApplyCardBehaviorsScope(
                GameObject cardInstance, 
                ExportParams exportParams, 
                CsvSheet csvSheet, 
                int recordIndex)
            {
                exportBehaviours = cardInstance.GetComponentsInChildren<ExportBehaviour>(true);
                foreach (var eb in exportBehaviours)
                {
                    eb.Process(exportParams);
                }
                
                csvBehaviours = cardInstance.GetComponentsInChildren<CsvDataBehaviour>(true);
                foreach (var cb in csvBehaviours.OrderBy(c => c.Priority))
                {
                    cb.Process(csvSheet, recordIndex);
                }
            }
            
            public void Dispose()
            {
                if (exportBehaviours != null)
                {
                    foreach (var eb in exportBehaviours)
                    {
                        if (eb != null)
                        {
                            eb.Cleanup();
                        }
                    }

                    exportBehaviours = null;
                }

                if (csvBehaviours != null)
                {

                    foreach (var cb in csvBehaviours.OrderByDescending(c => c.Priority))
                    {
                        if (cb != null)
                        {
                            cb.Cleanup();
                        }
                    }

                    csvBehaviours = null;
                }
            }
        }
        
        private static readonly Regex NonAlphaNumericRegex = new Regex("[^a-zA-Z0-9 -]", RegexOptions.Compiled);
        private static readonly StringBuilder INSTANTANEOUS_STRING_BUILDER = new StringBuilder();

        private const int CUT_MARKER_LENGTH_UNITS = 8;
        private const int CUT_MARKER_WIDTH_UNITS = 1;

        [SerializeField] private string csvPath = string.Empty;
        [SerializeField] private bool pathIsRelative = false;
        [SerializeField] private CsvSheet csvSheet;

        [SerializeField] private DeckardCanvas cardPrefab;

        [SerializeField] private bool prependCardCounts = true;
        [SerializeField] private bool includeBleeds = false;

        [SerializeField] private Vector2 oneSheetSizeInches = new Vector2(8.5f, 11f);
        [SerializeField] private float oneSheetBleedInches = .125f;
        [SerializeField] private float oneSheetSpacingInches = .125f;
        [SerializeField] private bool oneSheetShowCutMarkers = true;

        [SerializeField] private string cardNameKey;
        [SerializeField] private string cardNameKey2;
        
        [SerializeField] private int dpi = 300;
        public int DPI => dpi;

        public string CsvAbsolutePath
        {
            get
            {
                if (!string.IsNullOrEmpty(csvPath))
                {
                    if (pathIsRelative)
                    {
                        return Path.Combine(Application.dataPath, csvPath);
                    }
                    else
                    {
                        return csvPath;
                    }
                }

                return string.Empty;
            }
        }

        public string CsvDirectoryPath
        {
            get
            {
                var path = CsvAbsolutePath;
                return Path.GetDirectoryName(CsvAbsolutePath);
            }
        }

        private string GlobalExportPrefName => $"{GetType().Name}_LastExportPath";
        private string ExportPrefName => $"{GetType().Name}_{name}_LastExportPath";
        public string LastExportPath
        {
            get
            {
#if UNITY_EDITOR
                var customPath = EditorPrefs.GetString(ExportPrefName, null);
    if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
                {
                    return customPath;
                }

                // If we've never saved this particular asset, use the last path that any deck saved to
                var globalPath = EditorPrefs.GetString(GlobalExportPrefName, null);
                if (!string.IsNullOrEmpty(globalPath) && Directory.Exists(globalPath))
                {
                    return globalPath;
                }
                
#endif
                return null;
            }

            set
            {
#if UNITY_EDITOR
                EditorPrefs.SetString(GlobalExportPrefName, value);
                EditorPrefs.SetString(ExportPrefName, value);
#else
                throw new InvalidOperationException("Cannot set the deck asset export path in a build.");
#endif
            }
        }

        public string CsvDisplayPath => csvPath;
        public CsvSheet CsvSheet => csvSheet;
        public bool ReadyToExport => csvSheet.RecordCount > 0 && cardPrefab != null;

        public void SetCsvPath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                csvPath = absolutePath.Substring(Application.dataPath.Length + 1);
                pathIsRelative = true;
            }
            else
            {
                csvPath = absolutePath;
                pathIsRelative = false;
            }
            
            RefreshCsvSheet();
        }

        public void RefreshCsvSheet()
        {
            if (File.Exists(CsvAbsolutePath))
            {
                var input = File.ReadAllText(CsvAbsolutePath);
                csvSheet = CsvSheet.Parse(input);
            }
            else
            {
                Debug.LogError($"Unable to find CSV sheet to load at path: {CsvAbsolutePath}");
            }
        }

        public void ExportSheetImages(string path)
        {
            ExportSheetImagesInternal(path);
            OpenInFileBrowser.Open(path);
            LastExportPath = path;
        }

        public void ExportCardImages(string path, bool includeBleed)
        {
            ExportCardImagesInternal(path, includeBleed, false);
            OpenInFileBrowser.Open(path);
            LastExportPath = path;
        }
        
        private void ExportCardImagesInternal(string path, bool includeBleed, bool respectCount)
        {
            if (cardPrefab == null)
            {
                throw new InvalidOperationException("No card prefab specified");
            }

            var exportParams = new ExportParams()
            {
                ExportType = ExportType.Cards,
                IncludeBleeds = includeBleed
            };
            
            var cardInstance = Instantiate(cardPrefab);

            EditorUtility.DisplayProgressBar("Exporting card images...", "", 0f);

            try
            {
                for (var recordIndex = 0; recordIndex < csvSheet.RecordCount; recordIndex++)
                {
                    if (!csvSheet.TryGetIntValue("Count", recordIndex, out var sheetCount))
                    {
                        sheetCount = 1;
                    }

                    var exportCount = respectCount ? sheetCount : 1;

                    using (new ApplyCardBehaviorsScope(cardInstance.gameObject, exportParams, csvSheet, recordIndex))
                    {
                        var texture = cardInstance.Render(dpi, includeBleed);

                        for (var countIndex = 0; countIndex < exportCount; countIndex++)
                        {
                            var cardName = GetNameForRecord(recordIndex, countIndex, cardNameKey, cardNameKey2);
                            EditorUtility.DisplayProgressBar(
                                "Exporting card images...",
                                cardName,
                                recordIndex / (float) csvSheet.RecordCount);

                            var fileName = $"{cardName}.png";

                            if (prependCardCounts)
                            {
                                // Prepend the count for easy importing into Tabletop Simulator
                                fileName = $"{sheetCount:00}x " + fileName;
                            }
                            
                            var filePath = Path.Combine(path, fileName);
                            DeckardCanvas.SaveTextureAsPng(texture, filePath);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            GameObject.DestroyImmediate(cardInstance.gameObject);
        }
        
        private void ExportSheetImagesInternal(string path)
        {
            if (cardPrefab == null)
            {
                throw new InvalidOperationException("No card prefab specified");
            }

            EditorUtility.DisplayProgressBar("Generating sheet template...", "", 0f);
            
            GenerateSheetTemplate(oneSheetShowCutMarkers, out var sheetCanvas, out var cardInstances);

            if (cardInstances.Count == 0)
            {
                Debug.LogError("Unable to generate a card sheet -- could not fit any cards into the given sheet size!");
                return;
            }
            
            EditorUtility.DisplayProgressBar("Exporting sheet images...", "", 0f);

            try
            {
                var maxCardsPerPage = cardInstances.Count;
                var nextCardInstanceIndex = 0;

                var sheetIndex = 0;
                var activeBehaviourScopes = new List<ApplyCardBehaviorsScope>();

                var exportParams = new ExportParams()
                {
                    ExportType = ExportType.Sheet,
                    IncludeBleeds = true,
                };
                
                void ExportSheetAndClearScopes()
                {
                    sheetIndex++;
                    var filePath = Path.Combine(path, $"{name}_Sheet_{sheetIndex}.png");
                    var texture = sheetCanvas.Render(dpi, includeBleed: true);
                    DeckardCanvas.SaveTextureAsPng(texture, filePath);

                    foreach (var behaviourScope in activeBehaviourScopes)
                    {
                        behaviourScope.Dispose();
                    }
                    activeBehaviourScopes.Clear();
                }

                for (var recordIndex = 0; recordIndex < csvSheet.RecordCount; recordIndex++)
                {
                    if (!csvSheet.TryGetIntValue("Count", recordIndex, out var count))
                    {
                        count = 1;
                    }

                    for (var countIndex = 0; countIndex < count; countIndex++)
                    {
                        var cardName = GetNameForRecord(recordIndex, countIndex, cardNameKey, cardNameKey2);
                        EditorUtility.DisplayProgressBar(
                            "Configuring card...",
                            cardName,
                            recordIndex / (float) csvSheet.RecordCount);
                        
                        var cardInstance = cardInstances[nextCardInstanceIndex];
                        nextCardInstanceIndex++;
                        
                        activeBehaviourScopes.Add(new ApplyCardBehaviorsScope(cardInstance.gameObject, exportParams, csvSheet, recordIndex));
                        
                        if (nextCardInstanceIndex >= maxCardsPerPage)
                        {
                            ExportSheetAndClearScopes();
                            nextCardInstanceIndex = 0;
                        }
                    }
                }

                // If there are any configured cards that haven't been exported yet,
                // export the sheet one last time
                if (activeBehaviourScopes.Count > 0)
                {
                    // Disable all the card instances that don't have records assigned
                    while (nextCardInstanceIndex < maxCardsPerPage)
                    {
                        cardInstances[nextCardInstanceIndex].gameObject.SetActive(false);
                        nextCardInstanceIndex++;
                    }
                        
                    // Export the sheet one last time
                    ExportSheetAndClearScopes();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            GameObject.DestroyImmediate(sheetCanvas.gameObject);
        }

        private string GetNameForRecord(int recordIndex, int countIndex, string nameKey = null, string nameKey2 = null)
        {
            var sb = INSTANTANEOUS_STRING_BUILDER;
            sb.Clear();

            sb.Append(name);
            
            if (!string.IsNullOrEmpty(nameKey))
            {
                if (csvSheet.TryGetStringValue(nameKey, recordIndex, out var cardName))
                {
                    cardName = NonAlphaNumericRegex.Replace(cardName, "");
                    sb.Append("_" + cardName);
                }
            }
            
            if (!string.IsNullOrEmpty(nameKey2))
            {
                if (csvSheet.TryGetStringValue(nameKey2, recordIndex, out var cardName2))
                {
                    cardName2 = NonAlphaNumericRegex.Replace(cardName2, "");
                    sb.Append("_" + cardName2);
                }
            }

            sb.Append("_" + recordIndex);

            if (countIndex > 0)
            {
                sb.Append($" ({countIndex + 1})");
            }
            
            return sb.ToString();
        }

        private void GenerateSheetTemplate(
            bool showCutMarkers,
            out DeckardCanvas sheetCanvas, 
            out List<DeckardCanvas> cardInstances)
        {
            sheetCanvas = new GameObject("SheetInstance").AddComponent<DeckardCanvas>();
            sheetCanvas.CardSizeInches = oneSheetSizeInches;
            sheetCanvas.BleedInches = Vector2.zero; // We will manually configure bleed via padding rather than 
            sheetCanvas.SafeZoneInches = Vector2.zero;

            var sheetBackground = sheetCanvas.gameObject.AddComponent<Image>();
            sheetBackground.color = Color.white;
            
            var sheetGrid = sheetCanvas.gameObject.AddComponent<GridLayoutGroup>();
            var padding = DeckardCanvas.InchesToUnits(oneSheetBleedInches);
            var spacing = DeckardCanvas.InchesToUnits(oneSheetSpacingInches);
            var cardSize = cardPrefab.RectTransform.rect.size;

            sheetGrid.cellSize = cardSize;
            sheetGrid.padding = new RectOffset(padding, padding, padding, padding);
            sheetGrid.spacing = Vector2.one * spacing;
            sheetGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            sheetGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            sheetGrid.childAlignment = TextAnchor.UpperCenter;

            cardInstances = new List<DeckardCanvas>();

            var sheetRect = sheetCanvas.RectTransform.rect;
            var availableWidth = sheetRect.width - (padding * 2);
            var availableHeight = sheetRect.height - (padding * 2);
            
            var maxHorizontalInstances = Mathf.FloorToInt((availableWidth + spacing) / (cardSize.x + spacing));
            var maxVerticalInstances = Mathf.FloorToInt((availableHeight + spacing) / (cardSize.y + spacing));
            var maxTotalInstances = maxHorizontalInstances * maxVerticalInstances;

            for (var i = 0; i < maxTotalInstances; i++)
            {
                var cardInstance = Instantiate(cardPrefab, sheetGrid.transform, false);

                if (showCutMarkers)
                {
                    InstantiateCutMarkers(cardInstance);
                }
                
                cardInstances.Add(cardInstance);
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(sheetCanvas.RectTransform);
        }

        private void InstantiateCutMarkers(DeckardCanvas cardRoot)
        {
            var cutMarkersPrefab = Resources.Load<RectTransform>("[CutMarkers]");
            var cutMarkersInstance = Instantiate(cutMarkersPrefab, cardRoot.transform, false);
            var cardSizeUnits = DeckardCanvas.InchesToUnits(cardRoot.CardSizeInches);
            cutMarkersInstance.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSizeUnits.x);
            cutMarkersInstance.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSizeUnits.y);
        }
    }
}
