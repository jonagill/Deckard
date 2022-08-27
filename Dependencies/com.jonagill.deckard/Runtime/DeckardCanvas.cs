using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Deckard
{
    [RequireComponent(typeof(Canvas))]
    [ExecuteAlways]
    public class DeckardCanvas : MonoBehaviour
    {
        // TextMesh Pro sizing assumes a DPI of 72, so enforce that here
        private const float UNITS_PER_INCH = 72f;

        private const int RENDER_LAYER = 31;

        private const float CAMERA_NEAR_PLANE = .01f;
        private const float CAMERA_FAR_PLANE = .1f;
        private const float CAMERA_RENDER_DISTANCE = .05f;

        public static int InchesToUnits(float inches)
        {
            return (int) (inches * UNITS_PER_INCH);
        }

        public static Vector2 InchesToUnits(Vector2 inches)
        {
            return new Vector2(InchesToUnits(inches.x), InchesToUnits(inches.y)); 
        }
        
        private Camera _renderCamera;
        private Camera RenderCamera
        {
            get
            {
                if (_renderCamera == null)
                {
                    _renderCamera = new GameObject("DeckardCamera").AddComponent<Camera>();
                    _renderCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    _renderCamera.orthographic = true;
                    _renderCamera.cullingMask = 1 << RENDER_LAYER;
                    _renderCamera.clearFlags = CameraClearFlags.SolidColor;
                    _renderCamera.backgroundColor = Color.clear;
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

        [FormerlySerializedAs("sizeInches")]
        [FormerlySerializedAs("contentSizeInches")]
        [SerializeField] private Vector2 contentSizeInches;
        /// <summary>
        /// The desired size of the card, not including bleed
        /// </summary>
        public Vector2 ContentSizeInches
        {
            get => contentSizeInches;
            set
            {
                if (contentSizeInches != value)
                {
                    contentSizeInches = value;
                    EnforceCorrectConfiguration();
                }
            }
        }
        
        [SerializeField] private Vector2 bleedInches;
        /// <summary>
        /// The amount of bleed that should be rendered when preparing files for printing.
        /// </summary>
        public Vector2 BleedInches
        {
            get => bleedInches;
            set
            {
                if (bleedInches != value)
                {
                    bleedInches = value;
                    EnforceCorrectConfiguration();
                }
            }
        }

        /// <summary>
        /// The total size of the canvas rendered when preparing files for printing. 
        /// </summary>
        public Vector2 TotalPrintSizeInches => contentSizeInches + (bleedInches * 2);

        /// <summary>
        /// The size of the safe zone on the card.
        /// Critical information should generally not be drawn outside of this
        /// </summary>
        [SerializeField] private Vector2 safeZoneInches;
        public Vector2 SafeZoneInches
        {
            get => safeZoneInches;
            set
            {
                if (safeZoneInches != value)
                {
                    safeZoneInches = value;
                    EnforceCorrectConfiguration();
                }
            }
        }

        private void Start()
        {
            var dt = new DrivenRectTransformTracker();
            dt.Clear();
            dt.Add(this, RectTransform, DrivenTransformProperties.All);
        }
        
        public Texture2D Render(int renderDpi, bool includeBleed, int superSample = 2)
        {
            var camera = RenderCamera;

            var renderSizeInches = includeBleed ? TotalPrintSizeInches : contentSizeInches;
            var width = (int) (renderSizeInches.x * renderDpi);
            var height = (int) (renderSizeInches.y * renderDpi);
            var renderWidth = width * superSample;
            var renderHeight = height * superSample;
            var renderTexture = GetTemporaryRenderTexture(renderWidth, renderHeight);

            // Cache off existing data
            var prevRenderTexture = RenderTexture.active;
            
            SetLayerRecursively(RENDER_LAYER);

            camera.aspect = renderWidth / (float) renderHeight;
            var heightUnits = renderSizeInches.y * UNITS_PER_INCH;
            camera.orthographicSize = heightUnits / 2f;
            camera.targetTexture = renderTexture;

            // Point the camera at the canvas
            camera.transform.position = transform.position + (transform.forward * -CAMERA_RENDER_DISTANCE); 
            camera.transform.forward = transform.forward;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
            Canvas.ForceUpdateCanvases();

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
            return RenderTexture.GetTemporary(width, height, 24, compatibleFormat, 1);
        }

        private void SetLayerRecursively(int layer)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                t.gameObject.layer = layer;
            }
        }

        private bool EnforceCorrectConfiguration()
        {
            var isDirty = false;
            
            isDirty |= ForceVector2Range(ref contentSizeInches, Vector2.zero, Vector2.positiveInfinity);
            var halfSize = contentSizeInches * .5f;
            isDirty |= ForceVector2Range(ref bleedInches, Vector2.zero, halfSize);
            isDirty |= ForceVector2Range(ref safeZoneInches, Vector2.zero, halfSize - bleedInches);
            
            if (Canvas.renderMode != RenderMode.WorldSpace)
            {
                Canvas.renderMode = RenderMode.WorldSpace;
                isDirty = true;
            }
                
            var scaler = GetComponent<CanvasScaler>();
            if (scaler)
            {
                DestroyImmediate(scaler);
                isDirty = true;
            }

            var sizeDelta = TotalPrintSizeInches * UNITS_PER_INCH;
            if (RectTransform.sizeDelta != sizeDelta)
            {
                RectTransform.sizeDelta = sizeDelta;
                isDirty = true;
            }

            var localScale = Vector3.one;
            if (RectTransform.localScale != localScale)
            {
                RectTransform.localScale = localScale;
                isDirty = true;
            }

            var pivot = Vector2.one * .5f;
            if (RectTransform.pivot != pivot)
            {
                RectTransform.pivot = pivot;
                isDirty = true;
            }

            return isDirty;
        }

        private bool ForceVector2Range(ref Vector2 v, Vector2 min, Vector2 max)
        {
            Vector2 v2;
            v2.x = Mathf.Clamp(v.x, min.x, max.x);
            v2.y = Mathf.Clamp(v.y, min.y, max.y);

            if (v2 != v)
            {
                v = v2;
                return true;
            }

            return false;
        }
        
        
#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (bleedInches != Vector2.zero)
            {
                DrawOutlineGizmo(InchesToUnits(bleedInches), Color.red);
            }

            if (safeZoneInches != Vector2.zero)
            {
                DrawOutlineGizmo(InchesToUnits(bleedInches + safeZoneInches), Color.cyan);
            }
        }
        
        private void DrawOutlineGizmo(Vector2 cornerOffsetUnits, Color color)
        {
            var halfSize = RectTransform.sizeDelta * .5f;
            var offsetSize = halfSize - cornerOffsetUnits;
            
            var corners = new []
            {
                new Vector3(-offsetSize.x, +offsetSize.y, 0),
                new Vector3(+offsetSize.x, +offsetSize.y, 0),
                new Vector3(+offsetSize.x, -offsetSize.y, 0),
                new Vector3(-offsetSize.x, -offsetSize.y, 0),
            };

            Gizmos.matrix = RectTransform.localToWorldMatrix;
            Gizmos.color = color;

            for (var i = 0; i < corners.Length; i++)
            {
                var j = (i + 1) % corners.Length;
                Gizmos.DrawLine(corners[i], corners[j]);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void Reset()
        {
            // Default to poker card size with standard 1/8" bleed and safe zones
            contentSizeInches = new Vector2(2.5f, 3.5f);
            bleedInches = new Vector2(.125f, .125f);
            safeZoneInches = new Vector2(.125f, .125f);
        }

        private void OnValidate()
        {
            EditorApplication.delayCall += (() =>
            {
                if (this == null)
                {
                    return;
                }

                var isDirty = EnforceCorrectConfiguration();

                if (isDirty)
                {
                    EditorUtility.SetDirty(gameObject);
                }
            });
        }
#endif
    }
}
