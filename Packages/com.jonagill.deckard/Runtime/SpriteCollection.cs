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
    [CreateAssetMenu(fileName = "New Sprite Collection", menuName = "Deckard/New Sprite Collection")]
    public class SpriteCollection : ScriptableObject
    {
        [Serializable]
        public class SpriteEntry
        {
            [Delayed]
            public string Key;
            public Sprite Sprite;

            public override string ToString()
            {
                var keyName = string.IsNullOrEmpty(Key) ? "<EMPTY>" : Key;
                var spriteName = Sprite == null ? "<NULL>" : Sprite.name;
                return $"{keyName} ({spriteName})";
            }
        }
        
        [Serializable]
        public class EntryList : ReorderableArray<SpriteEntry> {}

        [SerializeField, Reorderable(paginate = true, pageSize = 25)]
        private EntryList spriteEntries;

        public IReadOnlyList<SpriteEntry> SpriteEntries => spriteEntries.BackingList;

        public bool TryGetSpriteForKey(string key, out Sprite sprite)
        {
            if (!TryGetValidKey(key, out var validKey))
            {
                sprite = null;
                return false;
            }
            
            sprite = SpriteEntries.FirstOrDefault(e => e.Key == validKey)?.Sprite;
            return sprite != null;
        }

        public void AddSpriteEntry(string key, Sprite sprite)
        {
            spriteEntries.Add(new SpriteEntry()
            {
                Key = key,
                Sprite = sprite
            });
        }

        public bool TryGetValidKey(string key, out string validKey)
        {
            var entry = SpriteEntries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.InvariantCulture));
            if (entry != null)
            {
                // The provided key is valid
                validKey = key;
                return true;
            }

            // There is no sprite matching our key exactly -- try a case insensitive check
            entry = SpriteEntries.FirstOrDefault(e =>
                    string.Equals(e.Key, key, StringComparison.InvariantCultureIgnoreCase));
            if (entry != null)
            {
                // We found a valid key via case insensitive check. Use that instead
                validKey = entry.Key;
                return true;
            }

            validKey = null;
            return false;
        }
    }
}
