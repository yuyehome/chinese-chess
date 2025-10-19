// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// ��Ϸ�ܹ����������ع��󡿸����ʼ����Ϸ���������״̬�͵�ǰ����Ϸģʽ��
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }

    // ����������ǰ�������Ϸģʽ������
    public GameModeController CurrentGameMode { get; private set; }

    // �����BoardRenderer������
    private BoardRenderer boardRenderer;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // --- ������ȡ ---
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("�������Ҳ��� BoardRenderer!");
            return;
        }

        // --- �������ݳ�ʼ�� ---
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // --- ��Ϸģʽ��ʼ�� ---
        // �������������������Ϸģʽ��������Ĭ�������غ���ģʽ��
        // δ�����Ը������˵���ѡ����ʵ������ͬ�Ŀ�������
        CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
        Debug.Log("��Ϸ��ʼ���ѽ���غ���ģʽ��");

        // --- ��ʼ��Ⱦ ---
        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// ִ���ƶ�����������������ֲ��䣬��Ϊ��������һ��ԭ���Ե���Ϸ��Ϊ��
    /// ����������ģʽ�£����ƶ���������������ǲ���ġ�
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            boardRenderer.RemovePieceAt(to);
        }
        CurrentBoardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to);
    }
}