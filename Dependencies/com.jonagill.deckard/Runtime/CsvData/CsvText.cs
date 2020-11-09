using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Deckard.Data
{
    public class CsvText : CsvDataBehaviour
    {
        private static readonly Vector2 SPRITE_OFFSET = new Vector2(0f, .9f); 
        private static readonly FieldInfo MaterialReferenceManager_m_SpriteAssetReferenceLookup = 
            typeof(MaterialReferenceManager)
                .GetField("m_SpriteAssetReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic);
        
        private static readonly FieldInfo MaterialReferenceManager_m_FontMaterialReferenceLookup = 
            typeof(MaterialReferenceManager)
                .GetField("m_FontMaterialReferenceLookup", BindingFlags.Instance | BindingFlags.NonPublic);
        
        [SerializeField] private SpriteCollection spriteCollection;
        
        private string prevText;
        
        public override PriorityType Priority => PriorityType.Default;

        public override void Process(CsvSheet sheet, int index)
        {
            EnsureTMProSpriteCollection(spriteCollection);
            
            var target = GetComponent<TextMeshProUGUI>();
            if (target != null)
            {
                prevText = target.text;
                
                if (sheet.TryGetStringValue(key, index, out var value))
                {
                    if (spriteCollection != null)
                    {
                        var formattedText = FormatTMProStringTags(spriteCollection, value);
                        target.text = formattedText;                        
                    }
                    else
                    {
                        target.text = value;
                    }
                }
                else
                {
                    target.text = "";
                }
            }   
        }

        public override void Cleanup()
        {
            var target = GetComponent<TextMeshProUGUI>();
            if (target != null)
            {
                target.text = prevText;
            }
        }
        
        #region Sprite Collections

        private static string FormatTMProStringTags(SpriteCollection spriteCollection, string input)
        {
            string MatchEvaluator(Match match)
            {
                var key = match.Groups["Key"].Value;
                var assetName = GetTMProSpriteAssetName(spriteCollection, key);

                // Disable tinting if the key is prefaced with an exclamation point
                var shouldTint = string.IsNullOrEmpty(match.Groups["Exclamation"].Value);
                var tintTag = shouldTint ? " tint" : string.Empty;
                
                var tmProTag = $"<sprite=\"{assetName}\" name=\"{key}\"{tintTag}>";
                return tmProTag;
            }
            
            return Regex.Replace(input, "<<(?<Exclamation>!?)(?<Key>\\w+)>>", MatchEvaluator);
        }
        
        private static string GetTMProSpriteAssetName(SpriteCollection collection, string key)
        {
            return $"{collection.name}_{key}_TMPro";
        }

        private static int GetTMProSpriteAssetHashcode(SpriteCollection collection, string key)
        {
            return TMP_TextUtilities.GetSimpleHashCode(GetTMProSpriteAssetName(collection, key));
        }

        /// <summary>
        /// Since we are not saving our sprite assets to disk, Unity ends up deleting them
        /// between runs, but TMPro maintains dictionary references to them that
        /// prevent us from adding new assets on successive runs.
        /// To work around this, use reflection to collect the relevant dictionaries
        /// and remove any null entries from them before the next run.
        /// </summary>
        private static void ClearNullCachedTMProSpriteCollections()
        {
            if (MaterialReferenceManager.instance == null)
            {
                return;
            }
            
            var spriteAssetReferenceLookup =
                (Dictionary<int, TMP_SpriteAsset>) MaterialReferenceManager_m_SpriteAssetReferenceLookup
                .GetValue(MaterialReferenceManager.instance);
            ClearNullDictionaryEntries(spriteAssetReferenceLookup);
            
            var fontMaterialReferenceLookup =                 
                (Dictionary<int, Material>) MaterialReferenceManager_m_FontMaterialReferenceLookup
                .GetValue(MaterialReferenceManager.instance);
            ClearNullDictionaryEntries(fontMaterialReferenceLookup);
        }
        
        private static void EnsureTMProSpriteCollection(SpriteCollection collection)
        {
            if (collection == null)
            {
                return;
            }
            
            ClearNullCachedTMProSpriteCollections();

            foreach (var spriteEntry in collection.SpriteEntries)
            {
                AddTMProSpriteAsset(spriteEntry.Sprite.texture, collection, spriteEntry.Key);
            }
        }
        
        private static void AddTMProSpriteAsset(Texture2D texture, SpriteCollection collection, string key)
        {
            var hashCode = GetTMProSpriteAssetHashcode(collection, key);
            if (!MaterialReferenceManager.TryGetSpriteAsset(hashCode, out _))
            {
                // Generate a new runtime-only SpriteAsset for TMPro to reference
                // Based on the asset creation code in TMP_SpriteAssetImporter
                var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

                // Add the info for the new sprite asset
                spriteAsset.hashCode = hashCode;
                spriteAsset.spriteSheet = texture;
                spriteAsset.name = GetTMProSpriteAssetName(collection, key);;
                
                // Configure the info for the single sprite to include in the asset
                TMP_Sprite tmpSprite = new TMP_Sprite();

                tmpSprite.id = 0;
                tmpSprite.name = key;
                tmpSprite.hashCode = TMP_TextUtilities.GetSimpleHashCode(tmpSprite.name);
                tmpSprite.unicode = TMP_TextUtilities.StringHexToInt(tmpSprite.name);

                tmpSprite.x = 0;
                tmpSprite.y = 0;
                tmpSprite.width = texture.width;
                tmpSprite.height = texture.height;
                tmpSprite.pivot = new Vector2(.5f, .5f);

                tmpSprite.xAdvance = tmpSprite.width;
                tmpSprite.scale = 1.0f;
                tmpSprite.xOffset = tmpSprite.width * SPRITE_OFFSET.x;
                tmpSprite.yOffset = tmpSprite.height * SPRITE_OFFSET.y;

                var spriteInfoList = new List<TMP_Sprite> {tmpSprite};
                spriteAsset.spriteInfoList = spriteInfoList;

                // Add a default material to the sprite asset
                Shader shader = Shader.Find("TextMeshPro/Sprite");
                Material material = new Material(shader);
                material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);
                spriteAsset.material = material;
                
                // Run the update logic to calculate the rest of the required info
                spriteAsset.UpdateLookupTables();
                
                MaterialReferenceManager.AddSpriteAsset(spriteAsset);
            }
        }

        private static void ClearNullDictionaryEntries<T, U>(Dictionary<T, U> dictionary) where U : UnityEngine.Object
        {
            var nullEntryKeys = new List<T>();
            foreach (var entry in dictionary)
            {
                if (entry.Value == null)
                {
                    nullEntryKeys.Add(entry.Key);
                }
            }

            foreach (var key in nullEntryKeys)
            {
                dictionary.Remove(key);
            }
        }
        
        #endregion
    }
}
