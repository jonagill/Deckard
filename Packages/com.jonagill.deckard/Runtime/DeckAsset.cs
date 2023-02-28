using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deckard.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Deckard
{
    [CreateAssetMenu(fileName = "New Deck", menuName = "Deckard/New Deck")]
    public class DeckAsset : ScriptableObject
    {
        public enum CardBackBehavior
        {
            None,
            ShowInCorner,
            SeparateSheets
        }

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
        
        private class TempObjectScope : IDisposable
        {
            private GameObject gameObject;

            public TempObjectScope(GameObject gameObject)
            {
                this.gameObject = gameObject;
            }

            public void Dispose()
            {
                DestroyImmediate(gameObject);
            }
        }
        
        private static readonly Regex NonAlphaNumericRegex = new Regex("[^a-zA-Z0-9 -]", RegexOptions.Compiled);
        private static readonly StringBuilder INSTANTANEOUS_STRING_BUILDER = new StringBuilder();

        [SerializeField] private string csvPath = string.Empty;
        [SerializeField, FormerlySerializedAs("pathIsRelative")]
        private bool pathIsAssetDatabasePath = false;
        [SerializeField] private CsvSheet csvSheet;

        [SerializeField] private DeckardCanvas cardPrefab;
        [SerializeField] private Sprite backSprite;

        [FormerlySerializedAs("prependCardCounts")] [SerializeField] private bool soloPrependCardCounts = false;
        [FormerlySerializedAs("includeBleeds")] [SerializeField] private bool soloIncludeBleeds = false;

        [SerializeField] private Vector2 oneSheetSizeInches = new Vector2(8.5f, 11f);
        [SerializeField] private float oneSheetBleedInches = .125f;
        [SerializeField] private float oneSheetSpacingInches = .125f;
        [SerializeField] private bool oneSheetShowCutMarkers = true;
        [SerializeField] private bool oneSheetRespectCounts = true;
        [SerializeField] private CardBackBehavior oneSheetBackBehavior = CardBackBehavior.SeparateSheets;

        [SerializeField] private Vector2Int atlasDimensions = new Vector2Int(7, 5);
        [SerializeField] private bool atlasRespectCounts = false;
        [SerializeField] private CardBackBehavior atlasBackBehavior = CardBackBehavior.ShowInCorner;

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
                    if (pathIsAssetDatabasePath)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
                        if (asset != null)
                        {
                            return new FileInfo(Path.GetFullPath(csvPath)).FullName;
                        }
                    }
                    else
                    {
                        return csvPath;
                    }
                }

                return string.Empty;
            }
        }

        public bool TryGetCsvProjectPath(out string path)
        {
            if (pathIsAssetDatabasePath)
            {
                path = csvPath;
                return true;
            }

            path = null;
            return false;
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
            // Check if this path is within a folder that AssetDatabase can reference
            // This can't just check for Application.dataPath, since this path might be in a package
            // Referenced from Rider's implementation here:
            // https://github.com/JetBrains/resharper-unity/pull/2124/files#diff-e1db1dce6b73974c5729bef41ba2cd440fbd0faddd35f7bc8646f88b7df0bad1R78-R81
            var unityPath = AssetDatabase.GetAllAssetPaths()
                .FirstOrDefault(a =>
                    new FileInfo(Path.GetFullPath(a)).FullName ==
                    absolutePath);  // FileInfo normalizes separators (required on Windows)
            
            if (!string.IsNullOrEmpty(unityPath))
            {
                csvPath = unityPath;
                pathIsAssetDatabasePath = true;
            }
            else
            {
                csvPath = absolutePath;
                pathIsAssetDatabasePath = false;
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

        public void ExportPrintImages(string path)
        {
            var backsInCorner = oneSheetBackBehavior == CardBackBehavior.ShowInCorner;

            ExportSheetImagesInternal(
                path, 
                "Print_Front", 
                cardPrefab, 
                true,
                backsInCorner, 
                GridLayoutGroup.Corner.UpperLeft,
                TextAnchor.UpperCenter,
                oneSheetShowCutMarkers,
                oneSheetRespectCounts,
                oneSheetSizeInches,
                oneSheetBleedInches,
                oneSheetSpacingInches);

            if (oneSheetBackBehavior == CardBackBehavior.SeparateSheets && backSprite != null)
            {
                var cardBack = InstantiateCardBack(cardPrefab, backSprite);
                using (new TempObjectScope(cardBack.gameObject))
                {
                    ExportSheetImagesInternal(
                        path,
                        "Print_Back",
                        cardBack,
                        true,
                        backsInCorner,
                        GridLayoutGroup.Corner.UpperRight,
                        TextAnchor.UpperCenter,
                        oneSheetShowCutMarkers,
                        oneSheetRespectCounts,
                        oneSheetSizeInches,
                        oneSheetBleedInches,
                        oneSheetSpacingInches);
                }
            }

            OpenInFileBrowser.Open(path);
            LastExportPath = path;
        }
        
        public void ExportAtlasImages(string path)
        {
            if (cardPrefab == null)
            {
                throw new InvalidOperationException("No card prefab specified");
            }

            var sizeInches = new Vector2(
                cardPrefab.ContentSizeInches.x * atlasDimensions.x,
                cardPrefab.ContentSizeInches.y * atlasDimensions.y);

            var backsInCorner = atlasBackBehavior == CardBackBehavior.ShowInCorner;
            
            ExportSheetImagesInternal(
                path, 
                "Atlas_Front", 
                cardPrefab, 
                false,
                 backsInCorner,
                GridLayoutGroup.Corner.UpperLeft,
                TextAnchor.UpperLeft,
                false,
                atlasRespectCounts,
                sizeInches,
                0f,
                0f);

            if (atlasBackBehavior == CardBackBehavior.SeparateSheets && backSprite != null)
            {
                var cardBack = InstantiateCardBack(cardPrefab, backSprite);
                using (new TempObjectScope(cardBack.gameObject))
                {
                    ExportSheetImagesInternal(
                        path,
                        "Atlas_Back",
                        cardBack,
                        false,
                        backsInCorner,
                        GridLayoutGroup.Corner.UpperLeft,
                        TextAnchor.UpperLeft,
                        false,
                        atlasRespectCounts,
                        sizeInches,
                        0,
                        0);
                }
            }

            OpenInFileBrowser.Open(path);
            LastExportPath = path;
        }

        public void ExportCardImages(string path)
        {
            ExportCardImagesInternal(path, soloIncludeBleeds, false);
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
                    var exportCount = GetExportCountForRecord(recordIndex, respectCount, out var sheetCount);


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

                            if (soloPrependCardCounts)
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
        
        private void ExportSheetImagesInternal(
            string path, 
            string fileLabel,
            DeckardCanvas cellPrefab,
            bool showCardBleeds,
            bool showCardBackInCorner,
            GridLayoutGroup.Corner startCorner,
            TextAnchor childAlignment,
            bool showCutMarkers,
            bool respectCounts,
            Vector2 sizeInches,
            float bleedInches,
            float spacingInches)
        {
            if (cellPrefab == null)
            {
                throw new InvalidOperationException("No card prefab specified");
            }
            
            EditorUtility.DisplayProgressBar("Generating sheet template...", " ", 0f);
            
            GenerateGridCanvas(
                cellPrefab,
                showCardBleeds,
                showCardBackInCorner,
                backSprite,
                startCorner,
                childAlignment,
                showCutMarkers,
                sizeInches,
                bleedInches,
                spacingInches,
                out var sheetCanvas,
                out var cardInstances,
                out var cardBackInstance,
                out var spaceFillerInstances
            );

            if (cardInstances.Count == 0)
            {
                Debug.LogError("Unable to generate a card sheet -- could not fit any cards into the given sheet size!");
                return;
            }
            
            EditorUtility.DisplayProgressBar("Exporting sheet images...", " ", 0f);

            try
            {
                var maxCardsPerPage = cardInstances.Count;
                var nextCardInstanceIndex = 0;

                var sheetIndex = 0;
                var activeBehaviourScopes = new List<ApplyCardBehaviorsScope>();

                var exportParams = new ExportParams()
                {
                    ExportType = ExportType.Sheet,
                    IncludeBleeds = showCardBleeds,
                };
                
                void ExportSheetAndClearScopes()
                {
                    sheetIndex++;
                    var filePath = Path.Combine(path, $"{name}_{fileLabel}_Sheet_{sheetIndex}.png");
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
                    var count = GetExportCountForRecord(recordIndex, respectCounts, out _);

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
                        
                        // We've filled up a page -- export it!
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
                    var disabledInstances = maxCardsPerPage - nextCardInstanceIndex;
                    // Disable all the card instances that don't have records assigned
                    while (nextCardInstanceIndex < maxCardsPerPage)
                    {
                        cardInstances[nextCardInstanceIndex].gameObject.SetActive(false);
                        nextCardInstanceIndex++;
                    }

                    for (var i = 0; i < disabledInstances && i < spaceFillerInstances.Count; i++)
                    {
                        // Enable filler instances to keep the card back in the bottom corner
                        spaceFillerInstances[i].SetActive(true);
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

        private int GetExportCountForRecord(int recordIndex, bool respectCounts, out int sheetCount)
        {
            const string CountHeader = "Count";
                    
            // Default to outputting one image per record
            int exportCount = 1;
            sheetCount = 1;
            
            // See if this record has a Count column
            if (csvSheet.Headers.Contains(CountHeader))
            {
                // See if this record has a Count specified
                if (!csvSheet.TryGetIntValue("Count", recordIndex, out sheetCount))
                {
                    // The sheet has a Count column, but this record has no count specified
                    sheetCount = 0;
                }

                // If we're not respecting counts, output once per card with a valid count,
                // regardless of the count specified in the sheet
                if (!respectCounts && sheetCount > 1)
                {
                    exportCount = 1;
                }
                else
                {
                    exportCount = sheetCount;
                }
            }

            return exportCount;
        }

        private static void GenerateGridCanvas(
            DeckardCanvas cellPrefab,
            bool showCardBleeds,
            bool showCardBackInCorner,
            Sprite backSprite,
            GridLayoutGroup.Corner startCorner,
            TextAnchor childAlignment,
            bool showCutMarkers,
            Vector2 sizeInches,
            float bleedInches,
            float spacingInches,
            out DeckardCanvas sheetCanvas, 
            out List<DeckardCanvas> cardInstances,
            out DeckardCanvas cardBackInstance,
            out List<GameObject> spaceFillerInstances)
        {
            sheetCanvas = new GameObject("SheetInstance").AddComponent<DeckardCanvas>();
            sheetCanvas.ContentSizeInches = sizeInches;
            sheetCanvas.BleedInches = Vector2.zero; // We will manually configure bleed via padding rather than 
            sheetCanvas.SafeZoneInches = Vector2.zero;

            var sheetBackground = sheetCanvas.gameObject.AddComponent<Image>();
            sheetBackground.color = Color.white;
            
            var sheetGrid = sheetCanvas.gameObject.AddComponent<GridLayoutGroup>();
            var padding = DeckardCanvas.InchesToUnits(bleedInches);
            var spacing = DeckardCanvas.InchesToUnits(spacingInches);
            var cardSize = showCardBleeds ? cellPrefab.TotalPrintSizeUnits : cellPrefab.ContentSizeUnits;

            sheetGrid.cellSize = cardSize;
            sheetGrid.padding = new RectOffset(padding, padding, padding, padding);
            sheetGrid.spacing = Vector2.one * spacing;
            sheetGrid.startCorner = startCorner;
            sheetGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            sheetGrid.childAlignment = childAlignment;

            cardInstances = new List<DeckardCanvas>();

            var sheetRect = sheetCanvas.RectTransform.rect;
            var availableWidth = sheetRect.width - (padding * 2);
            var availableHeight = sheetRect.height - (padding * 2);
            
            var maxHorizontalInstances = Mathf.FloorToInt((availableWidth + spacing) / (cardSize.x + spacing));
            var maxVerticalInstances = Mathf.FloorToInt((availableHeight + spacing) / (cardSize.y + spacing));
            var maxTotalInstances = maxHorizontalInstances * maxVerticalInstances;
            if (showCardBackInCorner)
            {
                maxTotalInstances--;
            }

            for (var i = 0; i < maxTotalInstances; i++)
            {
                InstantiateCardHolder(
                    cellPrefab, 
                    sheetGrid.transform,
                    out var holderRoot,
                    out var holderMask);
                
                var cardInstance = Instantiate(cellPrefab, holderMask.transform, false);
                
                // Center the card in the middle of the mask
                var center = Vector2.one * .5f;
                cardInstance.RectTransform.anchorMax = center;
                cardInstance.RectTransform.anchorMin = center;
                cardInstance.RectTransform.anchoredPosition = Vector2.zero;

                if (showCutMarkers)
                {
                    InstantiateCutMarkers(cardInstance, holderRoot.transform);
                }
                
                cardInstances.Add(cardInstance);
            }

            // If we want to put the card back in the corner of the sheet,
            // add a bunch of disabled filler spaces, then add the card back
            cardBackInstance = null;
            spaceFillerInstances = new List<GameObject>();
            if (showCardBackInCorner)
            {
                for (var i = 0; i < maxTotalInstances; i++)
                {
                    var filler = InstantiateSpaceFiller(sheetGrid.transform);
                    spaceFillerInstances.Add(filler);
                    filler.SetActive(false);
                }
                cardBackInstance = InstantiateCardBack(cellPrefab, backSprite, sheetGrid.transform);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(sheetCanvas.RectTransform);
        }

        private static RectTransform InstantiateCutMarkers(DeckardCanvas cardRoot, Transform parent)
        {
            var cutMarkersPrefab = Resources.Load<RectTransform>("[CutMarkers]");
            var cutMarkersInstance = Instantiate(cutMarkersPrefab, parent, false);
            var cardSizeUnits = cardRoot.ContentSizeUnits;
            cutMarkersInstance.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSizeUnits.x);
            cutMarkersInstance.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSizeUnits.y);
            return cutMarkersInstance;
        }

        private static DeckardCanvas InstantiateCardBack(DeckardCanvas cardPrefab, Sprite backSprite, Transform parent = null)
        {
            if (cardPrefab == null) return null;
            var backPrefab = new GameObject("CardBack", typeof(Canvas));
            backPrefab.transform.SetParent(parent, false);
            
            var canvas = backPrefab.AddComponent<DeckardCanvas>();
            canvas.ContentSizeInches = cardPrefab.ContentSizeInches;
            canvas.BleedInches = cardPrefab.BleedInches;
            
            var graphic = backPrefab.gameObject.AddComponent<Image>();
            graphic.sprite = backSprite;
            graphic.type = Image.Type.Filled;

            return canvas;
        }
        
        private static void InstantiateCardHolder(
            DeckardCanvas cardPrefab,
            Transform parent, 
            out GameObject rootObject,
            out GameObject maskObject)
        {
            var cardSizeUnits = cardPrefab.ContentSizeUnits;
            
            rootObject = new GameObject("CardHolder", typeof(RectTransform));
            var rootTransform = rootObject.GetComponent<RectTransform>();
            rootTransform.SetParent(parent, false);
            rootTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSizeUnits.x);
            rootTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSizeUnits.y);
            
            maskObject = new GameObject("Mask", typeof(RectTransform));
            var maskTransform = maskObject.GetComponent<RectTransform>();
            maskTransform.SetParent(rootTransform, false);
            maskTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardSizeUnits.x);
            maskTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSizeUnits.y);
            maskObject.AddComponent<RectMask2D>();
        }

        private static GameObject InstantiateSpaceFiller(Transform parent)
        {
            var filler = new GameObject("Filler", typeof(RectTransform));
            filler.transform.SetParent(parent);
            return filler;
        }
    }
}
