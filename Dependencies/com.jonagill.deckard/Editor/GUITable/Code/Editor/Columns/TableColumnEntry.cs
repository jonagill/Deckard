﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EditorGUITable
{

	public class TableColumnEntry
	{

		public float defaultWidth = 0f;
		public float maxWidth = float.MaxValue;
		public float minWidth = 20f;
		public bool relative = false;
		public bool resizeable = true;
		public bool sortable = true;
		/// <summary>
		/// Defines if the cells are enabled (interactable) or disabled (grayed out). Default: true.
		/// </summary>
		public bool enabledCells = true;
		/// <summary>
		/// Defines if the column is sortable.
		/// </summary>
		public bool isSortable = true;
		/// <summary>
		/// Defines if the title is enabled (interactable) or disabled (grayed out). Default: true.
		/// </summary>
		public bool enabledTitle = true;
		/// <summary>
		/// Defines if the column can be hidden by right-clicking the column titles bar. Default: false.
		/// </summary>
		public bool optional = false;
		/// <summary>
		/// Defines if the column is visible by default. If this is false, and optional is false too. The column can never be viewed. Default: true.
		/// </summary>
		public bool visibleByDefault = true;
		/// <summary>
		/// Defines if the column is expandable.
		/// </summary>
		public bool expandWidth = false;
		public string title = null;
		/// <summary>
		/// Defines if the attributes are ignored when drawing the properties.
		/// Can be used to avoid issues when drawing fields with decorator attributes like [Header]... in the table.
		/// </summary>
		public bool ignoreAttributes = false;

		public System.Type cellType;

		public TableColumnEntry (TableColumnOption[] options)
		{
			ApplyOptions (options);
		}

		public virtual void ApplyOptions(TableColumnOption[] options)
		{
			if (options == null)
				return;
			foreach (TableColumnOption option in options)
			{
				switch (option.type)
				{
					case TableColumnOption.Type.ExpandWidth:
						this.expandWidth = (bool) option.value;
						break;
					case TableColumnOption.Type.MaxWidth:
						this.maxWidth = (float) option.value;
						break;
					case TableColumnOption.Type.MinWidth:
						this.minWidth = (float) option.value;
						break;
					case TableColumnOption.Type.Resizeable:
						this.resizeable = (bool) option.value;
						break;
					case TableColumnOption.Type.Sortable:
						this.sortable = (bool) option.value;
						break;
					case TableColumnOption.Type.Width:
						this.defaultWidth = (float) option.value;
						break;
					case TableColumnOption.Type.EnabledTitle:
						this.enabledTitle = (bool) option.value;
						break;
					case TableColumnOption.Type.EnabledCells:
						this.enabledCells = (bool) option.value;
						break;
					case TableColumnOption.Type.Optional:
						this.optional = (bool) option.value;
						break;
					case TableColumnOption.Type.VisibleByDefault:
						this.visibleByDefault = (bool) option.value;
						break;
					case TableColumnOption.Type.Relative:
						this.relative = (bool) option.value;
						break;
					case TableColumnOption.Type.Title:
						this.title = (string) option.value;
						break;
					case TableColumnOption.Type.IgnoreAttributes:
						this.ignoreAttributes = (bool)option.value;
						break;
					case TableColumnOption.Type.CellType:
						this.cellType = (System.Type)option.value;
						break;
				}
			}
		}

	}

}
