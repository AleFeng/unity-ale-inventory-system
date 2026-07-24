using Ale.Inventory.Runtime;

namespace Ale.Inventory.Editor
{
    /// <summary>
    /// 枚举下拉显示名映射。**仅在「多语言设定 → 枚举值」勾选后生效**；
    /// 未勾选时 <see cref="InventoryEditorL10n.TrEnum"/> 一律返回代码中的枚举原名。
    ///
    /// <para>中文名取自各枚举自身的 <c>[InspectorName]</c> 与 XML 文档；英文沿用枚举标识符
    /// （必要处加空格），日文按包内既有日文文档术语。</para>
    /// </summary>
    public static partial class InventoryEditorL10n
    {
        static partial void RegisterEnums()
        {
            // ── EFieldType（属性字段类型下拉）─────────────────────────────────────
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

            // ── ShopType（商店类型）──────────────────────────────────────────────
            AddEnum(ShopType.Sell,    "Sell",     "販売",     "售卖");
            AddEnum(ShopType.Recycle, "Buy-back", "買い取り", "回收");
            AddEnum(ShopType.Barter,  "Barter",   "等価交換", "等价交换");

            // ── ShopRefreshType（刷新周期）───────────────────────────────────────
            AddEnum(ShopRefreshType.Never,   "Never",   "更新しない", "不刷新");
            AddEnum(ShopRefreshType.Daily,   "Daily",   "毎日",       "每日");
            AddEnum(ShopRefreshType.Weekly,  "Weekly",  "毎週",       "每周");
            AddEnum(ShopRefreshType.Monthly, "Monthly", "毎月",       "每月");

            // ── ShopTimeType（刷新时间类型）──────────────────────────────────────
            AddEnum(ShopTimeType.GameTime,   "Game Time",   "ゲーム時間",   "游戏时间");
            AddEnum(ShopTimeType.LocalTime,  "Local Time",  "ローカル時間", "本地时间");
            AddEnum(ShopTimeType.ServerTime, "Server Time", "サーバー時間", "服务器时间");
        }
    }
}
