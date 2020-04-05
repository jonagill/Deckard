using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deckard.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard
{
    [CreateAssetMenu(fileName = "New Deck", menuName = "Deckard/New Deck")]
    public class DeckAsset : ScriptableObject
    {
        private static readonly Regex NonAlphaNumericRegex = new Regex("[^a-zA-Z0-9 -]", RegexOptions.Compiled);
        private static readonly StringBuilder INSTANTANEOUS_STRING_BUILDER = new StringBuilder();

        [SerializeField] private string csvPath = string.Empty;
        [SerializeField] private bool pathIsRelative = false;
        [SerializeField] private CsvSheet csvSheet;

        [SerializeField] private DeckardCanvas cardPrefab;


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
                if (!string.IsNullOrEmpty(customPath))
                {
                    return customPath;
                }

                // If we've never saved this particular asset, use the last path that any deck saved to
                return EditorPrefs.GetString(GlobalExportPrefName, null);
#else
                return null;
#endif
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

        public void Export(string path, float aspectRatio = -1)
        {
            ExportCardImages(path, aspectRatio);
            OpenInFileBrowser.Open(path);
            LastExportPath = path;
        }
        
        private void ExportCardImages(string path, float aspectRatio = -1)
        {
            if (cardPrefab == null)
            {
                throw new InvalidOperationException("No card prefab specified");
            }

            var cardInstance = Instantiate(cardPrefab);

            EditorUtility.DisplayProgressBar("Exporting card images...", "", 0f);

            try
            {
                for (var recordIndex = 0; recordIndex < csvSheet.RecordCount; recordIndex++)
                {
                    if (!csvSheet.TryGetIntValue("Count", recordIndex, out var count))
                    {
                        count = 1;
                    }

                    var csvBehaviours = cardInstance.GetComponentsInChildren<CsvDataBehaviour>(true);
                    foreach (var cb in csvBehaviours.OrderBy(c => c.Priority))
                    {
                        cb.Process(csvSheet, recordIndex);
                    }
                    
                    var texture = cardInstance.Render(dpi);
                    
                    for (var countIndex = 0; countIndex < count; countIndex++)
                    {
                        var cardName = GetNameForRecord(recordIndex, countIndex, cardNameKey);
                        EditorUtility.DisplayProgressBar(
                            "Exporting card images...", 
                            cardName,
                            recordIndex / (float) csvSheet.RecordCount);
                        var filePath = Path.Combine(path, cardName + ".png");
                        DeckardCanvas.SaveTextureAsPng(texture, filePath);
                    }
                    
                    foreach (var cb in csvBehaviours.OrderByDescending(c => c.Priority))
                    {
                        if (cb != null)
                        {
                            cb.Cleanup();
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
        
        private string GetNameForRecord(int recordIndex, int countIndex, string nameKey = null)
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

            if (countIndex > 0)
            {
                sb.Append($" ({countIndex + 1})");
            }
            
            return sb.ToString();
        }
    }
}
