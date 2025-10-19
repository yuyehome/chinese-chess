// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// ��Ϸ�ܹ�����������Э����Ϸ���̺ͺ���״̬��
/// ���õ���ģʽ������ȫ�ַ��ʡ�
/// </summary>
public class GameManager : MonoBehaviour
{
    // ����ģʽ�ľ�̬ʵ��
    public static GameManager Instance { get; private set; }

    // ��Ϸ�ĺ�������״̬���������������������ӵ��߼�λ�ú���Ϣ
    public BoardState CurrentBoardState { get; private set; }

    // �����BoardRenderer�����ã�����������ʱƵ�����ң��������
    private BoardRenderer boardRenderer;

    private void Awake()
    {
        // ʵ�ּ򵥵ĵ���ģʽ��ȷ��������ֻ��һ��GameManagerʵ��
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
        // ����Ϸ��ʼʱ���ҵ������е�BoardRenderer
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            // ����Ҳ���������һ�����ش�����Ҫ����ִֹͣ��
            Debug.LogError("�������Ҳ��� BoardRenderer!");
            return;
        }

        // ��������ʼ�����̵��߼�����
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // ֪ͨ��Ⱦ�����ݳ�ʼ�����������ݣ��ڳ����д�������ģ��
        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// ִ��һ���ƶ�����Ӳ�����������Ϸ״̬�����Ψһ��ڣ�ȷ�����߼��ļ��п��ơ�
    /// </summary>
    /// <param name="from">�����ƶ�����ʼ����</param>
    /// <param name="to">�����ƶ���Ŀ������</param>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        // --- �����ƶ��߼� ---

        // 1. ��ִ���ƶ�֮ǰ���ȼ��Ŀ��λ���Ƿ������ӽ����Ե�
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            // ��������ӣ�֪ͨ��Ⱦ���ӳ������Ƴ������ӵ��Ӿ�����(GameObject)
            boardRenderer.RemovePieceAt(to);
        }

        // 2. �����ݲ�(BoardState)�и������ӵ�λ����Ϣ
        //    ������Ϸ״̬�ġ���ʵ��Դ�������������Ӿ�����
        CurrentBoardState.MovePiece(from, to);

        // 3. ���Ӿ���(BoardRenderer)��ƽ�����ƶ����ӵ�GameObject
        boardRenderer.MovePiece(from, to);

        // TODO: �����������ĩβ�����Լ��뽫����������ʤ���жϵȺ����߼�
    }
}