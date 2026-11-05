using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Poyo.AssetCleaner.Editor
{
    /// <summary>
    /// メインウィンドウ。Roots / Protection / Scan Result の3セクション。
    /// スキャンは常に非破壊。退避は明示的なボタン + 確認ダイアログでのみ実行する。
    /// </summary>
    public class AssetCleanerWindow : EditorWindow
    {
        private enum Tab { Roots, Protection, Scan, Duplicates, Cache, About }

        private const int CandidateGroupCollapseThreshold = 8;

        private AssetCleanerSettings _settings;
        private Tab _tab = Tab.Roots;
        private Vector2 _scroll;

        // Roots
        private Object _individualToAdd;
        private List<string> _autoDetected = new List<string>();
        private bool _autoDetectRun;
        private RootSet _inspectedRoots;
        private bool _implicitRootsFoldout = false;

        // Protection
        private string _newGlob = "";

        // Scan
        private ScanResult _result;
        private bool _scanBlocked;
        private bool _scanTargetsFoldout;
        private bool _scanOptionsFoldout;
        private bool _protectedFoldout = false;
        private bool _implicitOnlyUsedFoldout = false;
        private readonly Dictionary<int, bool> _candidateGroupFoldouts = new Dictionary<int, bool>();
        private List<TrashFolderInfo> _trashFolders = new List<TrashFolderInfo>();

        // Duplicates
        private DuplicateReport _dupReport;

        // Cache
        private List<CacheClean.CacheTarget> _cacheTargets;
        private bool _cacheMeasured;
        private long _cacheLibSize;
        private int _cacheAssetCount;
        private long _cacheAssetSize;

        [MenuItem(AssetCleanerStrings.MenuPath)]
        private static void Open()
        {
            var window = GetWindow<AssetCleanerWindow>();
            window.titleContent = new GUIContent(AssetCleanerStrings.WindowTitle);
            window.minSize = new Vector2(640, 480);
            window.Show();
        }

        private void OnEnable()
        {
            _settings = AssetCleanerSettings.GetOrCreate();
            RefreshTrashFolders();
        }

        private void OnGUI()
        {
            if (_settings == null) _settings = AssetCleanerSettings.GetOrCreate();

            EditorGUILayout.LabelField(AssetCleanerStrings.ToolName, EditorStyles.boldLabel);
            DrawTabBar();
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Roots: DrawRootsTab(); break;
                case Tab.Protection: DrawProtectionTab(); break;
                case Tab.Scan: DrawScanTab(); break;
                case Tab.Duplicates: DrawDuplicatesTab(); break;
                case Tab.Cache: DrawCacheTab(); break;
                case Tab.About: DrawAboutTab(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawTabBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var previousTab = _tab;
                _tab = (Tab)GUILayout.Toolbar((int)_tab, new[]
                {
                    AssetCleanerStrings.TabRoots, AssetCleanerStrings.TabProtection, AssetCleanerStrings.TabScan,
                    AssetCleanerStrings.TabDuplicates, AssetCleanerStrings.TabCache, AssetCleanerStrings.TabAbout,
                });
                if (_tab == Tab.Scan && previousTab != Tab.Scan) RefreshTrashFolders();
            }
        }

        // ------------------------------------------------------------------ Roots

        private void DrawRootsTab()
        {
            EditorGUILayout.LabelField(AssetCleanerStrings.RootsHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.RootsHelp, MessageType.Info);

            DrawDirectoryDropArea(_settings.avatarRootDirectories, AssetCleanerStrings.RootsDropArea, false);

            if (GUILayout.Button(AssetCleanerStrings.RootsSelectFolder))
            {
                var abs = EditorUtility.OpenFolderPanel(AssetCleanerStrings.RootsSelectFolder, Application.dataPath, "");
                var assetPath = AbsToAssetPath(abs);
                if (assetPath != null) AddDirectory(_settings.avatarRootDirectories, assetPath, false);
            }

            DrawDirectoryList(_settings.avatarRootDirectories, AssetCleanerStrings.RootsUnregistered);

            // 個別ファイル追加
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.RootsAddIndividualHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.RootsAddIndividualHelp, MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                _individualToAdd = EditorGUILayout.ObjectField(_individualToAdd, typeof(Object), false);
                using (new EditorGUI.DisabledScope(_individualToAdd == null))
                {
                    if (GUILayout.Button(AssetCleanerStrings.AddButton, GUILayout.Width(60)))
                    {
                        var p = AssetDatabase.GetAssetPath(_individualToAdd);
                        var ext = AssetCleanerUtil.Ext(p);
                        if (ext == ".prefab" || ext == ".unity")
                        {
                            if (!_settings.additionalRootAssets.Contains(p))
                            {
                                _settings.additionalRootAssets.Add(p);
                                _settings.Save();
                            }
                            _individualToAdd = null;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                                AssetCleanerStrings.RootsInvalidIndividualAsset, AssetCleanerStrings.Ok);
                        }
                    }
                }
            }
            DrawPathList(_settings.additionalRootAssets, AssetCleanerStrings.RootsUnregistered);

            // 自動検出
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.RootsAutoDetectHeader, EditorStyles.boldLabel);
            if (GUILayout.Button(AssetCleanerStrings.RootsAutoDetectButton))
            {
                _autoDetected = AvatarAutoDetector.FindAvatarPrefabs();
                _autoDetectRun = true;
            }
            if (_autoDetectRun)
            {
                if (_autoDetected.Count == 0)
                {
                    EditorGUILayout.HelpBox(AssetCleanerStrings.RootsAutoDetectNone, MessageType.None);
                }
                else
                {
                    EditorGUILayout.LabelField(AssetCleanerStrings.RootsAutoDetectExcludeHint, EditorStyles.miniLabel);
                    var excluded = _settings.excludedAutoDetectedRoots;
                    foreach (var p in _autoDetected)
                    {
                        bool included = !excluded.Contains(p);
                        bool newIncluded = EditorGUILayout.ToggleLeft(p, included);
                        if (newIncluded != included)
                        {
                            if (newIncluded) excluded.Remove(p);
                            else if (!excluded.Contains(p)) excluded.Add(p);
                            _settings.Save();
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            string implicitHeader = AssetCleanerStrings.RootsImplicitHeaderNotInspected;
            if (_inspectedRoots != null)
            {
                int implicitCount = _inspectedRoots.ImplicitRoots.Count +
                    _inspectedRoots.ExcludedImplicitRoots.Count;
                implicitHeader = string.Format(AssetCleanerStrings.RootsImplicitHeaderFormat, implicitCount);
            }
            _implicitRootsFoldout = EditorGUILayout.Foldout(_implicitRootsFoldout,
                implicitHeader);
            if (_implicitRootsFoldout)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.RootsImplicitHelp, MessageType.Info);
                if (GUILayout.Button(AssetCleanerStrings.RootsImplicitInspect))
                    _inspectedRoots = RootCollector.Collect(_settings);
                if (_inspectedRoots == null)
                {
                    EditorGUILayout.LabelField(AssetCleanerStrings.RootsImplicitNotInspected, EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(AssetCleanerStrings.RootsAutoDetectExcludeHint, EditorStyles.miniLabel);
                    var allImplicit = _inspectedRoots.ImplicitRoots
                        .Concat(_inspectedRoots.ExcludedImplicitRoots).Distinct().OrderBy(p => p).ToList();
                    foreach (var p in allImplicit)
                    {
                        bool included = !_settings.excludedImplicitRoots.Contains(p);
                        bool newIncluded = EditorGUILayout.ToggleLeft(p, included);
                        if (newIncluded == included) continue;
                        if (newIncluded) _settings.excludedImplicitRoots.Remove(p);
                        else if (!_settings.excludedImplicitRoots.Contains(p)) _settings.excludedImplicitRoots.Add(p);
                        _settings.Save();
                    }
                }
            }
        }

        private void DrawDirectoryDropArea(List<string> directories, string label, bool assetsOnly)
        {
            var rect = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            GUI.Box(rect, label, EditorStyles.helpBox);

            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        var p = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
                        {
                            AddDirectory(directories, p, assetsOnly);
                        }
                    }
                }
                evt.Use();
            }
        }

        private void AddDirectory(List<string> directories, string assetPath, bool assetsOnly)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                    string.Format(AssetCleanerStrings.FolderRequiredFormat, assetPath), AssetCleanerStrings.Ok);
                return;
            }
            if (assetsOnly && assetPath != "Assets" && !assetPath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                    AssetCleanerStrings.AssetsFolderRequired, AssetCleanerStrings.Ok);
                return;
            }
            if (!directories.Contains(assetPath))
            {
                directories.Add(assetPath);
                _settings.Save();
            }
        }

        private void DrawDirectoryList(List<string> directories, string emptyLabel)
        {
            DrawPathList(directories, emptyLabel);
        }

        private void DrawPathList(List<string> paths, string emptyLabel)
        {
            EditorGUILayout.Space();
            int removeIndex = -1;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(string.Format(
                    AssetCleanerStrings.RegisteredItemsFormat, paths.Count), EditorStyles.miniBoldLabel);
                if (paths.Count == 0)
                {
                    EditorGUILayout.LabelField(emptyLabel, EditorStyles.miniLabel);
                }
                else
                {
                    Color separatorColor = EditorGUIUtility.isProSkin
                        ? new Color(1f, 1f, 1f, 0.10f)
                        : new Color(0f, 0f, 0f, 0.12f);
                    for (int i = 0; i < paths.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(new GUIContent(paths[i], paths[i]),
                                    EditorStyles.linkLabel, GUILayout.ExpandWidth(true)))
                                PingPath(paths[i]);
                            if (GUILayout.Button(AssetCleanerStrings.RemoveButton, GUILayout.Width(60)))
                                removeIndex = i;
                        }
                        if (i < paths.Count - 1)
                        {
                            var separator = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                            EditorGUI.DrawRect(separator, separatorColor);
                        }
                    }
                }
            }
            if (removeIndex >= 0)
            {
                paths.RemoveAt(removeIndex);
                _settings.Save();
            }
            EditorGUILayout.Space();
        }

        // ------------------------------------------------------------- Protection

        private void DrawProtectionTab()
        {
            EditorGUILayout.LabelField(AssetCleanerStrings.ProtHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.ProtHelp, MessageType.Info);

            EditorGUILayout.LabelField(AssetCleanerStrings.ProtStructureHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(AssetCleanerStrings.ProtStructureDetail, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.ProtToolListHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                string.Join(", ", ProtectionRules.DefaultToolNames),
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.ProtExtHeader, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                string.Join(" ", ProtectionRules.AlwaysProtectedExtensions),
                EditorStyles.wordWrappedMiniLabel);

            // ユーザーホワイトリスト
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.ProtWhitelistHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.ProtWhitelistHelp, MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newGlob = EditorGUILayout.TextField(_newGlob);
                if (GUILayout.Button(AssetCleanerStrings.ProtWhitelistAdd, GUILayout.Width(60)))
                {
                    AddWhitelistGlob(_newGlob);
                    _newGlob = "";
                    GUI.FocusControl(null);
                }
            }
            for (int i = _settings.userWhitelistGlobs.Count - 1; i >= 0; i--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(_settings.userWhitelistGlobs[i]);
                    if (GUILayout.Button(AssetCleanerStrings.RemoveButton, GUILayout.Width(60)))
                    {
                        _settings.userWhitelistGlobs.RemoveAt(i);
                        _settings.Save();
                    }
                }
            }
        }

        private void AddWhitelistGlob(string glob)
        {
            glob = glob?.Trim();
            if (string.IsNullOrEmpty(glob)) return;
            if (!_settings.userWhitelistGlobs.Contains(glob))
            {
                _settings.userWhitelistGlobs.Add(glob);
                _settings.Save();
            }
        }

        // ------------------------------------------------------------------- Scan

        private void DrawScanTab()
        {
            _scanTargetsFoldout = EditorGUILayout.Foldout(
                _scanTargetsFoldout, AssetCleanerStrings.ScanTargetsFoldout, true);
            if (_scanTargetsFoldout)
            {
                EditorGUILayout.LabelField(AssetCleanerStrings.ScanScopeHeader, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(AssetCleanerStrings.ScanScopeHelp, MessageType.Info);
                DrawDirectoryDropArea(_settings.scanScopeDirectories, AssetCleanerStrings.ScanScopeDropArea, true);
                if (GUILayout.Button(AssetCleanerStrings.ScanScopeSelectFolder))
                {
                    var abs = EditorUtility.OpenFolderPanel(
                        AssetCleanerStrings.ScanScopeSelectFolder, Application.dataPath, "");
                    var assetPath = AbsToAssetPath(abs);
                    if (assetPath != null)
                        AddDirectory(_settings.scanScopeDirectories, assetPath, true);
                }
                DrawDirectoryList(_settings.scanScopeDirectories, AssetCleanerStrings.ScanScopeUnconfigured);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(AssetCleanerStrings.ScanToolOutputHeader, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(AssetCleanerStrings.ScanToolOutputHelp, MessageType.Info);
                DrawDirectoryDropArea(
                    _settings.toolOutputDirectories, AssetCleanerStrings.ScanToolOutputDropArea, true);
                if (GUILayout.Button(AssetCleanerStrings.ScanToolOutputSelectFolder))
                {
                    var abs = EditorUtility.OpenFolderPanel(
                        AssetCleanerStrings.ScanToolOutputSelectFolder, Application.dataPath, "");
                    var assetPath = AbsToAssetPath(abs);
                    if (assetPath != null)
                        AddDirectory(_settings.toolOutputDirectories, assetPath, true);
                }
                DrawDirectoryList(
                    _settings.toolOutputDirectories, AssetCleanerStrings.ScanToolOutputUnconfigured);
            }

            EditorGUILayout.Space();
            _scanOptionsFoldout = EditorGUILayout.Foldout(
                _scanOptionsFoldout, AssetCleanerStrings.ScanOptionsFoldout, true);
            if (_scanOptionsFoldout)
            {
                // 判定粒度設定
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _settings.autoEstimateGranularity = EditorGUILayout.ToggleLeft(
                        AssetCleanerStrings.ScanAutoGranularityToggle,
                        _settings.autoEstimateGranularity, GUILayout.Width(220));
                    using (new EditorGUI.DisabledScope(_settings.autoEstimateGranularity))
                    {
                        EditorGUILayout.LabelField(
                            AssetCleanerStrings.ScanFixedDepthLabel, GUILayout.Width(60));
                        _settings.granularityDepth = Mathf.Max(1,
                            EditorGUILayout.IntField(
                                _settings.granularityDepth, GUILayout.Width(40)));
                    }
                    if (EditorGUI.EndChangeCheck()) _settings.Save();
                }

                // ファイル単位モード
                EditorGUI.BeginChangeCheck();
                _settings.fileUnitMode = EditorGUILayout.ToggleLeft(
                    AssetCleanerStrings.ScanFileUnitToggle, _settings.fileUnitMode);
                if (EditorGUI.EndChangeCheck()) _settings.Save();
                if (_settings.fileUnitMode)
                {
                    EditorGUILayout.HelpBox(AssetCleanerStrings.ScanFileUnitHelp, MessageType.Warning);
                }
            }

            DrawScanSectionHeader(AssetCleanerStrings.ScanExecuteHeader);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AssetCleanerStrings.ScanButton, GUILayout.Height(28)))
                {
                    RunScan();
                }
            }

            DrawScanSectionHeader(AssetCleanerStrings.ScanResultsHeader);
            if (_scanBlocked)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.ScanNoRootsError, MessageType.Error);
            }
            else if (_result == null)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.ScanNotRunYet, MessageType.Info);
            }
            else
            {
                DrawScanResult();
            }

            DrawTrashFoldersSection();
        }

        private static void DrawScanSectionHeader(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.Box(GUIContent.none, GUI.skin.horizontalSlider,
                GUILayout.ExpandWidth(true), GUILayout.Height(1));
            EditorGUILayout.Space();
        }

        private void RunScan()
        {
            _result = null;
            _scanBlocked = false;
            _implicitOnlyUsedFoldout = false;
            _candidateGroupFoldouts.Clear();
            try
            {
                EditorUtility.DisplayProgressBar(
                    AssetCleanerStrings.ProgressTitle, AssetCleanerStrings.ProgressCollectRoots, 0.05f);
                var roots = RootCollector.Collect(_settings);

                // ルート未設定ガード
                if (roots.HasNoAvatarRoots)
                {
                    _scanBlocked = true;
                    return;
                }

                _result = UnusedAssetScanner.Scan(_settings, roots);
                _settings.lastScanTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _settings.Save();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void DrawScanResult()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format(
                AssetCleanerStrings.ScanSummaryFormat,
                _result.Candidates.Count,
                AssetCleanerUtil.HumanSize(_result.TotalCandidateSize),
                _result.UsedUnits, _result.ProtectedCount, _result.RootCount),
                EditorStyles.boldLabel);

            if (_result.ScopeDirectories.Count > 0)
            {
                EditorGUILayout.LabelField(string.Format(AssetCleanerStrings.ScanScopeSummaryFormat,
                    string.Join("、", _result.ScopeDirectories), _result.OutOfScopeUnits));
                if (_result.TotalUnits == 0 && _result.OutOfScopeUnits > 0)
                    EditorGUILayout.HelpBox(AssetCleanerStrings.ScanScopeNoUnitsWarning, MessageType.Warning);
            }

            DrawProtectedEntries();
            DrawImplicitOnlyUsedEntries();

            if (_result.Candidates.Count == 0)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.ScanEmpty, MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AssetCleanerStrings.ScanSelectAll, GUILayout.Width(80)))
                    foreach (var e in _result.Candidates) e.Selected = true;
                if (GUILayout.Button(AssetCleanerStrings.ScanSelectNone, GUILayout.Width(80)))
                    foreach (var e in _result.Candidates) e.Selected = false;
            }

            // ヘッダ行
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(AssetCleanerStrings.ScanColSelect, GUILayout.Width(36));
                GUILayout.Label(AssetCleanerStrings.ScanColPath, GUILayout.ExpandWidth(true));
                GUILayout.Label(AssetCleanerStrings.ScanColSize, GUILayout.Width(80));
                GUILayout.Label(AssetCleanerStrings.ScanColType, GUILayout.Width(80));
                GUILayout.Label(AssetCleanerStrings.ScanColReason, GUILayout.Width(140));
                GUILayout.Label(AssetCleanerStrings.ScanColProtect, GUILayout.Width(90));
            }

            foreach (var group in _result.Candidates.GroupBy(e => e.GroupId))
            {
                var members = group.ToList();
                if (members.Count == 1)
                {
                    DrawCandidateRow(members[0], members, false);
                    continue;
                }

                EditorGUILayout.Space();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool expanded = DrawCandidateGroupHeader(group.Key, members);
                    if (expanded)
                    {
                        bool hasGenerated = members.Any(IsToolOutputUnit);
                        bool hasSource = members.Any(m => !IsToolOutputUnit(m));
                        bool showRoles = hasGenerated && hasSource;
                        foreach (var e in members)
                        {
                            DrawCandidateRow(e, members, showRoles);
                        }
                    }
                }
                EditorGUILayout.Space();
            }

            // 選択サマリ
            var selected = _result.Candidates.Where(c => c.Selected).ToList();
            long selectedSize = selected.Sum(c => c.SizeBytes);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format(
                AssetCleanerStrings.ScanSelectedSummaryFormat, selected.Count, AssetCleanerUtil.HumanSize(selectedSize)));

            // 退避前 .unitypackage バックアップ
            EditorGUI.BeginChangeCheck();
            _settings.exportUnityPackageBeforeRelocate = EditorGUILayout.ToggleLeft(
                AssetCleanerStrings.RelocateExportPackageToggle, _settings.exportUnityPackageBeforeRelocate);
            if (EditorGUI.EndChangeCheck()) _settings.Save();
            if (_settings.exportUnityPackageBeforeRelocate)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.RelocateExportPackageHelp, MessageType.None);
            }

            using (new EditorGUI.DisabledScope(selected.Count == 0))
            {
                if (GUILayout.Button(AssetCleanerStrings.RelocateButton, GUILayout.Height(28)))
                {
                    Relocate(selected, selectedSize);
                }
            }

        }

        private void DrawProtectedEntries()
        {
            if (_result.ProtectedEntries.Count == 0) return;
            _protectedFoldout = EditorGUILayout.Foldout(_protectedFoldout,
                string.Format(AssetCleanerStrings.ScanProtectedFoldoutFormat, _result.ProtectedEntries.Count));
            if (!_protectedFoldout) return;
            EditorGUILayout.HelpBox(AssetCleanerStrings.ScanProtectedHelp, MessageType.Info);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(AssetCleanerStrings.ScanColPath, GUILayout.ExpandWidth(true));
                GUILayout.Label(AssetCleanerStrings.ScanColCount, GUILayout.Width(70));
                GUILayout.Label(AssetCleanerStrings.ScanColSize, GUILayout.Width(80));
                GUILayout.Label(AssetCleanerStrings.ScanColReason, GUILayout.Width(220));
            }
            foreach (var e in _result.ProtectedEntries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent(e.UnitPath, e.UnitPath), EditorStyles.linkLabel,
                            GUILayout.ExpandWidth(true))) PingPath(e.UnitPath);
                    GUILayout.Label(string.Format(AssetCleanerStrings.ScanFileCountFormat, e.FileCount), GUILayout.Width(70));
                    GUILayout.Label(AssetCleanerUtil.HumanSize(e.SizeBytes), GUILayout.Width(80));
                    GUILayout.Label(e.Reason, GUILayout.Width(220));
                }
            }
        }

        private void DrawImplicitOnlyUsedEntries()
        {
            if (_result.ImplicitOnlyUsedEntries.Count == 0) return;
            _implicitOnlyUsedFoldout = EditorGUILayout.Foldout(_implicitOnlyUsedFoldout,
                string.Format(AssetCleanerStrings.ScanImplicitOnlyFoldoutFormat,
                    _result.ImplicitOnlyUsedEntries.Count));
            if (!_implicitOnlyUsedFoldout) return;

            EditorGUILayout.HelpBox(AssetCleanerStrings.ScanImplicitOnlyHelp, MessageType.Info);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(AssetCleanerStrings.ScanColPath, GUILayout.ExpandWidth(true));
                GUILayout.Label(AssetCleanerStrings.ScanColCount, GUILayout.Width(70));
                GUILayout.Label(AssetCleanerStrings.ScanColSize, GUILayout.Width(80));
                GUILayout.Label(AssetCleanerStrings.ScanColImplicitRoots, GUILayout.Width(260));
                GUILayout.Space(90);
            }
            foreach (var entry in _result.ImplicitOnlyUsedEntries)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent(entry.UnitPath, entry.UnitPath),
                            EditorStyles.linkLabel, GUILayout.ExpandWidth(true))) PingPath(entry.UnitPath);
                    GUILayout.Label(string.Format(AssetCleanerStrings.ScanFileCountFormat, entry.FileCount),
                        GUILayout.Width(70));
                    GUILayout.Label(AssetCleanerUtil.HumanSize(entry.SizeBytes), GUILayout.Width(80));
                    string roots = entry.PinnedByImplicitRoots.Count > 0
                        ? string.Join("\n", entry.PinnedByImplicitRoots)
                        : (_result.ImplicitRootAttributionSkipped
                            ? AssetCleanerStrings.ScanImplicitRootsAttributionSkipped
                            : AssetCleanerStrings.ScanImplicitRootsNotIdentified);
                    GUILayout.Label(roots, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(260));
                    using (new EditorGUI.DisabledScope(entry.PinnedByImplicitRoots.Count == 0))
                    {
                        if (GUILayout.Button(AssetCleanerStrings.ScanExcludeImplicitRoots, GUILayout.Width(90)))
                        {
                            foreach (var implicitRoot in entry.PinnedByImplicitRoots)
                            {
                                if (!_settings.excludedImplicitRoots.Contains(implicitRoot))
                                    _settings.excludedImplicitRoots.Add(implicitRoot);
                            }
                            _settings.Save();
                            RunScan();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        private void DrawCandidateRow(
            ScanResultEntry entry,
            List<ScanResultEntry> members,
            bool showRoles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool rowSelected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(36));
                if (rowSelected != entry.Selected)
                    foreach (var member in members) member.Selected = rowSelected;

                string tooltip = entry.UnitPath;
                if (entry.ReferencedByUnits.Count > 0)
                    tooltip += "\n\n" + string.Format(AssetCleanerStrings.ScanReferencedByTooltipFormat,
                        string.Join("\n", entry.ReferencedByUnits));
                if (members.Count > 1) GUILayout.Space(16);
                if (showRoles)
                {
                    bool generated = IsToolOutputUnit(entry);
                    GUILayout.Label(generated ? AssetCleanerStrings.ScanGroupGeneratedAsset :
                        AssetCleanerStrings.ScanGroupSourceAsset,
                        generated ? EditorStyles.miniLabel : EditorStyles.boldLabel,
                        GUILayout.Width(70));
                }
                if (GUILayout.Button(new GUIContent(entry.UnitPath, tooltip),
                        EditorStyles.linkLabel, GUILayout.ExpandWidth(true))) PingPath(entry.UnitPath);

                GUILayout.Label(AssetCleanerUtil.HumanSize(entry.SizeBytes), GUILayout.Width(80));
                GUILayout.Label(new GUIContent(AssetCleanerUtil.KindLabel(entry.Kind), entry.KindDetail),
                    GUILayout.Width(80));
                GUILayout.Label(entry.Reason, GUILayout.Width(140));

                if (GUILayout.Button(AssetCleanerStrings.ScanProtectButton, GUILayout.Width(90)))
                {
                    AddWhitelistGlob(entry.UnitPath + "/**");
                    AddWhitelistGlob(entry.UnitPath);
                    RunScan();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private bool DrawCandidateGroupHeader(int groupId, List<ScanResultEntry> members)
        {
            if (!_candidateGroupFoldouts.TryGetValue(groupId, out var expanded))
                expanded = members.Count <= CandidateGroupCollapseThreshold;
            bool selected = members.Any(m => m.Selected);
            long size = members.Sum(m => m.SizeBytes);
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var foldoutRect = GUILayoutUtility.GetRect(
                        16, EditorGUIUtility.singleLineHeight, GUILayout.Width(16));
                    expanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, true);
                    bool newSelected = EditorGUILayout.ToggleLeft(string.Format(
                        AssetCleanerStrings.ScanGroupHeaderFormat, members.Count, AssetCleanerUtil.HumanSize(size)), selected);
                    if (newSelected != selected)
                        foreach (var member in members) member.Selected = newSelected;
                }
                bool hasToolOutput = members.Any(m => _settings.toolOutputDirectories.Any(o =>
                    m.UnitPath == o || m.UnitPath.StartsWith(o.TrimEnd('/') + "/", System.StringComparison.Ordinal)));
                EditorGUILayout.LabelField(hasToolOutput ? AssetCleanerStrings.ScanGroupToolOutputHelp :
                    AssetCleanerStrings.ScanGroupGenericHelp, EditorStyles.wordWrappedMiniLabel);
            }
            _candidateGroupFoldouts[groupId] = expanded;
            return expanded;
        }

        private bool IsToolOutputUnit(ScanResultEntry entry)
        {
            return _settings.toolOutputDirectories.Any(output =>
                entry.UnitPath.StartsWith(
                    output.TrimEnd('/') + "/", System.StringComparison.Ordinal));
        }

        private void Relocate(List<ScanResultEntry> selected, long selectedSize)
        {
            if (selected.Count == 0)
            {
                EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName, AssetCleanerStrings.RelocateNothingSelected, AssetCleanerStrings.Ok);
                return;
            }

            var foldersToFold = AssetRelocator.CollectFoldersThatBecomeEmpty(
                selected.Select(entry => entry.UnitPath).ToList());
            var confirmMessage = string.Format(
                AssetCleanerStrings.RelocateConfirmFormat, selected.Count, AssetCleanerUtil.HumanSize(selectedSize));
            if (foldersToFold.Count > 0)
                confirmMessage += string.Format(AssetCleanerStrings.RelocateFoldFoldersConfirmFormat, foldersToFold.Count);

            // 確認ダイアログ。既定フォーカスはキャンセル。
            bool ok = EditorUtility.DisplayDialog(
                AssetCleanerStrings.RelocateConfirmTitle,
                confirmMessage,
                AssetCleanerStrings.RelocateConfirmOk,
                AssetCleanerStrings.RelocateConfirmCancel);
            if (!ok) return;

            var trashRoot = AssetRelocator.Relocate(
                selected, _settings.exportUnityPackageBeforeRelocate,
                out int moved, out string exportedPackage, out int foldedFolders);

            // 実行後ガイダンス
            var doneMsg = string.Format(AssetCleanerStrings.RelocateDoneFormat, moved, trashRoot);
            if (foldedFolders > 0)
                doneMsg += string.Format(AssetCleanerStrings.RelocateFoldFoldersDoneFormat, foldedFolders);
            if (!string.IsNullOrEmpty(exportedPackage))
                doneMsg += string.Format(AssetCleanerStrings.RelocateExportedFormat, exportedPackage);
            EditorUtility.DisplayDialog(AssetCleanerStrings.RelocateDoneTitle, doneMsg, AssetCleanerStrings.Ok);

            // 退避済みは結果から除外して再描画
            RefreshTrashFolders();
            RunScan();
            GUIUtility.ExitGUI();
        }

        private void RefreshTrashFolders()
        {
            _trashFolders = AssetRelocator.FindTrashFolders();
        }

        private void DrawTrashFoldersSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.RestoreTrashFoldersHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.RestoreTrashFoldersHelp, MessageType.None);
            if (GUILayout.Button(AssetCleanerStrings.RestoreTrashFoldersRefresh, GUILayout.Width(80)))
                RefreshTrashFolders();

            if (_trashFolders.Count == 0)
            {
                EditorGUILayout.LabelField(AssetCleanerStrings.RestoreTrashFoldersEmpty, EditorStyles.miniLabel);
            }
            else
            {
                foreach (var folder in _trashFolders)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(string.Format(AssetCleanerStrings.RestoreTrashFoldersEntryFormat,
                            AssetCleanerUtil.HumanDateTime(folder.CreatedAt), folder.EntryCount,
                            AssetCleanerUtil.HumanSize(folder.SizeBytes)));
                        if (folder.HasBackupPackage)
                            EditorGUILayout.LabelField(AssetCleanerStrings.RestoreTrashFoldersBackup, EditorStyles.miniLabel);
                        EditorGUILayout.LabelField(folder.AbsPath, EditorStyles.miniLabel);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(AssetCleanerStrings.RestoreTrashFoldersReveal))
                                EditorUtility.RevealInFinder(folder.AbsPath);
                            if (GUILayout.Button(AssetCleanerStrings.RestoreTrashFoldersRestore))
                            {
                                Restore(folder.AbsPath);
                                GUIUtility.ExitGUI();
                            }
                            if (GUILayout.Button(AssetCleanerStrings.RestoreTrashFoldersPurge))
                            {
                                Purge(folder.AbsPath);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }

            DrawRestoreSection();
            DrawPurgeSection();
        }

        private void DrawRestoreSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.RestoreHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.RestoreHelp, MessageType.None);
            if (GUILayout.Button(AssetCleanerStrings.RestoreSelectButton))
            {
                var abs = EditorUtility.OpenFolderPanel(
                    AssetCleanerStrings.RestoreSelectButton, AssetCleanerUtil.ProjectRoot, "");
                if (string.IsNullOrEmpty(abs)) return;
                abs = AssetCleanerUtil.Normalize(abs);

                if (!AssetRelocator.HasManifest(abs))
                {
                    EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName, AssetCleanerStrings.RestoreInvalidFolder, AssetCleanerStrings.Ok);
                    return;
                }

                Restore(abs);
                GUIUtility.ExitGUI();
            }
        }

        private void Restore(string trashRootAbs)
        {
            int count = CountManifestEntries(trashRootAbs);
            bool ok = EditorUtility.DisplayDialog(
                AssetCleanerStrings.RestoreConfirmTitle,
                string.Format(AssetCleanerStrings.RestoreConfirmFormat, count),
                AssetCleanerStrings.RelocateConfirmOk, AssetCleanerStrings.RelocateConfirmCancel);
            if (!ok) return;

            int restored = AssetRelocator.Restore(trashRootAbs, out bool trashFolderRemoved);
            if (restored < 0)
            {
                EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName, AssetCleanerStrings.RestoreInvalidFolder, AssetCleanerStrings.Ok);
                return;
            }
            var message = string.Format(AssetCleanerStrings.RestoreDoneFormat, restored)
                + (trashFolderRemoved ? AssetCleanerStrings.RestoreFolderRemoved : AssetCleanerStrings.RestoreFolderKept);
            EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName, message, AssetCleanerStrings.Ok);
            RefreshTrashFolders();
            if (_result != null) RunScan();
        }

        private static int CountManifestEntries(string trashRootAbs)
        {
            try
            {
                var path = AssetRelocator.FindManifestPath(trashRootAbs);
                if (path == null) return 0;
                var m = JsonUtility.FromJson<RelocationManifest>(File.ReadAllText(path));
                return m?.entries?.Count ?? 0;
            }
            catch { return 0; }
        }

        // -------------------------------------------------- 完全削除

        private void DrawPurgeSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.PurgeHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.PurgeHelp, MessageType.Warning);
            if (GUILayout.Button(AssetCleanerStrings.PurgeSelectButton))
            {
                var abs = EditorUtility.OpenFolderPanel(
                    AssetCleanerStrings.PurgeSelectButton, AssetCleanerUtil.ProjectRoot, "");
                if (string.IsNullOrEmpty(abs)) return;
                abs = AssetCleanerUtil.Normalize(abs);

                if (!AssetRelocator.HasManifest(abs))
                {
                    EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName, AssetCleanerStrings.PurgeNotTrashFolder, AssetCleanerStrings.Ok);
                    return;
                }
                Purge(abs);
                GUIUtility.ExitGUI();
            }
        }

        private void Purge(string trashRootAbs)
        {
            AssetRelocator.TryGetTrashFolderStats(trashRootAbs, out int count, out long size);

            // 第1段階（既定フォーカスはキャンセル側）
            bool stage1 = EditorUtility.DisplayDialog(
                AssetCleanerStrings.PurgeConfirmStage1Title,
                string.Format(AssetCleanerStrings.PurgeConfirmStage1Format,
                    trashRootAbs, count, AssetCleanerUtil.HumanSize(size)),
                AssetCleanerStrings.PurgeConfirmProceed, AssetCleanerStrings.Cancel);
            if (!stage1) return;

            // 第2段階（最終確認）
            bool stage2 = EditorUtility.DisplayDialog(
                AssetCleanerStrings.PurgeConfirmStage2Title,
                AssetCleanerStrings.PurgeConfirmStage2,
                AssetCleanerStrings.PurgeConfirmProceed, AssetCleanerStrings.Cancel);
            if (!stage2) return;

            if (AssetRelocator.PurgeTrashFolder(trashRootAbs, out string error))
            {
                EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                    string.Format(AssetCleanerStrings.PurgeDoneFormat, trashRootAbs), AssetCleanerStrings.Ok);
            }
            else
            {
                EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                    string.Format(AssetCleanerStrings.PurgeFailedFormat, error), AssetCleanerStrings.Ok);
            }
            RefreshTrashFolders();
        }

        // -------------------------------------------------- 重複検出（SHA-256）

        private void DrawDuplicatesTab()
        {
            EditorGUILayout.LabelField(AssetCleanerStrings.DupHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.DupHelp, MessageType.Info);

            if (GUILayout.Button(AssetCleanerStrings.DupScanButton, GUILayout.Height(28)))
            {
                _dupReport = DuplicateDetector.Scan(_settings);
                GUIUtility.ExitGUI();
            }

            if (_dupReport == null)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.DupNotRunYet, MessageType.None);
                return;
            }
            if (_dupReport.Groups.Count == 0)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.DupEmpty, MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format(
                AssetCleanerStrings.DupSummaryFormat,
                _dupReport.Groups.Count, AssetCleanerUtil.HumanSize(_dupReport.TotalWasted)),
                EditorStyles.boldLabel);

            foreach (var g in _dupReport.Groups)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(string.Format(
                    AssetCleanerStrings.DupGroupHeaderFormat,
                    g.Files.Count, AssetCleanerUtil.HumanSize(g.FileSize), AssetCleanerUtil.HumanSize(g.WastedBytes)),
                    EditorStyles.boldLabel);

                foreach (var f in g.Files)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string used = !_dupReport.UsedKnown || f.Used == null
                            ? "?"
                            : (f.Used.Value ? AssetCleanerStrings.DupUsedYes : AssetCleanerStrings.DupUsedNo);
                        GUILayout.Label(used, GUILayout.Width(48));
                        if (GUILayout.Button(new GUIContent(f.Path, f.Path),
                                EditorStyles.linkLabel, GUILayout.ExpandWidth(true)))
                        {
                            PingPath(f.Path);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------- キャッシュ掃除

        private void DrawCacheTab()
        {
            EditorGUILayout.LabelField(AssetCleanerStrings.CacheHeader, EditorStyles.boldLabel);

            // OS ガード（Phase 2 は Windows のみ）
            if (!CacheClean.IsWindows)
            {
                EditorGUILayout.HelpBox(AssetCleanerStrings.CacheWindowsOnly, MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheIntro, MessageType.None);
            // 目立つ警告
            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheWarnMain, MessageType.Warning);

            // 予約中の表示
            if (CacheClean.IsReserved())
            {
                var pending = CacheClean.ReadPending();
                EditorGUILayout.HelpBox(string.Format(
                    AssetCleanerStrings.CacheReservedNoteFormat, pending?.reservedAt ?? ""), MessageType.Warning);
                if (GUILayout.Button(AssetCleanerStrings.CacheCancelReserveButton))
                {
                    CacheClean.CancelReservation();
                }
                EditorGUILayout.Space();
            }

            // Logs を含めるか
            EditorGUI.BeginChangeCheck();
            _settings.cacheCleanIncludeLogs = EditorGUILayout.ToggleLeft(
                AssetCleanerStrings.CacheIncludeLogsToggle, _settings.cacheCleanIncludeLogs);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.Save();
                _cacheMeasured = false; // 対象が変わるので再計測を促す
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(AssetCleanerStrings.CacheMeasureButton))
            {
                MeasureCache();
            }

            // 数値の根拠
            if (_cacheMeasured)
            {
                EditorGUILayout.HelpBox(string.Format(
                    AssetCleanerStrings.CacheStatsFormat,
                    AssetCleanerUtil.HumanSize(_cacheLibSize),
                    _cacheAssetCount,
                    AssetCleanerUtil.HumanSize(_cacheAssetSize)), MessageType.Info);

                // 削除対象を全件列挙
                EditorGUILayout.LabelField(AssetCleanerStrings.CacheTargetsHeader, EditorStyles.boldLabel);
                foreach (var t in _cacheTargets.Where(t => t.Enabled))
                {
                    var size = t.SizeBytes >= 0 ? AssetCleanerUtil.HumanSize(t.SizeBytes) : AssetCleanerStrings.CacheUnmeasured;
                    EditorGUILayout.LabelField(
                        string.Format(AssetCleanerStrings.CacheTargetEntryFormat, t.RelativePath, size,
                            t.Exists ? "" : AssetCleanerStrings.CacheTargetMissing),
                        EditorStyles.miniLabel);
                }
                EditorGUILayout.LabelField(AssetCleanerStrings.CacheTargetsCsprojNote, EditorStyles.miniLabel);
            }

            // 所要時間の目安（実測優先）
            EditorGUILayout.Space();
            if (_settings.lastReimportSeconds > 0)
            {
                EditorGUILayout.HelpBox(string.Format(
                    AssetCleanerStrings.CacheTimeEstimateMeasuredFormat,
                    AssetCleanerUtil.HumanDuration(_settings.lastReimportSeconds)), MessageType.Info);
            }
            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheTimeEstimateGeneric, MessageType.None);
            // 実行タイミング助言
            EditorGUILayout.HelpBox(AssetCleanerStrings.CacheTimingAdvice, MessageType.None);

            // 実行導線（二段階確認は専用ウィンドウ）
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!_cacheMeasured))
            {
                if (GUILayout.Button(AssetCleanerStrings.CacheReserveButton, GUILayout.Height(26)))
                {
                    OpenCacheConfirm(CacheCleanConfirmWindow.Mode.Reserve);
                }
                if (GUILayout.Button(AssetCleanerStrings.CacheCleanNowButton, GUILayout.Height(22)))
                {
                    OpenCacheConfirm(CacheCleanConfirmWindow.Mode.CleanNow);
                }
            }

            // フォールバック
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!_cacheMeasured))
            {
                if (GUILayout.Button(AssetCleanerStrings.CacheFallbackButton))
                {
                    var path = CacheClean.WriteFallbackBat(_cacheTargets);
                    EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                        string.Format(AssetCleanerStrings.CacheFallbackDoneFormat, path), AssetCleanerStrings.Ok);
                }
            }
        }

        private void MeasureCache()
        {
            _cacheTargets = CacheClean.EnumerateTargets(_settings);
            CacheClean.MeasureSizes(_cacheTargets);
            _cacheLibSize = CacheClean.MeasureLibrarySize();
            CacheClean.MeasureAssets(out _cacheAssetCount, out _cacheAssetSize);
            _cacheMeasured = true;
        }

        private void OpenCacheConfirm(CacheCleanConfirmWindow.Mode mode)
        {
            if (!_cacheMeasured || _cacheTargets == null) MeasureCache();
            CacheCleanConfirmWindow.Open(
                mode, _cacheTargets, _cacheLibSize, _cacheAssetCount, _cacheAssetSize,
                _settings.lastReimportSeconds);
        }

        // -------------------------------------------------- About

        private void DrawAboutTab()
        {
            var packageInfo = FindPackageInfo();
            string displayName = packageInfo != null && !string.IsNullOrEmpty(packageInfo.displayName)
                ? packageInfo.displayName : AssetCleanerStrings.ToolName;
            string version = packageInfo != null && !string.IsNullOrEmpty(packageInfo.version)
                ? packageInfo.version : AssetCleanerStrings.AboutUnknownVersion;
            string reportInfo = string.Format(AssetCleanerStrings.AboutCopyFormat,
                displayName, version, Application.unityVersion,
                SystemInfo.operatingSystemFamily.ToString());

            EditorGUILayout.LabelField(string.Format(
                AssetCleanerStrings.AboutPackageFormat, displayName, version), EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.AboutIssuesHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.AboutIssuesHelp, MessageType.Warning);
            if (GUILayout.Button(AssetCleanerStrings.AboutOpenIssues, GUILayout.Height(30)))
                Application.OpenURL(AssetCleanerStrings.AboutIssuesUrl);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.AboutLinksHeader, EditorStyles.boldLabel);
            if (GUILayout.Button(AssetCleanerStrings.AboutRepository, EditorStyles.linkLabel))
                Application.OpenURL(AssetCleanerStrings.AboutRepositoryUrl);
            if (GUILayout.Button(AssetCleanerStrings.AboutChangelog, EditorStyles.linkLabel))
                Application.OpenURL(AssetCleanerStrings.AboutChangelogUrl);
            EditorGUILayout.LabelField(AssetCleanerStrings.AboutLicense);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetCleanerStrings.AboutReportInfoHeader, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(AssetCleanerStrings.AboutReportInfo, MessageType.Info);
            if (GUILayout.Button(AssetCleanerStrings.AboutCopyInfo))
            {
                EditorGUIUtility.systemCopyBuffer = reportInfo;
            }
            EditorGUILayout.LabelField(AssetCleanerStrings.AboutCopyPreview, EditorStyles.miniLabel);
            EditorGUILayout.SelectableLabel(
                reportInfo, EditorStyles.textArea, GUILayout.Height(58));
        }

        private static UnityEditor.PackageManager.PackageInfo FindPackageInfo()
        {
            try
            {
                return UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetCleanerWindow).Assembly);
            }
            catch
            {
                return null;
            }
        }

        private static void PingPath(string assetPath)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }

        private static string AbsToAssetPath(string abs)
        {
            if (string.IsNullOrEmpty(abs)) return null;
            abs = AssetCleanerUtil.Normalize(abs);
            var dataPath = AssetCleanerUtil.Normalize(Application.dataPath);
            if (abs == dataPath) return "Assets";
            if (abs.StartsWith(dataPath + "/"))
            {
                return "Assets" + abs.Substring(dataPath.Length);
            }
            EditorUtility.DisplayDialog(AssetCleanerStrings.ToolName,
                AssetCleanerStrings.AssetsFolderRequired, AssetCleanerStrings.Ok);
            return null;
        }
    }
}
