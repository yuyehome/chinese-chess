// File: _Scripts/Network/SteamManager.cs

using UnityEngine;
using Steamworks; // ����Steamworks�����ռ�

/// <summary>
/// �ײ����ģ�飬�����ʼ��Steamworks.NET���ṩ�������û���Ϣ��
/// ����һ��ȫ�ֵ�����ȷ����������Ϸ����������ֻ����һ��ʵ����
/// </summary>
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    public bool IsSteamInitialized { get; private set; }
    public string PlayerName { get; private set; }
    public CSteamID PlayerSteamId { get; private set; }

    private void Awake()
    {
        // ʵ�ֵ���ģʽ����ȷ���ڳ����л�ʱ��������
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // ���Գ�ʼ��Steam
        try
        {
            if (SteamAPI.Init())
            {
                IsSteamInitialized = true;
                PlayerName = SteamFriends.GetPersonaName();
                PlayerSteamId = SteamUser.GetSteamID();
                Debug.Log($"[SteamManager] Steam aPI ��ʼ���ɹ�! ���: {PlayerName} ({PlayerSteamId})");
            }
            else
            {
                IsSteamInitialized = false;
                Debug.LogError("[SteamManager] Steam API ��ʼ��ʧ��! ��ȷ��Steam�ͻ����������С�");
            }
        }
        catch (System.Exception e)
        {
            IsSteamInitialized = false;
            Debug.LogError($"[SteamManager] Steam API ��ʼ��ʱ�����쳣: {e.Message}");
        }
    }

    private void Update()
    {
        // Steamworks.NET Ҫ����Update�ж��ڵ��ô˺���������ص�
        if (IsSteamInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnApplicationQuit()
    {
        // ��Ϸ�˳�ʱ���ر�Steam API
        if (IsSteamInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[SteamManager] Steam API �ѹرա�");
        }
    }
}