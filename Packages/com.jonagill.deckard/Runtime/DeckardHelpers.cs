using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Deckard
{
    internal static class DeckardHelpers
    {
        public static bool IsPartOfReadOnlyPackage(Object asset)
        {
#if UNITY_EDITOR
            // Referencing https://forum.unity.com/threads/check-if-asset-inside-package-is-readonly.900902/
            var path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path))
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo != null)
                {
                    if (packageInfo.source != PackageSource.Embedded && 
                        packageInfo.source != PackageSource.Local)
                    {
                        return true;
                    }                
                }
            }
#endif

            return false;
        }
    }

}
