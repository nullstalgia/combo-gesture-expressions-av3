﻿using System;
using System.Linq;
using System.Reflection;
using Hai.ComboGesture.Scripts.Components;
using Hai.ComboGesture.Scripts.Editor.EditorUI.Effectors;
using Hai.ComboGesture.Scripts.Editor.EditorUI.Layouts;
using Hai.ComboGesture.Scripts.Editor.EditorUI.Modules;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUIUtility;

namespace Hai.ComboGesture.Scripts.Editor.EditorUI
{
    public enum ActivityEditorMode
    {
        SetFaceExpressions,
        PreventEyesBlinking,
        AdditionalEditors,
        OtherOptions
    }

    public enum PuppetEditorMode
    {
        ManipulateTrees,
        PreventEyesBlinking,
        OtherOptions
    }

    public enum AdditionalEditorsMode
    {
        CreateBlendTrees,
        ViewBlendTrees,
        CombineFaceExpressions
    }

    public class CgeEditorWindow : EditorWindow
    {
        private const int RightSpace = 30 + 120 + 220;
        private CgeLayoutCommon _common;
        private CgeEditorEffector _editorEffector;
        private CgeLayoutPreventEyesBlinking _layoutPreventEyesBlinking;
        private CgeLayoutFaceExpressionCombiner _layoutFaceExpressionCombiner;
        private CgeLayoutOtherOptions _layoutOtherOptions;
        private CgeLayoutSetFaceExpressions _layoutSetFaceExpressions;
        private CgeLayoutManipulateTrees _layoutManipulateTrees;

        private Vector2 _scrollPos;
        private Texture _helpIcon16;
        public CgeWindowHandler WindowHandler { get; private set; }

        private void OnEnable()
        {
            _editorEffector = new CgeEditorEffector(new CgeEditorState());
            var blendTreeEffector = new CgeBlendTreeEffector();
            var memoization = Cge.Get().Memoization;
            var renderingCommands = new EeRenderingCommands();
            var activityPreviewQueryAggregator = new CgeActivityPreviewQueryAggregator(memoization, _editorEffector, blendTreeEffector, renderingCommands);
            var cgeMemoryQuery = new CgeMemoryQuery(memoization);
            _common = new CgeLayoutCommon(Repaint, _editorEffector, activityPreviewQueryAggregator, cgeMemoryQuery);
            var driver = new CgeActivityEditorDriver(_editorEffector);
            _layoutPreventEyesBlinking = new CgeLayoutPreventEyesBlinking(_common, _editorEffector);
            _layoutFaceExpressionCombiner = new CgeLayoutFaceExpressionCombiner(_common, driver, _editorEffector, renderingCommands, activityPreviewQueryAggregator);
            _layoutOtherOptions = new CgeLayoutOtherOptions(_common, _editorEffector, activityPreviewQueryAggregator);
            _layoutSetFaceExpressions = new CgeLayoutSetFaceExpressions(_common, driver, _layoutFaceExpressionCombiner /* FIXME it is not normal to inject the layout here */, _editorEffector, Repaint, blendTreeEffector);
            _layoutManipulateTrees = new CgeLayoutManipulateTrees(_common, _editorEffector, blendTreeEffector);

            WindowHandler = new CgeWindowHandler(this, _editorEffector);

            _common.GuiInit();
            _helpIcon16 = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Hai/ComboGesture/Icons/help-16.png");
        }

        private void OnInspectorUpdate()
        {
            var active = Selection.activeGameObject;
            if (active == null) return;

            TryNowEditingAnActivity(active);
            TryNowEditingAPuppet(active);
        }

        private void TryNowEditingAnActivity(GameObject active)
        {
            var selectedActivity = active.GetComponent<ComboGestureActivity>();
            if (selectedActivity != null && selectedActivity != _editorEffector.GetActivity())
            {
                WindowHandler.RetargetActivity(selectedActivity);
                Repaint();
            }
        }

        private void TryNowEditingAPuppet(GameObject active)
        {
            var selectedPuppet = active.GetComponent<ComboGesturePuppet>();
            if (selectedPuppet != null && selectedPuppet != _editorEffector.GetPuppet())
            {
                WindowHandler.RetargetPuppet(selectedPuppet);
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (_editorEffector.GetCurrentlyEditing() == CurrentlyEditing.Nothing)
            {
                return;
            }

            if (_editorEffector.IsFirstTimeSetup() && _editorEffector.IsPreviewSetupValid())
            {
                _editorEffector.ClearFirstTimeSetup();
            }

            GUILayout.BeginArea(new Rect(position.width - 320, CgeLayoutCommon.SingleLineHeight * 2 + 5, 200, CgeLayoutCommon.SingleLineHeight + 2));
            if (GUILayout.Button(new GUIContent("❈ ExpressionsEditor"), GUILayout.Width(170), GUILayout.Height(CgeLayoutCommon.SingleLineHeight + 2)))
            {
                ShowExpressionsEditor(_editorEffector);
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(position.width - 130, CgeLayoutCommon.SingleLineHeight * 2 + 5, 100, CgeLayoutCommon.SingleLineHeight + 2));
            if (GUILayout.Button(new GUIContent(" " + CgeLocale.CGEE_Tutorials, _helpIcon16), GUILayout.Width(100), GUILayout.Height(CgeLayoutCommon.SingleLineHeight + 2)))
            {
                Application.OpenURL(CgeLocale.DocumentationUrl());
            }
            GUILayout.EndArea();

            switch (_editorEffector.GetCurrentlyEditing())
            {
                case CurrentlyEditing.Nothing:
                    break;
                case CurrentlyEditing.Activity:
                    EditingAnActivity();
                    break;
                case CurrentlyEditing.Puppet:
                    EditingAPuppet();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void ShowExpressionsEditor(CgeEditorEffector cgeEditorEffector)
        {
            var visualExpressionsEditorType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .First(type => type.Name == "VisualExpressionsEditorWindow");
            visualExpressionsEditorType.GetMethod("OpenEditor", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] {new MenuCommand(cgeEditorEffector.PreviewSetup())});
        }

        private void EditingAnActivity()
        {
            _editorEffector.SpUpdate();

            CreateActivityToolbarArea();

            GUILayout.Space(singleLineHeight * 4);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(position.height - singleLineHeight * 4));
            switch (_editorEffector.CurrentActivityMode())
            {
                case ActivityEditorMode.PreventEyesBlinking:
                    _layoutPreventEyesBlinking.Layout(position);
                    break;
                case ActivityEditorMode.AdditionalEditors:
                    switch (_editorEffector.GetAdditionalEditor())
                    {
                        case AdditionalEditorsMode.CreateBlendTrees:
                            _layoutManipulateTrees.LayoutAssetCreator(position);
                            break;
                        case AdditionalEditorsMode.ViewBlendTrees:
                            _layoutManipulateTrees.LayoutTreeViewer(position);
                            break;
                        case AdditionalEditorsMode.CombineFaceExpressions:
                            _layoutFaceExpressionCombiner.Layout(Repaint);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case ActivityEditorMode.OtherOptions:
                    _layoutOtherOptions.Layout(Repaint, position);
                    break;
                // ReSharper disable once RedundantCaseLabel
                case ActivityEditorMode.SetFaceExpressions:
                default:
                    _layoutSetFaceExpressions.Layout(position);
                    break;
            }
            GUILayout.EndScrollView();

            _editorEffector.ApplyModifiedProperties();
        }

        private void CreateActivityToolbarArea()
        {
            GUILayout.BeginArea(new Rect(0, singleLineHeight, position.width, singleLineHeight * 3));
            _editorEffector.SwitchTo((ActivityEditorMode) GUILayout.Toolbar((int) _editorEffector.CurrentActivityMode(), new[]
            {
                CgeLocale.CGEE_Set_face_expressions, CgeLocale.CGEE_Prevent_eyes_blinking, CgeLocale.CGEE_Additional_editors, CgeLocale.CGEE_Other_options
            }));
            if (_editorEffector.CurrentActivityMode() == ActivityEditorMode.SetFaceExpressions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                if (_editorEffector.GetActivity().oneHandMode != ComboGestureActivity.CgeOneHandMode.Disabled)
                {
                    _editorEffector.SpEditorTool().intValue = GUILayout.Toolbar(_editorEffector.SpEditorTool().intValue, new[]
                    {
                        CgeLocale.CGEE_OneHandMode
                    }, GUILayout.ExpandWidth(true));
                }
                else if (!_editorEffector.GetActivity().enablePermutations)
                {
                    _editorEffector.SpEditorTool().intValue = GUILayout.Toolbar(_editorEffector.SpEditorTool().intValue, new[]
                    {
                        CgeLocale.CGEE_All_combos, CgeLocale.CGEE_Singles, CgeLocale.CGEE_Analog_Fist, CgeLocale.CGEE_Combos, CgeLocale.CGEE_Permutations, CgeLocale.CGEE_OneHandMode
                    }, GUILayout.ExpandWidth(true));
                }
                else
                {
                    _editorEffector.SpEditorTool().intValue = GUILayout.Toolbar(_editorEffector.SpEditorTool().intValue, new[] {CgeLocale.CGEE_Simplified_view, CgeLocale.CGEE_Complete_view, CgeLocale.CGEE_Permutations});
                }
                GUILayout.Space(RightSpace);
                GUILayout.EndHorizontal();
                _editorEffector.SwitchCurrentEditorToolTo(_editorEffector.SpEditorTool().intValue);
            }

            else if (_editorEffector.CurrentActivityMode() == ActivityEditorMode.AdditionalEditors)
            {
                CreateAdditionalEditorsToolbar();
            }

            GUILayout.EndArea();
        }

        private void EditingAPuppet()
        {
            _editorEffector.SpUpdate();

            CreatePuppetToolbarArea();

            GUILayout.Space(singleLineHeight * 4);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(position.height - singleLineHeight * 4));
            switch (_editorEffector.CurrentPuppetMode())
            {
                case PuppetEditorMode.PreventEyesBlinking:
                    _layoutPreventEyesBlinking.Layout(position);
                    break;
                case PuppetEditorMode.OtherOptions:
                    _layoutOtherOptions.Layout(Repaint, position);
                    break;
                // ReSharper disable once RedundantCaseLabel
                case PuppetEditorMode.ManipulateTrees:
                default:
                    _layoutManipulateTrees.Layout(position);
                    break;
            }
            GUILayout.EndScrollView();

            _editorEffector.ApplyModifiedProperties();
        }

        private void CreatePuppetToolbarArea()
        {
            GUILayout.BeginArea(new Rect(0, singleLineHeight, position.width, singleLineHeight * 3));
            _editorEffector.SwitchTo((PuppetEditorMode) GUILayout.Toolbar((int) _editorEffector.CurrentPuppetMode(), new[]
            {
                CgeLocale.CGEE_Manipulate_trees, CgeLocale.CGEE_Prevent_eyes_blinking, CgeLocale.CGEE_Other_options
            }));
            GUILayout.EndArea();
        }

        private void CreateAdditionalEditorsToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            _editorEffector.SwitchAdditionalEditorTo((AdditionalEditorsMode)GUILayout.Toolbar((int)_editorEffector.GetAdditionalEditor(), new[]
            {
                CgeLocale.CGEE_Create_blend_trees, CgeLocale.CGEE_View_blend_trees, CgeLocale.CGEE_Combine_expressions
            }, GUILayout.ExpandWidth(true)));

            GUILayout.Space(RightSpace);
            GUILayout.EndHorizontal();
        }
    }
}
