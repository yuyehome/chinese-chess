// File: _Scripts/PlayerInput.cs

using UnityEngine;

/// <summary>
/// �����������봦��ű���
/// ����Ψһְ���Ǽ����������������Ŀ�꣨���ӡ���ǡ����̣���
/// ������Щԭʼ�����¼�ת������ǰ�������Ϸģʽ�����������߼�����
/// </summary>
public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // ������������
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// ����������¼����������߲��ַ����롣
    /// </summary>
    private void HandleMouseClick()
    {
        // ��ȡ��ǰ����Ϸģʽ������������������򲻽����κβ���
        GameModeController gameMode = GameManager.Instance.CurrentGameMode;
        if (gameMode == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // --- �����¼�ת�� ---
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent clickedMarker = hit.collider.GetComponent<MoveMarkerComponent>();

            if (clickedPiece != null)
            {
                // ���������
                gameMode.OnPieceClicked(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                // ������ƶ����
                gameMode.OnMarkerClicked(clickedMarker);
            }
            else
            {
                // ��������̵���������
                gameMode.OnBoardClicked(hit);
            }
        }
    }
}