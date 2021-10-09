using UnityEngine;

namespace Deckard.Data
{
    public class EnableForBleed : EnableToggleBassBehaviour
    {
        [SerializeField] private bool enabledForBleeds;
        
        protected override bool ShouldEnable(ExportParams exportParams)
        {
            return enabledForBleeds == exportParams.IncludeBleeds;
        }
    }
}
