using TMPro;
using UnityEngine;

namespace Deckard.Data
{
    public class CsvText : CsvDataBehaviour
    {
        private string prevText;
        
        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<TextMeshProUGUI>();
            if (target != null)
            {
                prevText = target.text;
                
                if (sheet.TryGetStringValue(key, index, out var value))
                {
                    target.text = value;
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
    }
}
