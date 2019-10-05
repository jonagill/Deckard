using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

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
                    _renderCamera.allowMSAA = false;
                }

                return _renderCamera;
            }
        }
        
        public static Texture2D RenderCanvas(Canvas canvas, int width, int height, int superSample = 2)
        {
            var camera = RenderCamera;
            var rectTransform = canvas.GetComponent<RectTransform>();
            var sizeDelta = rectTransform.sizeDelta;

            var renderWidth = width * superSample;
            var renderHeight = height * superSample;
            var renderTexture = GetTemporaryRenderTexture(renderWidth, renderHeight);

            // Cache off existing data
            var prevRenderMode = canvas.renderMode;
            var prevWorldCamera = canvas.worldCamera;
            var prevSizeDelta = sizeDelta;
            var prevTargetTexture = camera.targetTexture;
            var prevRenderTexture = RenderTexture.active;

            // Configure the canvas and camera
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;

            // Scale the canvas's width to match the desired aspect ratio for the render
            var aspect = width / (float) height;
            var widthRatio = aspect / camera.aspect;
            sizeDelta.x *= widthRatio;
            rectTransform.sizeDelta = sizeDelta;
            
            camera.targetTexture = renderTexture;
            
            // Perform the render
            RenderTexture.active = renderTexture;
            camera.Render();
            
            // Downsample to the final resolution
            var blitTexture = GetTemporaryRenderTexture(width, height);
            Graphics.Blit(renderTexture, blitTexture);
            
            // Copy the data out into the new Texture2D
            var texture2d = new Texture2D(width, height, TextureFormat.ARGB32, false);
            RenderTexture.active = blitTexture;
            texture2d.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture2d.Apply();
            
            // Restore previous data
            canvas.renderMode = prevRenderMode;
            canvas.worldCamera = prevWorldCamera;
            rectTransform.sizeDelta = prevSizeDelta;
            camera.targetTexture = prevTargetTexture;
            RenderTexture.active = prevRenderTexture;

            // Release the render texture
            RenderTexture.ReleaseTemporary(renderTexture);
            RenderTexture.ReleaseTemporary(blitTexture);
            
            return texture2d;
        }

        public static void SaveTextureAsPng(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }
        
        private static RenderTexture GetTemporaryRenderTexture(int width, int height)
        {
            GraphicsFormat graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            GraphicsFormat compatibleFormat = SystemInfo.GetCompatibleFormat(graphicsFormat, FormatUsage.Render);
            return RenderTexture.GetTemporary(width, height, 0, compatibleFormat, 1);
        }

    }
}
