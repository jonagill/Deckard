﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace EditorGUITable
{

	/// <summary>
	/// Main Class of the Table Plugin.
	/// This contains static functions to draw a table, from the most basic
	/// to the most customizable.
	/// </summary>
	public static class GUITable
	{

		static readonly Color TABLE_BG_COLOR = new Color (0.3f, 0.3f, 0.3f);

		/// <summary>
		/// Draw a table just from the collection's property.
		/// This will create columns for all the visible members in the elements' class,
		/// similar to what Unity would show in the classic vertical collection display, but as a table instead.
		/// </summary>
		/// <returns>The updated table state.</returns>
		/// <param name="rect">The table's containing rectangle.</param>
		/// <param name="tableState">The Table state.</param>
		/// <param name="collectionProperty">The serialized property of the collection.</param>
		/// <param name="options">The table options.</param>
		public static GUITableState DrawTable (
			Rect rect,
			GUITableState tableState,
			SerializedProperty collectionProperty,
			params GUITableOption[] options) 
		{

			List <string> properties = SerializationHelpers.GetElementsSerializedFields (collectionProperty);
			if (properties == null && collectionProperty.arraySize == 0)
			{
				DrawTable (
					rect,
					null, 
					new List<TableColumn> () 
					{ 
						new TableColumn (TableColumn.Title (collectionProperty.displayName + "(properties unknown, add at least 1 element)"), TableColumn.Sortable (false), TableColumn.Resizeable (false))
					}, 
					new List <List <TableCell>> (),
					collectionProperty, 
					options);
				return tableState;
			}
			return DrawTable (rect, tableState, collectionProperty, properties, options);
		}

		/// <summary>
		/// Draw a table using just the paths of the properties to display.
		/// This will create columns automatically using the property name as title, and will create
		/// PropertyCell instances for each element.
		/// </summary>
		/// <returns>The updated table state.</returns>
		/// <param name="rect">The table's containing rectangle.</param>
		/// <param name="tableState">The Table state.</param>
		/// <param name="collectionProperty">The serialized property of the collection.</param>
		/// <param name="properties">The paths (names) of the properties to display.</param>
		/// <param name="options">The table options.</param>
		public static GUITableState DrawTable (
			Rect rect,
			GUITableState tableState,
			SerializedProperty collectionProperty, 
			List<string> properties, 
			params GUITableOption[] options) 
		{

			List<SelectorColumn> columns = ParsingHelpers.ParseColumns(properties);

			if (SerializationHelpers.IsObjectReferencesCollection(collectionProperty))
				columns.Insert(0, new SelectObjectReferenceColumn());

			return DrawTable (rect, tableState, collectionProperty, columns, options);
		}

		/// <summary>
		/// Draw a table from the columns' settings, the path for the corresponding properties and a selector function
		/// that takes a SerializedProperty and returns the TableCell to put in the corresponding cell.
		/// </summary>
		/// <returns>The updated table state.</returns>
		/// <param name="rect">The table's containing rectangle.</param>
		/// <param name="tableState">The Table state.</param>
		/// <param name="collectionProperty">The serialized property of the collection.</param>
		/// <param name="columns">The Selector Columns.</param>
		/// <param name="options">The table options.</param>
		public static GUITableState DrawTable (
			Rect rect,
			GUITableState tableState,
			SerializedProperty collectionProperty, 
			List<SelectorColumn> columns, 
			params GUITableOption[] options) 
		{
			GUITableEntry tableEntry = new GUITableEntry (options);
			List<List<TableCell>> rows = new List<List<TableCell>>();
			for (int i = 0 ; i < collectionProperty.arraySize ; i++)
			{
				SerializedProperty sp = collectionProperty.FindPropertyRelative (SerializationHelpers.GetElementAtIndexRelativePath (i));
				if (tableEntry.filter != null && !tableEntry.filter (sp))
					continue;
				List<TableCell> row = new List<TableCell>();
				foreach (SelectorColumn col in columns)
				{
					row.Add ( col.GetCell (sp));
				}
				rows.Add(row);
			}

			return DrawTable (rect, tableState, columns.Select((col) => (TableColumn) col).ToList(), rows, collectionProperty, options);
		}

		/// <summary>
		/// Draw a table completely manually.
		/// Each cell has to be created and given as parameter in cells.
		/// A collectionProperty is needed for reorderable tables. Use an overload with a collectionProperty.
		/// </summary>
		/// <returns>The updated table state.</returns>
		/// <param name="rect">The table's containing rectangle.</param>
		/// <param name="tableState">The Table state.</param>
		/// <param name="columns">The Columns of the table.</param>
		/// <param name="cells">The Cells as a list of rows.</param>
		/// <param name="options">The table options.</param>
		public static GUITableState DrawTable (
			Rect rect,
			GUITableState tableState,
			List<TableColumn> columns, 
			List<List<TableCell>> cells, 
			params GUITableOption[] options)
		{
			return DrawTable(rect, tableState, columns, cells, null, options);
		}

		// Used for ReorderableList's callbacks access
		static List<List<TableCell>> orderedRows;
		static List<List<TableCell>> staticCells;

		/// <summary>
		/// Draw a table completely manually.
		/// Each cell has to be created and given as parameter in cells.
		/// </summary>
		/// <returns>The updated table state.</returns>
		/// <param name="rect">The table's containing rectangle.</param>
		/// <param name="tableState">The Table state.</param>
		/// <param name="columns">The Columns of the table.</param>
		/// <param name="cells">The Cells as a list of rows.</param>
		/// <param name="collectionProperty">The SerializeProperty of the collection. This is useful for reorderable tables.</param>
		/// <param name="options">The table options.</param>
		public static GUITableState DrawTable (
			Rect rect,
			GUITableState tableState,
			List<TableColumn> columns, 
			List<List<TableCell>> cells, 
			SerializedProperty collectionProperty,
			params GUITableOption[] options)
		{
			GUITableEntry tableEntry = new GUITableEntry (options);

			if (tableState == null)
				tableState = new GUITableState();

			if (tableEntry.reorderable)
			{
				if (collectionProperty == null)
				{
					Debug.LogError ("The collection's serialized property is needed to draw a reorderable table.");
					return tableState;
				}

				staticCells = cells;
			
				if (tableState.reorderableList == null)
				{
					ReorderableList list = new ReorderableList (
						collectionProperty.serializedObject, 
						collectionProperty,
						true, true, true, true);

					list.drawElementCallback = (Rect r, int index, bool isActive, bool isFocused) => {
						DrawLine (tableState, columns, orderedRows[index], r.xMin + (list.draggable ? 0 : 14), r.yMin, tableEntry.rowHeight);
					};

					list.elementHeight = tableEntry.rowHeight;

					list.drawHeaderCallback = (Rect r) => { 
						DrawHeaders(r, tableState, columns, r.xMin + 12, r.yMin); 
					};

					list.onRemoveCallback = (l) => 
					{
						l.serializedProperty.DeleteArrayElementAtIndex (staticCells.IndexOf (orderedRows[l.index]));
					};

					tableState.SetReorderableList (list);
				}

				tableState.reorderableList.serializedProperty = collectionProperty;
			}
			
			tableState.CheckState(columns, tableEntry, rect.width);

			orderedRows = cells;
			if (tableState.sortByColumnIndex >= 0)
			{
				if (tableState.sortIncreasing)
					orderedRows = cells.OrderBy (row => row [tableState.sortByColumnIndex]).ToList();
				else
					orderedRows = cells.OrderByDescending (row => row [tableState.sortByColumnIndex]).ToList();
			}

			if (tableEntry.reorderable)
			{
				tableState.reorderableList.DoList(new Rect (rect.x, rect.y, tableState.totalWidth + 23f, rect.height));
				collectionProperty.serializedObject.ApplyModifiedProperties ();
				return tableState;
			}


			float rowHeight = tableEntry.rowHeight;

			float currentX = rect.x;
			float currentY = rect.y + (tableEntry.rotateHeaders ? HeadersHeight(tableEntry) - EditorGUIUtility.singleLineHeight : 5);

			DrawHeaders(rect, tableState, columns, currentX - tableState.scrollPos.x, currentY, tableEntry.rotateHeaders);

			GUI.enabled = true;

			currentY += EditorGUIUtility.singleLineHeight;

			if (tableEntry.allowScrollView)
			{
				tableState.scrollPos = GUI.BeginScrollView (
					new Rect (currentX, currentY, rect.width, TableHeight (tableEntry, cells.Count)),
					tableState.scrollPos, 
					new Rect(0f, 0f, tableState.totalWidth, tableEntry.rowHeight * cells.Count));
				currentX = 0f;
				currentY = 0f;
			}

			if (orderedRows.Count == 0)
			{
				currentX = tableEntry.allowScrollView ? 0 : rect.x;
				GUIHelpers.DrawRect (new Rect (currentX, currentY, tableState.totalWidth, rowHeight), TABLE_BG_COLOR);
				GUI.Label (new Rect (currentX + 5, currentY, rect.width, rowHeight), "Collection is empty");
			}
			else
			{
				foreach (List<TableCell> row in orderedRows)
				{
					currentX = tableEntry.allowScrollView ? 0 : rect.x;
					DrawLine (tableState, columns, row, currentX, currentY, rowHeight);
					currentY += rowHeight;
				}
			}

			GUI.enabled = true;

			if (tableEntry.allowScrollView)
			{
				GUI.EndScrollView ();
			}

			tableState.Save();

			return tableState;
		}

	

		public static void DrawHeaders (
			Rect rect,
			GUITableState tableState,
			List<TableColumn> columns,
			float currentX,
			float currentY,
			bool rotated = false)
		{
			tableState.RightClickMenu (columns, rect);
			for (int i = 0 ; i < columns.Count ; i++)
			{
				TableColumn column = columns[i];
				if (!tableState.columnVisible [i])
					continue;
				string columnName = column.entry.title;
				if (tableState.sortByColumnIndex == i)
				{
					if (tableState.sortIncreasing)
						columnName += " " + '\u25B2'.ToString();
					else
						columnName += " " + '\u25BC'.ToString();
				}

				GUI.enabled = true;

				if (!rotated)
					tableState.ResizeColumn (columns, i, currentX, rect);

				GUI.enabled = column.entry.enabledTitle;
				Rect headerRect = new Rect(currentX, currentY, tableState.absoluteColumnSizes[i] + 4, EditorGUIUtility.singleLineHeight);
				GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButtonMid);

				Matrix4x4 matrix = GUI.matrix;
				if (rotated)
				{
					Vector2 pivot = new Vector2(headerRect.center.x, headerRect.center.y);
					GUIUtility.RotateAroundPivot(-45f, pivot);
					buttonStyle.normal.background = null;
					buttonStyle.active.background = null;
					buttonStyle.alignment = TextAnchor.MiddleLeft;
					headerRect = new Rect(headerRect.center.x - 10f, headerRect.min.y, buttonStyle.CalcSize(new GUIContent(columnName)).x, headerRect.height);
				}
				if (GUI.Button(headerRect, columnName, buttonStyle) && column.entry.isSortable)
				{
					if (tableState.sortByColumnIndex == i && tableState.sortIncreasing)
					{
						tableState.sortIncreasing = false;
					}
					else if (tableState.sortByColumnIndex == i && !tableState.sortIncreasing)
					{
						tableState.sortByColumnIndex = -1;
					}
					else
					{
						tableState.sortByColumnIndex = i;
						tableState.sortIncreasing = true;
					}
				}
				if (rotated)
					GUI.matrix = matrix;

				currentX += tableState.absoluteColumnSizes[i] + 4f;
			}
		}

		public static void DrawLine (
			GUITableState tableState,
			List<TableColumn> columns,
			List<TableCell> row, 
			float currentX,
			float currentY,
			float rowHeight)
		{

			for (int i = 0 ; i < row.Count ; i++)
			{
				if (i >= columns.Count)
				{
					Debug.LogWarning ("The number of cells in this row is more than the number of columns");
					continue;
				}
				if (!tableState.columnVisible [i])
					continue;
				TableColumn column = columns [i];
				TableCell property = row[i];
				GUI.enabled = column.entry.enabledCells;
				property.DrawCell (new Rect(currentX, currentY, tableState.absoluteColumnSizes[i], rowHeight));
				currentX += tableState.absoluteColumnSizes[i] + 4f;
			}
		}

		public static float TableHeight (GUITableEntry tableEntry, int nbRows)
		{
			return HeadersHeight(tableEntry) + RowsHeight(tableEntry, nbRows) + ExtraHeight(tableEntry);
		}

		public static float HeadersHeight(GUITableEntry tableEntry)
		{
			return tableEntry.rotateHeaders ? 70f : EditorGUIUtility.singleLineHeight;
		}

		public static float RowsHeight(GUITableEntry tableEntry, int nbRows)
		{
			return RowHeight(tableEntry) * Mathf.Max(1, nbRows);
		}

		public static float RowHeight(GUITableEntry tableEntry)
		{
			return (tableEntry.rowHeight + (tableEntry.reorderable ? 5 : 0));
		}

		public static float ExtraHeight(GUITableEntry tableEntry)
		{
			return (tableEntry.reorderable ? EditorGUIUtility.singleLineHeight : 0f);
		}

	}
}
