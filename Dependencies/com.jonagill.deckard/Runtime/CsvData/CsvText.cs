using TMPro;
using UnityEngine;

namespace Deckard.Data
{
    public class CsvText : CsvDataBehaviour
    {
        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<TextMeshProUGUI>();
            if (target != null)
            {
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
    }
}
