using UnityEngine;

namespace Deckard.Data
{
    public abstract class CsvDataBehaviour : MonoBehaviour
    {
        [SerializeField]
        protected string key;

        public abstract void Process(CsvSheet sheet, int index);
    }
}
