using System;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace Deckard.Test
{
    public class DeckardTestWindow : EditorWindow
    {
        [MenuItem("Deckard/Test/Test Window")]
        static void Init()
        {
            DeckardTestWindow window = (DeckardTestWindow)GetWindow(typeof(DeckardTestWindow));
            window.titleContent = new GUIContent("Deckard Test");
            window.Show();
        }

        private int renderWidth = 310;
        private int renderHeight = 440;

        private Texture lastRenderedTexture;

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Render Tests");

                    renderWidth = EditorGUILayout.IntField("Width", renderWidth);
                    renderHeight = EditorGUILayout.IntField("Height", renderHeight);
                    
                    var selectedCanvas = Selection.activeGameObject?.GetComponentInParent<Canvas>();
                    GUI.enabled = selectedCanvas != null;
                    if (GUILayout.Button("Render Current Canvas"))
                    {
                        if (lastRenderedTexture != null)
                        {
                            DestroyImmediate(lastRenderedTexture);
                        }
                        lastRenderedTexture = PngExporter.RenderCanvas(selectedCanvas, renderWidth, renderHeight);
                    }
                    GUI.enabled = true;

                    if (lastRenderedTexture != null)
                    {
                        GUILayout.Label(lastRenderedTexture);
                    }
                }
            }
        }
    }
}