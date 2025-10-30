using UnityEngine;
using FishNet;

/// <summary>
/// ��Ϸ�����������ߺ����ÿ����� (Orchestrator)��
/// ����Ψһְ���Ǹ��ݵ�ǰ��PVP����PVEģʽ����ȷ�س�ʼ����Ϸ������
/// ����������ҿ�����������AI����������ȡ�
/// ��ɳ�ʼ�������㲻�ٻ��
/// </summary>
public class GameSetupController : MonoBehaviour
{
    private void Start()
    {
        bool isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

        if (isPVPMode)
        {
            // ��PVPģʽ�£����ǲ����������κ��¡�
            // ���ǵȴ� GameNetworkManager ͨ���¼�֪ͨ���Ǳ�����ҵ����ݡ�
            Debug.Log("[GameSetup] ��⵽PVPģʽ���ȴ������������...");
            GameNetworkManager.OnLocalPlayerDataReceived += InitializeLocalPlayerForPVP;
        }
        else
        {
            // ��PVE��������ģʽ�£����������������á�
            Debug.Log("[GameSetup] ��⵽PVEģʽ��������ʼ��������ս...");
            InitializeForPVE();
        }
    }

    private void OnDestroy()
    {
        // ȷ���ڶ�������ʱȡ�����ģ���ֹ�ڴ�й©
        GameNetworkManager.OnLocalPlayerDataReceived -= InitializeLocalPlayerForPVP;
    }

    /// <summary>
    /// [PVP-Callback] ���ӷ��������յ�������ҵ����ݺ󣬴˷��������á�
    /// </summary>
    private void InitializeLocalPlayerForPVP(PlayerNetData localPlayerData)
    {
        Debug.Log($"[GameSetup] �յ�����������ݣ���ʼΪ {localPlayerData.Color} �����ñ�������...");

        // ��ȡ�򴴽� PlayerInputController
        PlayerInputController playerController = GetComponent<PlayerInputController>();
        if (playerController == null)
        {
            playerController = gameObject.AddComponent<PlayerInputController>();
        }

        // ��ʼ���������������ἤ����
        playerController.Initialize(localPlayerData.Color, GameManager.Instance);

        // ������䵽���Ǻڷ���ֱ������Ԥ������λ�ú���ת
        if (localPlayerData.Color == PlayerColor.Black)
        {
            Debug.Log("[GameSetup] �������Ϊ�ڷ�������Ԥ������Transform��");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0.6f, 0.15f);
                mainCamera.transform.rotation = Quaternion.Euler(80f, 180f, 0f);
            }
        }

        // ������ɣ����Խ���������Ϊ����ʹ���Ѿ������
        this.enabled = false;
    }

    /// <summary>
    /// [PVE-Setup] ��ʼ��������Ϸģʽ��
    /// </summary>
    private void InitializeForPVE()
    {
        // PVEģʽ�£���ҹ̶�Ϊ�췽
        PlayerInputController playerController = GetComponent<PlayerInputController>();
        if (playerController == null) playerController = gameObject.AddComponent<PlayerInputController>();
        playerController.Initialize(PlayerColor.Red, GameManager.Instance);

        // �����Ѷ�ѡ�񴴽�AI
        IAIStrategy aiStrategy;
        switch (GameModeSelector.SelectedAIDifficulty)
        {
            case AIDifficulty.VeryHard:
                aiStrategy = new VeryHardAIStrategy();
                break;
            case AIDifficulty.Hard:
                aiStrategy = new HardAIStrategy();
                break;
            case AIDifficulty.Easy:
            default:
                aiStrategy = new EasyAIStrategy();
                break;
        }
        AIController aiController = gameObject.AddComponent<AIController>();
        aiController.Initialize(PlayerColor.Black, GameManager.Instance);
        aiController.SetupAI(aiStrategy);

        // PVE������ɣ���������
        this.enabled = false;
    }
}