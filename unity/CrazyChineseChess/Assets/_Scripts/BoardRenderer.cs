// File: _Scripts/BoardRenderer.cs
using UnityEngine;

public class BoardRenderer : MonoBehaviour
{
    // ��Unity�༭��������ǵ� Prefab �� Materials �ϵ�����
    public GameObject piecePrefab;
    public Material redMaterial;
    public Material blackMaterial;
    
    // ���ڴ洢��ǰ�������������ӵ�GameObject�������������
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];

    /// <summary>
    /// ���ݴ���� BoardState ���ݣ���Ⱦ��������
    /// </summary>
    public void RenderBoard(BoardState boardState)
    {
        // �������̵�ÿһ������
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                
                // ����������������
                if (piece.Type != PieceType.None)
                {
                    // 1. ������������� BoardVisual �ı�������
                    Vector3 localPosition = GetLocalPosition(x, y);
                    
                    // 2. ʵ�������ӣ���ָ��������
                    //    Instantiate ���Զ��� localPosition ����Ϊ����ڸ����������
                    GameObject pieceGO = Instantiate(piecePrefab, this.transform);
                    pieceGO.transform.localPosition = localPosition; // ֱ�����ñ�������

                    pieceGO.name = $"{piece.Color}_{piece.Type}_{x}_{y}";

                    // ����������ɫ���ò���
                    MeshRenderer renderer = pieceGO.GetComponent<MeshRenderer>();
                    if (piece.Color == PlayerColor.Red)
                    {
                        renderer.material = redMaterial;
                    }
                    else if (piece.Color == PlayerColor.Black)
                    {
                        renderer.material = blackMaterial;
                    }
                    
                    // �洢�����GameObject������
                    pieceObjects[x, y] = pieceGO;
                }
            }
        }
    }
    
    /// <summary>
    /// �����̸������� (x,y) ת��Ϊ����ڴ˶���(BoardVisual)�ı������ꡣ
    /// ����汾�ǻ�����Ƴߴ磬��ģ�ͱ����С������ȶ���
    /// ���̶̹��ߴ�45X45cm�����ӹ̶��ߴ�35mm
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // --- ��Ƴ��� ---
        // �����ڴ����ж������̵��߼��ߴ磬����������ģ�͡�
        // �����ܿ�� (X��, 8�����)
        const float boardLogicalWidth = 0.45f; 
        // �����ܸ߶� (Z��, 9�����)
        const float boardLogicalHeight = 0.45f * (10f / 9f); // ���������㣬�й����������ǳ����ε�
        
        // --- ���� ---
        // ����ÿ�����ӵļ��
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);
        
        // ����ƫ������ʹ������������ (0,0)
        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;
        
        // �������ձ�������
        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;

        // ��ȡ���ӵĸ߶ȣ�ʹ��պø���������
        float pieceHeight = 0.0175f; // ��Ӧ��������߶�

        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }

}