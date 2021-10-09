using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deckard.Editor.Test
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

        private int renderDpi = 72;
        private int superSample = 2;
        private bool includeBleed;

        private Texture2D lastRenderedTexture;
        private string lastSaveDirectory;
        private string lastSavePath;

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

                    renderDpi = EditorGUILayout.IntField("Render DPI", renderDpi);
                    superSample = EditorGUILayout.IntField("Supersample", superSample);
                    includeBleed = EditorGUILayout.Toggle("Include Bleed", includeBleed);
                    
                    var selectedCanvas = Selection.activeGameObject?.GetComponentInParent<DeckardCanvas>();
                    GUI.enabled = selectedCanvas != null;
                    if (GUILayout.Button("Render Current Canvas"))
                    {
                        if (lastRenderedTexture != null)
                        {
                            DestroyImmediate(lastRenderedTexture);
                        }
                        
                        lastRenderedTexture = selectedCanvas.Render(renderDpi, includeBleed, superSample);
                    }
                    GUI.enabled = true;

                    GUI.enabled = lastRenderedTexture != null;
                    if (GUILayout.Button("Save Texture"))
                    {
                        var path = EditorUtility.SaveFilePanel("Save Image", lastSaveDirectory, "", "png");
                        if (path != null)
                        {
                            lastSaveDirectory = Path.GetDirectoryName(path);
                            DeckardCanvas.SaveTextureAsPng(lastRenderedTexture, path);
                            OpenInFileBrowser.Open(path);
                        }
                    }
                    GUI.enabled = true;
                    
                    GUI.enabled = !string.IsNullOrEmpty(lastSaveDirectory);
                    if (GUILayout.Button("Open Directory"))
                    {
                        OpenInFileBrowser.Open(lastSaveDirectory);
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