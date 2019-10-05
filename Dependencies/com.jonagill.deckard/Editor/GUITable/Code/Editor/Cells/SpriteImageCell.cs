using UnityEngine;
using UnityEditor;
namespace EditorGUITable
{

	public class SpriteImageCell : TableCell
	{

		SerializedProperty sp;

		public override void DrawCell(Rect rect)
		{
			if (sp != null)
			{
				sp.objectReferenceValue = (Sprite)EditorGUI.ObjectField(rect, GUIContent.none, sp.objectReferenceValue, typeof(Sprite), false);
				sp.serializedObject.ApplyModifiedProperties();
			}
			else
			{
				Debug.LogWarningFormat("Property not found: {0} -> {1}", sp.serializedObject.targetObject.name, sp.propertyPath);
			}
		}

		public override string comparingValue
		{
			get
			{
				return GetPropertyValueAsString(sp);
			}
		}

		public SpriteImageCell(SerializedProperty property)
		{
			this.sp = property;
		}

	}

}
