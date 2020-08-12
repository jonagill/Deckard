using System;
using System.Collections.Generic;
using System.Linq;
using Deckard.Data;
using UnityEditor;
using UnityEngine;

namespace Deckard.Editor
{
    [CustomEditor(typeof(CsvDataBehaviour), true)]
    public class CsvDataBehaviourEditor : UnityEditor.Editor
    {
        private CsvDataBehaviour Target => (CsvDataBehaviour) target;

        private List<string> allKeyOptions;
        private string[] filteredKeyOptions;
        private string filter;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            RenderKeySelectionGUI();
        }

        protected void RenderKeySelectionGUI()
        {
            EditorGUILayout.Separator();
            
            if (allKeyOptions == null || filteredKeyOptions == null)
            {
                RefreshKeyOptions();
            }
            
            using (new EditorGUILayout.VerticalScope("box"))
            {
                var keyProperty = serializedObject.FindProperty("key");
                if (!allKeyOptions.Contains(keyProperty.stringValue))
                {
                    EditorGUILayout.HelpBox($"Key value \"{keyProperty.stringValue}\" is not present in any deck.", MessageType.Error);
                }

                Undo.RecordObject(Target, "set CSV data key");
                keyProperty.stringValue = EditorGUILayout.DelayedTextField("Key", keyProperty.stringValue);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.LabelField("Search:", GUILayout.Width(75));
                        filter = EditorGUILayout.TextField(filter, GUILayout.Width(160));

                        if (changeScope.changed)
                        {
                            RefreshFilteredKeyOptions(filter);
                        }
                    }
                    
                    var optionIndex = Array.IndexOf(filteredKeyOptions, keyProperty.stringValue);
                    optionIndex = EditorGUILayout.Popup(
                        optionIndex,
                        filteredKeyOptions);
                    if (optionIndex >= 0)
                    {
                        keyProperty.stringValue = filteredKeyOptions[optionIndex];
                    } 
                    
                    if (GUILayout.Button(EditorGUIUtility.IconContent("refresh"), GUILayout.ExpandWidth(false)))
                    {
                        RefreshKeyOptions();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshKeyOptions()
        {
            allKeyOptions = GetKeyOptions();
            RefreshFilteredKeyOptions(filter);
        }

        private void RefreshFilteredKeyOptions(string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                filteredKeyOptions = allKeyOptions.ToArray();
            }
            else
            {
                filteredKeyOptions = allKeyOptions
                    .Where(h => h.ToLower().Contains(filter.ToLower()))
                    .ToArray();
            }
        }
        
        private List<string> GetKeyOptions()
        {
            var allDecks = AssetDatabase.FindAssets("t:DeckAsset")
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(p => AssetDatabase.LoadAssetAtPath<DeckAsset>(p))
                .Where(a => a != null);
            
            var allHeaders = allDecks.SelectMany(d => d.CsvSheet.Headers).Distinct();
            return allHeaders.ToList();
        }
    }
}
