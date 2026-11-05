using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Poyo.AssetCleaner.Editor
{
    /// <summary>
    /// 設定の永続化。
    /// Assets/zzz_pytr/Asset-Cleaner/Settings.asset に保存する。
    /// </summary>
    internal class AssetCleanerSettings : ScriptableObject
    {
        /// <summary>設定フォルダ。Project ウィンドウの末尾側に並ぶよう、先頭に zzz_ を付けている。</summary>
        public const string SettingsFolder = "Assets/zzz_pytr/Asset-Cleaner";
        public const string SettingsPath = SettingsFolder + "/Settings.asset";

        /// <summary>以前の版の設定フォルダ。新しい順に探す。移行と保護のためだけに残す。</summary>
        public static readonly string[] LegacySettingsFolders =
        {
            "Assets/zzz_pytr/Asset-Keshichaumon-Nator",
            "Assets/Asset-Keshichaumon-Nator",
        };

        [Tooltip("アバターのルートフォルダ（Assets 内のフォルダのパス）")]
        public List<string> avatarRootDirectories = new List<string>();

        [Tooltip("個別に追加したルート（Prefab / Scene のアセットパス）")]
        public List<string> additionalRootAssets = new List<string>();

        [Tooltip("自動検出したが、ユーザーが除外したアバターのアセットパス")]
        public List<string> excludedAutoDetectedRoots = new List<string>();

        [Tooltip("暗黙ルートのうち、ユーザーが起点から除外したアセットのパス")]
        public List<string> excludedImplicitRoots = new List<string>();

        [Tooltip("保護するパス（glob パターン）。一致したパスは常に保護する")]
        public List<string> userWhitelistGlobs = new List<string>();

        [Tooltip("未使用スキャンの対象範囲（Assets 配下のフォルダのアセットパス）。空ならプロジェクト全体")]
        public List<string> scanScopeDirectories = new List<string>();

        [Tooltip("ツールの出力フォルダ（Assets 配下のフォルダのアセットパス）。ツール本体と切り離して判定する")]
        public List<string> toolOutputDirectories = new List<string>();

        [Tooltip("判定の単位となるフォルダの深さ（Assets からの階層数）。既定 2")]
        public int granularityDepth = 2;

        [Tooltip("アセットを含む最も浅いフォルダを自動で推定する（深さの指定は使わない）")]
        public bool autoEstimateGranularity = true;

        [Tooltip("上級者向け: フォルダにまとめず、ファイルごとに判定する。既定 OFF")]
        public bool fileUnitMode = false;

        [Tooltip("退避前に対象を .unitypackage に書き出す。既定 OFF")]
        public bool exportUnityPackageBeforeRelocate = false;

        [Tooltip("キャッシュ掃除で Logs/ も削除対象に含める。既定 OFF")]
        public bool cacheCleanIncludeLogs = false;

        [Tooltip("前回キャッシュ掃除後にかかった再インポート時間（秒）。0 は未計測")]
        public double lastReimportSeconds = 0;

        [Tooltip("前回スキャン日時")]
        public string lastScanTime = "";

        private static AssetCleanerSettings _cached;

        public static AssetCleanerSettings GetOrCreate()
        {
            if (_cached != null) return _cached;

            var settings = AssetDatabase.LoadAssetAtPath<AssetCleanerSettings>(SettingsPath);
            if (settings == null)
            {
                foreach (var legacyFolder in LegacySettingsFolders)
                {
                    var legacySettingsPath = legacyFolder + "/Settings.asset";
                    var legacySettings = AssetDatabase.LoadAssetAtPath<AssetCleanerSettings>(legacySettingsPath);
                    if (legacySettings != null)
                    {
                        // 旧版がまだ旧い場所の設定を使っているため、移動や作り直しをしない。
                        if (AssetCleanerLegacy.IsInstalled)
                        {
                            settings = legacySettings;
                            break;
                        }
                        EnsureSettingsFolder();
                        var moveError = AssetDatabase.MoveAsset(legacySettingsPath, SettingsPath);
                        if (string.IsNullOrEmpty(moveError))
                        {
                            settings = AssetDatabase.LoadAssetAtPath<AssetCleanerSettings>(SettingsPath);
                            if (settings == null)
                            {
                                // MoveAsset 自体は成功しているため、空の設定を作らず既存インスタンスを使う。
                                settings = legacySettings;
                                Debug.LogWarning("設定の移行後に新しいパスから読み直せませんでした。移行した設定をそのまま使います。");
                            }

                            Debug.Log($"設定を {legacySettingsPath} から {SettingsPath} へ移行しました。");
                        }
                        else
                        {
                            // 空の設定で上書きせず、次回起動時にもう一度移行を試す。
                            settings = legacySettings;
                            Debug.LogWarning(
                                $"設定を移せませんでした。旧パスの設定をそのまま使います: {moveError}");
                        }
                        break;
                    }
                }

                if (settings == null)
                {
                    EnsureSettingsFolder();
                    settings = CreateInstance<AssetCleanerSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                    AssetDatabase.SaveAssets();
                }
            }
            CleanupLegacySettingsFolder();
            _cached = settings;
            return settings;
        }

        private static void CleanupLegacySettingsFolder()
        {
            if (AssetCleanerLegacy.IsInstalled) return;
            foreach (var legacyFolder in LegacySettingsFolders)
                CleanupLegacySettingsFolder(legacyFolder);
        }

        private static void CleanupLegacySettingsFolder(string legacyFolder)
        {
            if (!AssetDatabase.IsValidFolder(legacyFolder)) return;

            var absolutePath = AssetCleanerUtil.ToAbsolute(legacyFolder);
            if (!Directory.Exists(absolutePath)) return;
            using (var entries = Directory.EnumerateFileSystemEntries(absolutePath).GetEnumerator())
            {
                // ユーザーのファイルや移行途中のデータが少しでも残っていれば削除しない。
                if (entries.MoveNext()) return;
            }

            if (AssetDatabase.DeleteAsset(legacyFolder))
            {
                Debug.Log($"空の旧設定フォルダを削除しました: {legacyFolder}");
            }
            else
            {
                // 次のドメインリロード時に再試行するため、設定の読み込み自体は続行する。
                Debug.LogWarning($"空の旧設定フォルダを削除できませんでした: {legacyFolder}");
            }
        }

        private static void EnsureSettingsFolder()
        {
            const string authorFolder = "Assets/zzz_pytr";
            if (!AssetDatabase.IsValidFolder(authorFolder))
                AssetDatabase.CreateFolder("Assets", "zzz_pytr");
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder(authorFolder, "Asset-Cleaner");
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
