using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Deckard.Editor
{
    [CustomEditor(typeof(SpriteCollection))]
    public class SpriteCollectionEditor : UnityEditor.Editor
    {
        private const int MAX_WARNINGS = 3;

        private DefaultAsset bulkImportFolder; 
        
        private SpriteCollection Target => (SpriteCollection) target;

        public override void OnInspectorGUI()
        {
            DrawDuplicateWarnings();
            
            EditorGUILayout.Separator();
            
            DrawBulkImport();
            
            EditorGUILayout.Separator();
            
            base.OnInspectorGUI();
        }

        private void DrawDuplicateWarnings()
        {
            int shownWarnings = 0;
            
            for (var i = 0; i < Target.SpriteEntries.Count; i++)
            {
                for (var j = i+1; j < Target.SpriteEntries.Count; j++)
                {
                    var entry = Target.SpriteEntries[i];
                    var entry2 = Target.SpriteEntries[j];
                    
                    if (entry.Key == entry2.Key)
                    {
                        EditorGUILayout.HelpBox($"Entry {entry} has the same key as entry {entry2}. Only one will be used!", MessageType.Warning);

                        shownWarnings++;
                        if (shownWarnings >= MAX_WARNINGS)
                        {
                            return;
                        }
                        
                        EditorGUILayout.Separator();
                    }
                }
            }
        }

        private void DrawBulkImport()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Bulk import from folder");
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    bulkImportFolder =
                        (DefaultAsset) EditorGUILayout.ObjectField("Folder", bulkImportFolder, typeof(DefaultAsset), false);

                    using (new EditorGUI.DisabledScope(bulkImportFolder == null))
                    {
                        if (GUILayout.Button("Import"))
                        {
                            var path = AssetDatabase.GetAssetPath(bulkImportFolder);
                            ImportSpritesFromFolder(path);
                        }
                    }
                }
            }
        }

        private void ImportSpritesFromFolder(string folderPath)
        {
            Undo.RecordObject(Target, "bulk sprite import");
            
            var spriteAssetGuids = AssetDatabase.FindAssets("t:sprite", new[] {folderPath});
            foreach (var guid in spriteAssetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).Select(a => a as Sprite).Where(s => s != null);

                foreach (var sprite in sprites)
                {
                    if (!Target.SpriteEntries.Any(e => e.Sprite == sprite))
                    {
                        Target.AddSpriteEntry(sprite.name, sprite);
                    }
                }
            }
            
            EditorUtility.SetDirty(Target);
        }
    }
}
