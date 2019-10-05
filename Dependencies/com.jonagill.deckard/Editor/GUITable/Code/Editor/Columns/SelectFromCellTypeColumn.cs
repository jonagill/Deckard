using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace EditorGUITable
{

	/// <summary>
	/// This class represents a column that will draw Property Cells from the given property name, 
	/// relative to the collection element's serialized property.
	/// </summary>
	[Obsolete("Use TableColumn.CellType option with SelectFromPropertyName instead.")]
	public class SelectFromCellTypeColumn : SelectorColumn
	{
		public string propertyName;

		public Type cellType;

		[Obsolete("Use TableColumn.CellType option with SelectFromPropertyName instead.")]
		public SelectFromCellTypeColumn (string propertyName, Type cellType, params TableColumnOption[] options) : base (options)
		{
			this.propertyName = propertyName;
			this.cellType = cellType;
			InitEntry (options);
		}

		[Obsolete("Use TableColumn.CellType option with SelectFromPropertyName instead.")]
		public SelectFromCellTypeColumn (string propertyName, Type cellType, string title, params TableColumnOption[] options) : base (title, options)
		{
			this.propertyName = propertyName;
			this.cellType = cellType;
			// Not calling InitEntry again, because the title is set by parameter
		}

		public override TableCell GetCell (SerializedProperty elementProperty)
		{
			if (!cellType.IsSubclassOf (typeof (TableCell)))
			{
				Debug.LogErrorFormat ("Type {0} is not a TableCell type", cellType);
				return null;
			}

			ConstructorInfo c = cellType.GetConstructors ().FirstOrDefault (ci => (ci.GetParameters().Length == 1 || ci.GetParameters().Length >= 1 && ci.GetParameters().Skip(1).All(pi => pi.IsOptional)) && ci.GetParameters ()[0].ParameterType == typeof (SerializedProperty));
			if (c == default (ConstructorInfo))
			{
				Debug.LogErrorFormat ("Type {0} doesn't have a constructor taking a SerializedProperty as parameter.", cellType);
				return null;
			}
			List<object> parameters = new List<object> { elementProperty.FindPropertyRelative(propertyName) };
			for (int i = 0; i < c.GetParameters().Length - 1; i++)
				parameters.Add(null);
			return (TableCell) c.Invoke (parameters.ToArray());
		}

		protected override string GetDefaultTitle ()
		{
			return ObjectNames.NicifyVariableName (propertyName);
		}
	}

}