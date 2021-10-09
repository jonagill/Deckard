using UnityEngine;

namespace Deckard.Data
{
    public enum ExportType
    {
        Cards,
        Sheet
    }
    
    public struct ExportParams
    {
        public ExportType ExportType;
        public bool IncludeBleeds;
    }
}
