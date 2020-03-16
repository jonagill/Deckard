using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace EditorGUITable
{

	/// <summary>
	/// This cell class draws a string in a TextArea.
	/// This is useful for visualizing and editing multiline strings.
	/// </summary>
	public class TextAreaCell : TableCell
	{
		SerializedProperty sp;

		public override void DrawCell (Rect rect)
		{
			if (sp != null)
			{
				sp.stringValue = GUI.TextArea(rect, sp.stringValue);
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
				return sp.stringValue;
			}
		}

		public TextAreaCell(SerializedProperty sp)
		{
			this.sp = sp;
		}
	}

}
