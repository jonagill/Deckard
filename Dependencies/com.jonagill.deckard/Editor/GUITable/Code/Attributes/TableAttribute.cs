using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace EditorGUITable
{

	/// <summary>
	/// Attribute that automatically draws a collection as a table
	/// 
	/// Example:
	/// 
	/// <code>
	///public class TableAttributeExample : MonoBehaviour {
	///
	///	[System.Serializable]
	///	public class SimpleObject
	///	{
	///		public string stringProperty;
	///		public float floatProperty;
	///	}
	///
	///	[TableAttribute]
	///	public List<SimpleObject> simpleObjects;
	///
	///}
	/// 
	/// </code>
	/// 
	/// </summary>
	public class TableAttribute : PropertyAttribute
	{

		public string[] properties = null;
		public string[] tableOptions = null;

		/// <summary>
		/// This attribute will display the collection in a table, instead of the classic Unity list.
		/// </summary>
		public TableAttribute () 
		{
		}

		/// <summary>
		/// This attribute will display the collection's chosen properties in a table, instead of the classic Unity list.
		/// Options can be added to a property's column by adding ":" after the property name, followed by the options separated by commas (Example: "propertyName: Option1(param), Option2(param)")
		/// </summary>
		/// <param name="properties"> The properties to display in the table </param>
		public TableAttribute (params string[] properties)
		{
			this.properties = properties;
		}

		/// <summary>
		/// This attribute will display the collection's chosen properties in a table, with the chosen column sizes, instead of the classic Unity list.
		/// </summary>
		/// <param name="properties"> The properties to display in the table</param>
		/// <param name="widths"> The widths of the table's columns</param>
		/// <param name="tableOptions"> The table's options, in the form "OptionName(param)", separated by commas</param>
		public TableAttribute (string[] properties, float[] widths, params string[] tableOptions)
		{
			this.properties = properties.Select((p, i) => (widths != null && widths.Length > i) ? AppendWidth(p, widths[i]) : p).ToArray();
			this.tableOptions = tableOptions;
		}

		/// <summary>
		/// This attribute will display the collection's chosen properties in a table, instead of the classic Unity list.
		/// Options can be added to a property's column by adding ":" after the property name, followed by the options separated by commas (Example: "propertyName: Option1(param), Option2(param)")
		/// </summary>
		/// <param name="properties"> The properties to display in the table </param>
		/// <param name="tableOptions"> The table's options, in the form "OptionName(param)", separated by commas</param>
		public TableAttribute (string[] properties, params string[] tableOptions)
		{
			this.properties = properties;
			this.tableOptions = tableOptions;
		}

		string AppendWidth(string property, float width)
		{
			if (!property.Contains(":"))
				return string.Format("{0} : Width({1})", property, width);
			else
				return string.Format("{0}, Width({1})", property, width);
		}

	}

}