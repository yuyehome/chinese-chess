// File: _Scripts/PlayerInput.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // ����Linq�Ա�ʹ�� .FirstOrDefault()

public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;
    private BoardRenderer boardRenderer; // �����BoardRenderer�����ã��������

    private PieceComponent selectedPiece = null; // ��ǰѡ�е�����
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>(); // ��ǰѡ�����ӵ����кϷ��ƶ���

    void Start()
    {
        mainCamera = Camera.main;
        // ��Start�л�ȡ������BoardRenderer�������Ƽ�������������ÿ�ζ�Find
        boardRenderer = FindObjectOfType<BoardRenderer>();
    }

    void Update()
    {
        // ÿһ֡������������Ƿ���
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// ����������¼��������
    /// </summary>
    private void HandleMouseClick()
    {
        // ��������������λ�÷���һ������
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // --- [�������Դ���] ---
        // ��������㣬�������߷��򣬻�һ����ɫ���ߣ�����2����
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 10.0f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // --- [�������Դ���] ---
            // ��ӡ����������������֣��Լ���������ʲô���ǹ��ĵ����
            Debug.Log($"���߻�����: {hit.collider.gameObject.name}");
            PieceComponent pc = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent mc = hit.collider.GetComponent<MoveMarkerComponent>();
            if (pc != null) Debug.Log("���� ����һ�� PieceComponent!");
            if (mc != null) Debug.Log("���� ����һ�� MoveMarkerComponent!");
            // --------------------

            // --- ��Ϊ�ַ� ---
            // �������������������ʲô
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent clickedMarker = hit.collider.GetComponent<MoveMarkerComponent>(); // ����������Ƿ�㵽�ƶ����

            if (clickedPiece != null)
            {
                // 1. ����������һ������
                HandlePieceClick(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                // 2. ����������һ���ƶ���� (С��Ƭ)
                HandleMarkerClick(clickedMarker);
            }
            else
            {
                // 3. �����������̻������ط�
                HandleBoardClick(hit);
            }
        } else
        {

            // --- [�������Դ���] ---
            Debug.Log("����û�л����κδ�����ײ������塣");
            // --------------------
        }
    }

    /// <summary>
    /// ���������ӵ��߼�
    /// </summary>
    private void HandlePieceClick(PieceComponent piece)
    {
        // ����Ѿ������ӱ�ѡ��
        if (selectedPiece != null)
        {
            // ��鱻����������ǲ���һ���Ϸ��Ĺ���Ŀ��
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                // �ǺϷ�����Ŀ�ִ꣬���ƶ�/����
                GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                ClearSelection(); // ������ɺ����ѡ��״̬
            }
            else
            {
                // �������Ĳ��ǺϷ�Ŀ�꣬���л�ѡ�������������
                SelectPiece(piece);
            }
        }
        else
        {
            // ���֮ǰû�����ӱ�ѡ�У���ѡ���������
            SelectPiece(piece);
        }
    }

    /// <summary>
    /// ���������������ƶ���ǵ��߼�
    /// </summary>
    private void HandleMarkerClick(MoveMarkerComponent marker)
    {
        // ���û�����ӱ�ѡ�У��������ε��
        if (selectedPiece == null) return;

        // ��������Ƕ�Ӧ��λ���Ƿ��ںϷ��ƶ��б��� (˫�ر���)
        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            // ִ���ƶ�
            GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            ClearSelection(); // ������ɺ����ѡ��״̬
        }
    }

    /// <summary>
    /// ���������̻������հ�������߼�
    /// </summary>
    private void HandleBoardClick(RaycastHit hit)
    {
        // �κ���������ѡ��״̬�µĿհ׵��������Ϊȡ��ѡ��
        if (selectedPiece != null)
        {
            ClearSelection();
        }
    }

    /// <summary>
    /// ѡ��һ�����ӣ�����ʾ��Ϸ����ƶ�λ��
    /// </summary>
    private void SelectPiece(PieceComponent piece)
    {
        ClearSelection(); // �������һ�ε�ѡ��״̬
        selectedPiece = piece;

        // �����ݺ��Ļ�ȡ���ӵ��߼���Ϣ
        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        // ���ù�������������кϷ��ƶ�
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);

        // ֪ͨBoardRenderer��ʾ����
        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, GameManager.Instance.CurrentBoardState);
    }

    /// <summary>
    /// �������ѡ��״̬��UI����
    /// </summary>
    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        boardRenderer.ClearAllHighlights();
    }
}
