using UnityEngine;

namespace Deckard.Data
{
    public abstract class CsvDataBehaviour : MonoBehaviour
    {
        public enum PriorityType
        {
            PreProcessing = 0,
            Default = 5,
            PostProcessing = 10,
        }
        
        [SerializeField]
        protected string key;
        
        public abstract PriorityType Priority { get; }

        public abstract void Process(CsvSheet sheet, int index);

        public abstract void Cleanup();
    }
}
