﻿#define PROBUILDER_4_0_OR_NEWER
//#define POLYBRUSH_DEBUG

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Polybrush;
using UnityEditor.SettingsManagement;
using System;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Interface and settings for Polybrush
    /// </summary>
    internal class PolybrushEditor : ConfigurableWindow
	{
		static PolybrushEditor s_Instance = null;
		internal static PolybrushEditor instance { get { return s_Instance; } }

		const string k_BrushSettingsAssetPref = "Polybrush::Editor.brushSettingsAsset";
		const string k_BrushSettingsPref = "Polybrush::Editor.brushSettings";
        const string k_BrushSettingsName = "Polybrush::Editor.brushSettingsName";

		const double k_EditorTargetFrameLow = .016667;
		const double k_EditorTargetFramerateHigh = .03;

        static readonly Vector2 k_EditorWindowMinimumSize = new Vector2(320, 180);

        static List<Ray> s_Rays = new List<Ray>();

        /// <summary>
        /// Set true to lock brush to the object it starts to apply on.
        /// </summary>
        [UserSetting("General Settings", "Lock Brush to First", "When applying a brush this prevents any other mesh from intercepting the stroke.  Disable this if you want to apply across multiple meshes.")]
        internal static Pref<bool> s_LockBrushToFirst = new Pref<bool>("Brush.LockBrushToFirstObject", true, SettingsScope.Project);

        /// <summary>
        /// Set true to always try to raycast against selection first, even if a mesh is in the way.
        /// </summary>
        [UserSetting]
        internal static Pref<bool> s_IgnoreUnselected = new Pref<bool>("Brush.IgnoreUnselected", true, SettingsScope.Project);

        /// <summary>
        /// Set true to have Polybrush window in floating mode (no dockable).
        /// </summary>
        [UserSetting]
        internal static Pref<bool> s_FloatingWindow = new Pref<bool>("Editor.FloatingWindow", false, SettingsScope.Project);

        [SerializeField]
        List<BrushMode> modes = new List<BrushMode>();

        // A reference to the saved preset brush settings.
        [SerializeField]
        BrushSettings brushSettingsAsset;

        // Editor for the current brush settings.
        BrushSettingsEditor m_BrushEditor = null;
        double m_LastBrushUpdate = 0.0;

        /// <summary>
        /// Editor for the brush mirror settings
        /// </summary>
        MirrorSettingsEditor m_BrushMirrorEditor = null;

        // gameobjects that are temporarily ignored by HandleUtility.PickGameObject.
        List<GameObject> m_IgnoreDrag = new List<GameObject>(8);
        // All objects that have been hovered by the mouse
        Dictionary<GameObject, BrushTarget> m_Hovering = new Dictionary<GameObject, BrushTarget>();
        GameObject m_LastHoveredGameObject = null;
        int m_CurrentBrushIndex = 0;
        IReadOnlyCollection<BrushSettings> m_AvailableBrushes = null;
        string[] m_AvailableBrushesStrings = null;

        // Keep track of the objects that have been registered for undo, allowing the editor to
        // restrict undo calls to only the necessary meshes when applying a brush swath.
        List<GameObject> m_UndoQueue = new List<GameObject>();
        bool m_WantsRepaint = false;
        bool m_ApplyingBrush = false;
        Vector2 m_Scroll = Vector2.zero;

        // The current editing mode (RaiseLower, Smooth, Color, etc).
        internal BrushTool tool = BrushTool.None;

		// The current brush status
		internal BrushTarget brushTarget = null;

        // The first GameObject being stroked
        internal GameObject firstGameObject = null;

		// The current brush settings
		internal BrushSettings brushSettings;

        GUIContent[] m_GCToolmodeIcons = null;

		GUIContent m_GCSaveBrushSettings  = new GUIContent("Save", "Save the brush settings as a preset");

        // The current editing mode (RaiseLower, Smooth, Color, etc).
        internal BrushMode mode
        {
            get
            {
                return modes.Count > 0 ? modes[0] : null;
            }
            set
            {
                if (modes.Contains(value))
                    modes.Remove(value);
                modes.Insert(0, value);
            }
        }

        internal BrushSettingsEditor brushEditor
        {
            get
            {
                if (m_BrushEditor == null && brushSettings != null)
                {
                    m_BrushEditor = (BrushSettingsEditor)Editor.CreateEditor(brushSettings);
                }
                else if (m_BrushEditor.target != brushSettings)
                {
                    GameObject.DestroyImmediate(m_BrushEditor);

                    if (brushSettings != null)
                        m_BrushEditor = (BrushSettingsEditor)Editor.CreateEditor(brushSettings);
                }

                return m_BrushEditor;
            }
        }

        /// <summary>
        /// Switch between floating window and dockable window
        /// </summary>
        /// <param name="floating">should reopen the window as floating or not ?</param>
		void SetWindowFloating(bool floating)
		{
            s_FloatingWindow.value = floating;

            BrushTool tool = s_Instance.tool;

            GetWindow<PolybrushEditor>().Close();
            MenuItems.MenuInitEditorWindow();

            s_Instance.SetTool(tool);
		}

		void OnEnable()
		{
            if (!PrefUtility.VersionCheck())
                PrefUtility.ClearPrefs();

			PolybrushEditor.s_Instance = this;

            // Editor window setup
            titleContent = new GUIContent("Polybrush");
            wantsMouseMove = true;
            minSize = k_EditorWindowMinimumSize;

            m_BrushMirrorEditor = new MirrorSettingsEditor();

#if PROBUILDER_4_0_OR_NEWER
            if (ProBuilderBridge.ProBuilderExists())
                ProBuilderBridge.SubscribeToSelectModeChanged(OnProBuilderSelectModeChanged);
#endif

            m_GCToolmodeIcons = new GUIContent[]
            {
                EditorGUIUtility.TrIconContent(IconUtility.GetIcon("Toolbar/Sculpt"), "Sculpt on meshes"),
                EditorGUIUtility.TrIconContent(IconUtility.GetIcon("Toolbar/Smooth"), "Smooth mesh geometry"),
                EditorGUIUtility.TrIconContent(IconUtility.GetIcon("Toolbar/PaintVertexColors"), "Paint vertex colors on meshes"),
                EditorGUIUtility.TrIconContent(IconUtility.GetIcon("Toolbar/PaintPrefabs"), "Scatter Prefabs on meshes"),
                EditorGUIUtility.TrIconContent(IconUtility.GetIcon("Toolbar/PaintTextures"), "Paint textures on meshes"),
            };

#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            Undo.undoRedoPerformed += UndoRedoPerformed;

			// force update the preview
			m_LastHoveredGameObject = null;

			EnsureBrushSettingsListIsValid();
            PopulateAvailableBrushList();

            SetTool(BrushTool.RaiseLower, false);

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        void OnDisable()
		{
            Selection.selectionChanged -= OnSelectionChanged;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            //EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemChanged;

#if PROBUILDER_4_0_OR_NEWER
            if (ProBuilderBridge.ProBuilderExists())
                ProBuilderBridge.UnsubscribeToSelectModeChanged(OnProBuilderSelectModeChanged);
#endif

            // store local changes to brushSettings
            if (brushSettings != null)
            {
                var js = JsonUtility.ToJson(brushSettings, true);
                EditorPrefs.SetString(k_BrushSettingsPref, js);
            }

			// don't iterate here!  FinalizeAndReset does that
			OnBrushExit( m_LastHoveredGameObject );
			FinalizeAndResetHovering();

            PreviewsDatabase.UnloadCache();
        }

		void OnDestroy()
		{
			SetTool(BrushTool.None);

#if PROBUILDER_4_0_OR_NEWER
            if (ProBuilderBridge.ProBuilderExists())
                ProBuilderBridge.SetSelectMode(ProBuilderBridge.SelectMode.Object);
#endif

            foreach (BrushMode m in modes)
				GameObject.DestroyImmediate(m);


            if (brushSettings != null)
                GameObject.DestroyImmediate(brushSettings);

            if (m_BrushEditor != null)
				GameObject.DestroyImmediate(m_BrushEditor);
		}

		internal static void DoRepaint()
		{
			if(PolybrushEditor.instance != null)
				PolybrushEditor.instance.m_WantsRepaint = true;
		}

        void PopulateAvailableBrushList()
        {
            m_AvailableBrushes = BrushSettingsEditor.GetAvailableBrushes();
            m_AvailableBrushesStrings = m_AvailableBrushes.Select(x => x.name).ToArray();
            m_CurrentBrushIndex = System.Math.Max(Array.FindIndex<string>(m_AvailableBrushesStrings, x => x == brushSettings.name), 0);
            ArrayUtility.Add<string>(ref m_AvailableBrushesStrings, string.Empty);
            ArrayUtility.Add<string>(ref m_AvailableBrushesStrings, "Add Brush...");
        }

        void OnGUI()
		{
			Event e = Event.current;
			GUILayout.Space(8);

            DoContextMenu();
            DrawToolbar();
            CheckForEscapeKey(e);

            // Call current mode GUI
            if (mode != null)
			{
                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

                DrawBrushSettings();
                m_BrushMirrorEditor.OnGUI();
                if (tool != BrushTool.None)
                    DrawActiveToolmodeSettings();

                EditorGUILayout.Space();

                if (EditorGUI.EndChangeCheck())
                    mode.OnBrushSettingsChanged(brushTarget, brushSettings);

                EditorGUILayout.EndScrollView();
            }

#if POLYBRUSH_DEBUG
			GUILayout.Label("DEBUG", EditorStyles.boldLabel);

            GUILayout.Label("target: " + (Util.IsValid(brushTarget) ? brushTarget.editableObject.gameObjectAttached.name : "null"));
			GUILayout.Label("vertex: " + (Util.IsValid(brushTarget) ? brushTarget.editableObject.vertexCount : 0));
			GUILayout.Label("applying: " + m_ApplyingBrush);
			GUILayout.Label("lockBrushToFirst: " + s_LockBrushToFirst);
			GUILayout.Label("lastHoveredGameObject: " + m_LastHoveredGameObject);

			GUILayout.Space(6);

			foreach(var kvp in m_Hovering)
			{
				BrushTarget t = kvp.Value;
				EditableObject dbg_editable = t.editableObject;
				GUILayout.Label("Vertex Streams: " + dbg_editable.usingVertexStreams);
				GUILayout.Label("Original: " + (dbg_editable.originalMesh == null ? "null" : dbg_editable.originalMesh.name));
				GUILayout.Label("Active: " + (dbg_editable.editMesh == null ? "null" : dbg_editable.editMesh.name));
				GUILayout.Label("Graphics: " + (dbg_editable.graphicsMesh == null ? "null" : dbg_editable.graphicsMesh.name));
			}
#endif

			if(m_WantsRepaint)
			{
				m_WantsRepaint = false;
				Repaint();
			}
		}

        void DrawToolbar()
        {
            EditorGUI.BeginChangeCheck();

            int toolbarIndex = (int)tool - 1;

            using (new GUILayout.HorizontalScope())
            {
                toolbarIndex = GUILayout.Toolbar(toolbarIndex, m_GCToolmodeIcons, GUILayout.Width(s_Instance.position.width - 6), GUILayout.Height(23));
            }

            if (EditorGUI.EndChangeCheck())
            {
                BrushTool newTool = (BrushTool)(toolbarIndex + 1);
                SetTool(newTool == tool ? BrushTool.None : (BrushTool)toolbarIndex + 1);
            }
        }

        void DrawBrushSettings()
        {
            EnsureBrushSettingsListIsValid();

           // Brush preset selector
            using (new GUILayout.VerticalScope("box"))
            {
                // Show the settings header in PolyEditor so that the preset selector can be included in the block.
                // Can't move preset selector to BrushSettingsEditor because it's a CustomEditor for BrushSettings,
                // along with other issues.
                if (PolyGUILayout.HeaderWithDocsLink(PolyGUI.TempContent("Brush Settings")))
                    Application.OpenURL(PrefUtility.documentationBrushSettingsLink);

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    m_CurrentBrushIndex = EditorGUILayout.Popup(m_CurrentBrushIndex, m_AvailableBrushesStrings);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (m_CurrentBrushIndex >= m_AvailableBrushes.Count)
                            SetBrushSettings(BrushSettingsEditor.AddNew(brushSettings));
                        else
                            SetBrushSettings(m_AvailableBrushes.ElementAt<BrushSettings>(m_CurrentBrushIndex));
                    }

                    if (GUILayout.Button(m_GCSaveBrushSettings, GUILayout.Width(50)))
                    {
                        if (brushSettings != null && brushSettingsAsset != null)
                        {
                            // integer 0, 1 or 2 corresponding to ok, cancel and alt buttons
                            int res = EditorUtility.DisplayDialogComplex("Save Brush Settings", "Overwrite brush preset, or Create a New brush preset? ", "Overwrite", "Create New", "Cancel");

                            if (res == 0)
                            {
                                brushSettings.CopyTo(brushSettingsAsset);
                                EditorGUIUtility.PingObject(brushSettingsAsset);
                            }
                            else if (res == 1)
                            {
                                BrushSettings dup = BrushSettingsEditor.AddNew(brushSettings);
                                SetBrushSettings(dup);
                                EditorGUIUtility.PingObject(brushSettingsAsset);
                            }

                            GUIUtility.ExitGUI();
                        }
                        else
                        {
                            Debug.LogWarning("Something went wrong saving brush settings.");
                        }
                    }
                }
                EditorGUI.BeginChangeCheck();

                brushEditor.OnInspectorGUI();
            }
        }

        void DrawActiveToolmodeSettings()
        {
            // Toolmode section
            using (new GUILayout.VerticalScope("box"))
            {
                mode.DrawGUI(brushSettings);
            }
        }

        private void CheckForEscapeKey(Event e)
        {
            if (e.type == EventType.KeyDown)
                if (e.keyCode == KeyCode.Escape)
                {
                    SetTool(BrushTool.None);
                    e.Use();
                }
        }

        /// <summary>
        /// Delegate called when ProBuilder changes select mode.
        /// </summary>
        void OnProBuilderSelectModeChanged(int mode)
		{
			// Top = 0,
			// Geometry = 1,
			// Texture = 2,
			// Plugin = 4

			if(mode > 0 && tool != BrushTool.None)
				SetTool(BrushTool.None);
		}

        /// <summary>
        /// Switch Polybrush to the given tool.
        /// </summary>
        /// <param name="brushTool">Tool to show in Polybrush window.</param>
        /// <param name="enableTool">If true, will activate the given tool automatically. Default: true.</param>
        internal void SetTool(BrushTool brushTool, bool enableTool = true)
		{
			if(brushTool == tool && mode != null)
				return;

#if PROBUILDER_4_0_OR_NEWER
            if (ProBuilderBridge.ProBuilderExists())
                ProBuilderBridge.SetSelectMode(ProBuilderBridge.SelectMode.Object);
#endif

            if(mode != null)
			{
				// Exiting edit mode
				if(m_LastHoveredGameObject != null)
				{
					OnBrushExit( m_LastHoveredGameObject );
					FinalizeAndResetHovering();
				}

				mode.OnDisable();
			}
			else
			{
#if PROBUILDER_4_0_OR_NEWER
                if (ProBuilderBridge.ProBuilderExists())
                    ProBuilderBridge.SetSelectMode(ProBuilderBridge.SelectMode.None);
#endif
            }

            m_LastHoveredGameObject = null;

			System.Type modeType = brushTool.GetModeType();

			if(modeType != null)
			{
				mode = modes.FirstOrDefault(x => x != null && x.GetType() == modeType);

				if(mode == null)
					mode = (BrushMode) ScriptableObject.CreateInstance( modeType );
			}

            // Handle tool auto activation/deactivation.
			tool = enableTool? brushTool : BrushTool.None;

			if(tool != BrushTool.None)
			{
				Tools.current = Tool.None;
				mode.OnEnable();
			}

            EnsureBrushSettingsListIsValid();
			Repaint();
		}

        /// <summary>
        /// Makes sure we always have a valid BrushSettings selected in Polybrush then refresh the available list for the EditorWindow.
        /// Will create a new file if it cannot find any.
        /// </summary>
		internal void EnsureBrushSettingsListIsValid()
		{
            if (brushSettings == null)
            {
                if (brushSettingsAsset == null)
                    brushSettingsAsset = BrushSettingsEditor.LoadBrushSettingsAssets(EditorPrefs.GetString(k_BrushSettingsAssetPref, ""));

                if (EditorPrefs.HasKey(k_BrushSettingsPref))
                {
                    brushSettings = ScriptableObject.CreateInstance<BrushSettings>();
                    JsonUtility.FromJsonOverwrite(EditorPrefs.GetString(k_BrushSettingsPref), brushSettings);
                    if (EditorPrefs.HasKey(k_BrushSettingsName))
                        brushSettings.name = EditorPrefs.GetString(k_BrushSettingsName);
                }
                else
                {
                    SetBrushSettings(brushSettingsAsset != null ? brushSettingsAsset : PolyEditorUtility.GetFirstOrNew<BrushSettings>());
                }
            }
        }

        /// <summary>
        /// Get the default brush settings
        /// </summary>
        /// <returns>first found brush settings, or a new created one</returns>
        internal BrushSettings GetDefaultSettings()
        {
            return m_AvailableBrushes.FirstOrDefault(x => x.name.Contains("Default"));
        }

        /// <summary>
        /// Change brush settings
        /// </summary>
        /// <param name="settings">The new brush settings</param>
		internal void SetBrushSettings(BrushSettings settings)
		{
			if(brushSettings != null && brushSettings != settings)
				DestroyImmediate(brushSettings);

			EditorPrefs.SetString(k_BrushSettingsAssetPref, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings)));
            EditorPrefs.SetString(k_BrushSettingsName, settings.name);

			brushSettingsAsset = settings;
			brushSettings = settings.DeepCopy();
			brushSettings.hideFlags = HideFlags.HideAndDontSave;
		}

		void OnSceneGUI(SceneView sceneView)
		{
			if(mode == null)
				return;

			Event e = Event.current;

            CheckForEscapeKey(e);

            if (Tools.current != Tool.None)
				SetTool(BrushTool.None);

			if(brushSettings == null)
				SetBrushSettings(PolyEditorUtility.GetFirstOrNew<BrushSettings>());

            if (PolySceneUtility.SceneViewInUse(e) || tool == BrushTool.None)
            {
                // Force exit the current brush if user's mouse left
                // the SceneView while a brush was still in use.
                if (m_ApplyingBrush)
                    OnFinishApplyingBrush();
                return;
            }

			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			if( Util.IsValid(brushTarget) )
				HandleUtility.AddDefaultControl(controlID);

			switch( e.GetTypeForControl(controlID) )
			{
				case EventType.MouseMove:
					// Handles:
					//		OnBrushEnter
					//		OnBrushExit
					//		OnBrushMove
					if( EditorApplication.timeSinceStartup - m_LastBrushUpdate > GetTargetFramerate(brushTarget) )
					{
						m_LastBrushUpdate = EditorApplication.timeSinceStartup;
						UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
					}
					break;

				case EventType.MouseDown:
				case EventType.MouseDrag:
					// Handles:
					//		OnBrushBeginApply
					//		OnBrushApply
					//		OnBrushFinishApply
					if( EditorApplication.timeSinceStartup - m_LastBrushUpdate > GetTargetFramerate(brushTarget) )
					{
						m_LastBrushUpdate = EditorApplication.timeSinceStartup;
						UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                        ApplyBrush(Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
					}
					break;

				case EventType.MouseUp:
					if(m_ApplyingBrush)
					{
						OnFinishApplyingBrush();
                        UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                    }
					break;

				case EventType.ScrollWheel:
					ScrollBrushSettings(e);
					break;
                case EventType.KeyDown:
                    // Key down event continues as long as the key is held down. However, we only need to update the brush once while the key is down.
                    // Check if the key has already been marked as pressed in the brush settings, and don't update if it is.
                    if (((e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl) && !brushSettings.isUserHoldingControl) ||
                        ((e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift) && !brushSettings.isUserHoldingShift))
                    {
                        m_LastBrushUpdate = EditorApplication.timeSinceStartup;
                        UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                    }
                    break;
                case EventType.KeyUp:
                    // Key up only happens once, so don't need to check if we were already holding control/shift
                    if ((e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl) ||
                        (e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift))
                    {
                        m_LastBrushUpdate = EditorApplication.timeSinceStartup;
                        UpdateBrush(e.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
                    }
                    break;
            }

			if( Util.IsValid(brushTarget) )
				mode.DrawGizmos(brushTarget, brushSettings);
        }

        /// <summary>
        /// Get framerate according to the brush target
        /// </summary>
        /// <param name="target">The brush target</param>
        /// <returns>framerate</returns>
		static double GetTargetFramerate(BrushTarget target)
		{
			if(Util.IsValid(target) && target.vertexCount > 24000)
				return k_EditorTargetFrameLow;

			return k_EditorTargetFramerateHigh;
		}

        /// <summary>
        /// Get a EditableObject matching the GameObject go or create a new one.
        /// </summary>
        /// <param name="go">Gameobject to edit</param>
        /// <returns></returns>
        BrushTarget GetOrCreateBrushTarget(GameObject go)
		{
			BrushTarget target = null;

			if( !m_Hovering.TryGetValue(go, out target) )
			{
                target = new BrushTarget(EditableObject.Create(go));
                m_Hovering.Add(go, target);
			}
			else if( !target.IsValid() )
			{
                m_Hovering[go] = new BrushTarget(EditableObject.Create(go));
			}

			return target;
		}

        /// <summary>
        /// Will display a popup with correct message in case of mismatching method
        /// </summary>
        /// <param name="go">GameObject to check</param>
        /// <result>Will return true if the user decide to convert the GameObject, will return false if the user decide not to convert the GameObject</result>
        bool TryEditingObject(GameObject go)
        {
            bool isObjectUsingAVS = go.GetComponent<PolybrushMesh>() != null;

            return EditorUtility.DisplayDialog("Mismatched application method", isObjectUsingAVS ?
                        "The object you are trying to edit doesn't support instanced mesh editing, do you want to convert it?" :
                        "The object you are trying to edit doesn't support additional vertex streams, do you want to convert it?", "Yes", "No");
        }

        /// <summary>
        /// Update the current brush object and weights with the current mouse position.
        /// </summary>
        /// <param name="mousePosition">current mouse position (from Event)</param>
        /// <param name="isDrag">optional, is dragging the mouse cursor</param>
        /// <param name="overridenGO">optional, provides an already selected gameobject (used in unit tests only)</param>
        /// <param name="overridenRay"> optional, provides a ray already created (used in unit tests only)</param>
        internal void UpdateBrush(Vector2 mousePosition, bool isUserHoldingControl = false, bool isUserHoldingShift = false, bool isDrag = false, GameObject overridenGO = null, Ray? overridenRay = null)
		{
            MirrorSettings mirrorSettings = m_BrushMirrorEditor.settings;
            // NOTE: Quick fix for the lockBrushToFirst feature, probably need to refactor
            // some code in order to be able to do it properly
            if(firstGameObject != null && s_LockBrushToFirst)
            {
                Ray mouseRay2 = overridenRay != null ? (Ray)overridenRay : HandleUtility.GUIPointToWorldRay(mousePosition);
                DoMeshRaycast(mouseRay2, brushTarget, mirrorSettings);
                OnBrushMove();
                SceneView.RepaintAll();
                Repaint();
                return;
            }

			// Must check HandleUtility.PickGameObject only during MouseMoveEvents or errors will rain.
			GameObject go = null;
			brushTarget = null;

            if (isDrag && s_LockBrushToFirst && m_LastHoveredGameObject != null)
            {
				go = m_LastHoveredGameObject;
				brushTarget = GetOrCreateBrushTarget(go);
			}
            else if (s_IgnoreUnselected || isDrag)
            {
				GameObject cur = null;
				int max = 0;	// safeguard against unforeseen while loop errors crashing unity

				do
				{
					int tmp;
                    // overloaded PickGameObject ignores array of GameObjects, this is used
                    // when there are non-selected gameObjects between the mouse and selected
                    // gameObjects.
                    cur = overridenGO;
                    if (cur == null)
                    {
                        m_IgnoreDrag.RemoveAll(x => x == null);
                        cur = HandleUtility.PickGameObject(mousePosition, m_IgnoreDrag.ToArray(), out tmp);
                    }

                    if (cur != null)
					{
                        if ( !PolyEditorUtility.InSelection(cur) )
						{
							if(!m_IgnoreDrag.Contains(cur))
								m_IgnoreDrag.Add(cur);
						}
						else
						{
							brushTarget = GetOrCreateBrushTarget(cur);

							if(brushTarget != null)
								go = cur;
							else
								m_IgnoreDrag.Add(cur);
						}
					}
				} while( go == null && cur != null && max++ < 128);
			}
			else
			{
                go = overridenGO;
                if(go == null)
                {
                    go = HandleUtility.PickGameObject(mousePosition, false);
                }

                if ( go != null && PolyEditorUtility.InSelection(go))
					brushTarget = GetOrCreateBrushTarget(go);
				else
					go = null;
			}

			bool mouseHoverTargetChanged = false;
            Ray mouseRay = overridenRay != null ? (Ray)overridenRay :  HandleUtility.GUIPointToWorldRay(mousePosition);
            // if the mouse hover picked up a valid editable, raycast against that.  otherwise
            // raycast all meshes in selection
            if (go == null)
			{
				foreach(var kvp in m_Hovering)
				{
					BrushTarget t = kvp.Value;

                    if (Util.IsValid(t) && DoMeshRaycast(mouseRay, t, mirrorSettings))
                    {
						brushTarget = t;
						go = t.gameObject;
						break;
					}
				}

			}
			else
			{
                if (!DoMeshRaycast(mouseRay, brushTarget, mirrorSettings))
                {
					if(!isDrag || !s_LockBrushToFirst)
					{
						go = null;
						brushTarget = null;
					}

					return;
				}
			}

			// if m_Hovering off another gameobject, call OnBrushExit on that last one and mark the
			// target as having been changed
			if( go != m_LastHoveredGameObject)
			{
				OnBrushExit(m_LastHoveredGameObject);
				mouseHoverTargetChanged = true;
				m_LastHoveredGameObject = go;
			}

            SceneView.RepaintAll();
            this.Repaint();

            if (brushTarget == null)
				return;

            brushSettings.isUserHoldingControl = isUserHoldingControl;
            brushSettings.isUserHoldingShift = isUserHoldingShift;

            if (mouseHoverTargetChanged)
			{
                OnBrushEnter(brushTarget, brushSettings);

				// brush is in use, adding a new object to the undo
				if(m_ApplyingBrush && !m_UndoQueue.Contains(go))
				{
					int curGroup = Undo.GetCurrentGroup();
					brushTarget.editableObject.isDirty = true;
					OnBrushBeginApply(brushTarget, brushSettings);
					Undo.CollapseUndoOperations(curGroup);
				}
			}

			OnBrushMove();

			SceneView.RepaintAll();
			this.Repaint();
		}

        /// <summary>
        /// Calculate the weights for this ray.
        /// </summary>
        /// <param name="mouseRay">The ray used to calculate weights</param>
        /// <param name="target">The object on which to calculate the weights</param>
        /// <returns>true if mouseRay hits the target, false otherwise</returns>
        bool DoMeshRaycast(Ray mouseRay, BrushTarget target, MirrorSettings mirrorSettings)
		{
			if( !Util.IsValid(target) )
				return false;

			target.ClearRaycasts();

			EditableObject editable = target.editableObject;

			s_Rays.Clear();
			s_Rays.Add(mouseRay);

			if(mirrorSettings.Axes != BrushMirror.None)
			{
				for(int i = 0; i < 3; i++)
				{
					if( ((uint)mirrorSettings.Axes & (1u << i)) < 1 )
						continue;

					int len = s_Rays.Count;

					for(int n = 0; n < len; n++)
					{
						Vector3 flipVec = ((BrushMirror)(1u << i)).ToVector3();

						if(mirrorSettings.Space == MirrorCoordinateSpace.World)
						{
							Vector3 cen = editable.gameObjectAttached.GetComponent<Renderer>().bounds.center;
							s_Rays.Add( new Ray(	Vector3.Scale(s_Rays[n].origin - cen, flipVec) + cen,
												Vector3.Scale(s_Rays[n].direction, flipVec)));
						}
						else
						{
							Transform t = SceneView.lastActiveSceneView.camera.transform;
							Vector3 o = t.InverseTransformPoint(s_Rays[n].origin);
							Vector3 d = t.InverseTransformDirection(s_Rays[n].direction);
							s_Rays.Add(new Ray( 	t.TransformPoint(Vector3.Scale(o, flipVec)),
												t.TransformDirection(Vector3.Scale(d, flipVec))));
						}
					}
				}
			}

			bool hitMesh = false;

			int[] triangles = editable.editMesh.GetTriangles();

			foreach(Ray ray in s_Rays)
			{
				PolyRaycastHit hit;

				if( PolySceneUtility.WorldRaycast(ray, editable.transform, editable.visualMesh.vertices, triangles, out hit) )
				{
					target.raycastHits.Add(hit);
					hitMesh = true;
				}
			}

			PolySceneUtility.CalculateWeightedVertices(target, brushSettings, tool, mode);

			return hitMesh;
		}

        /// <summary>
        /// Apply brush to current brush target
        /// </summary>
        /// <param name="isUserHoldingControl"></param>
        /// <param name="isUserHoldingShift"></param>
        internal void ApplyBrush(bool isUserHoldingControl, bool isUserHoldingShift)
		{
            if (!brushTarget.IsValid())
				return;

            brushSettings.isUserHoldingControl = isUserHoldingControl;
            brushSettings.isUserHoldingShift = isUserHoldingShift;

            if (!m_ApplyingBrush)
			{
				m_UndoQueue.Clear();
				m_ApplyingBrush = true;
                OnBrushBeginApply(brushTarget, brushSettings);
			}

			mode.OnBrushApply(brushTarget, brushSettings);

			SceneView.RepaintAll();
		}

		void OnBrushBeginApply(BrushTarget brushTarget, BrushSettings settings)
		{
			PolySceneUtility.PushGIWorkflowMode();
            firstGameObject = brushTarget.gameObject;
			mode.RegisterUndo(brushTarget);
			m_UndoQueue.Add(brushTarget.gameObject);
			mode.OnBrushBeginApply(brushTarget, brushSettings);
        }

        /// <summary>
        /// Modify the current brush settings depending on which key the user is pressing while scrolling
        /// </summary>
        /// <param name="e"></param>
		void ScrollBrushSettings(Event e)
		{
			float nrm = 1f;

			switch(e.modifiers)
			{
				case EventModifiers.Control:
					nrm = Mathf.Sin(Mathf.Max(.001f, brushSettings.normalizedRadius)) * .03f * (brushSettings.brushRadiusMax - brushSettings.brushRadiusMin);
					brushSettings.radius = brushSettings.radius - (e.delta.y * nrm);
					break;

				case EventModifiers.Shift:
					nrm = Mathf.Sin(Mathf.Max(.001f, brushSettings.falloff)) * .03f;
					brushSettings.falloff = brushSettings.falloff - e.delta.y * nrm;
					break;

				case EventModifiers.Control | EventModifiers.Shift:
					nrm = Mathf.Sin(Mathf.Max(.001f, brushSettings.strength)) * .03f;
					brushSettings.strength = brushSettings.strength - e.delta.y * nrm;
					break;

				default:
					return;
			}

			EditorUtility.SetDirty(brushSettings);

			if(mode != null)
			{
				UpdateBrush(Event.current.mousePosition, Event.current.control, Event.current.shift && Event.current.type != EventType.ScrollWheel);
				mode.OnBrushSettingsChanged(brushTarget, brushSettings);
			}

			e.Use();
			Repaint();
			SceneView.RepaintAll();
		}

		void OnSelectionChanged()
		{
            // We want to delete deselected gameObjects from this.m_Hovering
            var toDelete = new List<GameObject>();
            var selectionGameObjects = Selection.gameObjects;

            foreach (var hovering in m_Hovering.Keys)
                if (!selectionGameObjects.Contains(hovering))
                    toDelete.Add(hovering);

            foreach (var go in toDelete)
                m_Hovering.Remove(go);

			m_IgnoreDrag.Clear();
		}

		void OnBrushEnter(BrushTarget target, BrushSettings settings)
		{
			mode.OnBrushEnter(target.editableObject, settings);
		}

		void OnBrushMove()
		{
			if( Util.IsValid(brushTarget) )
				mode.OnBrushMove( brushTarget, brushSettings );
		}

		void OnBrushExit(GameObject go)
		{
			BrushTarget target;

			if(go == null || !m_Hovering.TryGetValue(go, out target) || !Util.IsValid(target))
				return;

            mode.OnBrushExit(target.editableObject);

			if(!m_ApplyingBrush)
				target.editableObject.Revert();
		}

		internal void OnFinishApplyingBrush()
		{
			PolySceneUtility.PopGIWorkflowMode();
            firstGameObject = null;
			m_ApplyingBrush = false;
			mode.OnBrushFinishApply(brushTarget, brushSettings);
			FinalizeAndResetHovering();
			m_IgnoreDrag.Clear();
		}

		void FinalizeAndResetHovering()
		{
			foreach(var kvp in m_Hovering)
			{
				BrushTarget target = kvp.Value;

				if(!Util.IsValid(target))
					continue;

				// if mesh hasn't been modified, revert it back
				// to the original mesh so that unnecessary assets
				// aren't allocated.  if it has been modified, let
				// the editableObject apply those changes to the
				// pb_Object if necessary.
				if(!target.editableObject.isDirty)
					target.editableObject.Revert();
				else
					target.editableObject.Apply(true, true);
			}

			m_Hovering.Clear();
			brushTarget = null;
			m_LastHoveredGameObject = null;

#if PROBUILDER_4_0_OR_NEWER
            if (ProBuilderBridge.ProBuilderExists())
                ProBuilderBridge.RefreshEditor(false);
#endif
            Repaint();
		}

        void UndoRedoPerformed()
        {
            mode.UndoRedoPerformed(m_UndoQueue);

            for (int i = 0; i < m_UndoQueue.Count; ++i)
            {
                if (m_Hovering.ContainsKey(m_UndoQueue[i]))
                {
                    m_Hovering[m_UndoQueue[i]].editableObject.RemovePolybrushComponentsIfNecessary();
                }
            }

            if (EditableObject.s_RebuildCollisions)
            {
                foreach (GameObject go in m_UndoQueue.Where(x => x != null))
                {
                    MeshCollider mc = go.GetComponent<MeshCollider>();
                    MeshFilter mf = go.GetComponent<MeshFilter>();

                    if (mc == null || mf == null || mf.sharedMesh == null)
                        continue;

                    if (PrefabUtility.IsPartOfPrefabInstance(go))
                    {
                        SerializedObject obj = new SerializedObject(mc);
                        SerializedProperty prop = obj.FindProperty("m_Mesh");
                        PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
                    }
                    else
                    {
                        mc.sharedMesh = null;
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
            }

            m_Hovering.Clear();
            brushTarget = null;
            m_LastHoveredGameObject = null;
            m_UndoQueue.Clear();

            SceneView.RepaintAll();
            DoRepaint();
        }
    }
}
