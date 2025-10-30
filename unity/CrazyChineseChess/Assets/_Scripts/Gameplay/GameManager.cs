using UnityEngine;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// ��Ϸ�ܹ����� (Singleton)����Ϊ��Ϸ�����߼�����ں�Э���ߡ�
/// �̳���NetworkBehaviour������������ͬ��������������ֵ����
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    [Header("��Ϸƽ��������")]
    [SerializeField] private float maxEnergy = 4.0f;
    [SerializeField] private float energyRecoveryRate = 0.3f;
    [SerializeField] private int moveCost = 1;
    [SerializeField] private float startEnergy = 2.0f;
    [SerializeField] private float collisionDistance = 0.0175f;

    // --- ��������: ʹ����ȷ�� SyncTypeSetting ���� ---
    public readonly SyncVar<float> RedPlayerEnergy = new SyncVar<float>();
    public readonly SyncVar<float> BlackPlayerEnergy = new SyncVar<float>();

    // --- ����ϵͳ���� ---
    public BoardState CurrentBoardState { get; private set; }
    private GameModeController currentGameMode;
    public EnergySystem EnergySystem { get; private set; }
    public BoardRenderer BoardRenderer { get; private set; }
    public bool IsGameEnded { get; private set; } = false;

    private bool isPVPMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        RedPlayerEnergy.Value = startEnergy;
        BlackPlayerEnergy.Value = startEnergy;
    }

    void Start()
    {
        BoardRenderer = BoardRenderer.Instance;
        if (BoardRenderer == null)
        {
            Debug.LogError("�������Ҳ��� BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem(maxEnergy, energyRecoveryRate, moveCost);

        if (isPVPMode)
        {
            GameNetworkManager.OnNetworkStart += HandleNetworkStart;
        }
        else
        {
            RedPlayerEnergy.Value = startEnergy;
            BlackPlayerEnergy.Value = startEnergy;
            InitializeGameModeForPVE();
            BoardRenderer.RenderBoard(CurrentBoardState);
        }
    }

    private void OnDestroy()
    {
        GameNetworkManager.OnNetworkStart -= HandleNetworkStart;
    }

    private void HandleNetworkStart(bool isServer)
    {
        if (currentGameMode != null) return;

        Debug.Log($"[GameManager] �ӵ���������֪ͨ. IsServer: {isServer}");

        float collisionDistanceSquared = collisionDistance * collisionDistance;
        var rtController = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);

        if (isServer)
        {
            rtController.CombatManager.OnPieceKilled += HandlePieceKilled;
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.Server_InitializeBoard(CurrentBoardState);
            }
        }

        currentGameMode = rtController;
    }

    private void InitializeGameModeForPVE()
    {
        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                currentGameMode = new TurnBasedModeController(this, CurrentBoardState, BoardRenderer);
                break;
            case GameModeType.RealTime:
                float collisionDistanceSquared = collisionDistance * collisionDistance;
                var rtController = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);
                rtController.CombatManager.OnPieceKilled += HandlePieceKilled;
                currentGameMode = rtController;
                break;
        }
    }

    private void Update()
    {
        if (IsGameEnded || currentGameMode == null) return;

        // ������Ȩ����������
        if (IsServer)
        {
            if (currentGameMode is RealTimeModeController)
            {
                RedPlayerEnergy.Value = EnergySystem.Tick(RedPlayerEnergy.Value);
                BlackPlayerEnergy.Value = EnergySystem.Tick(BlackPlayerEnergy.Value);
            }
        }
        else if (!isPVPMode && GameModeSelector.SelectedMode == GameModeType.RealTime) // ����ģʽҲ��������
        {
            RedPlayerEnergy.Value = EnergySystem.Tick(RedPlayerEnergy.Value);
            BlackPlayerEnergy.Value = EnergySystem.Tick(BlackPlayerEnergy.Value);
        }

        // �������򵥻�ʵʱģʽ�£�������Ϸ�߼�����
        if (IsServer || (!isPVPMode && GameModeSelector.SelectedMode == GameModeType.RealTime))
        {
            if (currentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
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

    public void Client_RequestMove(Vector2Int from, Vector2Int to)
    {
        if (isPVPMode)
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
        else
        {
            Local_ProcessMoveRequest(PlayerColor.Red, from, to);
        }
    }

    public void Server_ProcessMoveRequest(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (!IsServer || IsGameEnded) return;

        if (!EnergySystem.CanSpendEnergy(color == PlayerColor.Red ? RedPlayerEnergy.Value : BlackPlayerEnergy.Value))
        {
            Debug.LogWarning($"[Server] ��� {color} �ƶ����󱻾ܣ��������㡣");
            return;
        }

        if (color == PlayerColor.Red)
        {
            RedPlayerEnergy.Value = EnergySystem.SpendEnergy(RedPlayerEnergy.Value);
        }
        else
        {
            BlackPlayerEnergy.Value = EnergySystem.SpendEnergy(BlackPlayerEnergy.Value);
        }

        if (currentGameMode is RealTimeModeController rtController)
        {
            Debug.Log($"[Server] ��֤ͨ��������ִ����� {color} ���ƶ�: �� {from} �� {to}");
            PieceComponent pieceToMove = rtController.ExecuteMoveCommand(from, to);

            if (pieceToMove != null)
            {
                pieceToMove.Observer_PlayMoveAnimation(from, to);
            }
        }
    }

    private void Local_ProcessMoveRequest(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (IsGameEnded) return;

        if (!EnergySystem.CanSpendEnergy(color == PlayerColor.Red ? RedPlayerEnergy.Value : BlackPlayerEnergy.Value)) return;

        if (color == PlayerColor.Red)
        {
            RedPlayerEnergy.Value = EnergySystem.SpendEnergy(RedPlayerEnergy.Value);
        }
        else
        {
            BlackPlayerEnergy.Value = EnergySystem.SpendEnergy(BlackPlayerEnergy.Value);
        }

        if (currentGameMode is RealTimeModeController rtController)
        {
            PieceComponent pieceToMove = rtController.ExecuteMoveCommand(from, to);

            if (pieceToMove != null)
            {
                pieceToMove.Observer_PlayMoveAnimation(from, to);
            }
        }
    }

    public void RequestMove(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (isPVPMode)
        {
            if (IsServer)
            {
                Server_ProcessMoveRequest(color, from, to);
            }
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
        var aiControllers = GetComponents<AIController>();
        foreach (var ai in aiControllers) ai.enabled = false;
    }

    private void HandlePieceKilled(PieceComponent killedPiece)
    {
        if (killedPiece == null) return;

        if (killedPiece.RTState.IsMoving)
        {
            BoardRenderer.RemovePiece(killedPiece);
        }
        else
        {
            CurrentBoardState.RemovePieceAt(killedPiece.RTState.LogicalPosition);
            BoardRenderer.RemovePieceAt(killedPiece.RTState.LogicalPosition);
        }

        if (killedPiece.Type.Value == PieceType.General)
        {
            GameStatus status = (killedPiece.Color.Value == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            HandleEndGame(status);
        }
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

    #endregion

    /// <summary>
    /// ����AI���ߵ�����ģ�����ݽṹ��
    /// </summary>
    public struct SimulatedPiece
    {
        public Piece PieceData;
        public Vector2Int BoardPosition;
    }
}