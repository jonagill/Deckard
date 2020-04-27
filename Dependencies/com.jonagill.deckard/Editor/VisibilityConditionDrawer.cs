using System;
using Deckard.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

namespace Deckard.Editor
{
    [CustomPropertyDrawer(typeof(CsvVisibility.VisibilityCondition))]
    public class VisibilityConditionDrawer : PropertyDrawer
    {
        private static readonly CsvVisibility.VisbilityBehavior[] BehaviorValues = 
            (CsvVisibility.VisbilityBehavior[]) Enum.GetValues(typeof(CsvVisibility.VisbilityBehavior));
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var behaviorValue = GetBehaviorValue(property);

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            
            // Retrieve fields
            var behaviorField = property.FindPropertyRelative(nameof(CsvVisibility.VisibilityCondition.behavior));
            
            SerializedProperty comparatorProperty = null;
            switch (behaviorValue)
            {
                case CsvVisibility.VisbilityBehavior.Match:
                case CsvVisibility.VisbilityBehavior.Different:
                    comparatorProperty =
                        property.FindPropertyRelative(nameof(CsvVisibility.VisibilityCondition.stringComparator));
                    break;
                case CsvVisibility.VisbilityBehavior.Equals:
                case CsvVisibility.VisbilityBehavior.NotEqual:
                case CsvVisibility.VisbilityBehavior.GreaterThan:
                case CsvVisibility.VisbilityBehavior.LessThan:
                case CsvVisibility.VisbilityBehavior.GreaterThanOrEqualTo:
                case CsvVisibility.VisbilityBehavior.LessThanOrEqualTo:
                    comparatorProperty =
                        property.FindPropertyRelative(nameof(CsvVisibility.VisibilityCondition.floatComparator));
                    break;
                case CsvVisibility.VisbilityBehavior.Empty:
                case CsvVisibility.VisbilityBehavior.NotEmpty:
                default:
                    break;
            }

            // Calculate rects
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var behaviorRect = new Rect(position.x, position.y, 200f, position.height);
            var comparatorRect = new Rect(position.x + 200f, position.y, position.width - 200f, position.height);

            // Draw fields
            EditorGUI.PropertyField(behaviorRect, behaviorField, GUIContent.none);
            
            if (comparatorProperty != null)
            {
                EditorGUI.PropertyField(comparatorRect, comparatorProperty, GUIContent.none);
            }

            EditorGUI.EndProperty();
        }

        private CsvVisibility.VisbilityBehavior GetBehaviorValue(SerializedProperty property)
        {
            var behaviorField = property.FindPropertyRelative(nameof(CsvVisibility.VisibilityCondition.behavior));
            return BehaviorValues[behaviorField.enumValueIndex];
        }
    }
}
