using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deckard.Data;
using Malee;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard
{
    [CreateAssetMenu(fileName = "New Color Collection", menuName = "Deckard/New Color Collection")]
    public class ColorCollection : ScriptableObject
    {
        [Serializable]
        public class ColorEntry
        {
            [Delayed]
            public string Key;
            public Color Color = UnityEngine.Color.white;

            public override string ToString()
            {
                var keyName = string.IsNullOrEmpty(Key) ? "<EMPTY>" : Key;
                return $"{keyName} ({Color.ToString()})";
            }
        }
        
        [Serializable]
        public class EntryList : ReorderableArray<ColorEntry> {}

        [SerializeField, Reorderable(paginate = true, pageSize = 25)]
        private EntryList colorEntries;

        public IReadOnlyList<ColorEntry> ColorEntries => colorEntries.BackingList;

        public bool TryGetColorForKey(string key, out Color color)
        {
            var entry = ColorEntries.FirstOrDefault(e => e.Key.ToLowerInvariant() == key.ToLowerInvariant());
            color = entry != null ? entry.Color : Color.magenta;
            return entry != null;
        }
    }
}
