using UnityEditor;

namespace Deckard.Editor
{
    [CustomEditor(typeof(ColorCollection))]
    public class ColorCollectionEditor : UnityEditor.Editor
    {
        private ColorCollection Target => (ColorCollection) target;

        public override void OnInspectorGUI()
        {
            for (var i = 0; i < Target.ColorEntries.Count; i++)
            {
                for (var j = i+1; j < Target.ColorEntries.Count; j++)
                {
                    var entry = Target.ColorEntries[i];
                    var entry2 = Target.ColorEntries[j];
                    
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
