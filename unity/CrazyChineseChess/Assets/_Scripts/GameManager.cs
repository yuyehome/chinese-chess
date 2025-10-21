// File: _Scripts/GameManager.cs

using UnityEngine;
using System;

/// <summary>
/// ��Ϸ�ܹ����� (Singleton)����Ϊ��Ϸ�����߼�����ں�Э���ߡ�
/// ���������Ϸ״̬��ģʽ�л����ͺ��Ĳ�����ִ�С�
/// </summary>
public class GameManager : MonoBehaviour
{
    // ����ʵ��������ȫ�ַ���
    public static GameManager Instance { get; private set; }

    // --- ����ϵͳ���� ---
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }
    public EnergySystem EnergySystem { get; private set; }

    // --- �ڲ�ģ������ ---
    private BoardRenderer boardRenderer;
    private bool isGameEnded = false;

    private void Awake()
    {
        // ��׼����ģʽʵ��
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

        // ��ʼ����������ϵͳ
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem();

        // ����ģʽѡ������ʵ������Ӧ����Ϸģʽ������ (����ģʽ)
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

        // ʵʱģʽ�£�ÿ֡������������Ϸ�߼��ĸ���
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (CurrentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    #region Public Game Actions

    /// <summary>
    /// ִ���ƶ��ļ򻯽ӿڣ���Ҫ���غ���ģʽ���á�
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        ExecuteMove(from, to, null, null);
    }

    /// <summary>
    /// ִ��һ�������ƶ����Ӿ����ֺ��߼���ʼ��
    /// ע�⣺�˷������ٴ����κ�ս���ж���ֻ�������Ӵ�BoardState�С����𡱣��������Ӿ��ƶ���
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        if (isGameEnded) return;

        // ���߼��Ͻ����Ӵ���㡰���𡱣�ʹ�����ƶ������в�ռ����ʼ��
        CurrentBoardState.RemovePieceAt(from);

        // ����������Ⱦ�����ƶ������������ݻص�����
        boardRenderer.MovePiece(from, to, CurrentBoardState, onProgressUpdate, onComplete);
    }

    /// <summary>
    /// ������Ϸ�������߼���
    /// </summary>
    public void HandleEndGame(GameStatus status)
    {
        if (isGameEnded) return; // ��ֹ�ظ�����
        isGameEnded = true;
        Debug.Log($"[GameFlow] ��Ϸ���������: {status}");

        // ����������룬��ֹ��Ϸ�������������
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }

    #endregion

    /// <summary>
    /// ��鲢��ӡ����״̬��Ŀǰ��Ҫ�ڻغ���ģʽ������ʾ���塣
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
}