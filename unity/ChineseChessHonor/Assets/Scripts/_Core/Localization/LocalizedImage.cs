using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class LocalizedImage : MonoBehaviour
{
    public string localizationKey;
    private Image _image;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void OnEnable()
    {
        LocalizationManager.Instance.OnLanguageChanged += UpdateImage;
        UpdateImage();
    }

    void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateImage;
        }
    }

    private void UpdateImage()
    {
        if (string.IsNullOrEmpty(localizationKey)) return;

        _image.sprite = LocalizationManager.Instance.GetAsset<Sprite>(localizationKey);
    }
}