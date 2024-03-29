﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Deckard.Data
{
    [System.Serializable]
    public class CsvSheet
    {
        [System.Serializable]
        public class Record
        {
            [SerializeField]
            private List<string> fields;
            public IReadOnlyList<string> Fields => fields;

            public bool TryGetField(int columnIndex, out string field)
            {
                field = null;
                
                if (columnIndex < 0 || columnIndex >= fields.Count)
                {
                    return false;
                }

                field = fields[columnIndex];
                return true;
            }

            public Record(IList<string> fields)
            {
                this.fields = new List<string>(fields.Count);
                this.fields.AddRange(fields);
            }
        }
        
        [SerializeField]
        private List<string> headers = new List<string>();
        public IReadOnlyList<string> Headers => headers;
        
        [SerializeField]
        private List<Record> records = new List<Record>();
        public IReadOnlyList<Record> Records => records;
        
        public static CsvSheet Parse(string input, char rowSeparator = '\n', char fieldSeparator = ',')
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }
            
            var sheet = new CsvSheet();
            
            // Split on any separators that are not enclosed in quotes
            Regex splitterRegex = new Regex($"{fieldSeparator}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            
            var rows = input.Split(rowSeparator);
            for (var i = 0; i < rows.Length; i++)
            {
                var fields = splitterRegex.Split(rows[i]).ToList();
                for (var j = 0; j < fields.Count; j++)
                {
                    var field = fields[j];
                    
                    // Remove any trailing whitespace
                    field = field.TrimEnd();
                    
                    // Remove quotes that might be wrapping the field
                    field  = field.Trim('\'', '\"', '\t');
                    
                    fields[j] = field;
                }
                
                if (i == 0)
                {
                    // Add all headers so we can refer to them later
                    sheet.headers.AddRange(fields);
                }
                else
                {
                    // Parse out any fields without a matching header
                    var fieldsWithHeaders = new List<string>();
                    for (var j = 0; j < fields.Count && j < sheet.headers.Count; j++)
                    {
                        if (!string.IsNullOrWhiteSpace(sheet.headers[j]))
                        {
                            fieldsWithHeaders.Add(fields[j]);
                        }
                    }
                    
                    if (fieldsWithHeaders.Count == 0 || fieldsWithHeaders.All(IsEmptyField))
                    {
                        // Don't add empty rows
                        continue;
                    }
                    
                    sheet.records.Add(new Record(fieldsWithHeaders));
                }
            }

            // Remove all empty headers
            sheet.headers.RemoveAll(string.IsNullOrWhiteSpace);

            return sheet;
        }
        
        private static bool IsEmptyField(string field)
        {
            // Ignore empty and boolean false fields (the default for validated toggle fields)
            return string.IsNullOrEmpty(field) || field.Equals("FALSE");
        }

        
        public int RecordCount => records.Count;

        public bool TryGetStringValue(string key, int recordIndex, out string value, bool logIfMissing = false)
        {
            value = null;
            if (recordIndex < 0 || recordIndex > records.Count)
            {
                throw new IndexOutOfRangeException("Invalid record index!");
            }
            
            // Remove any preceding or acceding whitespace or case-sensitivity
            key = key.Trim().ToLowerInvariant();

            var record = records[recordIndex];
            var fieldIndex = headers.FindIndex(h => h.Trim().ToLowerInvariant() == key);
            if (fieldIndex < 0)
            {
                if (logIfMissing)
                {
                    Debug.LogWarning($"Failed to find record \"{key}\"");
                }
                return false;
            }

            if (fieldIndex >= record.Fields.Count)
            {
                return false;
            }
            
            value = record.Fields[fieldIndex];

            return true;
        }
        
        public bool TryGetIntValue(string key, int recordIndex, out int value)
        {
            value = 0;
            if (TryGetStringValue(key, recordIndex, out var stringValue))
            {
                if (int.TryParse(stringValue, out value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetFloatValue(string key, int recordIndex, out float value)
        {
            value = 0;
            if (TryGetStringValue(key, recordIndex, out var stringValue))
            {
                if (float.TryParse(stringValue, out value))
                {
                    return true;
                }
            }

            return false;
        }
        
        public bool TryGetColorValue(string key, int recordIndex, out Color value)
        {
            value = Color.magenta;
            if (TryGetStringValue(key, recordIndex, out var stringValue))
            {
                if (ColorUtility.TryParseHtmlString(stringValue, out value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
