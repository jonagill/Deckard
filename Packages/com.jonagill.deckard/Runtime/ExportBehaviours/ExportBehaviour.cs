using UnityEngine;

namespace Deckard.Data
{
    public abstract class ExportBehaviour : MonoBehaviour
    {
        public abstract void Process(ExportParams exportParams);
        public abstract void Cleanup();
    }
}
