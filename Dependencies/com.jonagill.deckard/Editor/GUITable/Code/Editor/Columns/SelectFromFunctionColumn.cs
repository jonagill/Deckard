using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EditorGUITable
{
	
	/// <summary>
	/// This class represents a column that will draw cells using the given function from the element's serialized property.
	/// This allows to build any type of table cell.
	/// </summary>
	public class SelectFromFunctionColumn : SelectorColumn
	{
		public Func<SerializedProperty, TableCell> selector;

		public SelectFromFunctionColumn (Func<SerializedProperty, TableCell> selector, params TableColumnOption[] options) : base (options)
		{
			this.selector = selector;
		}

		[System.Obsolete ("Use SelectFromFunctionColumn(selector, options) instead, with the option TableColumn.Title()")]
		public SelectFromFunctionColumn (Func<SerializedProperty, TableCell> selector, string title, params TableColumnOption[] options) : base (title, options)
		{
			this.selector = selector;
		}

		[System.Obsolete ("Use SelectFromFunctionColumn(selector, options) instead, with the options TableColumn.Title() and TableColumn.Width()")]
		public SelectFromFunctionColumn (Func<SerializedProperty, TableCell> selector, string title, float width, params TableColumnOption[] options) : base (title, width, options)
		{
			this.selector = selector;
		}

		public override TableCell GetCell (SerializedProperty elementProperty)
		{
			return selector (elementProperty);
		}
	}


}