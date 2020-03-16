﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorGUITable;

public class CustomCellsWindow : EditorWindow 
{

	GUITableState tableState;

	void OnEnable ()
	{
		tableState = new GUITableState("tableState5");
	}

	void OnGUI () 
	{

		GUILayout.Label ("Customize the cells", EditorStyles.boldLabel);

		DrawCustomCells ();

	}

	void DrawCustomCells ()
	{
		SerializedObject serializedObject = new SerializedObject(SimpleExample.Instance);
		
		List<TableColumn> columns = new List<TableColumn>()
		{
			new TableColumn(TableColumn.Title ("String"), TableColumn.Width (60f)),
			new TableColumn(TableColumn.Title ("Float"), TableColumn.Width (50f)),
			new TableColumn(TableColumn.Title ("Object"), TableColumn.Width (110f)),
			new TableColumn(TableColumn.Title (""), TableColumn.Width(100f), TableColumn.EnabledTitle(false)),
		};

		List<List<TableCell>> rows = new List<List<TableCell>>();

		SimpleExample targetObject = (SimpleExample) serializedObject.targetObject;

		for (int i = 0 ; i < targetObject.simpleObjects.Count ; i++)
		{
			SimpleExample.SimpleObject entry = targetObject.simpleObjects[i];
			rows.Add (new List<TableCell>()
			{
				new LabelCell (entry.stringProperty),
				new PropertyCell (serializedObject, string.Format("simpleObjects.Array.data[{0}].floatProperty", i)),
				new PropertyCell (serializedObject, string.Format("simpleObjects.Array.data[{0}].objectProperty", i)),
				new ActionCell ("Reset", () => entry.Reset ()),
			});
		}

		tableState = GUITableLayout.DrawTable (tableState, columns, rows);
	}

}
