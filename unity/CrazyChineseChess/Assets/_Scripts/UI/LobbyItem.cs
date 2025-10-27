// File: _Scripts/Network/LobbyItem.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;

/// <summary>
/// ������Lobby�б���Prefab�ϵ������
/// ������������UIԪ�أ����ṩһ��ͳһ�Ľӿ���������ʾ���ݡ�
/// </summary>
public class LobbyItem : MonoBehaviour
{
    [Header("UI Ԫ������")]
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    private CSteamID lobbyId;
     
    /// <summary>
    /// ���ô��б�����ʾ�����ݣ����󶨼��밴ť���¼���
    /// </summary>
    public void Setup(CSteamID lobbyId)
    {
        this.lobbyId = lobbyId;

        // ��Steam��ȡ��Lobby������
        string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, LobbyManager.LobbyNameKey);
        int currentPlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

        // ��ȫ��鲢����UI
        if (roomNameText != null)
        {
            roomNameText.text = string.IsNullOrWhiteSpace(lobbyName) ? $"���� [{lobbyId.m_SteamID}]" : lobbyName;
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"{currentPlayers} / {maxPlayers}";
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners(); // ���Ƴ��ɵļ���������ֹ�ظ����
            joinButton.onClick.AddListener(OnJoinButtonClick);
        }
    }

    private void OnJoinButtonClick()
    {
        Debug.Log($"[UI] ������밴ť��Ŀ��Lobby ID: {lobbyId}");
        // ����LobbyManager�ļ��빦��
        LobbyManager.Instance.JoinLobby(this.lobbyId);
    }
}