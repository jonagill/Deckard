using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Deckard.Data
{
    public class CsvCount : CsvDataBehaviour
    {
        private List<GameObject> spawnedInstances = new List<GameObject>();
        
        public override void Process(CsvSheet sheet, int index)
        {
            if (sheet.TryGetIntValue(key, index, out var value) && 
                value > 0) 
            {
                // Spawn sibling copies of the given element
                for (var i = 1; i < value; i++)
                {
                    var clone = Instantiate(gameObject, transform.parent);
                    clone.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
                    spawnedInstances.Add(clone);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public override void Cleanup()
        {
            foreach (var instance in spawnedInstances)
            {
                DestroyImmediate(instance.gameObject);
            }
            spawnedInstances.Clear();
            
            gameObject.SetActive(true);
        }
    }
}
