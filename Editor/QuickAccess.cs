using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SceneObjectEntry = DL.QuickAccess.QuickAccessData.SceneObjectEntry;

namespace DL.QuickAccess
{
    public class QuickAccessWindow : EditorWindow
    {
        private static readonly Color OPEN_COLOR = new Color(0.2f, 0.8f, 0.2f);
        private static readonly Color PING_BUTTON_COLOR = new Color(0.9f, 0.75f, 0.2f);
        private static readonly Color HIGHLIGHT_COLOR = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ASSETS_ACCENT_COLOR = new Color(0.35f, 0.65f, 1f);
        private static readonly Color SCENES_ACCENT_COLOR = new Color(0.55f, 0.85f, 0.4f);
        private static readonly Color SCENE_OBJECTS_ACCENT_COLOR = new Color(0.85f, 0.55f, 0.9f);

        private const float ENTRY_HEIGHT_MULTIPLIER = 1.5f;
        private const float COLUMN_SPACING = 12f;
        private const double HIGHLIGHT_DURATION = 1.2d;
        private const float HIGHLIGHT_PEAK_ALPHA = 0.35f;

        private static GUIStyle _leftAlignedButtonStyle;
        private static GUIStyle _iconButtonStyle;
        private static GUIStyle _titleLabelStyle;
        private static GUIStyle _titleLabelOpenStyle;
        private static GUIStyle _headerTitleStyle;
        private static GUIStyle _countBadgeStyle;
        private static GUIStyle _sectionBoxStyle;
        private static GUIStyle _emptyStateStyle;

        private const double STATUS_MESSAGE_DURATION = 3d;

        private Vector2 _scrollPosition;
        private bool _isDraggingOver;
        private bool _isDraggingDuplicate;
        private string _statusMessage;
        private Color _statusMessageColor;
        private double _statusMessageEndTime;

        private Object _highlightObject;
        private string _highlightSceneObjectId;
        private double _highlightEndTime;
        private bool _scrollPending;

        private static QuickAccessData Data => QuickAccessData.instance;
        private static List<Object> _assets => Data.assets;
        private static List<Object> _scenes => Data.scenes;
        private static List<SceneObjectEntry> _sceneObjects => Data.sceneObjects;

        [MenuItem("Tools/Quick Access")]
        private static void ShowWindow()
        {
            QuickAccessWindow window = GetWindow<QuickAccessWindow>();
            window.titleContent = new GUIContent(" Quick Access", EditorGUIUtility.IconContent("Favorite").image);
            window.minSize = new Vector2(320, 260);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            bool needsRepaint = false;

            if (!string.IsNullOrEmpty(_statusMessage) && EditorApplication.timeSinceStartup >= _statusMessageEndTime)
            {
                _statusMessage = null;
                needsRepaint = true;
            }

            if (_highlightObject != null || _highlightSceneObjectId != null)
            {
                if (EditorApplication.timeSinceStartup >= _highlightEndTime)
                {
                    _highlightObject = null;
                    _highlightSceneObjectId = null;
                }

                needsRepaint = true;
            }

            if (needsRepaint)
            {
                Repaint();
            }
        }

        private static GUIStyle GetLeftAlignedButtonStyle()
        {
            if (_leftAlignedButtonStyle == null)
            {
                _leftAlignedButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(8, 6, 0, 0)
                };
            }

            return _leftAlignedButtonStyle;
        }

        private static GUIStyle GetIconButtonStyle()
        {
            if (_iconButtonStyle == null)
            {
                _iconButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            return _iconButtonStyle;
        }

        private static GUIStyle GetTitleLabelStyle()
        {
            if (_titleLabelStyle == null)
            {
                _titleLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            return _titleLabelStyle;
        }

        private static GUIStyle GetTitleLabelOpenStyle()
        {
            if (_titleLabelOpenStyle == null)
            {
                _titleLabelOpenStyle = new GUIStyle(GetTitleLabelStyle())
                {
                    normal = { textColor = OPEN_COLOR }
                };
            }

            return _titleLabelOpenStyle;
        }

        private static GUIStyle GetHeaderTitleStyle()
        {
            if (_headerTitleStyle == null)
            {
                _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            return _headerTitleStyle;
        }

        private static GUIStyle GetCountBadgeStyle()
        {
            if (_countBadgeStyle == null)
            {
                _countBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }

            return _countBadgeStyle;
        }

        private static GUIStyle GetSectionBoxStyle()
        {
            if (_sectionBoxStyle == null)
            {
                _sectionBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(6, 6, 6, 8),
                    margin = new RectOffset(0, 0, 0, 4)
                };
            }

            return _sectionBoxStyle;
        }

        private static GUIStyle GetEmptyStateStyle()
        {
            if (_emptyStateStyle == null)
            {
                _emptyStateStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 12,
                    normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
                };
            }

            return _emptyStateStyle;
        }

        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.4f) : new Color(0f, 0f, 0f, 0.2f));
            }
        }

        private static void DrawSectionHeader(string title, int count, Color accentColor, float entryHeight)
        {
            Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(entryHeight));

            if (Event.current.type == EventType.Repaint)
            {
                Rect barRect = new Rect(rect.x, rect.y + 2f, 3f, entryHeight - 4f);
                EditorGUI.DrawRect(barRect, accentColor);
            }

            GUILayout.Space(8);
            GUILayout.Label(title, GetTitleLabelStyle());
            GUILayout.FlexibleSpace();
            GUILayout.Label(count.ToString(), GetCountBadgeStyle(), GUILayout.Width(24));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawZebraStripe(Rect rowRect, int index, bool highlighted)
        {
            if (highlighted || Event.current.type != EventType.Repaint || index % 2 == 0)
            {
                return;
            }

            Color stripeColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.03f) : new Color(0f, 0f, 0f, 0.035f);
            EditorGUI.DrawRect(rowRect, stripeColor);
        }

        private static GUIContent GetContent(Object asset)
        {
            if (asset == null)
            {
                return new GUIContent("None");
            }

            GUIContent content = EditorGUIUtility.ObjectContent(asset, asset.GetType());
            return new GUIContent("  " + asset.name, content.image);
        }

        private static GUIContent GetContent(SceneObjectEntry entry, Object resolved)
        {
            if (resolved != null)
            {
                return GetContent(resolved);
            }

            string label = string.IsNullOrEmpty(entry.displayName) ? "Missing" : entry.displayName;
            GUIContent content = EditorGUIUtility.ObjectContent(null, typeof(GameObject));
            return new GUIContent("  " + label, content.image);
        }

        private static bool IsSceneOpen(SceneAsset scene)
        {
            string scenePath = AssetDatabase.GetAssetPath(scene);

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene openScene = EditorSceneManager.GetSceneAt(i);
                if (openScene.path == scenePath)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AddToList(List<Object> list, Object item)
        {
            if (item == null)
            {
                Debug.LogError("[QuickAccessWindow.AddToList] Cannot add a null reference.");
                return false;
            }

            if (list.Contains(item))
            {
                return false;
            }

            list.Add(item);
            return true;
        }

        private static string AddSceneObjectToList(List<SceneObjectEntry> list, Object item)
        {
            if (item == null)
            {
                Debug.LogError("[QuickAccessWindow.AddSceneObjectToList] Cannot add a null reference.");
                return null;
            }

            UnityEngine.SceneManagement.Scene scene = item is GameObject go ? go.scene
                : item is Component component ? component.gameObject.scene
                : default;

            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogError("[QuickAccessWindow.AddSceneObjectToList] Item must be an object inside a saved scene.");
                return null;
            }

            string globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(item).ToString();

            if (list.Exists(entry => entry.globalObjectId == globalObjectId))
            {
                return null;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            list.Add(new SceneObjectEntry
            {
                scene = sceneAsset,
                globalObjectId = globalObjectId,
                displayName = item.name
            });

            list.Sort((a, b) => string.Compare(a.scene != null ? a.scene.name : string.Empty, b.scene != null ? b.scene.name : string.Empty, System.StringComparison.OrdinalIgnoreCase));

            return sceneAsset != null ? sceneAsset.name : "<Missing>";
        }

        private static Object ResolveSceneObject(SceneObjectEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.globalObjectId))
            {
                return null;
            }

            if (!GlobalObjectId.TryParse(entry.globalObjectId, out GlobalObjectId globalObjectId))
            {
                return null;
            }

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
        }

        private static void PingSceneObject(SceneObjectEntry entry)
        {
            if (entry == null || entry.scene == null)
            {
                Debug.LogError("[QuickAccessWindow.PingSceneObject] Scene reference is missing.");
                return;
            }

            if (!IsSceneOpen(entry.scene))
            {
                OpenScene(entry.scene);
            }

            Object resolved = ResolveSceneObject(entry);
            if (resolved == null)
            {
                Debug.LogError($"[QuickAccessWindow.PingSceneObject] Could not resolve '{entry.displayName}' in scene '{entry.scene.name}'. It may have been deleted.");
                return;
            }

            if (entry.displayName != resolved.name)
            {
                entry.displayName = resolved.name;
                Data.Save();
            }

            PingAsset(resolved);
        }

        private static void PingAsset(Object asset)
        {
            if (asset == null)
            {
                Debug.LogError("[QuickAccessWindow.PingAsset] Asset reference is null.");
                return;
            }

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private static void OpenAsset(Object asset)
        {
            if (asset == null)
            {
                Debug.LogError("[QuickAccessWindow.OpenAsset] Asset reference is null.");
                return;
            }

            AssetDatabase.OpenAsset(asset);
        }

        private static void OpenScene(SceneAsset scene)
        {
            if (scene == null)
            {
                Debug.LogError("[QuickAccessWindow.OpenScene] Scene reference is null.");
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(scene);

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(4);

            bool noPinnedAssets = (_assets?.Count == 0) && (_scenes?.Count == 0) && (_sceneObjects?.Count == 0);
            if (noPinnedAssets)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    "Nothing pinned yet.\nDrag assets, scenes, or GameObjects into this window to pin them for quick access.",
                    GetEmptyStateStyle());
                GUILayout.FlexibleSpace();
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);

                DrawAssetColumn();

                GUILayout.Space(COLUMN_SPACING);

                DrawSceneColumn();

                GUILayout.Space(COLUMN_SPACING);

                DrawSceneObjectColumn();

                EditorGUILayout.EndScrollView();
            }

            HandleDragAndDrop();
            DrawStatusMessage();
        }

        private string AddItem(Object item)
        {
            if (item == null)
            {
                return null;
            }

            string result;

            if (item is SceneAsset sceneAsset)
            {
                result = AddToList(_scenes, sceneAsset) ? "Scenes" : null;
            }
            else if (!EditorUtility.IsPersistent(item))
            {
                string sceneName = AddSceneObjectToList(_sceneObjects, item);
                result = sceneName != null ? $"Scene: {sceneName}" : null;
            }
            else
            {
                result = AddToList(_assets, item) ? "Assets" : null;
            }

            if (result != null)
            {
                Data.Save();
            }

            return result;
        }

        private bool IsAlreadyPinned(Object item)
        {
            if (item == null)
            {
                return false;
            }

            if (item is SceneAsset sceneAsset)
            {
                return _scenes.Contains(sceneAsset);
            }

            if (!EditorUtility.IsPersistent(item))
            {
                UnityEngine.SceneManagement.Scene scene = item is GameObject go ? go.scene
                    : item is Component component ? component.gameObject.scene
                    : default;

                if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                {
                    return false;
                }

                string globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(item).ToString();
                return _sceneObjects.Exists(entry => entry.globalObjectId == globalObjectId);
            }

            return _assets.Contains(item);
        }

        private void SetHighlightTarget(Object item)
        {
            _highlightObject = null;
            _highlightSceneObjectId = null;

            if (item is SceneAsset || EditorUtility.IsPersistent(item))
            {
                _highlightObject = item;
            }
            else
            {
                UnityEngine.SceneManagement.Scene scene = item is GameObject go ? go.scene
                    : item is Component component ? component.gameObject.scene
                    : default;

                if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
                {
                    _highlightSceneObjectId = GlobalObjectId.GetGlobalObjectIdSlow(item).ToString();
                }
            }

            _highlightEndTime = EditorApplication.timeSinceStartup + HIGHLIGHT_DURATION;
            _scrollPending = true;
        }

        private bool IsHighlighted(Object candidate)
        {
            return _highlightObject != null && candidate == _highlightObject && EditorApplication.timeSinceStartup < _highlightEndTime;
        }

        private bool IsHighlighted(SceneObjectEntry entry)
        {
            return _highlightSceneObjectId != null && entry.globalObjectId == _highlightSceneObjectId && EditorApplication.timeSinceStartup < _highlightEndTime;
        }

        private Color GetHighlightColor()
        {
            float remaining = Mathf.Max(0f, (float)(_highlightEndTime - EditorApplication.timeSinceStartup));
            float t = 1f - Mathf.Clamp01(remaining / (float)HIGHLIGHT_DURATION);
            Color color = HIGHLIGHT_COLOR;
            color.a = Mathf.Lerp(HIGHLIGHT_PEAK_ALPHA, 0f, t);
            return color;
        }

        private void ScrollToRect(Rect rect)
        {
            float viewportHeight = Mathf.Max(0f, position.height - 60f);
            float target = Mathf.Max(0f, rect.y - (viewportHeight - rect.height) / 2f);
            _scrollPosition = new Vector2(_scrollPosition.x, target);
            _scrollPending = false;
            Repaint();
        }

        private void ShowStatusMessage(string message, Color color)
        {
            _statusMessage = message;
            _statusMessageColor = color;
            _statusMessageEndTime = EditorApplication.timeSinceStartup + STATUS_MESSAGE_DURATION;
        }

        private void DrawStatusMessage()
        {
            if (string.IsNullOrEmpty(_statusMessage) || _isDraggingOver)
            {
                return;
            }

            float height = EditorGUIUtility.singleLineHeight * ENTRY_HEIGHT_MULTIPLIER + 4f;
            Rect bannerRect = new Rect(0, 0, position.width, height);

            EditorGUI.DrawRect(bannerRect, new Color(0f, 0f, 0f, 0.88f));
            EditorGUI.DrawRect(new Rect(bannerRect.x, bannerRect.yMax - 2f, bannerRect.width, 2f), _statusMessageColor);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                normal = { textColor = _statusMessageColor }
            };
            GUI.Label(bannerRect, _statusMessage, style);
        }

        private void HandleDragAndDrop()
        {
            Event evt = Event.current;
            Rect windowRect = new Rect(0, 0, position.width, position.height);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (windowRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        _isDraggingOver = true;

                        _isDraggingDuplicate = false;
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (IsAlreadyPinned(draggedObject))
                            {
                                _isDraggingDuplicate = true;
                                break;
                            }
                        }

                        evt.Use();
                    }
                    else
                    {
                        _isDraggingOver = false;
                    }
                    Repaint();
                    break;

                case EventType.DragPerform:
                    if (windowRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.AcceptDrag();

                        List<string> addedListNames = new List<string>();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            SetHighlightTarget(draggedObject);

                            if (IsAlreadyPinned(draggedObject))
                            {
                                continue;
                            }

                            string listName = AddItem(draggedObject);
                            if (listName != null && !addedListNames.Contains(listName))
                            {
                                addedListNames.Add(listName);
                            }
                        }

                        if (addedListNames.Count > 0)
                        {
                            ShowStatusMessage($"Added in {string.Join(", ", addedListNames)} list", OPEN_COLOR);
                        }

                        evt.Use();
                    }
                    _isDraggingOver = false;
                    Repaint();
                    break;

                case EventType.DragExited:
                    _isDraggingOver = false;
                    Repaint();
                    break;
            }

            if (_isDraggingOver)
            {
                EditorGUI.DrawRect(windowRect, new Color(0f, 0f, 0f, 0.65f));

                Color accent = _isDraggingDuplicate ? new Color(0.9f, 0.3f, 0.3f) : OPEN_COLOR;
                EditorGUI.DrawRect(new Rect(windowRect.x, windowRect.y, windowRect.width, 3f), accent);
                EditorGUI.DrawRect(new Rect(windowRect.x, windowRect.yMax - 3f, windowRect.width, 3f), accent);

                GUIStyle overlayStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    normal = { textColor = _isDraggingDuplicate ? new Color(1f, 0.5f, 0.5f) : Color.white }
                };
                string overlayText = _isDraggingDuplicate ? "Object already exist" : "Pin for Quick Access";
                GUI.Label(windowRect, overlayText, overlayStyle);
            }
        }

        private void DrawAssetColumn()
        {
            if (_assets.Count == 0)
            {
                return;
            }

            float entryHeight = EditorGUIUtility.singleLineHeight * ENTRY_HEIGHT_MULTIPLIER;

            EditorGUILayout.BeginVertical(GetSectionBoxStyle());
            DrawSectionHeader("Assets", _assets.Count, ASSETS_ACCENT_COLOR, entryHeight);
            EditorGUILayout.Space(2);

            for (int i = 0; i < _assets.Count; i++)
            {
                Object asset = _assets[i];

                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(entryHeight));
                bool highlighted = IsHighlighted(asset);

                DrawZebraStripe(rowRect, i, highlighted);

                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = PING_BUTTON_COLOR;
                if (GUILayout.Button("@", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                {
                    PingAsset(asset);
                }
                GUI.backgroundColor = previousColor;

                if (GUILayout.Button(GetContent(asset), GetLeftAlignedButtonStyle(), GUILayout.Height(entryHeight), GUILayout.MinWidth(0), GUILayout.ExpandWidth(true)))
                {
                    OpenAsset(asset);
                }

                if (highlighted && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, GetHighlightColor());
                    if (_scrollPending)
                    {
                        ScrollToRect(rowRect);
                    }
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                {
                    _assets.RemoveAt(i);
                    Data.Save();
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = previousColor;
                    break;
                }
                GUI.backgroundColor = previousColor;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneColumn()
        {
            if (_scenes.Count == 0)
            {
                return;
            }

            float entryHeight = EditorGUIUtility.singleLineHeight * ENTRY_HEIGHT_MULTIPLIER;

            EditorGUILayout.BeginVertical(GetSectionBoxStyle());
            DrawSectionHeader("Scenes", _scenes.Count, SCENES_ACCENT_COLOR, entryHeight);
            EditorGUILayout.Space(2);

            for (int i = 0; i < _scenes.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(entryHeight));

                SceneAsset scene = _scenes[i] as SceneAsset;
                bool highlighted = IsHighlighted(scene);

                DrawZebraStripe(rowRect, i, highlighted);

                bool isOpen = scene != null && IsSceneOpen(scene);
                Color previousColor = GUI.backgroundColor;
                if (isOpen) GUI.backgroundColor = OPEN_COLOR;

                if (GUILayout.Button("@", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                    PingAsset(scene);

                if (GUILayout.Button(GetContent(scene), GetLeftAlignedButtonStyle(), GUILayout.Height(entryHeight), GUILayout.MinWidth(0), GUILayout.ExpandWidth(true)))
                    OpenScene(scene);

                if (GUILayout.Button("▶", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                {
                    OpenScene(scene);
                    EditorApplication.isPlaying = true;
                }

                if (highlighted && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, GetHighlightColor());
                    if (_scrollPending)
                    {
                        ScrollToRect(rowRect);
                    }
                }

                if (GUILayout.Button("X", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                {
                    _scenes.RemoveAt(i);
                    Data.Save();
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = previousColor;
                    break;
                }

                GUI.backgroundColor = previousColor;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneObjectColumn()
        {
            if (_sceneObjects.Count == 0)
            {
                return;
            }

            float entryHeight = EditorGUIUtility.singleLineHeight * ENTRY_HEIGHT_MULTIPLIER;

            EditorGUILayout.BeginVertical(GetSectionBoxStyle());
            DrawSectionHeader("Scene Objects", _sceneObjects.Count, SCENE_OBJECTS_ACCENT_COLOR, entryHeight);
            EditorGUILayout.Space(2);

            SceneAsset currentScene = null;
            int rowIndex = 0;

            for (int i = 0; i < _sceneObjects.Count; i++)
            {
                SceneObjectEntry entry = _sceneObjects[i];

                bool sceneOpen = entry.scene != null && IsSceneOpen(entry.scene);

                if (entry.scene != currentScene)
                {
                    currentScene = entry.scene;
                    rowIndex = 0;
                    if (i > 0)
                    {
                        EditorGUILayout.Space(6);
                    }
                    string sceneName = currentScene != null ? currentScene.name : "<Missing>";
                    sceneName = "  Scene: " + sceneName;
                    if (sceneOpen) sceneName += " (opened)";
                    GUIStyle titleStyle = sceneOpen ? GetTitleLabelOpenStyle() : GetTitleLabelStyle();
                    EditorGUILayout.LabelField(sceneName, titleStyle, GUILayout.Height(entryHeight * 0.85f));
                    EditorGUILayout.Space(2);
                }

                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(entryHeight));
                bool highlighted = IsHighlighted(entry);

                DrawZebraStripe(rowRect, rowIndex, highlighted);
                rowIndex++;

                Object resolved = sceneOpen ? ResolveSceneObject(entry) : null;

                if (GUILayout.Button("@", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                {
                    PingSceneObject(entry);
                }

                if (GUILayout.Button(GetContent(entry, resolved), GetLeftAlignedButtonStyle(), GUILayout.Height(entryHeight), GUILayout.MinWidth(0), GUILayout.ExpandWidth(true)))
                {
                    PingSceneObject(entry);
                }

                if (highlighted && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, GetHighlightColor());
                    if (_scrollPending)
                    {
                        ScrollToRect(rowRect);
                    }
                }

                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GetIconButtonStyle(), GUILayout.Width(entryHeight), GUILayout.Height(entryHeight)))
                {
                    _sceneObjects.RemoveAt(i);
                    Data.Save();
                    EditorGUILayout.EndHorizontal();
                    GUI.backgroundColor = previousColor;
                    EditorGUILayout.EndVertical();
                    return;
                }
                GUI.backgroundColor = previousColor;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
