using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvColor : CsvDataBehaviour
    {
        private Color prevColor;
        
        public override PriorityType Priority => PriorityType.Default;
        
        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<Graphic>();
            if (target != null)
            {
                sheet.TryGetColorValue(key, index, out var value);
                prevColor = target.color;
                target.color = value;
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
