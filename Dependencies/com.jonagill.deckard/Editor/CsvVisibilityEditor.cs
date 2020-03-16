using Deckard.Data;
using UnityEditor;

namespace Deckard.Editor
{
    [CustomEditor(typeof(CsvVisibility))]
    public class CsvVisibilityEditor : CsvDataBehaviourEditor
    {
        private CsvVisibility Target => (CsvVisibility) target;

        private SerializedProperty behaviorProperty;
        private SerializedProperty stringComparatorProperty;
        private SerializedProperty floatComparatorProperty;

        private void OnEnable()
        {
            behaviorProperty = serializedObject.FindProperty("behavior");
            stringComparatorProperty = serializedObject.FindProperty("stringComparator");
            floatComparatorProperty = serializedObject.FindProperty("floatComparator");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(behaviorProperty);
            
            switch (Target.Behavior)
            {
                case CsvVisibility.VisbilityBehavior.Match:
                case CsvVisibility.VisbilityBehavior.Different:
                    EditorGUILayout.PropertyField(stringComparatorProperty);                
                    break;
                case CsvVisibility.VisbilityBehavior.Equals:
                case CsvVisibility.VisbilityBehavior.NotEqual:
                case CsvVisibility.VisbilityBehavior.GreaterThan:
                case CsvVisibility.VisbilityBehavior.LessThan:
                case CsvVisibility.VisbilityBehavior.GreaterThanOrEqualTo:
                case CsvVisibility.VisbilityBehavior.LessThanOrEqualTo:
                    EditorGUILayout.PropertyField(floatComparatorProperty);   
                    break;
                case CsvVisibility.VisbilityBehavior.Empty:
                case CsvVisibility.VisbilityBehavior.NotEmpty:
                default:
                    break;
            }
            
            EditorGUILayout.Separator();
            
            base.OnInspectorGUI();
        }
    }
}
