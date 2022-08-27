using UnityEngine;

namespace Deckard.Data
{
    public class EnableForBleed : EnableToggleBaseBehaviour
    {
        [SerializeField] private bool enabledForBleeds;
        
        protected override bool ShouldEnable(ExportParams exportParams)
        {
            return enabledForBleeds == exportParams.IncludeBleeds;
        }
    }
}
