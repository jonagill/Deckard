using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace EditorGUITable
{

	public class TableAttributeParsingException : System.SystemException
	{
		public TableAttributeParsingException (string message) : base (message) { }
	}

	/// <summary>
	/// Drawer for the Table Attribute.
	/// See the TableAttribute class documentation for the limitations of this attribute.
	/// </summary>
	[CustomPropertyDrawer(typeof(TableAttribute))]
	public class TableDrawer : PropertyDrawer
	{

		protected GUITableState tableState;

		Rect lastRect;

		GUITableOption[] _tableOptions;
		GUITableOption[] tableOptions
		{
			get
			{
				if (_tableOptions == null)
				{
					TableAttribute tableAttribute = (TableAttribute)attribute;
					_tableOptions = ParsingHelpers.ParseTableOptions(tableAttribute.tableOptions);
					_tableOptions = ForceTableOptions(_tableOptions);
				}
				return _tableOptions;
			}
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			//Check that it is a collection
			Match match = Regex.Match(property.propertyPath, "^([a-zA-Z0-9_]*).Array.data\\[([0-9]*)\\]$");
			if (!match.Success)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			// Check that it's the first element
			string index = match.Groups[2].Value;

			GUITableEntry tableEntry = new GUITableEntry(tableOptions);

			if (index != "0")
				return GUITable.RowHeight(tableEntry);

			// On the first line, add to the normal rowHeight: the singleLineHeight for the Size field, the headers line height and the extra height depending on the table's options
			return GUITable.RowHeight(tableEntry) + EditorGUIUtility.singleLineHeight + GUITable.HeadersHeight(tableEntry) + GUITable.ExtraHeight(tableEntry);

		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			TableAttribute tableAttribute = (TableAttribute) attribute;

			//Check that it is a collection
			Match match = Regex.Match(property.propertyPath, "^([a-zA-Z0-9_]*).Array.data\\[([0-9]*)\\]$");
			if (!match.Success)
			{
				EditorGUI.LabelField(position, label.text, "Use the Table attribute with a collection.");
				return;
			}

			string collectionPath = match.Groups[1].Value;

			// Check that it's the first element
			string index = match.Groups[2].Value;

			if (index != "0")
				return;
			// Sometimes GetLastRect returns 0, so we keep the last relevant value
			if (GUILayoutUtility.GetLastRect().width > 1f)
				lastRect = GUILayoutUtility.GetLastRect();

			SerializedProperty collectionProperty = property.serializedObject.FindProperty(collectionPath);

			EditorGUI.indentLevel = 0;

			Rect r = new Rect(position.x + 15f, position.y, position.width - 15f, lastRect.height);

			tableState = DrawTable (r, collectionProperty, label, tableAttribute);
		}

		protected virtual GUITableState DrawTable (Rect rect, SerializedProperty collectionProperty, GUIContent label, TableAttribute tableAttribute)
		{
			try
			{
				if (tableAttribute.properties == null)
					return GUITable.DrawTable(rect, tableState, collectionProperty, tableOptions);
				else
					return GUITable.DrawTable(rect, tableState, collectionProperty, tableAttribute.properties.ToList(), tableOptions);
			}
			catch (TableAttributeParsingException e)
			{
				Debug.LogError (e.Message + "\nGiving up drawing table for " + collectionProperty.name );
				return tableState;
			}
		}

		GUITableOption[] ForceTableOptions (GUITableOption[] options)
		{
			return options
				.Concat (forcedTableOptions)
				.ToArray ();
		}

		protected virtual GUITableOption[] forcedTableOptions
		{
			get 
			{
				return new GUITableOption[] {GUITableOption.AllowScrollView(false)};
			}
		}

	}

}