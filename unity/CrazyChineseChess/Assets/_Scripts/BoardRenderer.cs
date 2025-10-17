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
                    // ����������3D�����е�λ��
                    Vector3 worldPosition = GetWorldPosition(x, y);
                    
                    // ʵ����һ������Prefab
                    GameObject pieceGO = Instantiate(piecePrefab, worldPosition, Quaternion.identity, this.transform);
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
    /// �����̸������� (0,0) ת��Ϊ�������ꡣ
    /// ����Ҫ�����������ģ�ʹ�С���е�����
    /// </summary>
    private Vector3 GetWorldPosition(int x, int y)
    {
        // Plane �Ĵ�С�� 10x10 units�����ǵ������� 9x10 ��
        // ������Ҫһ��ӳ���ϵ����������ģ�͵����½Ƕ�Ӧ��������� (-4.5, 0, -5)��
        float boardWidthUnits = 9.0f;
        float boardHeightUnits = 10.0f;

        float xPos = (x - (BoardState.BOARD_WIDTH - 1) / 2.0f) * (boardWidthUnits / (BoardState.BOARD_WIDTH -1));
        float zPos = (y - (BoardState.BOARD_HEIGHT - 1) / 2.0f) * (boardHeightUnits / (BoardState.BOARD_HEIGHT - 1));

        // ���ǽ����̵�Y����뵽�����Z��
        return new Vector3(xPos, 0.1f, zPos); // 0.1f ��Ϊ����������΢����������
    }
}