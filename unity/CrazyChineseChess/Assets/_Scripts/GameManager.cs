using UnityEngine;
using System;

/// <summary>
/// ��Ϸ�ܹ���������Ϊ��Ϸ�����߼�����ں�Э���ߡ�
/// ���������Ϸ״̬��ģʽ�л����ͺ��Ĳ�����ִ�С�
/// </summary>
public class GameManager : MonoBehaviour
{
    // ����ģʽ������ȫ�ַ���
    public static GameManager Instance { get; private set; }

    // �������ݺ�ϵͳ������
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }
    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("[Error] �������Ҳ��� BoardRenderer!");
            return;
        }

        // ��ʼ����������
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem();

        // ����ģʽѡ������ʵ������Ӧ����Ϸģʽ������
        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("[System] ��Ϸ��ʼ���ѽ��롾��ͳ�غ��ơ�ģʽ��");
                break;
            case GameModeType.RealTime:
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("[System] ��Ϸ��ʼ���ѽ��롾ʵʱ��ս��ģʽ��");
                break;
            default: // ���÷�֧����֤��Ϸ����������
                Debug.LogWarning("[Warning] δ֪����Ϸģʽ��Ĭ�Ͻ���غ��ơ�");
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        // 1. ��Ⱦ���̣������������ӵ�GameObject
        boardRenderer.RenderBoard(CurrentBoardState);

        // 2. �����ʵʱģʽ�������Ӵ�����Ϻ󣬳�ʼ�����ǵ�ʵʱ״̬
        if (CurrentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }
    }

    private void Update()
    {
        if (isGameEnded) return;

        // ʵʱģʽ�£�ÿ֡������������Ϸ�߼�
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (CurrentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    /// <summary>
    /// ִ���ƶ��ļ򻯽ӿڣ���Ҫ���غ���ģʽ����Ҫ�ص��ĳ���ʹ�á�
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        ExecuteMove(from, to, null, null);
    }

    /// <summary>
    /// ִ��һ�������ƶ������������������߼����º��Ӿ����ִ�����
    /// </summary>
    /// <param name="from">��ʼ����</param>
    /// <param name="to">Ŀ������</param>
    /// <param name="onProgressUpdate">��ʵʱģʽ�ã��������̻ص�</param>
    /// <param name="onComplete">��ʵʱģʽ�ã�������ɻص�</param>
    public void ExecuteMove(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        if (isGameEnded) return;

        Piece movingPieceData = CurrentBoardState.GetPieceAt(from);
        Piece targetPieceData = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPieceData.Type != PieceType.None;

        if (isCapture)
        {
            Debug.Log($"[Combat] ��������: {movingPieceData.Color}����{movingPieceData.Type} ������ {targetPieceData.Color}����{targetPieceData.Type}��");
            boardRenderer.RemovePieceAt(to);
            if (targetPieceData.Type == PieceType.General)
            {
                // ��Ϸ�������վ��ж�
                GameStatus status = (targetPieceData.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                CurrentBoardState.MovePiece(from, to);
                boardRenderer.MovePiece(from, to, CurrentBoardState, true, onProgressUpdate, onComplete);
                HandleEndGame(status);
                return;
            }
        }

        // ������������ģ��
        CurrentBoardState.MovePiece(from, to);
        // ���������Ӿ�����
        boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture, onProgressUpdate, onComplete);

        // ����Ƿ񽫾�����Ҫ���ڻغ��ƣ�
        CheckForCheck();
    }

    /// <summary>
    /// ��鲢��ӡ����״̬��Ŀǰ���ڻغ���ģʽ����ʵ�����塣
    /// </summary>
    private void CheckForCheck()
    {
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"[GameFlow] ������{nextPlayer} ����������������");
            }
        }
    }

    /// <summary>
    /// ������Ϸ�������߼���
    /// </summary>
    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true;
        Debug.Log($"[GameFlow] ��Ϸ���������: {status}");

        // ����������룬��ֹ��Ϸ�������������
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}