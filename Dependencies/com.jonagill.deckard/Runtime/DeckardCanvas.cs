using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Deckard
{
    [RequireComponent(typeof(Canvas))]
    [ExecuteAlways]
    public class DeckardCanvas : MonoBehaviour
    {
        // TextMesh Pro sizing assumes a DPI of 72, so enforce that here
        public const float EDIT_DPI = 72f;

        private const int RENDER_LAYER = 31;

        private const float CAMERA_NEAR_PLANE = .01f;
        private const float CAMERA_FAR_PLANE = .1f;
        private const float CAMERA_RENDER_DISTANCE = .05f;
        
        private Camera _renderCamera;
        private Camera RenderCamera
        {
            get
            {
                if (_renderCamera == null)
                {
                    _renderCamera = new GameObject("DeckardCamera").AddComponent<Camera>();
                    //_renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    _renderCamera.orthographic = true;
                    _renderCamera.cullingMask = 1 << RENDER_LAYER;
                    _renderCamera.clearFlags = CameraClearFlags.SolidColor;
                    _renderCamera.backgroundColor = Color.black;
                    _renderCamera.allowMSAA = false;
                    _renderCamera.nearClipPlane = CAMERA_NEAR_PLANE;
                    _renderCamera.farClipPlane = CAMERA_FAR_PLANE;
                }

                return _renderCamera;
            }
        }

        
        public RectTransform RectTransform => (RectTransform) transform;

        private Canvas _canvas;

        private Canvas Canvas
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = GetComponent<Canvas>();
                }

                return _canvas;
            }
        }
        
        private CanvasScaler _canvasScaler;

        private CanvasScaler CanvasScaler
        {
            get
            {
                if (_canvasScaler == null)
                {
                    _canvasScaler = GetComponent<CanvasScaler>();
                }

                return _canvasScaler;
            }
        }

        [SerializeField] private Vector2 sizeInches = new Vector2(2.5f, 3.5f);
        [SerializeField] private float renderDpi = 300f;

#if UNITY_EDITOR

        private void OnValidate()
        {
            Canvas.renderMode = RenderMode.WorldSpace;

            var scaler = GetComponent<CanvasScaler>();
            EditorApplication.delayCall += (() => DestroyImmediate(scaler));

            RectTransform.sizeDelta = sizeInches * EDIT_DPI;
            RectTransform.localScale = Vector3.one;
            RectTransform.pivot = Vector2.one * .5f;

            EditorUtility.SetDirty(gameObject);
        }

#endif

        private void Start()
        {
            var dt = new DrivenRectTransformTracker();
            dt.Clear();
            dt.Add(this, RectTransform, DrivenTransformProperties.All);
        }
        
        public Texture2D Render(int superSample = 2)
        {
            var camera = RenderCamera;

            var width = (int) (sizeInches.x * renderDpi);
            var height = (int) (sizeInches.y * renderDpi);
            var renderWidth = width * superSample;
            var renderHeight = height * superSample;
            var renderTexture = GetTemporaryRenderTexture(renderWidth, renderHeight);

            // Cache off existing data
            var prevRenderTexture = RenderTexture.active;
            
            SetLayerRecursively(RENDER_LAYER);

            camera.aspect = renderWidth / (float) renderHeight;
            camera.orthographicSize = RectTransform.rect.height / 2f;
            camera.targetTexture = renderTexture;

            // Point the camera at the canvas
            camera.transform.position = transform.position + (transform.forward * -CAMERA_RENDER_DISTANCE); 
            camera.transform.forward = transform.forward;
            
            // LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
            // Canvas.ForceUpdateCanvases();

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
            camera.targetTexture = null;
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

        private void SetLayerRecursively(int layer)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                t.gameObject.layer = layer;
            }
        }
    }
}
