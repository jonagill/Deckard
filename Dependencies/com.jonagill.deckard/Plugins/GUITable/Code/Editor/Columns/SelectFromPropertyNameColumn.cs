using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace EditorGUITable
{

	/// <summary>
	/// This class represents a column that will draw Property Cells from the given property name, 
	/// relative to the collection element's serialized property.
	/// </summary>
	public class SelectFromPropertyNameColumn : SelectorColumn
	{
		public string propertyName;

		public SelectFromPropertyNameColumn (string propertyName, params TableColumnOption[] options) : base (options)
		{
			this.propertyName = propertyName;
			InitEntry (options);
		}

		[System.Obsolete ("Use SelectFromPropertyNameColumn(propertyName, options) instead, with the option TableColumn.Title()")]
		public SelectFromPropertyNameColumn (string propertyName, string title, params TableColumnOption[] options) : base (title, options)
		{
			this.propertyName = propertyName;
			// Not calling InitEntry again, because the title is set by parameter
		}

		public override TableCell GetCell (SerializedProperty elementProperty)
		{
			if (entry.cellType != null)
			{
				if (!entry.cellType.IsSubclassOf(typeof(TableCell)))
				{
					Debug.LogErrorFormat("Type {0} is not a TableCell type", entry.cellType);
					return null;
				}

				ConstructorInfo c = entry.cellType.GetConstructors().FirstOrDefault(ci => (ci.GetParameters().Length == 1 || ci.GetParameters().Length >= 1 && ci.GetParameters().Skip(1).All(pi => pi.IsOptional)) && ci.GetParameters()[0].ParameterType == typeof(SerializedProperty));
				if (c == default(ConstructorInfo))
				{
					Debug.LogErrorFormat("Type {0} doesn't have a constructor taking a SerializedProperty as parameter.", entry.cellType);
					return null;
				}

				List<object> parameters;
				if (elementProperty.propertyType == SerializedPropertyType.ObjectReference)
				{
					if (elementProperty.objectReferenceValue == null)
						return new LabelCell("null");
					parameters = new List<object> { new SerializedObject(elementProperty.objectReferenceValue).FindProperty(propertyName) };
				}
				else
					parameters = new List<object> { elementProperty.FindPropertyRelative(propertyName) };

				for (int i = 0; i < c.GetParameters().Length - 1; i++)
					parameters.Add(null);
				return (TableCell)c.Invoke(parameters.ToArray());
			}
			else
			{

				if (elementProperty.propertyType == SerializedPropertyType.ObjectReference)
				{
					if (elementProperty.objectReferenceValue == null)
						return new LabelCell("null");
					return new PropertyCell(new SerializedObject(elementProperty.objectReferenceValue).FindProperty(propertyName), entry.ignoreAttributes);
				}
				else
					return new PropertyCell(elementProperty.FindPropertyRelative(propertyName), entry.ignoreAttributes);
			}
		}

		protected override string GetDefaultTitle ()
		{
			return ObjectNames.NicifyVariableName (propertyName);
		}
	
	}

}