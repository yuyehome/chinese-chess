// File: _Scripts/UI/PlayerInfoDisplay.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks; 
using System.Collections;

public class PlayerInfoDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private EnergyBarSegmentsUI energyBar;

    private Coroutine _fetchAvatarCoroutine;

    private void Awake()
    {
        if (energyBar != null)
        {
            energyBar.gameObject.SetActive(false);
        }
    }

    public void SetPlayerData(PlayerNetData? data)
    {
        if (_fetchAvatarCoroutine != null)
        {
            StopCoroutine(_fetchAvatarCoroutine);
            _fetchAvatarCoroutine = null;
        }

        if (!data.HasValue)
        {
            nicknameText.text = "等待玩家...";
            avatarImage.texture = null;
            avatarImage.color = new Color(0, 0, 0, 0.5f);
            if (energyBar != null) energyBar.gameObject.SetActive(false);
            return;
        }

        var playerData = data.Value;
        nicknameText.text = playerData.PlayerName;
        _fetchAvatarCoroutine = StartCoroutine(FetchAndDisplayAvatar(playerData.SteamId));

        if (energyBar != null)
        {
            energyBar.gameObject.SetActive(true);
        }
    }

    public void UpdateEnergyDisplay(float currentEnergy, float maxEnergy)
    {
        if (energyBar != null)
        {
            energyBar.UpdateEnergy(currentEnergy, maxEnergy);
        }
    }

    private IEnumerator FetchAndDisplayAvatar(CSteamID steamId)
    {
        int avatarId = SteamFriends.GetLargeFriendAvatar(steamId);

        if (avatarId == -1)
        {
            Callback<AvatarImageLoaded_t> avatarLoadedCallback = null;
            bool isAvatarReady = false;

            avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(result =>
            {
                if (result.m_steamID == steamId)
                {
                    isAvatarReady = true;
                    avatarLoadedCallback?.Dispose();
                }
            });

            float timeout = 5f;
            while (!isAvatarReady && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!isAvatarReady)
            {
                Debug.LogWarning($"[Avatar] 获取 {steamId} 的头像超时。");
                yield break;
            }

            avatarId = SteamFriends.GetLargeFriendAvatar(steamId);
        }

        if (avatarId > 0)
        {
            if (SteamUtils.GetImageSize(avatarId, out uint imageWidth, out uint imageHeight))
            {
                byte[] imageData = new byte[imageWidth * imageHeight * 4];
                if (SteamUtils.GetImageRGBA(avatarId, imageData, imageData.Length))
                {
                    Texture2D avatarTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
                    avatarTexture.LoadRawTextureData(imageData);
                    avatarTexture.Apply();

                    if (avatarImage != null)
                    {
                        avatarImage.texture = avatarTexture;
                        avatarImage.color = Color.white;
                    }
                }
            }
        }

        _fetchAvatarCoroutine = null;
    }
}