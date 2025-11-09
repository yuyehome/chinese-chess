using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引入TextMeshPro命名空间

[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    public string localizationKey;

    private Text _legacyText;
    private TextMeshProUGUI _tmpText;

    void Awake()
    {
        // 兼容两种Text组件
        _legacyText = GetComponent<Text>();
        _tmpText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        LocalizationManager.Instance.OnLanguageChanged += UpdateText;
        UpdateText(); // 立即更新一次
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    private void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey)) return;

        string value = LocalizationManager.Instance.GetText(localizationKey);
        if (_legacyText != null) _legacyText.text = value;
        if (_tmpText != null) _tmpText.text = value;
    }
}