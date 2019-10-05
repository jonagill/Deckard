using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace EditorGUITable
{

	/// <summary>
	/// Base class for all table columns.
	/// It only takes a title and a width in the constructor, but other settings are available to customize the column.
	/// </summary>
	public class TableColumn
	{

		public TableColumnEntry entry;

		/// <summary>
		/// Initializes a column with its title and options.
		/// Edit the other public properties for more settings.
		/// </summary>
		/// <param name="options">The column options.</param>
		public TableColumn (params TableColumnOption[] options)
		{
			// This function uses GetDefaultTitle. So, in inherited classes, if GetDefaultTitle needs fields that are set in the 
			// inherited constructor, this function must be called again after these fields are set.
			InitEntry (options);
		}

		/// <summary>
		/// Initializes a column with its title and options.
		/// Edit the other public properties for more settings.
		/// </summary>
		/// <param name="title">The column title.</param>
		/// <param name="options">The column options.</param>
		[System.Obsolete ("Use TableColumn(options) instead, with TableColumn.Title() to set the title")]
		public TableColumn (string title, params TableColumnOption[] options)
		{
			this.entry = new TableColumnEntry(options.Concat(new TableColumnOption[] {TableColumn.Title (title)}).ToArray ());
		}

		[System.Obsolete ("Use TableColumn(options) instead, with TableColumn.Width() and Title() to set the width and title")]
		public TableColumn (string title, float width, params TableColumnOption[] options) : this (title, options.Concat(new[] { TableColumn.Width (width) }).ToArray ())
		{
			this.entry = new TableColumnEntry(options.Concat(new TableColumnOption[] {TableColumn.Title (title), TableColumn.Width (width)}).ToArray ());
		}

		protected void InitEntry (params TableColumnOption[] options)
		{
			if (!options.Any (op => op.type == TableColumnOption.Type.Title))
				options = options.Concat (new TableColumnOption[] {TableColumn.Title (GetDefaultTitle ())}).ToArray ();
			this.entry = new TableColumnEntry(options);
		}

		public float GetDefaultWidth ()
		{
			if (entry.defaultWidth > 0f)
				return entry.defaultWidth;
			else
			{
				float minWidth, maxWidth;
				GUI.skin.button.CalcMinMaxWidth(new GUIContent(entry.title), out minWidth, out maxWidth);
				return minWidth;
			}
		}

		protected virtual string GetDefaultTitle ()
		{
			return "Unnamed";
		}

		// TODO These options are not ready yet 
		//		public static TableColumnOption ExpandWidth (bool enable)
		//		{
		//			return new TableColumnOption (TableColumnOption.Type.ExpandWidth, enable);
		//		}

		/// <summary>
		/// Sets the minimum width of the column that the user can manually resize it to.
		/// </summary>
		public static TableColumnOption MinWidth (float value)
		{
			return new TableColumnOption (TableColumnOption.Type.MinWidth, value);
		}

		/// <summary>
		/// Sets the maximum width of the column that the user can manually resize it to.
		/// </summary>
		public static TableColumnOption MaxWidth (float value)
		{
			return new TableColumnOption (TableColumnOption.Type.MaxWidth, value);
		}

		/// <summary>
		/// Sets the default width of the column.
		/// </summary>
		public static TableColumnOption Width (float value)
		{
			return new TableColumnOption (TableColumnOption.Type.Width, value);
		}

		/// <summary>
		/// If true, the Width value will be relative to the containing view, instead of absolute (0.5 means 50% of the containing view, etc).
		/// Relative columns will automatically resize with the containing view.
		/// </summary>
		public static TableColumnOption Relative (bool value = true)
		{
			return new TableColumnOption (TableColumnOption.Type.Relative, value);
		}

		/// <summary>
		/// If <see langword="true"/>, the user will be able to resize this column by dragging the right edge of the title cell.
		/// </summary>
		public static TableColumnOption Resizeable (bool value)
		{
			return new TableColumnOption (TableColumnOption.Type.Resizeable, value);
		}

		/// <summary>
		/// If <see langword="true"/>, the user will be able to sort the table by this column by clicking on the title.
		/// </summary>
		public static TableColumnOption Sortable (bool value)
		{
			return new TableColumnOption (TableColumnOption.Type.Sortable, value);
		}

		/// <summary>
		/// If <see langword="false"/>, the title cell for this columns will be disabled (so not interactable).
		/// </summary>
		public static TableColumnOption EnabledTitle (bool value)
		{
			return new TableColumnOption (TableColumnOption.Type.EnabledTitle, value);
		}

		/// <summary>
		/// If <see langword="false"/>, the cells for this columns will be disabled (so not interactable).
		/// </summary>
		public static TableColumnOption EnabledCells (bool value)
		{
			return new TableColumnOption (TableColumnOption.Type.EnabledCells, value);
		}

		/// <summary>
		/// If <see langword="true"/>, the user will be able to hide or show this column by right-clicking on the titles area.
		/// </summary>
		public static TableColumnOption Optional (bool value)
		{
			return new TableColumnOption (TableColumnOption.Type.Optional, value);
		}

		/// <summary>
		/// If <see langword="false"/> and Optional is <see langword="true"/>, the column will be hidden by default,
		/// and shown only if the user right-clicks on the title area and selects this column.
		/// </summary>
		public static TableColumnOption VisibleByDefault (bool value)
		{
			return new TableColumnOption (TableColumnOption.Type.VisibleByDefault, value);
		}

		/// <summary>
		/// Sets the title for this column.
		/// </summary>
		public static TableColumnOption Title (string value)
		{
			return new TableColumnOption (TableColumnOption.Type.Title, value);
		}

		/// <summary>
		/// If true, all attributes declared above this field will be ignored.
		/// This can be used to avoid issues when drawing fields with decorator attributes like [Header]... in the table.
		/// This option will not work on properties of type: Quaternion, LayerMask, AnimationCurve and Gradient
		/// </summary>
		public static TableColumnOption IgnoreAttributes(bool value)
		{
			return new TableColumnOption(TableColumnOption.Type.IgnoreAttributes, value);
		}

		public static TableColumnOption CellType(System.Type value)
		{
			return new TableColumnOption(TableColumnOption.Type.CellType, value);
		}
	}

}