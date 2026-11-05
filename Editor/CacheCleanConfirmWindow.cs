using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Poyo.AssetCleaner.Editor
{
    /// <summary>
    /// キャッシュ掃除の二段階確認ウィンドウ。
    ///   第1段階: 説明と見積り（再掲）
    ///   第2段階: 最終確認。「時間がかかることを理解しました」チェックが入るまで実行ボタンを無効化する。
    /// 実行ボタンは既定でフォーカスされず（IMGUI）、Enter 連打で実行されない。キャンセルは常に可能。
    /// </summary>
    internal class CacheCleanConfirmWindow : EditorWindow
    {
        internal enum Mode { Reserve, CleanNow }

        private Mode _mode;
        private int _step = 1;              // 1 = 説明/見積り, 2 = 最終確認
        private bool _ack;                  // 「理解しました」チェックボックス
        private List<CacheClean.CacheTarget> _targets;
        private long _libSize;
        private int _assetCount;
        private long _assetSize;
        private double _measuredReimport;
        private Vector2 _scroll;

        public static void Open(Mode mode, List<CacheClean.CacheTarget> targets,
            long libSize, int assetCount, long assetSize, double measuredReimport)
        {
            var w = CreateInstance<CacheCleanConfirmWindow>();
            w.titleContent = new GUIContent(mode == Mode.Reserve
                ? AssetCleanerStrings.CacheReserveButton
                : AssetCleanerStrings.CacheCleanNowButton);
            w._mode = mode;
            w._targets = targets;
            w._libSize = libSize;
            w._assetCount = assetCount;
            w._assetSize = assetSize;
            w._measuredReimport = measuredReimport;
            w.minSize = new Vector2(560, 520);
            w.ShowModalUtility();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField(AssetCleanerStrings.CacheHeader, EditorStyles.boldLabel);

            // 目立つ警告
            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheWarnMain, MessageType.Warning);

            // 数値の根拠
            EditorGUILayout.HelpBox(string.Format(
                AssetCleanerStrings.CacheStatsFormat,
                AssetCleanerUtil.HumanSize(_libSize),
                _assetCount,
                AssetCleanerUtil.HumanSize(_assetSize)), MessageType.Info);

            // 所要時間の目安（実測があれば優先）
            if (_measuredReimport > 0)
            {
                EditorGUILayout.HelpBox(string.Format(
                    AssetCleanerStrings.CacheTimeEstimateMeasuredFormat,
                    AssetCleanerUtil.HumanDuration(_measuredReimport)), MessageType.Info);
            }
            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheTimeEstimateGeneric, MessageType.None);

            // 削除対象を全件列挙
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.CacheTargetsHeader, EditorStyles.boldLabel);
            foreach (var t in _targets.Where(t => t.Enabled))
            {
                var size = t.SizeBytes >= 0 ? AssetCleanerUtil.HumanSize(t.SizeBytes) : "未計測";
                EditorGUILayout.LabelField($"・{t.RelativePath}/   （{size}{(t.Exists ? "" : " / 無し")}）",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.LabelField(AssetCleanerStrings.CacheTargetsCsprojNote, EditorStyles.miniLabel);

            // 実行タイミング助言
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheTimingAdvice, MessageType.None);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (_step == 1) DrawStage1();
            else DrawStage2();
        }

        private void DrawStage1()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AssetCleanerStrings.Cancel, GUILayout.Height(28)))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button(AssetCleanerStrings.CacheNextButton, GUILayout.Height(28)))
                {
                    _step = 2;
                }
            }
        }

        private void DrawStage2()
        {
            EditorGUILayout.LabelField(AssetCleanerStrings.CacheConfirmStage2Title, EditorStyles.boldLabel);

            // チェックしなければ実行ボタンを有効化しない
            _ack = EditorGUILayout.ToggleLeft(AssetCleanerStrings.CacheAckCheckbox, _ack);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AssetCleanerStrings.Cancel, GUILayout.Height(28)))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
                using (new EditorGUI.DisabledScope(!_ack))
                {
                    var label = _mode == Mode.Reserve
                        ? AssetCleanerStrings.CacheReserveConfirmButton
                        : AssetCleanerStrings.CacheCleanNowConfirmButton;
                    if (GUILayout.Button(label, GUILayout.Height(28)))
                    {
                        Execute();
                    }
                }
            }
        }

        private void Execute()
        {
            CacheClean.Reserve(_targets);

            if (_mode == Mode.Reserve)
            {
                var pending = CacheClean.ReadPending();
                Close();
                EditorUtility.DisplayDialog(
                    AssetCleanerStrings.ToolName,
                    string.Format(AssetCleanerStrings.CacheReservedDialogFormat,
                        string.Format(AssetCleanerStrings.CacheReservedNoteFormat, pending?.reservedAt ?? "")),
                    AssetCleanerStrings.Ok);
                GUIUtility.ExitGUI();
            }
            else // CleanNow: 予約 + ヘルパー起動 + 即 Exit
            {
                CacheClean.SuppressQuitPrompt = true;
                // EditorApplication.Exit は quitting を確実には発火させないため、
                // ここでヘルパーを起動しておく（PID の終了を待つので Exit 前起動で問題ない）。
                CacheClean.LaunchHelper();
                Close();
                EditorApplication.Exit(0);
            }
        }
    }
}
