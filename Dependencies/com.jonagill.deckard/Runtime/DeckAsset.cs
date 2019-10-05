using System.Collections;
using System.Collections.Generic;
using System.IO;
using Deckard.Parsing;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deckard
{
    [CreateAssetMenu(fileName = "New Deck", menuName = "Deckard/New Deck")]
    public class DeckAsset : ScriptableObject
    {
        [SerializeField] private string csvPath = string.Empty;
        [SerializeField] private bool pathIsRelative = false;
        [SerializeField] private CsvSheet csvSheet;

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
    }
}
