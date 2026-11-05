using UnityEditor;
using UnityEngine;

namespace Poyo.AssetCleaner.Editor
{
    internal static class AssetCleanerLegacy
    {
        // if の条件で使うため、到達できないコードの警告を避ける。
        internal static readonly bool IsInstalled =
#if ASSET_CLEANER_HAS_LEGACY
            true;
#else
            false;
#endif

#if ASSET_CLEANER_HAS_LEGACY
        [InitializeOnLoadMethod]
        private static void Notify()
        {
            const string key = "net.maaaaa.asset-cleaner.legacyNoticeShown";
            if (Application.isBatchMode || SessionState.GetBool(key, false)) return;
            SessionState.SetBool(key, true);
            Debug.LogWarning("[Poyo's Asset Cleaner] 旧名のいらないアセット消しちゃうもんネーター（net.maaaaa.asset-keshichaumon-nator）が同じプロジェクトに入っています。Poyo's Asset Cleaner はいらないアセット消しちゃうもんネーターの新しい名前です。VCC などのパッケージ管理ツールでいらないアセット消しちゃうもんネーターを削除してください。");
        }
#endif
    }
}
