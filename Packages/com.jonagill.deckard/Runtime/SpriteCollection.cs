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
            sprite = SpriteEntries.FirstOrDefault(e => e.Key == key)?.Sprite;
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
    }
}
