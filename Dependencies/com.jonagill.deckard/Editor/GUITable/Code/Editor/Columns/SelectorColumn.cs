﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace EditorGUITable
{

	/// <summary>
	/// This class represents a column that can automatically get a cell from an element's serialized property.
	/// It is abstract so there can be several ways to get this cell.
	/// </summary>
	public abstract class SelectorColumn : TableColumn
	{

		public SelectorColumn (params TableColumnOption[] options) : base (options) {}

		[System.Obsolete ("Use SelectorColumn(options) instead, with the option TableColumn.Title()")]
		public SelectorColumn (string title, params TableColumnOption[] options) : base (title, options) {}

		[System.Obsolete ("Use SelectorColumn(options) instead, with the options TableColumn.Title() and TableColumn.Width()")]
		public SelectorColumn (string title, float width, params TableColumnOption[] options) : base (title, width, options) {}

		public abstract TableCell GetCell (SerializedProperty elementProperty);

	}

}