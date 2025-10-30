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
            Debug.Log("[GameSetup] ��⵽PVPģʽ���ȴ������������...");
            // �����¼�
            GameNetworkManager.OnLocalPlayerDataReceived += InitializeLocalPlayerForPVP;

            // ���˶��ģ����ǻ�Ҫ���һ�������ǲ����Ѿ�����
            // ��� GameNetworkManager ��ʵ�����ڣ������Ѿ������˱����������
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.LocalPlayerData.Color != PlayerColor.None)
            {
                Debug.Log("[GameSetup] ��⵽�����������ǰ�������ִ�г�ʼ����");
                // ֱ�����Ѿ����ڵ����ݽ��г�ʼ��
                InitializeLocalPlayerForPVP(GameNetworkManager.Instance.LocalPlayerData);
            }
        }
        else
        {
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
        // ��ȫ����ȷ�������ʼ���߼�ִֻ��һ��
        if (this.enabled == false) return;

        Debug.Log($"[GameSetup] �յ�����������ݣ���ʼΪ {localPlayerData.Color} �����ñ�����������...");

        // �Ƴ����ܴ��ڵľ������ȷ�������Ǵ�һ���ɾ���״̬��ʼ
        if (TryGetComponent<PlayerInputController>(out var oldController))
        {
            Destroy(oldController);
        }
        // �������һ���µġ��ɾ��Ŀ��������
        PlayerInputController playerController = gameObject.AddComponent<PlayerInputController>();

        // ��ʼ���������������ἤ����
        playerController.Initialize(localPlayerData.Color, GameManager.Instance);
        Debug.Log($"[GameSetup] PlayerInputController ��Ϊ {localPlayerData.Color} ����ʼ����");

        // ������䵽���Ǻڷ�����ת���
        if (localPlayerData.Color == PlayerColor.Black)
        {
            Debug.Log("[GameSetup] �������Ϊ�ڷ������ڵ�������ӽǡ�");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0.6f, 0.15f);
                mainCamera.transform.rotation = Quaternion.Euler(80f, 180f, 0f);
            }
        }

        // ������ɣ�����������Ϊ����ʹ���Ѿ������
        this.enabled = false;
        Debug.Log("[GameSetup] ��ʼ����ɣ�GameSetupController �ѽ��á�");
    }

    private void InitializeForPVE()
    {
        // �Ƴ����ܴ��ڵľ����
        if (TryGetComponent<PlayerInputController>(out var oldPlayerController)) Destroy(oldPlayerController);
        if (TryGetComponent<TurnBasedInputController>(out var oldTurnBasedController)) Destroy(oldTurnBasedController);
        if (TryGetComponent<AIController>(out var oldAIController)) Destroy(oldAIController);

        // ������Ϸģʽ�������ʵĿ�����
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            // PVEʵʱģʽ�£���ҹ̶�Ϊ�췽
            PlayerInputController playerController = gameObject.AddComponent<PlayerInputController>();
            playerController.Initialize(PlayerColor.Red, GameManager.Instance);

            // �����Ѷ�ѡ�񴴽�AI
            IAIStrategy aiStrategy;
            switch (GameModeSelector.SelectedAIDifficulty)
            {
                case AIDifficulty.VeryHard: aiStrategy = new VeryHardAIStrategy(); break;
                case AIDifficulty.Hard: aiStrategy = new HardAIStrategy(); break;
                case AIDifficulty.Easy: default: aiStrategy = new EasyAIStrategy(); break;
            }
            AIController aiController = gameObject.AddComponent<AIController>();
            aiController.Initialize(PlayerColor.Black, GameManager.Instance);
            aiController.SetupAI(aiStrategy);
        }
        else if (GameModeSelector.SelectedMode == GameModeType.TurnBased)
        {
            // PVE�غ���ģʽ
            TurnBasedInputController turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            turnBasedInput.Initialize(PlayerColor.Red, GameManager.Instance);
            // ������Ը�����Ҫ��ӻغ���AI
        }
        // PVE������ɣ���������
        this.enabled = false;
    }

}