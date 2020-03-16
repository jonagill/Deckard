using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvSprite : CsvDataBehaviour
    {
        [SerializeField] private SpriteCollection spriteCollection;

        private Sprite prevSprite;
        
        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<Image>();
            if (target != null)
            {
                prevSprite = target.sprite;
                
                if (spriteCollection != null)
                {
                    if (sheet.TryGetStringValue(key, index, out var spriteKey) &&
                        spriteCollection.TryGetSpriteForKey(spriteKey, out var sprite))
                    {
                        target.sprite = sprite;
                    }
                }
                else
                {
                    Debug.LogWarning($"CsvSprite {gameObject.name} has no assigned sprite collection.");
                }
            }   
        }

        public override void Cleanup()
        {
            var target = GetComponent<Image>();
            if (target != null)
            {
                target.sprite = prevSprite;
            }
        }
    }
}
