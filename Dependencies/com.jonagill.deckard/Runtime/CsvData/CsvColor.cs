using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvColor : CsvDataBehaviour
    {
        public override void Process(CsvSheet sheet, int index)
        {
            var target = GetComponent<Graphic>();
            if (target != null)
            {
                if (sheet.TryGetStringValue(key, index, out var value) && 
                    ColorUtility.TryParseHtmlString(value, out var color))
                {
                    target.color = color;
                }
                else
                {
                    target.color = Color.black;
                }
            }   
        }
    }
}
