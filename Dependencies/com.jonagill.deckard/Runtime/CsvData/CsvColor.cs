using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvColor : CsvDataBehaviour
    {
        [SerializeField] private ColorCollection collection;
        
        private Color prevColor;
        
        public override PriorityType Priority => PriorityType.Default;
        
        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<Graphic>();
            if (target != null)
            {
                prevColor = target.color;
                
                if (sheet.TryGetColorValue(key, index, out var rawColorValue))
                {
                    target.color = rawColorValue;
                }
                else if (collection != null && 
                         sheet.TryGetStringValue(key, index, out var stringValue) && 
                         collection.TryGetColorForKey(stringValue, out var entryColor))
                {
                    target.color = entryColor;
                }
            }   
        }

        public override void Cleanup()
        {
            var target = GetComponent<Graphic>();
            if (target != null)
            {
                target.color = prevColor;
            }
        }
    }
}
