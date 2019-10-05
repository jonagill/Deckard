using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;


namespace EditorGUITable
{

	/// <summary>
	/// This cell class just uses EditorGUILayout.PropertyField to draw a given property.
	/// This is the basic way to use GUITable. It will draw the properties the same way Unity would by default.
	/// </summary>
	public class PropertyCell : TableCell
	{
		SerializedProperty sp;
		SerializedObject so;
		string propertyPath;
		bool ignoreAttributes;

		public override void DrawCell (Rect rect)
		{
			if (sp != null)
			{
				if (ignoreAttributes)
				{
					DefaultPropertyField(rect, sp, GUIContent.none);
					so.ApplyModifiedProperties();
				}
				else
				{
					Type t;
					System.Reflection.FieldInfo fieldInfo = ReflectionHelpers.GetFieldInfoFromPropertyPath(sp.serializedObject.targetObject.GetType(), sp.propertyPath, out t);

					bool hasTextAreaAttribute = fieldInfo.GetCustomAttributes(typeof(TextAreaAttribute), true).Length > 0;
					if (hasTextAreaAttribute)
						rect = new Rect(rect.x, rect.y - EditorGUIUtility.singleLineHeight, rect.width, rect.height + EditorGUIUtility.singleLineHeight);
					EditorGUI.PropertyField(rect, sp, GUIContent.none);
					so.ApplyModifiedProperties();
				}
			}
			else
			{
				Debug.LogWarningFormat ("Property not found: {0} -> {1}", so.targetObject.name, propertyPath);
			}
		}

		public override string comparingValue
		{
			get
			{
				return GetPropertyValueAsString (sp);
			}
		}

		public override int CompareTo (object other)
		{
			
			TableCell otherCell = (TableCell) other;
			if (otherCell == null)
				throw new ArgumentException ("Object is not a TableCell");

			PropertyCell otherPropCell = (PropertyCell) other;
			if (otherPropCell == null)
				return base.CompareTo(other);

			SerializedProperty otherSp = otherPropCell.sp;

			return CompareTwoSerializedProperties (sp, otherSp);
		}

		public PropertyCell (SerializedProperty property, bool ignoreAttributes = false)
		{
			this.sp = property;
			this.so = property.serializedObject;
			this.propertyPath = property.propertyPath;
			this.ignoreAttributes = ignoreAttributes;
		}

		public PropertyCell (SerializedObject so, string propertyPath, bool ignoreAttributes = false) 
			: this(so.FindProperty(propertyPath), ignoreAttributes) {}



		#region Manual Property Drawing

		// This section contains Unity Internal code, copied from https://github.com/Unity-Technologies/UnityCsReference
		// It is used to reproduce the drawing of a property without its attributes, similar to the Inspector's debug mode.
		// Some parts were left out or simplified, otherwise it would require copying the whole Unity code...

		static void DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedPropertyType type = property.propertyType;

			switch (type)
			{
				case SerializedPropertyType.Integer:
					{
						EditorGUI.BeginChangeCheck();
						long newValue = EditorGUI.LongField(position, label, property.longValue);
						if (EditorGUI.EndChangeCheck())
						{
							property.longValue = newValue;
						}
						break;
					}
				case SerializedPropertyType.Float:
					{
						EditorGUI.BeginChangeCheck();

						// Necessary to check for float type to get correct string formatting for float and double.
						bool isFloat = property.type == "float";
						double newValue = isFloat ? EditorGUI.FloatField(position, label, property.floatValue) :
							EditorGUI.DoubleField(position, label, property.doubleValue);
						if (EditorGUI.EndChangeCheck())
						{
							property.doubleValue = newValue;
						}
						break;
					}
				case SerializedPropertyType.String:
					{
						EditorGUI.BeginChangeCheck();
						string newValue = EditorGUI.TextField(position, label, property.stringValue);
						if (EditorGUI.EndChangeCheck())
						{
							property.stringValue = newValue;
						}
						break;
					}
				// Multi @todo: Needs style work
				case SerializedPropertyType.Boolean:
					{
						EditorGUI.BeginChangeCheck();
						bool newValue = EditorGUI.Toggle(position, label, property.boolValue);
						if (EditorGUI.EndChangeCheck())
						{
							property.boolValue = newValue;
						}
						break;
					}
				case SerializedPropertyType.Color:
					{
						EditorGUI.BeginChangeCheck();
						Color newColor = EditorGUI.ColorField(position, label, property.colorValue);
						if (EditorGUI.EndChangeCheck())
						{
							property.colorValue = newColor;
						}
						break;
					}
#if UNITY_2017_1_OR_NEWER
				case SerializedPropertyType.ArraySize:
				{
					EditorGUI.BeginChangeCheck();
					int newValue = EditorGUI.IntField(position, label, property.intValue, EditorStyles.numberField);
					if (EditorGUI.EndChangeCheck())
					{
						property.intValue = newValue;
					}
					break;
				}
				case SerializedPropertyType.FixedBufferSize:
					{
						EditorGUI.BeginChangeCheck();
						int newValue = EditorGUI.IntField(position, label, property.intValue, EditorStyles.numberField);
						if (EditorGUI.EndChangeCheck())
						{
							property.intValue = newValue;
						}
						break;
				}
#endif
				case SerializedPropertyType.Enum:
					{
						Popup(position, property, label);
						break;
					}
				// Multi @todo: Needs testing for texture types
				case SerializedPropertyType.ObjectReference:
					{

						EditorGUI.ObjectField(position, property, label);
						break;
					}
				//case SerializedPropertyType.LayerMask:
					//{
					//	EditorGUI.LayerMaskField(position, property, label);
					//	break;
					//}
				case SerializedPropertyType.Character:
					{
						char[] value = { (char)property.intValue };

						bool wasChanged = GUI.changed;
						GUI.changed = false;
						string newValue = EditorGUI.TextField(position, label, new string(value));
						if (GUI.changed)
						{
							if (newValue.Length == 1)
							{
								property.intValue = newValue[0];
							}
							// Value didn't get changed after all
							else
							{
								GUI.changed = false;
							}
						}
						GUI.changed |= wasChanged;
						break;
					}
				//case SerializedPropertyType.AnimationCurve:
					//{
					//	int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
					//	EditorGUI.DoCurveField(PrefixLabel(position, id, label), id, null, kCurveColor, new Rect(), property);
					//	break;
					//}
				//case SerializedPropertyType.Gradient:
					//{
					//	int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
					//	DoGradientField(PrefixLabel(position, id, label), id, null, property, false);
					//	break;
					//}
				case SerializedPropertyType.Vector3:
					{
						Vector3Field(position, property, label);
						break;
					}
				case SerializedPropertyType.Vector4:
					{
						Vector4Field(position, property, label);
						break;
					}
				case SerializedPropertyType.Vector2:
					{
						Vector2Field(position, property, label);
						break;
					}
#if UNITY_2017_2_OR_NEWER
				case SerializedPropertyType.Vector2Int:
					{
						Vector2Field(position, property, label);
						break;
					}
				case SerializedPropertyType.Vector3Int:
					{
						Vector3Field(position, property, label);
						break;
					}
#endif
				case SerializedPropertyType.Rect:
					{
						RectField(position, property, label);
						break;
					}
#if UNITY_2017_2_OR_NEWER
				case SerializedPropertyType.RectInt:
				{
					RectField(position, property, label);
					break;
				}
#endif
				case SerializedPropertyType.Bounds:
					{
						BoundsField(position, property, label);
						break;
					}
#if UNITY_2017_2_OR_NEWER
				case SerializedPropertyType.BoundsInt:
				{
					BoundsField(position, property, label);
					break;
				}
#endif
				default:
					{
						EditorGUI.PropertyField(position, property);
						//int genericID = GUIUtility.GetControlID(s_GenericField, FocusType.Keyboard, position);
						//PrefixLabel(position, genericID, label);
						break;
					}
				}
			}

		private static void Popup(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();

			int idx = EditorGUI.Popup(position, label, property.hasMultipleDifferentValues ? -1 : property.enumValueIndex, property.enumDisplayNames.Select(n => new GUIContent(n)).ToArray());
			if (EditorGUI.EndChangeCheck())
			{
				property.enumValueIndex = idx;
			}
		}

		private static readonly GUIContent[] s_XYLabels = { new GUIContent("X"), new GUIContent("Y") };
		private static readonly GUIContent[] s_XYZLabels = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") }; 
		private static readonly GUIContent[] s_XYZWLabels = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") }; 
		private static readonly GUIContent[] s_WHLabels = { new GUIContent("W"), new GUIContent("H") };
		const float kSingleLineHeight = 16f;
		const int kVerticalSpacingMultiField = 0;


		private static void Vector2Field(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty cur = property.Copy();
			cur.Next(true);
			EditorGUI.MultiPropertyField(position, s_XYLabels, cur);
		}

		private static void Vector3Field(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty cur = property.Copy();
			cur.Next(true);
			EditorGUI.MultiPropertyField(position, s_XYZLabels, cur);
		}
		private static void Vector4Field(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty cur = property.Copy();
			cur.Next(true);
			EditorGUI.MultiPropertyField(position, s_XYZWLabels, cur);
		}
		private static void RectField(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty cur = property.Copy();
			cur.Next(true);
			EditorGUI.MultiPropertyField(position, s_XYLabels, cur);
			position.y += kSingleLineHeight + kVerticalSpacingMultiField;

			EditorGUI.MultiPropertyField(position, s_WHLabels, cur);
		}
		private static void BoundsField(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty cur = property.Copy();
			cur.Next(true);
			cur.Next(true);
			EditorGUI.MultiPropertyField(position, s_XYZLabels, cur);
			cur.Next(true);
			position.y += kSingleLineHeight + kVerticalSpacingMultiField;
			EditorGUI.MultiPropertyField(position, s_XYZLabels, cur);
		}

		#endregion

	}

}
