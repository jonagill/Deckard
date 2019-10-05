using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EditorGUITable
{

	public class TableColumnOption
	{

		public enum Type
		{
			ExpandWidth,
			Width,
			MinWidth,
			MaxWidth,
			Resizeable,
			Sortable,
			EnabledCells,
			EnabledTitle,
			Optional,
			VisibleByDefault,
			Relative,
			Title,
			IgnoreAttributes,
			CellType
		}

		public Type type;
		public object value;

		public TableColumnOption(Type type, object value)
		{
			this.type = type;
			this.value = value;
		}

		public static readonly Dictionary<Type, System.Type> VALUE_TYPES = new Dictionary<Type, System.Type>()
		{
			{ Type.ExpandWidth,         typeof(bool) },
			{ Type.Width,               typeof(float) },
			{ Type.MinWidth,            typeof(float) },
			{ Type.MaxWidth,            typeof(float) },
			{ Type.Resizeable,          typeof(bool) },
			{ Type.Sortable,            typeof(bool) },
			{ Type.EnabledCells,        typeof(bool) },
			{ Type.EnabledTitle,        typeof(bool) },
			{ Type.Optional,            typeof(bool) },
			{ Type.VisibleByDefault,    typeof(bool) },
			{ Type.Relative,            typeof(bool) },
			{ Type.Title,               typeof(string) },
			{ Type.IgnoreAttributes,    typeof(bool) },
			{ Type.CellType,			typeof(System.Type) },
		};
	}

}
