using UnityEngine;

namespace Deckard.Data
{
    public class EnableForExportType : EnableToggleBassBehaviour
    {
        [SerializeField] private ExportType exportType;
        
        protected override bool ShouldEnable(ExportParams exportParams)
        {
            return exportType == exportParams.ExportType;
        }
    }
}
