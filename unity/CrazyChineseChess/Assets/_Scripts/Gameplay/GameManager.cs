using UnityEngine;
using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object; // ������������ռ���ʹ�� NetworkBehaviour ������

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

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // isPVPMode���ж�Ӧ��Awake�����
        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;
    }

    void Start()
    {
        BoardRenderer = BoardRenderer.Instance;
        if (BoardRenderer == null)
        {
            Debug.LogError("[Error] �������Ҳ��� BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem(maxEnergy, energyRecoveryRate, moveCost, startEnergy);

        if (isPVPMode)
        {
            Debug.Log("[GameManager] PVPģʽ�Ѽ�⡣���ڶ���GameNetworkManager�������¼�...");
            // ��������GNM���¼��������ڷ������Ϳͻ��˸���׼����ʱ����
            GameNetworkManager.OnNetworkStart += HandleNetworkStart;
            GameNetworkManager.OnLocalPlayerDataReceived += InitializeLocalPlayerController;
        }
        else
        {
            Debug.Log("[System] ��⵽����ģʽ����ʼ��PVE��ս...");
            InitializeForPVE();
        }
    }

    private void OnDestroy()
    {
        // ȷ��ȡ�����������¼�
        GameNetworkManager.OnNetworkStart -= HandleNetworkStart;
        GameNetworkManager.OnLocalPlayerDataReceived -= InitializeLocalPlayerController;
    }

    private void HandleNetworkStart(bool isServer)
    {
        Debug.Log($"[GameManager] �ӵ���������֪ͨ. IsServer: {isServer}");
        if (isServer)
        {
            // �������˵ĳ�ʼ��
            if (currentGameMode != null) return;
            Debug.Log("[GameManager-Server] ���ڳ�ʼ������������Ϸģʽ...");
            float collisionDistanceSquared = collisionDistance * collisionDistance;
            var rtController = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);
            rtController.CombatManager.OnPieceKilled += HandlePieceKilled;
            currentGameMode = rtController;

            // ����GNM��������
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.Server_InitializeBoard(CurrentBoardState);
            }
        }
        else
        {
            // �ͻ��˶˵ĳ�ʼ��
            if (currentGameMode != null) return;
            Debug.Log("[GameManager-Client] ����Ϊ�ͻ��˳�ʼ����Ϸģʽ...");
            currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, 0);
        }
    }


    /// <summary>
    /// ���ӷ��������յ�������ҵ����ݺ󣬴˷��������á�
    /// </summary>
    public void InitializeLocalPlayerController(PlayerNetData localPlayerData)
    {
        if (controllers.ContainsKey(localPlayerData.Color))
        {
            Debug.LogWarning($"[DIAG-5B] Controller for {localPlayerData.Color} already exists. Aborting.");
            return;
        }

        PlayerInputController playerController = GetComponent<PlayerInputController>();
        if (playerController == null)
        {
            playerController = gameObject.AddComponent<PlayerInputController>();
        }
        else
        {
        }

        if (playerController != null)
        {
            playerController.Initialize(localPlayerData.Color, this);
            controllers.Add(localPlayerData.Color, playerController);

            // ����Ǻڷ�����ת���
            if (localPlayerData.Color == PlayerColor.Black)
            {
                Debug.Log("[Client Setup] ��⵽�������Ϊ�ڷ������ڵ����ӽ�...");
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.transform.rotation = Quaternion.Euler(0, 180f, 0);
                }
            }
        }
        else
        {
            Debug.LogError("[DIAG-5G-ERROR] FATAL: playerController is NULL after get/add! Cannot initialize.");
        }
    }

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

        InitializeControllers();
        BoardRenderer.RenderBoard(CurrentBoardState);

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
            if (playerController == null) playerController = gameObject.AddComponent<PlayerInputController>();
            playerController.Initialize(PlayerColor.Red, this);
            controllers.Add(PlayerColor.Red, playerController);

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
            AIController aiController = gameObject.AddComponent<AIController>();
            aiController.Initialize(PlayerColor.Black, this);
            aiController.SetupAI(aiStrategy);
            controllers.Add(PlayerColor.Black, aiController);
        }
        else if (currentGameMode is TurnBasedModeController)
        {
            TurnBasedInputController turnBasedInput = GetComponent<TurnBasedInputController>();
            if (turnBasedInput == null) turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            turnBasedInput.Initialize(PlayerColor.Red, this);
        }
    }

    private void Update()
    {
        if (IsGameEnded) return;

        if (InstanceFinder.IsServer)
        {
            if (currentGameMode is RealTimeModeController rtController)
            {
                EnergySystem?.Tick();
                rtController.Tick();
            }
        }
        else if (!isPVPMode)
        {
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

    public struct SimulatedPiece
    {
        public Piece PieceData;
        public Vector2Int BoardPosition;
    }

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

    public List<PieceComponent> GetAllPiecesOfColor(PlayerColor color)
    {
        var pieces = new List<PieceComponent>();
        if (BoardRenderer == null) return pieces;

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                PieceComponent pc = BoardRenderer.GetPieceComponentAt(new Vector2Int(x, y));
                if (pc != null && pc.Color.Value == color && pc.RTState != null && !pc.RTState.IsDead)
                {
                    pieces.Add(pc);
                }
            }
        }
        return pieces;
    }

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
                    var tempPc = new GameObject("TempPiece").AddComponent<PieceComponent>();
                    if (tempPc.PieceData.Color == color)
                    {
                        pieces.Add(tempPc);
                    }
                    Destroy(tempPc.gameObject);
                }
            }
        }
        return pieces;
    }

    /// <summary>
    /// [Client-Side Entry] ���ͻ��˵�������������ã����ڷ���һ���ƶ�����
    /// </summary>
    public void Client_RequestMove(Vector2Int from, Vector2Int to)
    {
        if (GameNetworkManager.Instance != null)
        {
            Debug.Log($"[Client] �����ƶ����󵽷�����: �� {from} �� {to}");
            GameNetworkManager.Instance.CmdRequestMove(from, to);
        }
        else
        {
            Debug.LogError("[Client] Client_RequestMove �޷��ҵ� GameNetworkManager.Instance��");
        }
    }

    /// <summary>
    /// [Server-Side Logic] ��������������֤���ƶ�����
    /// </summary>
    public void Server_ProcessMoveRequest(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        // �ؼ�������ȷ�����߼�ֻ�ڷ�����������
        if (!InstanceFinder.IsServer) return;

        if (IsGameEnded) return;

        // �������Ⱥ����߼�������ȫ�ڷ������Ͻ���
        if (!EnergySystem.CanSpendEnergy(color))
        {
            Debug.LogWarning($"[Server] ��� {color} ���ƶ����󱻾ܾ����������㡣");
            return;
        }
        EnergySystem.SpendEnergy(color);

        // ���ƶ�ָ���ʵʱģʽ������ִ��
        if (currentGameMode is RealTimeModeController rtController)
        {
            Debug.Log($"[Server] ��֤ͨ��������ִ����� {color} ���ƶ�: �� {from} �� {to}");
            PieceComponent pieceToMove = rtController.ExecuteMoveCommand(from, to);

            // ����ɹ�ִ�����ƶ��߼�������������������пͻ����ϲ��Ŷ���
            if (pieceToMove != null)
            {
                pieceToMove.Observer_PlayMoveAnimation(from, to);
            }
        }
    }

    /// <summary>
    /// [Local/Single-Player Logic] ����ģʽ�´����ƶ������˽�з�����
    /// </summary>
    private void Local_ProcessMoveRequest(PlayerColor color, Vector2Int from, Vector2Int to)
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
            PieceComponent pieceToMove = rtController.ExecuteMoveCommand(from, to);

            if (pieceToMove != null)
            {
                pieceToMove.Observer_PlayMoveAnimation(from, to);
            }
        }
    }

    /// <summary>
    /// [ͳһ���] �����ƶ����ӡ�
    /// �����������ݵ�ǰ������ģʽ���ǵ���ģʽ�������Ƿ���RPC����ֱ��ִ�б����߼���
    /// </summary>
    public void RequestMove(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (isPVPMode)
        {
            Client_RequestMove(from, to);
        }
        else
        {
            Local_ProcessMoveRequest(color, from, to);
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

        if (killedPiece.Type.Value == PieceType.General)
        {
            Debug.Log($"[GameFlow] {killedPiece.Type.Value} ����ɱ����Ϸ������");
            GameStatus status = (killedPiece.Color.Value == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            HandleEndGame(status);
        }
    }
    #endregion
}