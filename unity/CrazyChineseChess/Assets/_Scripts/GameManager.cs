// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// ������������Ϸ�ܹ��������վ��ж��߼��Ѹ����Է���GDD��
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }

    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false; // ����һ����־λ����ֹ��Ϸ�������ܼ�������

    public bool IsAnimating { get; private set; } = false; // ������������״̬��

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
            Debug.LogError("�������Ҳ��� BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // ��������ֻ����ʵʱģʽ�²���Ҫ����EnergySystem
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem = new EnergySystem();
        }

        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("��Ϸ��ʼ���ѽ��롾��ͳ�غ��ơ�ģʽ��");
                break;
            case GameModeType.RealTime:
                // ���ؼ�������ȷ�������ｫ�Ѿ������� EnergySystem ʵ�����ݽ�ȥ
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("��Ϸ��ʼ���ѽ��롾ʵʱ��ս��ģʽ��");
                break;
            default:
                Debug.LogError("δ֪����Ϸģʽ��Ĭ�Ͻ���غ��ơ�");
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        boardRenderer.RenderBoard(CurrentBoardState);
    }


    // Update��������������ϵͳ
    private void Update()
    {
        // ֻ����ʵʱģʽ�²Ÿ�������
        if (GameModeSelector.SelectedMode == GameModeType.RealTime && !isGameEnded)
        {
            EnergySystem?.Tick();

            // (��ѡ) �������ӡ����ֵ���ڵ���
            // Debug.Log($"Red Energy: {EnergySystem.GetEnergyInt(PlayerColor.Red)}, Black Energy: {EnergySystem.GetEnergyInt(PlayerColor.Black)}");
        }
    }

    /// <summary>
    /// ����������ִ���ƶ������������ƶ����齫������Ϸ�������Ե���/˧����
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        if (IsAnimating) return;
        if (isGameEnded) return;

        // 1. �����ĸĶ��������в���֮ǰ�����ж�����ƶ��Ƿ��ǳ���
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPiece.Type != PieceType.None;

        // 2. ����ǳ��ӣ������վ��жϺ��Ӿ��Ƴ�
        if (isCapture)
        {
            boardRenderer.RemovePieceAt(to);
            if (targetPiece.Type == PieceType.General)
            {
                GameStatus status = (targetPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                CurrentBoardState.MovePiece(from, to);
                // ���޸ġ��� isCapture ��Ϣ���ݹ�ȥ
                boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture);
                HandleEndGame(status);
                return;
            }
        }

        // 3. �������ݲ�
        CurrentBoardState.MovePiece(from, to);

        // 4. ���޸ġ������Ӿ��ƶ�������ȷ��֪�����Ƿ���һ�γ���
        boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture);

        // 5. ��齫��
        CheckForCheck();
    }

    /// <summary>
    /// ���������������������ڴ��ⲿ����BoardRenderer�����ö���״̬��
    /// </summary>
    public void SetAnimating(bool isAnimating)
    {
        this.IsAnimating = isAnimating;
    }


    /// <summary>
    /// ����������ֻ��齫��״̬�����ж���Ϸ������
    /// </summary>
    private void CheckForCheck()
    {
        // �ڻغ���ģʽ�£������һ���ж����Ƿ񱻽���
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"������{nextPlayer} ����������������");
                // TODO: �����ﲥ�š���������Ч������ʾ����UI��ʾ
            }
        }
        // TODO: ��ʵʱģʽ�£�������Ҫͬʱ���˫���Ƿ񱻽���
    }

    /// <summary>
    /// ������Ϸ�������߼���
    /// </summary>
    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true; // ������Ϸ������־
        Debug.Log($"��Ϸ���������: {status}");
        // TODO: �����ﲥ�š�ʤ��/ʧ��/���塱��Ч������ʾ��Ϸ�������

        // �����������
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}