using Deckard.Data;
using UnityEditor;

namespace Deckard.Editor
{
    [CustomEditor(typeof(CsvSprite))]
    public class CsvSpriteEditor : CsvDataBehaviourEditor
    {
        private CsvSprite Target => (CsvSprite) target;

        private SerializedProperty spriteCollectionProperty;

        private void OnEnable()
        {
            spriteCollectionProperty = serializedObject.FindProperty("spriteCollection");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spriteCollectionProperty);
            EditorGUILayout.Separator();
            
            base.OnInspectorGUI();
        }
    }
}
