using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace EditorGUITable
{
	
	[CustomPropertyDrawer(typeof(ReorderableTableAttribute))]
	public class ReorderableTableDrawer : TableDrawer 
	{

		protected override GUITableOption[] forcedTableOptions
		{
			get 
			{
				return new GUITableOption[] {GUITableOption.AllowScrollView(false), GUITableOption.Reorderable(true)};
			}
		}

	}

}
