﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorGUITable;


public class PropertyAttributeExample : MonoBehaviour {
	
	[System.Serializable]
	public class SimpleObject
	{
		public string stringProperty;
		public float floatProperty;
		public GameObject objectProperty;
		public Vector2 v2Property;
	}

	public List<SimpleObject> simpleObjectsDefaultDisplay;

	[Table]
	public List<SimpleObject> simpleObjectsUsingTableAttribute;

	[ReorderableTable(new string[] { "stringProperty", "floatProperty:Width(40),Title(float)", "v2Property" }, "RowHeight(22)")]
	public List<SimpleObject> simpleObjectsUsingReorderableTableAttribute;

	[Table]
	public List <Enemy> enemies;

	void OnGUI ()
	{
		GUILayout.Label ("Select the PropertyAttribute scene object to visualize the table in the inspector");
	}

}
