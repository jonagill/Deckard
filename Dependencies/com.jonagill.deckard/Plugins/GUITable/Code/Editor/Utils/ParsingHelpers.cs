using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace EditorGUITable
{

	public static class ParsingHelpers
	{

		static readonly string COLUMN_OPTIONS = string.Join("|", System.Enum.GetNames(typeof(TableColumnOption.Type)));
		static readonly string TABLE_OPTIONS = string.Join("|", System.Enum.GetNames(typeof(GUITableOption.Type)));

		public static List<SelectorColumn> ParseColumns(List<string> columnsStrings)
		{
			return columnsStrings.Select((cs, i) => ParseColumn(cs)).ToList();
		}

		public static SelectorColumn ParseColumn(string columnString)
		{
			Match match = Regex.Match(columnString, string.Format("^([a-zA-Z0-9_]*)\\s*(:\\s*(((\\s*({0})\\s*\\(\\s*[a-zA-Z0-9\\\"\\.]*\\s*\\))\\s*\\,*)*))*$", COLUMN_OPTIONS));
			if (!match.Success)
			{
				throw new TableAttributeParsingException(string.Format("There was an error parsing column: {0}", columnString));
			}
			TableColumnOption[] columnOptions = ParseColumnOptions(match.Groups[3].Value);
			return new SelectFromPropertyNameColumn(match.Groups[1].Value, columnOptions);
		}

		public static TableColumnOption[] ParseColumnOptions(string optionsString)
		{
			string[] optionsStrings = optionsString.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries);
			return optionsStrings.Select(os => ParseColumnOption(os)).ToArray();

		}
		public static TableColumnOption ParseColumnOption(string optionString)
		{
			Regex regex = new Regex(string.Format("^\\s*({0})\\s*\\(\\s*([a-zA-Z0-9\\\"\\.]*)\\s*\\)\\s*$", COLUMN_OPTIONS));
			Match match = regex.Match(optionString);
			if (!match.Success)
			{
				throw new TableAttributeParsingException(string.Format("There was an error parsing column option: {0}", optionString));
			}
			TableColumnOption.Type option = (TableColumnOption.Type)System.Enum.Parse(typeof(TableColumnOption.Type), match.Groups[1].Value);
			System.Type optionValueType = TableColumnOption.VALUE_TYPES[option];
			string stringValue = match.Groups[2].Value.Trim();
			object optionValue = null;
			try
			{
				if (option == TableColumnOption.Type.CellType)
				{
					try { optionValue = System.Type.GetType(string.Format("{0}.{1}", typeof(GUITable).Namespace, stringValue), true); }
					catch { optionValue = System.Type.GetType(stringValue, true); }
				}
				else if (optionValueType == typeof(string))
					optionValue = (stringValue.StartsWith("\"") && stringValue.EndsWith("\"")) ? stringValue.Substring(1, stringValue.Length - 2) : stringValue;
				else if (optionValueType == typeof(bool))
					optionValue = bool.Parse(stringValue);
				else if (optionValueType == typeof(float))
					optionValue = float.Parse(
						stringValue.EndsWith("f") ? stringValue.Substring(0, stringValue.Length - 1) : stringValue,
						System.Globalization.NumberStyles.Any,
						System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (System.Exception e)
			{
				throw new TableAttributeParsingException(string.Format(
					"The option parameter '{0}' in the column option '{1}' cannot be parsed to a {2}\n{3}",
					stringValue,
					optionString,
					optionValueType,
					e.Message));
			}
			return new TableColumnOption(option, optionValue);
		}

		public static GUITableOption[] ParseTableOptions(string[] optionsStrings)
		{
			if (optionsStrings == null)
				return new GUITableOption[] { };
			return optionsStrings.Select(os => ParseTableOption(os)).ToArray();
		}

		public static GUITableOption ParseTableOption(string optionString)
		{
			Match match = Regex.Match(optionString, string.Format("^\\s*({0})\\s*\\(\\s*([a-zA-Z0-9\\.]*)\\s*\\)\\s*$", TABLE_OPTIONS));
			if (!match.Success)
			{
				throw new TableAttributeParsingException(string.Format("There was an error parsing table option: {0}", optionString));
			}
			float floatValue = float.NaN;
			bool boolValue = false;
			bool isFloat = float.TryParse(
				match.Groups[2].Value,
				System.Globalization.NumberStyles.Any,
				System.Globalization.CultureInfo.InvariantCulture,
				out floatValue);
			bool isBool = bool.TryParse(match.Groups[2].Value, out boolValue);
			if (!isBool && !isFloat)
			{
				throw new TableAttributeParsingException(string.Format(
					"The option parameter '{0}' in the table option '{1}' cannot be parsed to a bool or a float",
					match.Groups[2].Value,
					optionString));
			}
			return new GUITableOption((GUITableOption.Type)System.Enum.Parse(typeof(GUITableOption.Type), match.Groups[1].Value), isFloat ? (object)floatValue : (object)boolValue);
		}
	}


}