﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EditorGUITable
{

	public static class GUIHelpers
	{

		private static GUIStyle _staticRectStyle;

		public static void DrawRect(Rect position, Color color)
		{

			if (_staticRectStyle == null)
			{
				_staticRectStyle = new GUIStyle();
				_staticRectStyle.normal.background = Texture2D.whiteTexture;
			}
			Color prevColor = GUI.color;
			GUI.color = color;
			GUI.Box(position, GUIContent.none, _staticRectStyle);
			GUI.color = prevColor;

		}

		public static int GetViewWidth()
		{
			return (int)(Screen.width / EditorGUIUtility.pixelsPerPoint) - 40;
		}

	}

}
