// File: _Scripts/Network/LobbyItem.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;

/// <summary>
/// 挂载在Lobby列表项Prefab上的组件。
/// 负责管理自身的UI元素，并提供一个统一的接口来设置显示内容。
/// </summary>
public class LobbyItem : MonoBehaviour
{
    [Header("UI 元素引用")]
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private CSteamID lobbyId;
     
    /// <summary>
    /// 设置此列表项显示的数据，并绑定加入按钮的事件。
    /// </summary>
    public void Setup(CSteamID lobbyId)
    {
        this.lobbyId = lobbyId;

        // 从Steam获取该Lobby的数据
        string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, LobbyManager.LobbyNameKey);
        int currentPlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

        // 安全检查并更新UI
        if (roomNameText != null)
        {
            roomNameText.text = string.IsNullOrWhiteSpace(lobbyName) ? $"房间 [{lobbyId.m_SteamID}]" : lobbyName;
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"{currentPlayers} / {maxPlayers}";
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners(); // 先移除旧的监听器，防止重复添加
            joinButton.onClick.AddListener(OnJoinButtonClick);
        }
    }

    private void OnJoinButtonClick()
    {
        Debug.Log($"[UI] 点击加入按钮，目标Lobby ID: {lobbyId}");
        // 调用LobbyManager的加入功能
        LobbyManager.Instance.JoinLobby(this.lobbyId);
    }
}