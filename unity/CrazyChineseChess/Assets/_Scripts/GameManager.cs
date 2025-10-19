// File: _Scripts/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ����ģʽ������ȫ�ַ��� GameManager ʵ��
    public static GameManager Instance { get; private set; }
    
    public BoardState CurrentBoardState { get; private set; }

    private BoardRenderer boardRenderer; // �����BoardRenderer������


    private void Awake()
    {
        // ʵ�ּ򵥵ĵ���ģʽ
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
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

        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// ִ��һ���ƶ�����Ӳ�����������Ϸ״̬�����Ψһ��ڡ�
    /// </summary>
    /// <param name="from">��ʼ����</param>
    /// <param name="to">Ŀ������</param>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        // 1. ���Ŀ��λ���Ƿ������ӱ���
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            // �����ӱ��ԣ�֪ͨ��Ⱦ���Ƴ������ӵ��Ӿ�����
            boardRenderer.RemovePieceAt(to);
        }

        // 2. �����ݲ�ִ���ƶ�
        CurrentBoardState.MovePiece(from, to);

        // 3. ���Ӿ���ִ���ƶ�
        boardRenderer.MovePiece(from, to);

        // TODO: ��������Լ��뽫�����������ж��߼�
    }

}