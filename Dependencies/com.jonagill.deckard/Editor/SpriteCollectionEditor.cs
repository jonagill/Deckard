using UnityEditor;

namespace Deckard.Editor
{
    [CustomEditor(typeof(SpriteCollection))]
    public class SpriteCollectionEditor : UnityEditor.Editor
    {
        private SpriteCollection Target => (SpriteCollection) target;

        public override void OnInspectorGUI()
        {
            for (var i = 0; i < Target.SpriteEntries.Count; i++)
            {
                for (var j = i+1; j < Target.SpriteEntries.Count; j++)
                {
                    var entry = Target.SpriteEntries[i];
                    var entry2 = Target.SpriteEntries[j];
                    
                    if (entry.Key == entry2.Key)
                    {
                        EditorGUILayout.HelpBox($"Entry {entry} has the same key as entry {entry2}. Only one will be used!", MessageType.Warning);
                        EditorGUILayout.Separator();
                    }
                }
            }
            
            base.OnInspectorGUI();
        }
    }
}
