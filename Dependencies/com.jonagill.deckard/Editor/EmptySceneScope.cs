using System;
using UnityEditor.SceneManagement;

namespace Deckard.Editor
{
    /// <summary>
    /// Reusable scope that loads into an empty scene to prepare for image rendering and exporting
    /// </summary>
    public class EmptySceneScope : IDisposable
    {
        private string prevScenePath;
        
        public EmptySceneScope()
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        
            // Cache out the current scene path because the scene gets torn down when we load a new scene
            var prevScene = EditorSceneManager.GetSceneAt(0);
            prevScenePath = prevScene.path;

            // Load into an empty scene to prevent any possible rendering conflicts
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        
        public void Dispose()
        {
            if (prevScenePath == null)
            {
                return;
            }
            
            EditorSceneManager.OpenScene(prevScenePath);
            prevScenePath = null;
        }
    }
}
