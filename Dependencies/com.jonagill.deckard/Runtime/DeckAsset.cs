﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Deckard.Parsing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deckard
{
    [CreateAssetMenu(fileName = "New Deck", menuName = "Deckard/New Deck")]
    public class DeckAsset : ScriptableObject
    {
        // Match points-per-inch used for text layout so that TMP text sizes are correct
        public const int UNITS_PER_INCH = 72;
        
        private static readonly Regex NonAlphaNumericRegex = new Regex("[^a-zA-Z0-9 -]", RegexOptions.Compiled);
        private static readonly StringBuilder INSTANTANEOUS_STRING_BUILDER = new StringBuilder();

        [SerializeField] private string csvPath = string.Empty;
        [SerializeField] private bool pathIsRelative = false;
        [SerializeField] private CsvSheet csvSheet;

        [SerializeField] private Canvas cardPrefab;

        [SerializeField] private string lastExportPath;
        public string LastExportPath => lastExportPath;

        [SerializeField] private string cardNameKey;
        public string CardNameKey => cardNameKey;
        
        
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

        public void Export(string path, float aspectRatio = -1)
        {
            ExportCardImages(path, aspectRatio);
            ExportCsvForDataMerge(path);
            lastExportPath = path;
        }
        
        private void ExportCardImages(string path, float aspectRatio = -1)
        {
            if (cardPrefab == null)
            {
                throw new InvalidOperationException("No card prefab specified");
            }

            var cardInstance = Instantiate(cardPrefab);

            var sizeDelta = cardInstance.GetComponent<RectTransform>().sizeDelta;
            if (aspectRatio < 0)
            {
                aspectRatio = sizeDelta.x / sizeDelta.y;
            }

            // Transform from Unity units to inches
            var sizeInches = sizeDelta / UNITS_PER_INCH;
            
            // Rescale x based on the calculated aspect ratio
            sizeInches.x = sizeInches.y * aspectRatio;
            var sizePixels = sizeInches * dpi;

            try
            {
                for (var i = 0; i < csvSheet.RecordCount; i++)
                {
                    var cardName = GetNameForRecord(i, cardNameKey);
                    EditorUtility.DisplayProgressBar(
                        "Exporting card images...", 
                        cardName,
                i / (float) csvSheet.RecordCount);

                    var filePath = Path.Combine(path, cardName + ".png");

                    // TODO: setup card prefab properly
                    var texture = PngExporter.RenderCanvas(cardInstance, (int) sizePixels.x, (int) sizePixels.y);
                    PngExporter.SaveTextureAsPng(texture, filePath);

                    DestroyImmediate(texture);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            GameObject.DestroyImmediate(cardInstance.gameObject);
        }

        private void ExportCsvForDataMerge(string path)
        {
            for (var i = 0; i < csvSheet.RecordCount; i++)
            {
                if (!csvSheet.TryGetIntValue("Count", i, out var count))
                {
                    count = 1;
                }
            }
        }
        
        private string GetNameForRecord(int recordIndex, string nameKey = null)
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

            sb.Append("_" + recordIndex);
            return sb.ToString();
        }
    }
}
