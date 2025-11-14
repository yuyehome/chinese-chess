using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

/// <summary>
/// 定义所有支持的语言
/// </summary>
public enum Language
{
    ZH_CN, // 中文
    EN_US  // 英文
}

// --- 文本本地化数据结构 ---

[Serializable]
public class TextEntry
{
    public string key;
    [TextArea(3, 10)] // 让Value在Inspector里更容易编辑
    public string value;
}

[CreateAssetMenu(fileName = "Language_Text_ZH_CN", menuName = "ChessHonor/Localization/Text Data")]
public class LanguageTextData : ScriptableObject
{
    public Language language;
    public List<TextEntry> texts;
}


// --- 资源本地化数据结构 ---

[Serializable]
public class AssetEntry
{
    public string key;
    public UnityObject asset;
}

[CreateAssetMenu(fileName = "Language_Asset_ZH_CN", menuName = "ChessHonor/Localization/Asset Data")]
public class LanguageAssetData : ScriptableObject
{
    public Language language;
    public List<AssetEntry> assets;
}