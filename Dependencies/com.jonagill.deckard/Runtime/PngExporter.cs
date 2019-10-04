﻿using System.Collections;
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
        
        public static Texture2D RenderCanvas(Canvas canvas, int width, int height)
        {
            var camera = RenderCamera;

            var renderTexture = RenderTexture.GetTemporary(width, height);

            // Cache off existing data
            var prevRenderMode = canvas.renderMode;
            var prevWorldCamera = canvas.worldCamera;
            var prevTargetTexture = camera.targetTexture;
            var prevRenderTexture = RenderTexture.active;

            // Configure the canvas and camera
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;

            var aspect = width / (float) height;
            camera.aspect = aspect;
            camera.
            camera.targetTexture = renderTexture;
            
            // Perform the render
            RenderTexture.active = renderTexture;
            camera.Render();
            
            // Copy the data out into the new Texture2D
            var texture2d = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture2d.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture2d.Apply();
            
            // Restore previous data
            canvas.renderMode = prevRenderMode;
            canvas.worldCamera = prevWorldCamera;
            camera.ResetAspect();
            camera.targetTexture = prevTargetTexture;
            RenderTexture.active = prevRenderTexture;

            // Release the render texture
            RenderTexture.ReleaseTemporary(renderTexture);
            
            return texture2d;
        }
    }
}
