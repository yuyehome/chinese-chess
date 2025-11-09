using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

public class LocalizationManager : PersistentSingleton<LocalizationManager>
{
    [Header("配置")]
    [SerializeField] private List<LanguageTextData> textDataList;
    [SerializeField] private List<LanguageAssetData> assetDataList;
    [SerializeField] private Language defaultLanguage = Language.ZH_CN;

    public event Action OnLanguageChanged;

    public Language CurrentLanguage { get; private set; }

    // 高效查找的数据结构: <语言, <Key, Value>>
    private Dictionary<Language, Dictionary<string, string>> _textDictionaries;
    private Dictionary<Language, Dictionary<string, UnityObject>> _assetDictionaries;

    protected override void Awake()
    {
        base.Awake();
        LoadAndProcessData();

        // 实际项目中, 这里应该读取玩家的存档设置
        // PlayerPrefs.GetString("Language", defaultLanguage.ToString());
        SetLanguage(defaultLanguage);
    }

    private void LoadAndProcessData()
    {
        _textDictionaries = new Dictionary<Language, Dictionary<string, string>>();
        foreach (var data in textDataList)
        {
            var dictionary = data.texts.ToDictionary(entry => entry.key, entry => entry.value);
            _textDictionaries[data.language] = dictionary;
        }

        _assetDictionaries = new Dictionary<Language, Dictionary<string, UnityObject>>();
        foreach (var data in assetDataList)
        {
            var dictionary = data.assets.ToDictionary(entry => entry.key, entry => entry.asset);
            _assetDictionaries[data.language] = dictionary;
        }
    }

    public void SetLanguage(Language language)
    {
        CurrentLanguage = language;
        // 实际项目中, 在这里保存玩家设置
        // PlayerPrefs.SetString("Language", language.ToString());

        Debug.Log($"[LocalizationManager] Language changed to: {language}");
        OnLanguageChanged?.Invoke();
    }

    public string GetText(string key)
    {
        if (_textDictionaries[CurrentLanguage].TryGetValue(key, out string value))
        {
            return value;
        }

        // 如果当前语言找不到, 尝试在默认语言中查找 (Fallback)
        if (CurrentLanguage != defaultLanguage && _textDictionaries[defaultLanguage].TryGetValue(key, out value))
        {
            Debug.LogWarning($"[LocalizationManager] Key '{key}' not found for language '{CurrentLanguage}', using default '{defaultLanguage}'.");
            return value;
        }

        Debug.LogError($"[LocalizationManager] Key '{key}' not found for any language!");
        return $"[MISSING_KEY: {key}]";
    }

    public T GetAsset<T>(string key) where T : UnityObject
    {
        if (_assetDictionaries[CurrentLanguage].TryGetValue(key, out UnityObject asset) && asset is T)
        {
            return asset as T;
        }

        // Fallback
        if (CurrentLanguage != defaultLanguage && _assetDictionaries[defaultLanguage].TryGetValue(key, out asset) && asset is T)
        {
            Debug.LogWarning($"[LocalizationManager] Asset key '{key}' not found for language '{CurrentLanguage}', using default '{defaultLanguage}'.");
            return asset as T;
        }

        Debug.LogError($"[LocalizationManager] Asset key '{key}' of type '{typeof(T)}' not found for any language!");
        return null;
    }
}