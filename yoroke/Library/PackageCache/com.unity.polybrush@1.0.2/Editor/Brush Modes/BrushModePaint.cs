﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Polybrush;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Vertex painter brush mode.
    /// </summary>
    internal class BrushModePaint : BrushModeMesh
	{
        [System.Serializable]
        struct VertexColorsPaintInfo
        {
            /// <summary>
            /// Copy of colors array loaded from active mesh.
            /// </summary>
            public Color32[] OriginalColors;

            /// <summary>
            /// Colors array used when applying brush.
            /// </summary>
            public Color32[] TargetColors;

            /// <summary>
            /// Colors array used when erasing color.
            /// Only used in "brush" mode. "Flood" and "Fill" modes will use white color.
            /// </summary>
            public Color32[] EraseColors;

            /// <summary>
            /// Current active colors applied on active mesh.
            /// </summary>
            public Color32[] Colors;

            /// <summary>
            /// Refresh this instance with new informations based on a given mesh vertex colors.
            /// </summary>
            /// <param name="baseColors">Vertex colors array from a given mesh. It'll be used to initialize every fields of this struct.</param>
            public void Build(Color32[] baseColors)
            {
                OriginalColors = baseColors;
                Colors = new Color32[baseColors.Length];
                TargetColors = new Color32[baseColors.Length];
                EraseColors = new Color32[baseColors.Length];
            }

            /// <summary>
            /// Refresh brush fields TargetColors and EraseColors based on a given color.
            /// </summary>
            /// <param name="color">New colors to apply in TargetColors and EraseColors fields.</param>
            /// <param name="strength">Brush strength to apply on <paramref name="color"/>.</param>
            /// <param name="mask">Selected channels on which we will apply <paramref name="color"/>.</param>
            public void RebuildColorTargets(Color color, float strength, ColorMask mask)
            {
                if (OriginalColors == null || TargetColors == null || OriginalColors.Length != TargetColors.Length)
                    return;

                for (int i = 0; i < OriginalColors.Length; i++)
                {
                    TargetColors[i] = Util.Lerp(OriginalColors[i], color, mask, strength);
                    EraseColors[i] = Util.Lerp(OriginalColors[i], s_WhiteColor, mask, strength);
                }
            }
        }

		// how many applications it should take to reach the full strength
		const float k_StrengthModifier = 1f/8f;
		static readonly Color32 s_WhiteColor = new Color32(255, 255, 255, 255);

		[SerializeField]
        internal PaintMode paintMode = PaintMode.Brush;

		[SerializeField]
        bool m_LikelySupportsVertexColors = false;

        [SerializeField]
        VertexColorsPaintInfo m_MeshVertexColors;

        [SerializeField]
        Color32 m_BrushColor = Color.green;

        // The current color palette.
        [SerializeField]
        ColorPalette m_ColorPalette = null;

        internal ColorMask mask = new ColorMask(true, true, true, true);

		ColorPalette[] m_AvailablePalettes = null;
		string[] m_AvailablePalettesAsString = null;
		int m_CurrentPaletteIndex = -1;

		// temp vars
		PolyEdge[] m_FillModeEdges = new PolyEdge[3];
		List<int> m_FillModeAdjacentTriangles = null;

		// used for fill mode
		Dictionary<PolyEdge, List<int>> m_TriangleLookup = null;

		internal GUIContent[] modeIcons = new GUIContent[]
		{
			new GUIContent("Brush", "Brush" ),
			new GUIContent("Fill", "Fill" ),
			new GUIContent("Flood", "Flood" )
		};

		internal ColorPalette colorPalette
		{
			get
			{
				if(m_ColorPalette == null)
					colorPalette = PolyEditorUtility.GetFirstOrNew<ColorPalette>();
				return m_ColorPalette;
			}
			set
			{
				m_ColorPalette = value;
			}
		}

        internal override bool SetDefaultSettings()
        {
            RefreshAvailablePalettes();
            ColorPalette defaultPalette = m_AvailablePalettes.FirstOrDefault(x => x.name.Contains("Default"));
            if (defaultPalette == null)
            {
                return false;
            }

            SetColorPalette(defaultPalette);

            //other settings
            paintMode = PaintMode.Brush;
            SetBrushColor(Color.red, 1f);
            mask = new ColorMask(true, true, true, true);

            return true;
        }

        // An Editor for the colorPalette.
        ColorPaletteEditor _colorPaletteEditor = null;

		ColorPaletteEditor colorPaletteEditor
		{
			get
			{
				if(_colorPaletteEditor == null || _colorPaletteEditor.target != colorPalette)
				{
					_colorPaletteEditor = (ColorPaletteEditor) Editor.CreateEditor(colorPalette);
					_colorPaletteEditor.hideFlags = HideFlags.HideAndDontSave;
				}

				return _colorPaletteEditor;
			}
		}

        /// <summary>
        /// The message that will accompany Undo commands for this brush.  Undo/Redo is handled by PolyEditor.
        /// </summary>
        internal override string UndoMessage { get { return "Paint Brush"; } }
		protected override string ModeSettingsHeader { get { return "Color Paint Settings"; } }
		protected override string DocsLink { get { return PrefUtility.documentationColorBrushLink; } }

		internal override void OnEnable()
		{
			base.OnEnable();

			RefreshAvailablePalettes();
            m_BrushColor = colorPalette.current;
		}

		internal override void OnDisable()
		{
			base.OnDisable();
			if(_colorPaletteEditor != null)
				Object.DestroyImmediate(_colorPaletteEditor);
		}

        /// <summary>
        /// Inspector GUI shown in the Editor window.  Base class shows BrushSettings by default
        /// </summary>
        /// <param name="brushSettings">Current brush settings</param>
        internal override void DrawGUI(BrushSettings brushSettings)
		{
			base.DrawGUI(brushSettings);

            using (new GUILayout.HorizontalScope())
            {
                if (colorPalette == null)
                    RefreshAvailablePalettes();

                EditorGUI.BeginChangeCheck();
                m_CurrentPaletteIndex = EditorGUILayout.Popup(m_CurrentPaletteIndex, m_AvailablePalettesAsString);
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_CurrentPaletteIndex >= m_AvailablePalettes.Length)
                        SetColorPalette(ColorPaletteEditor.AddNew());
                    else
                        SetColorPalette(m_AvailablePalettes[m_CurrentPaletteIndex]);
                }

                paintMode = (PaintMode)GUILayout.Toolbar((int)paintMode, modeIcons);
            }

			if(!m_LikelySupportsVertexColors)
				EditorGUILayout.HelpBox("It doesn't look like any of the materials on this object support vertex colors!", MessageType.Warning);

			colorPaletteEditor.onSelectIndex = (color) => { SetBrushColor(color, brushSettings.strength); };
			colorPaletteEditor.onSaveAs = SetColorPalette;

			mask = PolyGUILayout.ColorMaskField("Color Mask", mask);

			colorPaletteEditor.OnInspectorGUI();
		}

		internal void SetBrushColor(Color color, float strength)
		{
			m_BrushColor = color;
			m_MeshVertexColors.RebuildColorTargets(color, strength, mask);
		}

		internal void SetColorPalette(ColorPalette palette)
		{
			colorPalette = palette;
            m_BrushColor = colorPalette.current;

            RefreshAvailablePalettes();
        }

		internal override void OnBrushSettingsChanged(BrushTarget target, BrushSettings settings)
		{
			base.OnBrushSettingsChanged(target, settings);

            m_MeshVertexColors.RebuildColorTargets(m_BrushColor, settings.strength, mask);
		}

		// Called when the mouse begins hovering an editable object.
		internal override void OnBrushEnter(EditableObject target, BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);

			if(target.graphicsMesh == null)
				return;

			RebuildCaches(target, settings);

			m_TriangleLookup = PolyMeshUtility.GetAdjacentTriangles(target.editMesh);

			MeshRenderer mr = target.gameObjectAttached.GetComponent<MeshRenderer>();

			if(mr != null && mr.sharedMaterials != null)
				m_LikelySupportsVertexColors = mr.sharedMaterials.Any(x => x != null && x.shader != null && PolyShaderUtil.SupportsVertexColors(x.shader));
			else
				m_LikelySupportsVertexColors = false;
		}

		// Called whenever the brush is moved.  Note that @target may have a null editableObject.
		internal override void OnBrushMove(BrushTarget target, BrushSettings settings)
		{
			base.OnBrushMove(target, settings);

			if(!Util.IsValid(target))
				return;

			bool invert = settings.isUserHoldingControl;

			PolyMesh mesh = target.editableObject.editMesh;
			int vertexCount = mesh.vertexCount;
			float[] weights = target.GetAllWeights();

			switch(paintMode)
			{
				case PaintMode.Flood:
					for(int i = 0; i < vertexCount; i++)
                        m_MeshVertexColors.Colors[i] = invert? s_WhiteColor : m_MeshVertexColors.TargetColors[i];
					break;

				case PaintMode.Fill:

					System.Array.Copy(m_MeshVertexColors.OriginalColors, m_MeshVertexColors.Colors, vertexCount);
					int[] indices = target.editableObject.editMesh.GetTriangles();
					int index = 0;

					foreach(PolyRaycastHit hit in target.raycastHits)
					{
						if(hit.triangle > -1)
						{
							index = hit.triangle * 3;

                            m_MeshVertexColors.Colors[indices[index + 0]] = invert ? s_WhiteColor : m_MeshVertexColors.TargetColors[indices[index + 0]];
                            m_MeshVertexColors.Colors[indices[index + 1]] = invert ? s_WhiteColor : m_MeshVertexColors.TargetColors[indices[index + 1]];
                            m_MeshVertexColors.Colors[indices[index + 2]] = invert ? s_WhiteColor : m_MeshVertexColors.TargetColors[indices[index + 2]];

							m_FillModeEdges[0].x = indices[index+0];
							m_FillModeEdges[0].y = indices[index+1];

							m_FillModeEdges[1].x = indices[index+1];
							m_FillModeEdges[1].y = indices[index+2];

							m_FillModeEdges[2].x = indices[index+2];
							m_FillModeEdges[2].y = indices[index+0];

							for(int i = 0; i < 3; i++)
							{
								if(m_TriangleLookup.TryGetValue(m_FillModeEdges[i], out m_FillModeAdjacentTriangles))
								{
									for(int n = 0; n < m_FillModeAdjacentTriangles.Count; n++)
									{
										index = m_FillModeAdjacentTriangles[n] * 3;

                                        m_MeshVertexColors.Colors[indices[index + 0]] = invert ? s_WhiteColor : m_MeshVertexColors.TargetColors[indices[index + 0]];
                                        m_MeshVertexColors.Colors[indices[index + 1]] = invert ? s_WhiteColor : m_MeshVertexColors.TargetColors[indices[index + 1]];
                                        m_MeshVertexColors.Colors[indices[index + 2]] = invert ? s_WhiteColor : m_MeshVertexColors.TargetColors[indices[index + 2]];
									}
								}
							}
						}
					}

					break;

                default:
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        m_MeshVertexColors.Colors[i] = Util.Lerp(m_MeshVertexColors.OriginalColors[i],
                            invert ? m_MeshVertexColors.EraseColors[i] : m_MeshVertexColors.TargetColors[i],
                            mask,
                            weights[i]);
                    }

                    break;
                }
			}

			target.editableObject.editMesh.colors = m_MeshVertexColors.Colors;
			target.editableObject.ApplyMeshAttributes(MeshChannel.Color);
		}

		// Called when the mouse exits hovering an editable object.
		internal override void OnBrushExit(EditableObject target)
		{
			base.OnBrushExit(target);

			if(target.editMesh != null)
			{
				target.editMesh.colors = m_MeshVertexColors.OriginalColors;
				target.ApplyMeshAttributes(MeshChannel.Color);
			}

			m_LikelySupportsVertexColors = true;
		}

		// Called every time the brush should apply itself to a valid target.  Default is on mouse move.
		internal override void OnBrushApply(BrushTarget target, BrushSettings settings)
		{
			System.Array.Copy(m_MeshVertexColors.Colors, m_MeshVertexColors.OriginalColors, m_MeshVertexColors.Colors.Length);
			target.editableObject.editMesh.colors = m_MeshVertexColors.OriginalColors;
            target.editableObject.modifiedChannels |= MeshChannel.Color;

            base.OnBrushApply(target, settings);
		}

        /// <summary>
        /// set mesh colors back to their original state before registering for undo
        /// </summary>
        /// <param name="brushTarget">Target object of the brush</param>
        internal override void RegisterUndo(BrushTarget brushTarget)
		{
			brushTarget.editableObject.editMesh.colors = m_MeshVertexColors.OriginalColors;
			brushTarget.editableObject.ApplyMeshAttributes(MeshChannel.Color);

			base.RegisterUndo(brushTarget);
		}

		internal override void DrawGizmos(BrushTarget target, BrushSettings settings)
		{
			if(Util.IsValid(target) && paintMode == PaintMode.Fill)
			{
				Vector3[] vertices = target.editableObject.editMesh.vertices;
				int[] indices = target.editableObject.editMesh.GetTriangles();

				PolyHandles.PushMatrix();
				PolyHandles.PushHandleColor();

				Handles.matrix = target.transform.localToWorldMatrix;

				int index = 0;

				foreach(PolyRaycastHit hit in target.raycastHits)
				{
					if(hit.triangle > -1)
					{
						Handles.color = m_MeshVertexColors.TargetColors[indices[index]];

						index = hit.triangle * 3;

						Handles.DrawLine(vertices[indices[index+0]] + hit.normal * .1f, vertices[indices[index+1]] + hit.normal * .1f);
						Handles.DrawLine(vertices[indices[index+1]] + hit.normal * .1f, vertices[indices[index+2]] + hit.normal * .1f);
						Handles.DrawLine(vertices[indices[index+2]] + hit.normal * .1f, vertices[indices[index+0]] + hit.normal * .1f);

						m_FillModeEdges[0].x = indices[index+0];
						m_FillModeEdges[0].y = indices[index+1];

						m_FillModeEdges[1].x = indices[index+1];
						m_FillModeEdges[1].y = indices[index+2];

						m_FillModeEdges[2].x = indices[index+2];
						m_FillModeEdges[2].y = indices[index+0];

						for(int i = 0; i < 3; i++)
						{
							if(m_TriangleLookup.TryGetValue(m_FillModeEdges[i], out m_FillModeAdjacentTriangles))
							{
								for(int n = 0; n < m_FillModeAdjacentTriangles.Count; n++)
								{
									index = m_FillModeAdjacentTriangles[n] * 3;

									Handles.DrawLine(vertices[indices[index+0]] + hit.normal * .1f, vertices[indices[index+1]] + hit.normal * .1f);
									Handles.DrawLine(vertices[indices[index+1]] + hit.normal * .1f, vertices[indices[index+2]] + hit.normal * .1f);
									Handles.DrawLine(vertices[indices[index+2]] + hit.normal * .1f, vertices[indices[index+0]] + hit.normal * .1f);
								}
							}
						}
					}
				}

				PolyHandles.PopHandleColor();
				PolyHandles.PopMatrix();
			}
			else
			{
				base.DrawGizmos(target, settings);
			}
		}

        void RefreshAvailablePalettes()
        {
            m_AvailablePalettes = PolyEditorUtility.GetAll<ColorPalette>().ToArray();

            if (m_AvailablePalettes.Length < 1)
                colorPalette = PolyEditorUtility.GetFirstOrNew<ColorPalette>();

            m_AvailablePalettesAsString = m_AvailablePalettes.Select(x => x.name).ToArray();
            ArrayUtility.Add<string>(ref m_AvailablePalettesAsString, string.Empty);
            ArrayUtility.Add<string>(ref m_AvailablePalettesAsString, "Add Palette...");
            m_CurrentPaletteIndex = System.Array.IndexOf(m_AvailablePalettes, colorPalette);
        }

        void RebuildCaches(EditableObject target, BrushSettings settings)
        {
            PolyMesh m = target.editMesh;
            int vertexCount = m.vertexCount;
            Color32[] newBaseColors = null;

            if (m.colors != null && m.colors.Length == vertexCount)
                newBaseColors = Util.Duplicate(m.colors);
            else
                newBaseColors = Util.Fill<Color32>(x => { return Color.white; }, vertexCount);

            m_MeshVertexColors.Build(newBaseColors);
            m_MeshVertexColors.RebuildColorTargets(m_BrushColor, settings.strength, mask);
        }
    }
}
