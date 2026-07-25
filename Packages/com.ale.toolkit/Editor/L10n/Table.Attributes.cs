using Ale.Toolkit.Runtime;

namespace Ale.Toolkit.Editor
{
    /// <summary>
    /// 属性系统编辑器 UI 的英 / 日译表：属性字段类型枚举（<c>EFieldType</c>）、枚举类型面板
    /// （<c>EnumTypePanel</c>）、属性值显示（<c>AttributeFieldDrawer</c>）、数字格式面板
    /// （<c>NumberFormatConfigPanel</c>）。以中文原文为键，供纯 toolkit 环境也具备三语。
    /// </summary>
    public static partial class ToolkitEditorL10n
    {
        static partial void RegisterAttributes()
        {
            // ── EFieldType（属性字段类型下拉；仅「多语言设定 → 枚举值」勾选后生效）───────
            AddEnum(EFieldType.Int,               "Int",               "整数",                   "整数");
            AddEnum(EFieldType.Float,             "Float",             "浮動小数点数",           "浮点数");
            AddEnum(EFieldType.String,            "String",            "文字列",                 "字符串");
            AddEnum(EFieldType.Bool,              "Bool",              "ブール値",               "布尔值");
            AddEnum(EFieldType.Enum,              "Enum",              "列挙",                   "枚举");
            AddEnum(EFieldType.Text,              "Text",              "テキスト",               "文本");
            AddEnum(EFieldType.Vector2,           "Vector2",           "Vector2",                "二维向量");
            AddEnum(EFieldType.Vector3,           "Vector3",           "Vector3",                "三维向量");
            AddEnum(EFieldType.Vector4,           "Vector4",           "Vector4",                "四维向量");
            AddEnum(EFieldType.VectorInt2,        "VectorInt2",        "VectorInt2",             "二维整数向量");
            AddEnum(EFieldType.VectorInt3,        "VectorInt3",        "VectorInt3",             "三维整数向量");
            AddEnum(EFieldType.VectorInt4,        "VectorInt4",        "VectorInt4",             "四维整数向量");
            AddEnum(EFieldType.StringIntPair,     "StringIntPair",     "文字列-整数ペア",        "字符串-整数对");
            AddEnum(EFieldType.EnumIntPair,       "EnumIntPair",       "列挙-整数ペア",          "枚举-整数对");
            AddEnum(EFieldType.Color,             "Color",             "カラー",                 "颜色");
            AddEnum(EFieldType.Prefab,            "Prefab",            "プレハブ",               "预制体");
            AddEnum(EFieldType.Sprite,            "Sprite",            "スプライト",             "精灵图");
            AddEnum(EFieldType.Texture,           "Texture",           "テクスチャ",             "贴图");
            AddEnum(EFieldType.Material,          "Material",          "マテリアル",             "材质");
            AddEnum(EFieldType.AudioClip,         "AudioClip",         "オーディオクリップ",     "音频剪辑");
            AddEnum(EFieldType.AnimationClip,     "AnimationClip",     "アニメーションクリップ", "动画剪辑");
            AddEnum(EFieldType.AnimationCurve,    "AnimationCurve",    "アニメーションカーブ",   "动画曲线");
            AddEnum(EFieldType.PhysicsMaterial,   "PhysicsMaterial",   "物理マテリアル",         "物理材质");
            AddEnum(EFieldType.PhysicsMaterial2D, "PhysicsMaterial2D", "物理マテリアル 2D",      "物理材质 2D");

            // ── 枚举类型面板（EnumTypePanel）──────────────────────────────────────
            Add("请选择或新建一个枚举类型。",
                "Select or create an enum type.",
                "列挙型を選択または新規作成してください。");
            Add("枚举名称", "Enum Name", "列挙名");
            Add("枚举项属性字段", "Enum Item Attribute Fields", "列挙項目の属性フィールド");
            Add("▸ {0}  的属性值", "▸ {0} — attribute values", "▸ {0} の属性値");
            Add("枚举项（值由系统分配、只读；点击行选中以编辑属性值）",
                "Enum items (values are system-assigned and read-only; click a row to edit its attribute values)",
                "列挙項目（値はシステムが自動採番・読み取り専用。行をクリックして属性値を編集）");

            // ── 属性值显示（AttributeFieldDrawer）─────────────────────────────────
            Add("是", "Yes", "はい");
            Add("否", "No",  "いいえ");
            Add("曲线({0}帧)", "Curve ({0} keys)", "カーブ（{0} フレーム）");

            // ── 数字格式面板（NumberFormatConfigPanel）────────────────────────────
            Add("（未命名）", "(Unnamed)",    "（名称未設定）");
            Add("新格式",     "New Format",   "新規フォーマット");
            Add("请选择或新建一个数字格式配置。",
                "Select or create a number-format config.",
                "数値フォーマット設定を選択または新規作成してください。");
            Add("配置名称", "Config Name", "設定名");
            Add("⚠ 名称为空时无法被引用。",
                "⚠ A config with an empty name cannot be referenced.",
                "⚠ 名称が空の設定は参照できません。");
            Add("⚠ 名称重复，引用将命中第一个同名配置。",
                "⚠ Duplicate name; references will match the first config with this name.",
                "⚠ 名称が重複しています。参照は最初の同名設定に一致します。");
            Add("语言与规则", "Languages & Rules", "言語とルール");
        }
    }
}
