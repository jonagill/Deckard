using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvSprite : CsvDataBehaviour
    {
        [SerializeField] private SpriteCollection spriteCollection;
        [Tooltip("If an AspectRatioFitter is on the same component, sets the target aspect ratio to the aspect of the assigned sprite.")]
        [SerializeField] private bool setAspectRatio = true;

        private Sprite prevSprite;
        private bool prevEnabled;

        public override PriorityType Priority => PriorityType.Default;

        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<Image>();
            if (target != null)
            {
                prevSprite = target.sprite;
                prevEnabled = target.enabled;
                
                if (spriteCollection != null)
                {
                    if (sheet.TryGetStringValue(key, index, out var spriteKey))
                    {
                        if (spriteCollection.TryGetSpriteForKey(spriteKey, out var sprite))
                        {
                            target.sprite = sprite;
                            target.enabled = true;

                            if (setAspectRatio)
                            {
                                ApplyAspectRatio(target);
                            }
                        }
                        else
                        {
                            target.enabled = false;
                        }
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
                target.enabled = prevEnabled;
            }
        }

        private void ApplyAspectRatio(Image target)
        {
            if (target.sprite != null && TryGetComponent<AspectRatioFitter>(out var aspectRatioFitter))
            {
                var textureRect = target.sprite.textureRect;
                if (textureRect.height > 0f)
                {
                    aspectRatioFitter.aspectRatio = textureRect.width / textureRect.height;
                }
            }
        }

        private void OnValidate()
        {
            if (setAspectRatio && TryGetComponent<Image>(out var image))
            {
                ApplyAspectRatio(image);
            }
        }
    }
}
