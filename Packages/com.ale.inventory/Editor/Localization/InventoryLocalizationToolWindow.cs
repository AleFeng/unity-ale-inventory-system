#if IS_LOCALIZATION
using System;
using System.Collections.Generic;
using Ale.Inventory.Runtime;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 本地化工具窗口（仅 IS_LOCALIZATION 编译）。为指定 <see cref="InventoryDatabase"/>：
    /// ① 按当前 Localization 设置生成 / 关联一个 String Table 集合（表名 <c>{前缀}_{数据库名}</c>，1:1 对应关系以
    /// SharedTableData GUID 记录在数据库上）；② 遍历库内所有 <see cref="EFieldType.Text"/> 字段，
    /// 自动生成唯一中文 Key、写回 tableRef/entryKey、并在表中建 Key→Value 条目（源语言值取纯文本 fallback）；
    /// ③ 一键打开 Localization Tables 编辑器。
    ///
    /// <para>「选数据库 + 逐帧步进 + 进度条 + 日志 + 取消 + 完成收尾」继承自 <see cref="InventoryToolWindowBase"/>；
    /// Text 字段遍历见 <see cref="InventoryTextFieldCollector"/>。生成 Key 前询问是否覆盖已分配的 Key：
    /// 不覆盖 = 跳过已配；覆盖 = 已配也改用自动生成的 Key（命名与现有相同则不动）。</para>
    /// </summary>
    public class InventoryLocalizationToolWindow : InventoryToolWindowBase
    {
        private const string PrefKeyFolder = "InventorySystem.Localization.GenFolder"; // EditorPrefs 存储的生成文件夹路径
        private const string PrefKeyPrefix = "InventorySystem.Localization.TablePrefix"; // EditorPrefs 存储的表名前缀
        private const string DefaultFolder = "Assets/Localization/InventorySystem"; // EditorPrefs 默认生成文件夹路径
        private const string DefaultPrefix = "InventoryStrings"; // EditorPrefs 默认表名前缀

        [SerializeField] private string folder; // EditorPrefs 存储的生成文件夹路径
        [SerializeField] private string prefix; // EditorPrefs 存储的表名前缀

        [SerializeField] private bool overwriteKeys;         // 「覆盖 已存在多语言Key」：勾选后已配 Key 的字段也改用自动生成的 Key
        [SerializeField] private bool fillSourceText = true; // 「填入 Text中的String文本」：勾选后把源纯文本作为初始值填入各语言表的空条目

        [MenuItem("Tools/Inventory System/Localization/本地化工具窗口", priority = 200)]
        public static void Open()
        {
            var win = GetWindow<InventoryLocalizationToolWindow>(true, "本地化建表 / Key 生成", true);
            win.minSize = new Vector2(500f, 460f);
            win.Show();
        }
        
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(folder)) folder = EditorPrefs.GetString(PrefKeyFolder, DefaultFolder);
            if (string.IsNullOrEmpty(prefix)) prefix = EditorPrefs.GetString(PrefKeyPrefix, DefaultPrefix);
        }

        // ── 绘制（基类骨架的钩子）─────────────────────────────────────────────────────

        protected override string DoneVerb => "已生成"; // 进度条完成时的动词（如「已生成 123 个 Key」）
        
        /// <summary>
        /// 绘制 窗口顶部的说明文字
        /// （继承自 <see cref="InventoryToolWindowBase"/>，在数据库选择框上方）。
        /// </summary>
        protected override void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("为指定数据库生成 / 关联 String Table 集合，并为所有 Text 字段自动生成中文 Key。",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4);
        }
        
        /// <summary>
        /// 绘制 窗口中间的操作按钮 / 输入框 / 标签
        /// （继承自 <see cref="InventoryToolWindowBase"/>，在数据库选择
        /// </summary>
        protected override void DrawOperations()
        {
            using (new EditorGUI.DisabledScope(IsRunning))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    folder = EditorGUILayout.TextField("生成文件夹", folder);
                    if (GUILayout.Button("浏览", GUILayout.Width(48))) BrowseFolder();
                }
                prefix = EditorGUILayout.TextField("表名前缀", prefix);
            }

            var col = ResolveCollection();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(IsRunning || !database))
                {
                    EditorGUI.BeginChangeCheck();
                    var picked = (StringTableCollection)EditorGUILayout.ObjectField(
                        "关联多语言表", col, typeof(StringTableCollection), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        BindCollection(picked);
                        col = picked;   // 同帧同步下方按钮的可用态
                    }
                }
                // 「编辑」始终显示、无关联表时禁用：选中该关联表并在 Table Editor 中打开、聚焦到它。
                using (new EditorGUI.DisabledScope(!col))
                {
                    if (GUILayout.Button("编辑", GUILayout.Width(48)))
                        OpenTableEditor(col);
                }
            }
            EditorGUILayout.LabelField(
                "可用「① 生成 关联多语言表」自动创建并绑定，或手动新建 String Table Collection 后拖到「关联多语言表」处。",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(IsRunning || !database))
            {
                if (GUILayout.Button("① 生成 关联多语言表", GUILayout.Height(26)))
                    GenerateOrLinkTable();

                using (new EditorGUI.DisabledScope(!col))
                {
                    if (GUILayout.Button("② 生成 多语言Key", GUILayout.Height(26)))
                        StartGenerateKeys();
                }

                // 两个勾选项（在「② 生成 多语言Key」按钮下方）：勾选后才执行对应行为。
                overwriteKeys  = EditorGUILayout.ToggleLeft("覆盖 已存在多语言Key", overwriteKeys);
                fillSourceText = EditorGUILayout.ToggleLeft("填入 Text中的String文本", fillSourceText);
            }
        }

        // ── 数据库 ↔ 表 关联 ─────────────────────────────────────────────────────────

        /// <summary>按数据库存储的 GUID 解析所关联的表集合；未关联 / 解析不到返回 null。</summary>
        private StringTableCollection ResolveCollection()
        {
            if (!database) return null;
            string guid = database.LocalizationTableCollectionGuid;
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            var shared = AssetDatabase.LoadAssetAtPath<SharedTableData>(path);
            if (!shared) return null;
            foreach (var c in LocalizationEditorSettings.GetStringTableCollections())
                if (c && c.SharedData == shared) return c;
            return null;
        }

        /// <summary>手动关联（或解除）表集合：把其 SharedTableData GUID 写入数据库（供 ObjectField 挂载 / 清除时调用）。</summary>
        private void BindCollection(StringTableCollection picked)
        {
            if (!database) return;
            Undo.RegisterCompleteObjectUndo(database, "关联本地化表");
            if (picked && picked.SharedData)
            {
                database.LocalizationTableCollectionGuid =
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(picked.SharedData));
                Log($"已关联表集合「{picked.TableCollectionName}」。");
            }
            else
            {
                database.LocalizationTableCollectionGuid = string.Empty;
                Log("已解除表集合关联。");
            }
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        /// <summary>选中目标表集合并在 Localization Table Editor 中打开、聚焦到它（等同 Inspector 的「Open In Table Editor」）。</summary>
        private static void OpenTableEditor(StringTableCollection col)
        {
            if (!col) return;
            Selection.activeObject = col;                 // 选中：Inspector 同步显示该表集合
            LocalizationTablesWindow.ShowWindow(col);     // 打开 Table Editor 并聚焦到它
        }

        /// <summary>
        /// 生成或关联表集合：
        /// </summary>
        private void GenerateOrLinkTable()
        {
            if (!database) return;

            var existing = ResolveCollection();
            if (existing)
            {
                Log($"已关联表集合「{existing.TableCollectionName}」，无需重建。");
                return;
            }

            var locales = LocalizationEditorSettings.GetLocales();
            if (locales == null || locales.Count == 0)
            {
                EditorUtility.DisplayDialog("尚无语言",
                    "Localization 设置里没有任何 Locale。\n请先在 Edit > Project Settings > Localization 添加语言后再生成。",
                    "知道了");
                return;
            }

            string folderPath = NormalizeFolder(this.folder);
            if (folderPath == null) { Log("⚠ 生成文件夹无效（需在 Assets 目录下）。"); return; }
            EnsureFolder(folderPath);

            string prefixStr = string.IsNullOrEmpty(this.prefix) ? DefaultPrefix : this.prefix;
            string nameStr   = $"{prefixStr}_{database.name}";

            StringTableCollection col;
            try { col = LocalizationEditorSettings.CreateStringTableCollection(nameStr, folderPath); }
            catch (Exception e) { Log($"✖ 建表失败：{e.Message}"); return; }
            if (!col || !col.SharedData) { Log("✖ 建表失败：返回空集合。"); return; }

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(col.SharedData));
            Undo.RegisterCompleteObjectUndo(database, "关联本地化表");
            database.LocalizationTableCollectionGuid = guid;
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            PersistPrefs();
            Log($"✔ 已生成表集合「{nameStr}」（{col.StringTables.Count} 张语言表）并关联到「{database.name}」。GUID={guid}");
        }

        // ── Key 生成（逐帧步进）──────────────────────────────────────────────────────
        
        /// <summary>
        /// 遍历数据库内所有 Text 字段，逐帧生成 Key + 写回引用 + 建表条目（幂等）。
        /// </summary>
        private void StartGenerateKeys()
        {
            var col = ResolveCollection();
            if (!col) { Log("⚠ 请先「生成 关联」多语言表。"); return; }
            if (IsRunning) return;

            var refs = InventoryTextFieldCollector.Collect(database);
            if (refs.Count == 0) { Log("没有含文本内容的 Text 字段。"); return; }

            // 勾选「覆盖 已存在多语言Key」时弹确认；未勾选则跳过已配 Key。
            bool overwrite = overwriteKeys;
            if (overwrite && !EditorUtility.DisplayDialog("覆盖已存在的多语言 Key",
                    "已勾选「覆盖 已存在多语言Key」。\n" +
                    "执行后，已分配 Key 的 Text 字段将改用自动生成的 Key（自动生成的 Key 命名与现有相同则不动）。\n\n是否执行？",
                    "执行", "取消"))
                return;

            bool fill = fillSourceText;

            var    shared         = col.SharedData;
            string collectionName = col.TableCollectionName;
            var    tables         = col.StringTables;   // 所有语言表

            Undo.RegisterCompleteObjectUndo(database, "生成本地化 Key");

            var steps = new List<Func<string>>(refs.Count);
            foreach (var r in refs)
            {
                var cap = r;
                steps.Add(() => GenKeyStep(cap, shared, tables, collectionName, overwrite, fill));
            }

            RunSteps(steps, $"—— 开始生成 Key（{(overwrite ? "覆盖" : "不覆盖")}{(fill ? "，填入文本" : "")}）：「{database.name}」，共 {refs.Count} 处 Text ——");
        }

        /// <summary>
        /// 为单个 Text 位置生成 Key + 写回引用 + 建表条目。返回一条日志（跳过返回 null）。
        /// <paramref name="overwrite"/> = false：已有 Key 的字段跳过；= true：已有 Key 也改用自动生成的 Key，
        /// 但自动生成的 Key 命名与现有相同则不动。源文本有效值作为初始值填入<b>所有语言表</b>（仅填空条目）。
        /// </summary>
        private string GenKeyStep(TextFieldRef r, SharedTableData shared, IList<StringTable> tables,
            string collectionName, bool overwrite, bool writeSourceText)
        {
            var (_, entryKey) = r.Value.GetLocalizedStringRef(r.Element);
            bool hasKey = !string.IsNullOrEmpty(entryKey);
            if (hasKey)
            {
                if (!overwrite)          return null;   // 不覆盖：跳过已配
                if (entryKey == r.KeyPath) return null;   // 覆盖：命名相同则不动
            }

            // 建 / 取 SharedTableData 条目（Key 名唯一，正常不会已存在）
            var entry = shared.Contains(r.KeyPath)
                ? shared.GetEntry(shared.GetId(r.KeyPath))
                : shared.AddKey(r.KeyPath);
            if (entry == null) return $"⚠ 建 Key 失败：{r.KeyPath}";

            // 勾选「填入 Text中的String文本」且源文本有效时 → 作为初始值填入所有语言表（仅填空条目，不覆盖已有译文）
            string plain = r.Value.GetTextValue(r.Element);
            if (writeSourceText && !string.IsNullOrEmpty(plain) && tables != null)
            {
                foreach (var t in tables)
                {
                    if (!t) continue;
                    var ste = t.GetEntry(entry.Id) ?? t.AddEntry(entry.Id, string.Empty);
                    if (ste != null && string.IsNullOrEmpty(ste.Value))
                        ste.Value = plain;
                }
            }

            // 写回 Text 字段的本地化引用（tableRef=集合名、entryKey=Key 名；覆盖模式会替换旧 entryKey）
            r.Value.SetLocalizedStringRef(r.Element, collectionName, entry.Key);
            Changed++;
            return hasKey ? $"{r.KeyPath}  ⟲覆盖  ←  {plain}" : $"{r.KeyPath}  ←  {plain}";
        }
        
        /// <summary>
        /// 逐帧生成 Key 完成后收尾：标记表 / 数据库脏、保存、输出日志。
        /// </summary>
        protected override void OnRunComplete()
        {
            MarkTablesDirty();
            if (database) EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Log($"✔ 完成：共生成 {Changed} 个 Key。");
        }
        
        /// <summary>
        /// 逐帧生成 Key 完成后弹窗提示（继承自 <see cref="InventoryToolWindowBase"/>）。
        /// </summary>
        protected override void OnRunFinished()
        {
            EditorUtility.DisplayDialog("生成完成",
                $"已为「{(database ? database.name : "?")}」生成 {Changed} 个本地化 Key" +
                "（写入 tableRef/entryKey 并在表中建条目，源语言值 = 各字段纯文本）。\n\n" +
                "可点「打开 Localization Tables 编辑器」翻译其它语言。",
                "知道了");
        }
        
        /// <summary>
        /// 逐帧生成 Key 被取消后收尾：标记表 / 数据库脏、保存、输出日志。
        /// </summary>
        protected override void OnRunCanceled()
        {
            MarkTablesDirty();
            if (database) EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Log($"■ 已取消：已生成 {Changed} 个 Key（进度 {StepIndex}/{StepCount}）。");
        }
        
        /// <summary>
        /// 标记数据库 + 关联的表集合为脏（以便保存），避免生成 Key 后未保存导致丢失。
        /// </summary>
        private void MarkTablesDirty()
        {
            var col = ResolveCollection();
            if (!col) return;
            if (col.SharedData) EditorUtility.SetDirty(col.SharedData);
            foreach (var t in col.StringTables)
                if (t) EditorUtility.SetDirty(t);
        }

        // ── 文件夹 / EditorPrefs ─────────────────────────────────────────────────────
        
        /// <summary>
        /// 打开文件夹选择对话框，选择生成文件夹（必须在 Assets 下），并写入 EditorPrefs。
        /// </summary>
        private void BrowseFolder()
        {
            string start = string.IsNullOrEmpty(folder) ? "Assets" : folder;
            string abs   = EditorUtility.OpenFolderPanel("选择生成文件夹", start, "");
            if (string.IsNullOrEmpty(abs)) return;
            string rel = AbsToAssetsPath(abs);
            if (rel == null) { EditorUtility.DisplayDialog("无效文件夹", "请选择 Assets 目录下的文件夹。", "知道了"); return; }
            folder = rel;
            EditorPrefs.SetString(PrefKeyFolder, folder);
        }
        
        /// <summary>
        /// 将当前生成文件夹 + 表名前缀写入 EditorPrefs（在生成 / 关联表时调用）。
        /// </summary>
        private void PersistPrefs()
        {
            EditorPrefs.SetString(PrefKeyFolder, folder);
            EditorPrefs.SetString(PrefKeyPrefix, prefix);
        }
        
        /// <summary>
        /// 将绝对路径转换为 Assets 相对路径（如 /Users/.../Project/Assets/Localization → Assets/Localization）。
        /// </summary>
        /// <param name="abs"></param>
        /// <returns></returns>
        private static string AbsToAssetsPath(string abs)
        {
            abs = abs.Replace('\\', '/');
            string data = Application.dataPath.Replace('\\', '/');
            if (abs == data) return "Assets";
            if (abs.StartsWith(data + "/")) return "Assets" + abs.Substring(data.Length);
            return null;
        }
        
        /// <summary>
        /// 规范化文件夹路径：替换反斜杠为正斜杠、去掉末尾斜杠、必须以 Assets 开头，否则返回 null。
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private static string NormalizeFolder(string f)
        {
            if (string.IsNullOrEmpty(f)) return null;
            f = f.Replace('\\', '/').TrimEnd('/');
            return f == "Assets" || f.StartsWith("Assets/") ? f : null;
        }
        
        /// <summary>
        /// 确保指定文件夹存在（若不存在则创建，支持多级路径）。
        /// </summary>
        /// <param name="folder"></param>
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');   // parts[0] == "Assets"
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
