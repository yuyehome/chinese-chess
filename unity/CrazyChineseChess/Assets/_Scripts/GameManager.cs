// File: _Scripts/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ����ģʽ������ȫ�ַ��� GameManager ʵ��
    public static GameManager Instance { get; private set; }
    
    public BoardState CurrentBoardState { get; private set; }

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
        // ��������ʼ�������߼�����
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        
        // ֪ͨ��Ⱦ�������µ�����״̬��������
        // ����ͨ�� FindObjectOfType �ҵ��������ŵķ�ʽ���¼�������ע�룬��Ŀǰ������ֱ��
        BoardRenderer renderer = FindObjectOfType<BoardRenderer>();
        if (renderer != null)
        {
            renderer.RenderBoard(CurrentBoardState);
        }
        else
        {
            Debug.LogError("�������Ҳ��� BoardRenderer!");
        }
    }
}