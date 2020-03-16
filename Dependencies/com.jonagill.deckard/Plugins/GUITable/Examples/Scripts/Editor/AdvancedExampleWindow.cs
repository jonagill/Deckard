using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EditorGUITable;

public class AdvancedExampleWindow : EditorWindow 
{

	SerializedObject serializedObject;

	GUITableState tableState;

	public static void Init ()
	{
		AdvancedExampleWindow window = EditorWindow.GetWindow<AdvancedExampleWindow>();
		window.position = new Rect(Screen.currentResolution.width / 2 - 300, Screen.currentResolution.height / 2 - 200, 600, 400);
		window.titleContent = new GUIContent("Enemies Window");
		window.Show();
	}

	void OnGUI () 
	{
		EditorStyles.boldLabel.alignment = TextAnchor.MiddleCenter;
		GUILayout.Label ("Enemies", EditorStyles.boldLabel);

		DrawObjectsTable ();

	}
	void DrawObjectsTable ()
	{

		SerializedObject serializedObject = new SerializedObject(AdvancedExample.Instance);
		AdvancedExample targetObject = (AdvancedExample) serializedObject.targetObject;

		List <SelectorColumn> columns = new List<SelectorColumn> ();
		columns.Add (new SelectObjectReferenceColumn (TableColumn.Title ("Enemy Prefab"), TableColumn.Width(75f), TableColumn.EnabledCells(false), TableColumn.Optional(true)));
		columns.Add (new SelectFromPropertyNameColumn ("type", TableColumn.Width(50f), TableColumn.Optional(true)));
		columns.Add (new SelectFromPropertyNameColumn ("health", TableColumn.Width(50f)));
		columns.Add (new SelectFromPropertyNameColumn ("speed", TableColumn.Width(50f)));
		columns.Add (new SelectFromPropertyNameColumn ("color", TableColumn.Width(50f), TableColumn.Optional(true)));
		columns.Add (new SelectFromPropertyNameColumn ("canSwim", TableColumn.Title ("Swim"), TableColumn.Width(35f), TableColumn.Optional(true)));
		columns.Add (new SelectFromFunctionColumn (
			sp => new SpawnersCell (new SerializedObject (sp.objectReferenceValue), "spawnersMask"), 
			TableColumn.Title ("Spawners"), 
			TableColumn.Width(80f), 
			TableColumn.Optional(true)));
		columns.Add (new SelectFromFunctionColumn (
			sp =>
		{
			Enemy enemy = (Enemy) sp.objectReferenceValue;
			int sentenceIndex = targetObject.introSentences.FindIndex(s => s.enemyType == enemy.type);
			return new PropertyCell (serializedObject, string.Format("introSentences.Array.data[{0}].sentence", sentenceIndex));
		}, 
			TableColumn.Title ("Intro (shared by type)"), 
			TableColumn.Width(150f), 
			TableColumn.Optional(true)));
		columns.Add (new SelectFromFunctionColumn (
			sp => 
		{
			Enemy enemy = (Enemy) sp.objectReferenceValue;
			return new ActionCell ("Instantiate", () => enemy.Instantiate ());
		}, 
			TableColumn.Title ("Instantiation"), 
			TableColumn.Width(110f), 
			TableColumn.Optional(true)));

		tableState = GUITableLayout.DrawTable (tableState, serializedObject.FindProperty ("enemies"), columns);

	}

}
