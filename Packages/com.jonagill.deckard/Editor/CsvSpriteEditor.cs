using Deckard.Data;
using UnityEditor;

namespace Deckard.Editor
{
    [CustomEditor(typeof(CsvSprite))]
    public class CsvSpriteEditor : CsvDataBehaviourEditor
    {
        private CsvSprite Target => (CsvSprite) target;
    }
}
