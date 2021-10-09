using UnityEngine;

namespace Deckard.Data
{
    public abstract class EnableToggleBassBehaviour : ExportBehaviour
    {
        [SerializeField] private Behaviour[] targetComponents;
        private bool[] prevEnabled;
        
        public override void Process(ExportParams exportParams)
        {
            var shouldEnable = ShouldEnable(exportParams);
            if (targetComponents != null)
            {
                prevEnabled = new bool[targetComponents.Length];
                for (var i = 0; i < targetComponents.Length; i++)
                {
                    if (targetComponents[i] != null)
                    {
                        prevEnabled[i] = targetComponents[i].enabled;
                        targetComponents[i].enabled = shouldEnable;
                    }
                }
            }
        }

        public override void Cleanup()
        {
            if (targetComponents != null)
            {
                for (var i = 0; i < targetComponents.Length; i++)
                {
                    if (targetComponents[i] != null)
                    {
                        targetComponents[i].enabled = prevEnabled[i];
                    }
                }
            }          
        }

        protected abstract bool ShouldEnable(ExportParams exportParams);
    }
}
