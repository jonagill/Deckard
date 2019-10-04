using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Deckard
{
    [InitializeOnLoad]
    public static class PngExporter
    {
        static PngExporter()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (_renderCamera)
                {
                    Object.DestroyImmediate(_renderCamera.gameObject);
                }
            };
        }
        
        private static Camera _renderCamera;
        private static Camera RenderCamera
        {
            get
            {
                if (_renderCamera == null)
                {
                    _renderCamera = new GameObject("PngCamera").AddComponent<Camera>();
                    // _renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    _renderCamera.orthographic = true;
                    _renderCamera.clearFlags = CameraClearFlags.SolidColor;
                    _renderCamera.backgroundColor = Color.black;
                }

                return _renderCamera;
            }
        }
        
        public static Texture2D RenderCanvas(Canvas canvas)
        {
            var rectTransform = canvas.GetComponent<RectTransform>();

            // Unparent and straighten the transform to make some calculations easier
            var prevRenderMode = canvas.renderMode;
            var prevParent = rectTransform.parent;
            var prevRotation = rectTransform.localRotation;
            rectTransform.SetParent(null, false);
            rectTransform.localRotation = Quaternion.identity;

            var camera = RenderCamera;
            
            var rect = rectTransform.rect;
            camera.aspect = rect.width / rect.height;
            camera.orthographicSize = rect.height * .5f;
            
            camera.transform.position = rectTransform.position + camera.transform.forward;
            camera.transform.forward = -rectTransform.forward;

            canvas.renderMode = prevRenderMode;
            rectTransform.SetParent(prevParent, false);
            rectTransform.localRotation = prevRotation;

            return null;
        }

        [MenuItem("Deckard/Test/Focus camera on current canvas", false)]
        public static void TestFocusCameraOnCurrentCanvas()
        {
            var canvas = Selection.activeGameObject?.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RenderCanvas(canvas);
            }
        }
        
        [MenuItem("Deckard/Test/Focus camera on current canvas", true)]
        public static bool ValidateTestFocusCameraOnCurrentCanvas()
        {
            var selection = Selection.activeGameObject;
            return selection != null && 
                   !EditorUtility.IsPersistent(selection) &&
                   selection.GetComponentInParent<RectTransform>();
        }
    }
}
