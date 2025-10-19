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

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false; // ����һ����־λ����ֹ��Ϸ�������ܼ�������

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

        CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
        Debug.Log("��Ϸ��ʼ���ѽ���غ���ģʽ��");

        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// ����������ִ���ƶ������������ƶ����齫������Ϸ�������Ե���/˧����
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        // �����Ϸ�Ѿ���������ִ���κβ���
        if (isGameEnded) return;

        // --- 1. ���������վ� ---
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            // TODO: �����ﲥ�š����ӡ���Ч
            boardRenderer.RemovePieceAt(to);

            // �����ĸĶ�����鱻�Ե������Ƿ��ǽ�/˧
            if (targetPiece.Type == PieceType.General)
            {
                // ��Ϸ������
                GameStatus status = (targetPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                HandleEndGame(status);
                // �����ڸ������ݺ��Ӿ�֮ǰ���أ���Ϊ��Ϸ�Ѿ�������
                CurrentBoardState.MovePiece(from, to); // ��Ȼִ�������ƶ����Ա�����״̬�����յ�
                boardRenderer.MovePiece(from, to);
                return;
            }
        }

        // --- 2. �������ݺ��Ӿ� ---
        CurrentBoardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to);

        // --- 3. ��齫��״̬ (��Ϊ��ʾ) ---
        CheckForCheck();
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