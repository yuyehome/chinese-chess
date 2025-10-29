// File: _Scripts/Core/GameManager.cs

using UnityEngine;
using System;
using System.Collections.Generic;
using FishNet; 
using FishNet.Object.Synchronizing; 

/// <summary>
/// ��Ϸ�ܹ����� (Singleton)����Ϊ��Ϸ�����߼�����ں�Э���ߡ�
/// ���������Ϸ״̬��ģʽ�л����ͺ��Ĳ�����ִ�С�
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("��Ϸƽ��������")]
    [SerializeField]
    [Tooltip("�������ֵ")]
    private float maxEnergy = 4.0f;
    [SerializeField]
    [Tooltip("����ÿ��ָ�����")]
    private float energyRecoveryRate = 0.3f;
    [SerializeField]
    [Tooltip("�ƶ�һ�����ĵ���������")]
    private int moveCost = 1;
    [SerializeField]
    [Tooltip("����ʱ�ĳ�ʼ����")]
    private float startEnergy = 2.0f;
    [SerializeField]
    [Tooltip("ʵʱģʽ�£�������ײ���ж�����")]
    private float collisionDistance = 0.0175f;

    // --- ����ϵͳ���� ---
    public BoardState CurrentBoardState { get; private set; }
    private GameModeController currentGameMode;
    public EnergySystem EnergySystem { get; private set; }
    public BoardRenderer BoardRenderer { get; private set; }
    public bool IsGameEnded { get; private set; } = false;

    // --- ���������� ---
    private Dictionary<PlayerColor, IPlayerController> controllers = new Dictionary<PlayerColor, IPlayerController>();

    private bool isPVPMode = false;

    private bool pvpInitialized = false; // ����һ����־λ����ֹ�ظ���ʼ��

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

    }

    void Start()
    {
        BoardRenderer = FindObjectOfType<BoardRenderer>();
        if (BoardRenderer == null)
        {
            Debug.LogError("[Error] �������Ҳ��� BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem(maxEnergy, energyRecoveryRate, moveCost, startEnergy);

        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

        if (isPVPMode)
        {
            Debug.Log("[System] ��⵽PVPģʽ�����ڵȴ�GameNetworkManager����...");

            // �ͻ��˺ͷ���������Ҫ����
            GameNetworkManager.OnInstanceReady += HandlePVPInitialization;
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.IsSpawned)
            {
                HandlePVPInitialization(GameNetworkManager.Instance);
            }
        }
        else
        {
            Debug.Log("[System] ��⵽����ģʽ����ʼ��PVE��ս...");
            InitializeForPVE();
        }
    }


    private void OnDestroy()
    {
        // ������ʱȡ�����ģ����Ǹ���ϰ��
        GameNetworkManager.OnInstanceReady -= HandlePVPInitialization;
    }

    private void HandlePVPInitialization(GameNetworkManager gnm)
    {
        // ��ΪHost��ͬʱ��Server��Client����һ����־λȷ�������߼�ִֻ��һ��
        if (pvpInitialized) return;
        pvpInitialized = true;

        GameNetworkManager.OnInstanceReady -= HandlePVPInitialization; // ȡ�����ģ���ֹ�ظ�ִ��

        Debug.Log("[GameManager] GameNetworkManager �Ѿ�������ʼPVP��ʼ����");

        // --- ���������߼� ---
        if (InstanceFinder.IsServer)
        {
            Debug.Log("[Server] ���������ڳ�ʼ����Ϸ�߼�ģ��...");
            // 1. �����߼������� (����)
            float collisionDistanceSquared = collisionDistance * collisionDistance;
            var rtController = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);
            rtController.CombatManager.OnPieceKilled += HandlePieceKilled;
            currentGameMode = rtController;

            // 2. [�����޸�] ����BoardRenderer�ڷ��������������绯�����̶���
            BoardRenderer.Server_SpawnBoard(CurrentBoardState);
            Debug.Log("[Server] ������ BoardRenderer �������绯���̡�");
        }

        // --- �ͻ����߼� ---
        if (InstanceFinder.IsClient)
        {
            Debug.Log("[Client] �ͻ�����׼���������ȴ�������Spawn�������(����)...");

            // �������ص���Ϸģʽ������ʵ�������ڴ���δ����������Ӿ�Ч��
            if (currentGameMode == null)
            {
                currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, 0);
            }

            // �ͻ����������ע���Լ�����Ϣ
            if (SteamManager.Instance != null && SteamManager.Instance.IsSteamInitialized)
            {
                // �ȴ�����ʵ��׼����
                if (GameNetworkManager.Instance != null)
                {
                    GameNetworkManager.Instance.CmdRegisterPlayer(SteamManager.Instance.PlayerSteamId, SteamManager.Instance.PlayerName);
                    Debug.Log("[Client] ����������������ע������");
                }
                else
                {
                    // ����һ�������ϵı߽��������Ϊ�¼�������InstanceӦ���Ѿ�����
                    Debug.LogError("[Client] ����ע����ң��� GameNetworkManager.Instance Ϊ�գ�");
                }
            }
        }
    }

    /// <summary>
    /// ��ʼ������PVEģʽ
    /// </summary>
    private void InitializeForPVE()
    {
        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                currentGameMode = new TurnBasedModeController(this, CurrentBoardState, BoardRenderer);
                Debug.Log("[System] ��Ϸ��ʼ���ѽ��롾��ͳ�غ��ơ�ģʽ��");
                break;
            case GameModeType.RealTime:
                float collisionDistanceSquared = collisionDistance * collisionDistance;
                currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);
                ((RealTimeModeController)currentGameMode).CombatManager.OnPieceKilled += HandlePieceKilled;
                Debug.Log("[System] ��Ϸ��ʼ���ѽ��롾ʵʱ��ս��ģʽ��");
                break;
            default:
                Debug.LogWarning("[Warning] δ֪����Ϸģʽ��Ĭ�Ͻ���غ��ơ�");
                currentGameMode = new TurnBasedModeController(this, CurrentBoardState, BoardRenderer);
                break;
        }

        InitializeControllers(); // ����ԭ�еĿ�������ʼ������

        BoardRenderer.RenderBoard(CurrentBoardState);

        if (currentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }
    }

    private void InitializeControllers()
    {
        if (isPVPMode)
        {
            Debug.LogWarning("[GameManager] InitializeControllers ��PVPģʽ�±����ã�������Ǹ�����PVP��������ʼ��Ӧ�е������߼���");
            return;
        }

        if (currentGameMode is RealTimeModeController)
        {
            PlayerInputController playerController = GetComponent<PlayerInputController>();
            if (playerController == null)
            {
                playerController = gameObject.AddComponent<PlayerInputController>();
            }
            playerController.Initialize(PlayerColor.Red, this);
            controllers.Add(PlayerColor.Red, playerController);

            // AI��������ʼ���߼�����

            // AI��������ʼ��
            IAIStrategy aiStrategy;

            switch (GameModeSelector.SelectedAIDifficulty)
            {
                case AIDifficulty.VeryHard:
                    aiStrategy = new VeryHardAIStrategy();
                    break;
                case AIDifficulty.Hard:
                    aiStrategy = new EasyAIStrategy();
                    break;
                case AIDifficulty.Easy:
                default:
                    aiStrategy = new EasyAIStrategy();
                    break;
            }
            // ����������ʼ��AI������
            AIController aiController = gameObject.AddComponent<AIController>();
            aiController.Initialize(PlayerColor.Black, this);
            aiController.SetupAI(aiStrategy);

            controllers.Add(PlayerColor.Black, aiController);

        }
        else if (currentGameMode is TurnBasedModeController)
        {
            TurnBasedInputController turnBasedInput = GetComponent<TurnBasedInputController>();
            if (turnBasedInput == null)
            {
                turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            }
            turnBasedInput.Initialize(PlayerColor.Red, this);
        }
    }

    private void Update()
    {
        if (IsGameEnded) return;

        // ��PVPģʽ�£������߼���ֻ�ڷ�������Tick
        if (InstanceFinder.IsServer)
        {
            // ʵʱģʽ��Tick�߼�
            if (currentGameMode is RealTimeModeController rtController)
            {
                EnergySystem?.Tick();
                rtController.Tick();
            }
            // ���δ���лغ�������棬����������������Tick�߼�
        }
        else if (!isPVPMode) // ����ǵ���ģʽ
        {
            // ԭ�еĵ���Tick�߼�
            if (GameModeSelector.SelectedMode == GameModeType.RealTime)
            {
                EnergySystem?.Tick();
                if (currentGameMode is RealTimeModeController rtController)
                {
                    rtController.Tick();
                }
            }

        }

    }

    // --- ����һ���������ṹ�壬�����ں�̨�̴߳���������Ϣ ---
    public struct SimulatedPiece
    {
        public Piece PieceData;
        public Vector2Int BoardPosition;
    }

    /// <summary>
    /// [�̰߳�ȫ] ��һ��������BoardState�У���ȡ����ָ����ɫ���������ӵ�(ģ��)��Ϣ�б�
    /// </summary>
    public List<SimulatedPiece> GetSimulatedPiecesOfColorFromBoard(PlayerColor color, BoardState board)
    {
        var pieces = new List<SimulatedPiece>();
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece pieceData = board.GetPieceAt(pos);
                if (pieceData.Type != PieceType.None && pieceData.Color == color)
                {
                    pieces.Add(new SimulatedPiece { PieceData = pieceData, BoardPosition = pos });
                }
            }
        }
        return pieces;
    }

    #region Public Game Actions & Helpers

    // --- NEW METHOD START ---
    /// <summary>
    /// ��ȡ����������ָ����ɫ�����д�����ӵ��б�
    /// ��Ҫ��AI������ʹ�ã��Ի�ȡ��ɲ����ĵ�λ��
    /// </summary>
    public List<PieceComponent> GetAllPiecesOfColor(PlayerColor color)
    {
        var pieces = new List<PieceComponent>();
        if (BoardRenderer == null) return pieces;

        // ������������Ѱ������
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                PieceComponent pc = BoardRenderer.GetPieceComponentAt(new Vector2Int(x, y));
                if (pc != null && pc.PieceData.Color == color && pc.RTState != null && !pc.RTState.IsDead)
                {
                    pieces.Add(pc);
                }
            }
        }

        // ע�⣺�����ѭ��ֻ�����˾�ֹ�����ӡ���ʵʱģʽ�£�AIҲӦ���ܻ�ȡ�������ƶ��еļ������ӡ�
        // ��Ϊ�˼��������ǰAI�����ǲ������ƶ��е����ӣ�������ʱ���Ժ��ԡ�
        // ���δ����Ҫ�����ӵ�AI����ı��ƶ������ӵ�Ŀ�꣩������Ҫ��RealTimeModeController�л�ȡmovingPieces�б�ɸѡ��

        return pieces;
    }

    /// <summary>
    /// ��һ��������BoardState�У���ȡ����ָ����ɫ���������ӵ�(��ʱ)PieceComponent�б�
    /// ��Ҫ��Minimax�㷨��ģ��������ʹ�á�
    /// </summary>
    public List<PieceComponent> GetAllPiecesOfColorFromBoard(PlayerColor color, BoardState board)
    {
        var pieces = new List<PieceComponent>();
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece pieceData = board.GetPieceAt(pos);
                if (pieceData.Type != PieceType.None && pieceData.Color == color)
                {
                    // ����һ����ʱ��Component��ֻ������Ҫ��Ϣ
                    pieces.Add(new PieceComponent { PieceData = pieceData, BoardPosition = pos });
                }
            }
        }
        return pieces;
    }


    public void RequestMove(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (IsGameEnded) return;

        if (!EnergySystem.CanSpendEnergy(color))
        {
            Debug.LogWarning($"[GameManager] ���� {color} ���ƶ����󱻾ܾ����������㡣");
            return;
        }
        EnergySystem.SpendEnergy(color);

        if (currentGameMode is RealTimeModeController rtController)
        {
            rtController.ExecuteMoveCommand(from, to);
        }
    }

    public BoardState GetLogicalBoardState()
    {
        if (currentGameMode is RealTimeModeController rtController)
        {
            return rtController.GetLogicalBoardState();
        }
        return CurrentBoardState;
    }

    public GameModeController GetCurrentGameMode()
    {
        return currentGameMode;
    }

    public void HandleEndGame(GameStatus status)
    {
        if (IsGameEnded) return;
        IsGameEnded = true;
        Debug.Log($"[GameFlow] ��Ϸ���������: {status}");

        // ������Һ�AI���������
        var playerInput = GetComponent<PlayerInputController>();
        if (playerInput != null) playerInput.enabled = false;
        var turnBasedInput = GetComponent<TurnBasedInputController>();
        if (turnBasedInput != null) turnBasedInput.enabled = false;
        var aiInput = GetComponent<AIController>();
        if (aiInput != null) aiInput.enabled = false;

    }

    private void HandlePieceKilled(PieceComponent killedPiece)
    {
        if (killedPiece == null) return;

        Debug.Log($"[GameManager] �յ� {killedPiece.name} �������¼���");

        if (killedPiece.RTState.IsMoving)
        {
            Debug.Log($"[GameManager] ���������� {killedPiece.name} �����ƶ���ֱ���Ƴ���GameObject��");
            BoardRenderer.RemovePiece(killedPiece);
        }
        else
        {
            Debug.Log($"[GameManager] ���������� {killedPiece.name} �Ǿ�ֹ�ģ��������Ƴ�������ģ�͡�");
            CurrentBoardState.RemovePieceAt(killedPiece.RTState.LogicalPosition);
            BoardRenderer.RemovePieceAt(killedPiece.RTState.LogicalPosition);
        }

        if (killedPiece.PieceData.Type == PieceType.General)
        {
            Debug.Log($"[GameFlow] {killedPiece.PieceData.Type} ����ɱ����Ϸ������");
            GameStatus status = (killedPiece.PieceData.Color == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            HandleEndGame(status);
        }
    }
    #endregion
}