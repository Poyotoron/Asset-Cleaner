namespace Poyo.AssetCleaner.Editor
{
    /// <summary>
    /// ユーザーに見える日本語文字列を一箇所に集約する（将来の多言語化のため）。
    /// コーディング規約: 文字列はここに集める。
    /// </summary>
    internal static class AssetCleanerStrings
    {
        // ---- ツール識別 ----
        public const string ToolName = "Poyo's Asset Cleaner";
        public const string MenuPath = "Tools/Poyo's Asset Cleaner";
        public const string WindowTitle = "Asset Cleaner";

        // ---- タブ ----
        public const string TabRoots = "ルート (Roots)";
        public const string TabProtection = "保護 (Protection)";
        public const string TabScan = "スキャン結果 (Scan Result)";
        public const string TabDuplicates = "重複検出 (Duplicates)";
        public const string TabCache = "キャッシュ掃除 (Cache)";
        public const string TabAbout = "情報 (About)";

        // ---- Roots タブ ----
        public const string RootsHeader = "アバターのルートフォルダ";
        public const string RootsHelp =
            "アバターの .prefab / .unity を含むフォルダを登録してください。\n" +
            "サブフォルダも調べ、見つかった Prefab / Scene を使用の起点（ルート）にします。\n" +
            "登録し忘れると、使用中のアセットを未使用と判定します。";
        public const string RootsDropArea = "ここにフォルダをドラッグ＆ドロップ";
        public const string RootsSelectFolder = "フォルダを選択して追加";
        public const string AddButton = "追加";
        public const string RootsInvalidIndividualAsset = "Prefab または Scene(.unity) を指定してください。";
        public const string RootsAddIndividualHeader = "個別ファイルをルートに追加（任意）";
        public const string RootsAddIndividualHelp =
            "Prefab / Scene を1件ずつルートに追加できます。";
        public const string RootsAutoDetectHeader = "自動検出されたアバター (VRCAvatarDescriptor)";
        public const string RootsAutoDetectButton = "アバターを自動検出";
        public const string RootsAutoDetectNone = "アバターが見つからないか、VRChat SDK が入っていません。";
        public const string RootsAutoDetectExcludeHint = "チェックを外すとルートから外れます。";
        public const string RootsImplicitHeaderFormat = "暗黙ルート ({0})";
        public const string RootsImplicitHeaderNotInspected = "暗黙ルート（未調査）";
        public const string RootsImplicitInspect = "暗黙ルートを調べる";
        public const string RootsImplicitHelp =
            "参照されていなくてもスキャンの起点にするファイルです。Resources 内などのファイルは、参照がなくても実行時に読み込まれるためです。\n" +
            "\n" +
            "「最後に変換した衣装」などを覚えている設定アセットが含まれると、変換元の衣装が使用中と判定され続けます。心当たりがあれば、チェックを外してルートから外してください。\n" +
            "\n" +
            "チェックを外したファイル自体は保護され、退避候補にはなりません。そのファイルからの参照だけを、スキャンの起点として扱わなくなります。\n" +
            "\n" +
            "ただし、参照先のアセットを退避すると、ツールの入力欄は空になります（参照が切れます）。入力欄の参照を残したい場合は、そのアセットを退避しないでください。";
        public const string RootsImplicitNotInspected = "「暗黙ルートを調べる」を押すと一覧を表示します。";
        public const string RemoveButton = "外す";
        public const string RootsUnregistered = "（未登録）";
        public const string AssetsFolderRequired = "Assets/ 配下のフォルダを選択してください。";
        public const string FolderRequiredFormat = "フォルダを指定してください: {0}";
        public const string RegisteredItemsFormat = "登録済み: {0} 件";

        // ---- Protection タブ ----
        public const string ProtHeader = "保護ルール";
        public const string ProtHelp =
            "次の条件に当てはまるフォルダとファイルは、参照されていなくても保護します。\n" +
            "判定が不確かな場合も、誤って退避しないように保護します。";
        public const string ProtStructureHeader = "フォルダの中身による保護（自動）";
        public const string ProtStructureDetail =
            "・.cs / .asmdef / .asmref / .dll / .shader / .hlsl / .cginc / .shadergraph を含む\n" +
            "・package.json / *.vpm.json を含む\n" +
            "・Editor/ フォルダを含む";
        public const string ProtToolListHeader = "名前で保護するツールとフォルダ（既定）";
        public const string ProtExtHeader = "常に保護する拡張子";
        public const string ProtWhitelistHeader = "保護するパス（glob パターン）";
        public const string ProtWhitelistHelp =
            "例: Assets/MyTools/**（一致したパスは常に保護されます）";
        public const string ProtWhitelistAdd = "追加";
        public const string ProtWhitelistPlaceholder = "Assets/…/**";

        // ---- Scan タブ ----
        public const string ScanTargetsFoldout = "スキャン範囲・対象";
        public const string ScanOptionsFoldout = "判定オプション";
        public const string ScanExecuteHeader = "スキャン実行";
        public const string ScanResultsHeader = "スキャン結果";
        public const string ScanScopeHeader = "スキャン範囲";
        public const string ScanAutoGranularityToggle = "判定の単位を自動で推定する";
        public const string ScanFixedDepthLabel = "フォルダの深さ:";
        public const string ScanScopeHelp =
            "スキャン範囲を絞っても、参照は常にプロジェクト全体で調べます。範囲外のアバターが使っているアセットも使用中と判定します。";
        public const string ScanScopeDropArea = "ここに対象フォルダをドラッグ＆ドロップ";
        public const string ScanScopeSelectFolder = "フォルダを選択して追加";
        public const string ScanScopeUnconfigured = "（未設定。プロジェクト全体が対象）";
        public const string ScanToolOutputHeader = "ツール出力フォルダ";
        public const string ScanToolOutputHelp =
            "衣装変換ツールなどが生成物を保存するフォルダを登録します。ツール本体（スクリプトや DLL）と生成物を別々に判定し、使われていない生成物を退避候補に出します。\n" +
            "\n" +
            "登録後もツール本体は保護されます。";
        public const string ScanToolOutputDropArea = "ここにツール出力フォルダをドラッグ＆ドロップ";
        public const string ScanToolOutputSelectFolder = "フォルダを選択して追加";
        public const string ScanToolOutputUnconfigured = "（未設定）";
        public const string ScanScopeSummaryFormat = "スキャン範囲: {0}（範囲外の判定の単位 {1} 件は調べていません）";
        public const string ScanScopeNoUnitsWarning =
            "指定した範囲に判定の単位となるフォルダがありません。範囲が、判定の単位となるフォルダより深い可能性があります。範囲を浅いフォルダにするか、判定の単位の設定を見直してください。";
        public const string ScanButton = "スキャン実行";
        public const string ScanNoRootsError =
            "ルート（アバター Prefab / Scene）が見つかりません。\n" +
            "全アセットが退避候補になるため、スキャンを止めました。\n" +
            "「ルート」タブでアバターのフォルダを登録するか、自動検出を実行してください。";
        public const string ScanColSelect = "選択";
        public const string ScanColPath = "パス";
        public const string ScanColSize = "サイズ";
        public const string ScanColType = "種別";
        public const string ScanColReason = "判定根拠";
        public const string ScanColProtect = "保護";
        public const string ScanColCount = "件数";
        public const string ReasonUnreachable = "ルートから参照なし";
        public const string ReasonExcludedImplicitRoot = "ルートから外したファイルを含むため保護";
        public const string ReasonImplicitRoot = "スキャンの起点（暗黙ルート）として保護";
        public const string ScanProtectButton = "保護に追加";
        public const string ScanEmpty = "退避候補は見つかりませんでした。";
        public const string ScanNotRunYet = "まだスキャンしていません。「スキャン実行」を押してください。";
        public const string ScanSelectAll = "全選択";
        public const string ScanSelectNone = "全解除";
        public const string ScanSummaryFormat =
            "退避候補: {0} 件 / 合計 {1}   （使用中: {2} / 保護: {3} / ルート: {4} 件）";
        public const string ScanSelectedSummaryFormat = "選択中: {0} 件 / {1}";
        public const string ScanProtectedFoldoutFormat = "保護された判定の単位 ({0})";
        public const string ScanProtectedHelp =
            "未使用でも退避候補に出さないものです。シェーダー、エディタ拡張、ツール本体、パッケージなど、退避すると動かなくなる可能性があるものを自動で保護します。";
        public const string ScanImplicitOnlyFoldoutFormat = "暗黙ルートだけで使用中 ({0})";
        public const string ScanImplicitOnlyHelp =
            "アバターやシーンからは参照されず、設定アセットなどの暗黙ルートからだけ参照されているものです。意図していない参照なら、その暗黙ルートを外すと退避候補になります。";
        public const string ScanColImplicitRoots = "使用中にしている暗黙ルート";
        public const string ScanExcludeImplicitRoots = "ルートから外す";
        public const string ScanImplicitRootsAttributionSkipped =
            "暗黙ルートのいずれか（件数が多いため、ルートタブで確認してください）";
        public const string ScanImplicitRootsNotIdentified =
            "暗黙ルートを特定できません（ルートタブで確認してください）";
        public const string ScanFileCountFormat = "{0} 件";
        public const string ScanGroupHeaderFormat = "まとめて退避する必要があります（{0} 件・合計 {1}）";
        public const string ScanGroupToolOutputHelp =
            "ツールの生成物と、その元になったアセットのまとまりです。一部だけを退避すると、残ったアセットの参照が切れます。";
        public const string ScanGroupGenericHelp =
            "互いに参照している候補のまとまりです。一部だけを退避すると、残ったアセットの参照が切れます。";
        public const string ScanGroupSourceAsset = "元アセット";
        public const string ScanGroupGeneratedAsset = "生成物";
        public const string ScanReferencedByTooltipFormat = "この候補を参照している候補:\n{0}";

        // ---- About タブ ----
        public const string AboutPackageFormat = "{0}  {1}";
        public const string AboutUnknownVersion = "バージョン不明";
        public const string AboutIssuesHeader = "バグ報告・要望・誤検出の報告";
        public const string AboutIssuesHelp =
            "バグ、要望、誤検出は GitHub の Issue で報告してください。\n" +
            "\n" +
            "使用中のアセットが退避候補に出たときは、退避せずに報告してください。";
        public const string AboutOpenIssues = "GitHub Issues を開く";
        public const string AboutIssuesUrl = "https://github.com/Poyotoron/Asset-Cleaner/issues";
        public const string AboutLinksHeader = "リンク";
        public const string AboutRepository = "リポジトリ";
        public const string AboutRepositoryUrl = "https://github.com/Poyotoron/Asset-Cleaner";
        public const string AboutChangelog = "変更履歴 (CHANGELOG)";
        public const string AboutChangelogUrl = "https://github.com/Poyotoron/Asset-Cleaner/blob/main/CHANGELOG.md";
        public const string AboutLicense = "ライセンス: MIT";
        public const string AboutReportInfoHeader = "報告に添える情報";
        public const string AboutReportInfo =
            "・このツールのバージョン\n" +
            "・Unity のバージョン\n" +
            "・スキャン結果に表示された判定根拠、または保護理由\n" +
            "・対象アセットのフォルダ構成";
        public const string AboutCopyInfo = "環境情報をコピー";
        public const string AboutCopyPreview = "この環境情報がコピーされます:";
        public const string AboutCopyFormat = "- ツール: {0} {1}\n- Unity: {2}\n- OS: {3}";

        // ---- 退避 / 復元 ----
        public const string RelocateButton = "選択項目を退避する";
        public const string RelocateConfirmTitle = "退避の確認";
        public const string RelocateConfirmFormat =
            "{0} 個のフォルダ / {1} をプロジェクト直下の退避フォルダへ退避します。\n" +
            "\n" +
            "削除はしません。「復元」で元の場所に戻せます。\n" +
            "退避後、Console に Missing 参照の警告がないか確認してください。\n" +
            "\n" +
            "退避しますか？";
        public const string RelocateConfirmOk = "退避する";
        public const string RelocateConfirmCancel = "キャンセル";
        public const string RelocateNothingSelected = "退避対象が選択されていません。";
        public const string RelocateDoneFormat =
            "{0} 件を退避しました。\n退避先: {1}\n\n" +
            "・Console に Missing 参照の警告が出ていないか確認してください。\n" +
            "・問題があれば「復元」から元に戻せます。";
        public const string RelocateFoldFoldersConfirmFormat =
            "\n" +
            "\n" +
            "空になる {0} 個のフォルダも退避します（復元で元に戻せます）。";
        public const string RelocateFoldFoldersDoneFormat = "\n" +
            "空になったフォルダを {0} 件退避しました。";
        public const string RelocateDoneTitle = "退避完了";
        public const string RestoreMetaAlreadyExistsFormat = "復元先に .meta があるため、この項目を復元しませんでした: {0}";

        public const string RestoreHeader = "退避したアセットの復元";
        public const string RestoreHelp =
            "過去に退避したフォルダ（プロジェクトルート直下の _UnusedAssets_… ）を選択すると、元の場所へ戻します。";
        public const string RestoreSelectButton = "退避フォルダを選んで復元";
        public const string RestoreConfirmTitle = "復元の確認";
        public const string RestoreConfirmFormat =
            "{0} 件を元の場所（Assets/ 配下）へ戻します。続行しますか？";
        public const string RestoreDoneFormat = "{0} 件を復元しました。";
        public const string RestoreFolderRemoved = "\n退避フォルダが空になったため削除しました。";
        public const string RestoreFolderKept =
            "\n一部が戻せなかったため、退避フォルダは残しています（残りの内容を確認してください）。";
        public const string RestoreInvalidFolder =
            "選択したフォルダに退避の記録ファイル（.asset-cleaner-relocation.json）がありません。";
        public const string RestoreTrashFoldersHeader = "退避済みフォルダ";
        public const string RestoreTrashFoldersHelp =
            "プロジェクト直下にある退避フォルダを一覧表示します。復元または完全削除できます。";
        public const string RestoreTrashFoldersRefresh = "再検出";
        public const string RestoreTrashFoldersEmpty = "退避済みフォルダはありません。";
        public const string RestoreTrashFoldersEntryFormat = "退避日時: {0}   /   {1} 件   /   {2}";
        public const string RestoreTrashFoldersBackup = "backup.unitypackage あり";
        public const string RestoreTrashFoldersReveal = "フォルダを開く";
        public const string RestoreTrashFoldersRestore = "復元";
        public const string RestoreTrashFoldersPurge = "完全削除";
        public const string RestoreTrashFoldersFindFailedFormat = "退避フォルダの一覧を取得できませんでした: {0}";

        // ---- 進捗 ----
        public const string ProgressTitle = "スキャン中…";
        public const string ProgressCollectRoots = "ルートを集めています…";
        public const string ProgressBuildReachable = "アセットの参照関係を調べています…";
        public const string ProgressBuildAvatarReachable = "アバターとシーンから参照をたどっています…";
        public const string ProgressAssignImplicitRoots = "使用中にしている暗黙ルートを調べています…";
        public const string ProgressEnumerate = "アセットの一覧を作っています…";
        public const string ProgressClassify = "判定しています…";
        public const string ProgressBuildCandidateGroups = "候補間の参照関係を調べています…";
        public const string ProgressRelocate = "退避しています…";
        public const string ProgressRestore = "復元しています…";
        public const string ProgressMeasureCache = "キャッシュサイズを計測しています…";
        public const string ProgressHashing = "ファイルのハッシュを計算しています…";
        public const string ProgressExportPackage = ".unitypackage を書き出しています…";

        public const string Ok = "OK";
        public const string Cancel = "キャンセル";

        // ---- 退避オプション ----
        public const string RelocateExportPackageToggle = "退避前に .unitypackage としてバックアップする";
        public const string RelocateExportPackageHelp =
            "退避前に対象を .unitypackage に書き出します。「復元」とは別にバックアップを残せます。\n" +
            "保存先はプロジェクト直下の退避フォルダ内（_UnusedAssets_…/backup.unitypackage）です。";
        public const string RelocateExportedFormat = "\n.unitypackage を書き出しました:\n{0}";

        // ---- 完全削除 ----
        public const string PurgeHeader = "退避フォルダの完全削除（取り消し不可）";
        public const string PurgeHelp =
            "退避フォルダ（_UnusedAssets_…）と中のアセットを完全に削除します。この操作は取り消せず、復元もできません。\n" +
            "不要と確認できたフォルダだけを削除してください。";
        public const string PurgeSelectButton = "退避フォルダを選んで完全削除";
        public const string PurgeConfirmStage1Title = "完全削除の確認（1/2）";
        public const string PurgeConfirmStage1Format =
            "次の退避フォルダを完全に削除します。\n" +
            "{0}\n" +
            "\n" +
            "対象: {1} 件 / {2}\n" +
            "\n" +
            "取り消しと復元はできません。完全に削除しますか？";
        public const string PurgeConfirmStage2Title = "完全削除の最終確認（2/2）";
        public const string PurgeConfirmStage2 =
            "このフォルダと中のアセットを完全に削除します。\n" +
            "復元はできません。完全に削除しますか？";
        public const string PurgeConfirmProceed = "完全に削除する";
        public const string PurgeDoneFormat = "退避フォルダを完全に削除しました:\n{0}";
        public const string PurgeNotTrashFolder =
            "選択したフォルダには退避の記録ファイル（.asset-cleaner-relocation.json）がありません。\n" +
            "このツールの退避フォルダ以外は、完全削除できません。";
        public const string PurgeFailedFormat = "削除に失敗しました: {0}";

        // ---- ファイル単位モード ----
        public const string ScanFileUnitToggle = "ファイル単位モード（上級者向け）";
        public const string ScanFileUnitHelp =
            "フォルダにまとめず、アセットを1ファイルずつ判定します。\n" +
            "使用中の衣装フォルダから未使用テクスチャだけを退避する、といった操作ができます。\n" +
            "ただし、1商品を1フォルダにまとめて保護する働きが弱くなります。OFF のまま使うことをおすすめします。";

        // ---- 重複検出（SHA-256） ----
        public const string DupHeader = "重複アセット検出（内容が同一のファイル）";
        public const string DupHelp =
            "Assets/ 内のファイルを SHA-256 で照合し、内容が完全に一致するものを調べます。\n" +
            "シェーダーやテクスチャなどを、複数の配布物が同梱している場合などに見つかります。\n" +
            "検出だけを行い、自動では退避も削除もしません。参照されている重複を消すと参照が切れます。\n" +
            "内容を確認して、削除するか手動で判断してください。";
        public const string DupScanButton = "重複を検出";
        public const string DupNotRunYet = "まだ検出していません。「重複を検出」を押してください。";
        public const string DupEmpty = "内容が重複するファイルは見つかりませんでした。";
        public const string DupSummaryFormat =
            "重複グループ: {0} 件   （重複により余分に消費している容量の目安: {1}）";
        public const string DupGroupHeaderFormat = "同一内容 {0} ファイル / 各 {1}   （余剰 {2}）";
        public const string DupColUsed = "使用";
        public const string DupUsedYes = "使用中";
        public const string DupUsedNo = "未参照";

        // ---- キャッシュ掃除 ----
        public const string CacheHeader = "ビルドキャッシュ（Library / Temp）の掃除";
        public const string CacheWindowsOnly =
            "この機能は現在 Windows のみ対応です。この環境では利用できません。";
        public const string CacheIntro =
            "アセットを退避しても Library/ 内のキャッシュは減りません。キャッシュをすべて削除すると、空き容量が増える場合があります。\n" +
            "ただし、次回 Unity 起動時にすべてのアセットが再インポートされます。";
        // 過小表現を避け、目立つ警告として出す
        public const string CacheWarnMain =
            "次回 Unity 起動時に、すべてのアセットが再インポートされます。\n" +
            "プロジェクトの規模により数分〜数十分かかり、それ以上になることもあります。\n" +
            "再インポートが終わるまで Unity は使えません。";
        // 見積りの根拠を数値で示す
        public const string CacheStatsFormat =
            "プロジェクトの規模:\n" +
            "・現在の Library/ サイズ: {0}\n" +
            "・Assets/ のアセット数: {1} 個 / 合計 {2}";
        // 所要時間の目安（環境差が大きいので断定しない）
        public const string CacheTimeEstimateGeneric =
            "かかる時間は CPU、ストレージ、アセットの構成によって変わります。\n" +
            "SSD でも数分、規模が大きい場合や HDD の場合は数十分以上かかることがあります。";
        public const string CacheTimeEstimateMeasuredFormat =
            "前回このプロジェクトでは、掃除後の起動に約 {0} かかりました（実測値）。";
        // 実行タイミングの助言
        public const string CacheTimingAdvice =
            "作業の終了時や離席前など、しばらく Unity を使わないときに実行してください。";
        // 削除対象（全件を列挙して見せる）
        public const string CacheTargetsHeader = "削除対象（全件）";
        public const string CacheMeasureButton = "サイズを計測 / 更新";
        public const string CacheReserveButton = "次回 Unity 終了時にキャッシュを掃除する（予約）…";
        public const string CacheReservedNoteFormat =
            "掃除が予約されています。次回 Unity を終了すると、ヘルパーがキャッシュを削除します。\n予約日時: {0}";
        public const string CacheCancelReserveButton = "予約をキャンセルする";
        public const string CacheCleanNowButton = "今すぐ Unity を終了して掃除する…";
        public const string CacheFallbackButton = "clean-cache.bat を作成する（手動実行用）";
        public const string CacheIncludeLogsToggle = "Logs/ も削除する（既定 OFF: 不具合調査に使う可能性があるため）";
        public const string CacheNextButton = "次へ（最終確認）";
        public const string CacheTargetsCsprojNote = "・*.csproj / *.sln （再生成されます）";
        public const string CacheUnmeasured = "未計測";
        public const string CacheTargetMissing = " / 無し";
        public const string CacheTargetEntryFormat = "・{0}/   （{1}{2}）";
        public const string CacheFallbackDoneFormat =
            "clean-cache.bat をプロジェクトルートに生成しました:\n{0}\n\n" +
            "Unity を完全に終了してから、このバッチをダブルクリックで実行してください。";

        // 予約ダイアログ（第1段階 / 第2段階は専用ウィンドウ）
        public const string CacheConfirmStage2Title = "キャッシュ掃除の最終確認";
        public const string CacheAckCheckbox = "再インポートに時間がかかることを理解しました";
        public const string CacheReserveConfirmButton = "予約する";
        public const string CacheCleanNowConfirmButton = "今すぐ終了して掃除する";
        public const string CacheReservedDialogFormat =
            "キャッシュ掃除を予約しました。\n次回 Unity を終了したときに、外部ヘルパーが Library/ 等を削除します。\n\n{0}";

        // 終了時の再警告
        public const string CacheQuitWarnTitle = "キャッシュ掃除が予約されています";
        public const string CacheQuitWarnMessage =
            "キャッシュ掃除が予約されています。\n" +
            "このまま Unity を終了すると外部ヘルパーが起動し、次回起動時に長時間の再インポートが発生します。\n\n" +
            "どうしますか？";
        public const string CacheQuitProceed = "掃除して終了";
        public const string CacheQuitCancelReservation = "予約を取り消して終了";
        public const string CacheQuitAbort = "終了しない";

        // 起動時の再インポート通知
        public const string CacheJustCleanedTitle = "キャッシュを掃除しました";
        public const string CacheJustCleanedMessage =
            "前回 Unity 終了時にビルドキャッシュを削除しました。\n" +
            "この起動時に、すべてのアセットが再インポートされています（または直前に完了しました）。\n" +
            "再インポート中は強制終了せず、完了までお待ちください。";
        public const string CacheJustCleanedMeasuredFormat =
            "\n\n今回の掃除後の起動には約 {0} かかりました。";

        // 起動時検証: 予約が残っている（前回掃除が完了しなかった）
        public const string CacheLeftoverTitle = "前回のキャッシュ掃除が完了しませんでした";
        public const string CacheLeftoverMessage =
            "キャッシュ掃除の予約が残っています。Unity のクラッシュや強制終了などで、前回の掃除が実行されなかった可能性があります。キャッシュは削除されていません。\n" +
            "\n" +
            "予約を解除しました。もう一度実行するには「キャッシュ掃除」タブで予約してください。";
    }
}
