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
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // ��������߼���������ȷ�ģ������������(0,0,0)Ϊ���ĵ�ƫ������
        // �����Ǳ�����������Ҫ�ġ�
        float boardWidthUnits = 9.0f;
        float boardHeightUnits = 10.0f;
        
        // ��������ģ�͵ĳߴ������ǵĵ�λ�ߴ�ƥ�䡣
        // Plane��Ĭ�ϴ�С��10x10������֮ǰ����Scale����(1, 1, 1.2)��
        // ��������ʵ�ʿ����10����λ���߶���12����λ��
        // Ϊ�˾�ȷƥ�䣬������Ҫ����ʵ��ģ�ͳߴ������
        // ��������һ������׳�ķ�����
        Renderer boardRenderer = GetComponentInChildren<Renderer>(); // ��ȡ�Ӷ���(BoardPlane)����Ⱦ��
        if (boardRenderer == null) {
             Debug.LogError("BoardVisual ���Ҳ�����Renderer������ƽ�棡");
             return Vector3.zero;
        }

        Vector3 boardSize = boardRenderer.bounds.size;

        // X�᣺�� -boardSize.x / 2 �� +boardSize.x / 2
        float xPos = (float)x / (BoardState.BOARD_WIDTH - 1) * boardSize.x - (boardSize.x / 2f);
        
        // Z�᣺�� -boardSize.z / 2 �� +boardSize.z / 2
        // ע�⣺Unity Plane��"�߶�"����Z����
        float zPos = (float)y / (BoardState.BOARD_HEIGHT - 1) * boardSize.z - (boardSize.z / 2f);

        // ���ر������ꡣYֵ�����ӵĸ߶ȡ�
        return new Vector3(xPos, 0.1f, zPos);
    }


}