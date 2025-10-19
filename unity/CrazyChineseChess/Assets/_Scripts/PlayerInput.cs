// File: _Scripts/PlayerInput.cs
using System.Collections.Generic;
using System.Linq; // ��Ҫʹ��Linq
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;
    
    private PieceComponent selectedPiece = null;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();
            
            if (clickedPiece != null)
            {
                // �������һ������
                HandlePieceClick(clickedPiece);
            }
            else
            {
                // ������������ϵĸ������(�������ط�)
                HandleBoardClick(hit);
            }
        }
    }

    private void HandlePieceClick(PieceComponent piece)
    {
        // �����ǰ�����ӱ�ѡ��
        if (selectedPiece != null)
        {
            // ���������������Ƿ���һ���Ϸ��Ĺ���Ŀ��
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                // �ǣ�ִ�г����ƶ�
                GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                ClearSelection();
            }
            else
            {
                // ���ǣ����л�ѡ�����������
                SelectPiece(piece);
            }
        }
        else
        {
            // ��ǰû�����ӱ�ѡ�У�ֱ��ѡ���������
            SelectPiece(piece);
        }
    }
    
    private void HandleBoardClick(RaycastHit hit)
    {
        // ��������ѡ�е�����
        if (selectedPiece == null) return;
        
        // ������Ҫ����������ĵ���㣬ת��Ϊ���̸�������
        Vector2Int clickedGridPos = ConvertWorldToGridPosition(hit.point);

        // ������ĸ����Ƿ��ںϷ����ƶ��б���
        if (currentValidMoves.Contains(clickedGridPos))
        {
            // �ǣ�ִ���ƶ�
            GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, clickedGridPos);
            ClearSelection();
        }
        else
        {
            // �������Чλ�ã����ѡ��
            ClearSelection();
        }
    }
    
    /// <summary>
    /// ѡ��һ�����Ӳ���ʾ����ƶ�λ��
    /// </summary>
    private void SelectPiece(PieceComponent piece)
    {
        // TODO: ������Ӫ�жϺͻغ��ж�
        ClearSelection();
        selectedPiece = piece;
        
        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);
        
        FindObjectOfType<BoardRenderer>().ShowValidMoves(currentValidMoves, pieceData.Color, GameManager.Instance.CurrentBoardState);
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        FindObjectOfType<BoardRenderer>().ClearAllHighlights();
    }

    /// <summary>
    /// ��������������������ת��Ϊ���̸�������
    /// </summary>
    private Vector2Int ConvertWorldToGridPosition(Vector3 worldPos)
    {
        // ���ת���� GetLocalPosition ��������
        // Ϊ�˼򻯣�����ֱ�Ӵ� BoardRenderer ��ȡת����ı�������
        Vector3 localPos = FindObjectOfType<BoardRenderer>().transform.InverseTransformPoint(worldPos);
        
        const float TOTAL_BOARD_WIDTH = 0.45f;
        const float TOTAL_BOARD_HEIGHT = 0.45f * (10f / 9f);
        const float MARGIN_X = 0.025f;
        const float MARGIN_Y = 0.025f;
        float playingAreaWidth = TOTAL_BOARD_WIDTH - 2 * MARGIN_X;
        float playingAreaHeight = TOTAL_BOARD_HEIGHT - 2 * MARGIN_Y;

        float startX = -playingAreaWidth / 2f;
        float startZ = -playingAreaHeight / 2f;

        // ͨ���������õ��ٷֱȣ�Ȼ����Ը�����
        int x = Mathf.RoundToInt((localPos.x - startX) / playingAreaWidth * (BoardState.BOARD_WIDTH - 1));
        int y = Mathf.RoundToInt((localPos.z - startZ) / playingAreaHeight * (BoardState.BOARD_HEIGHT - 1));

        return new Vector2Int(x, y);
    }
}