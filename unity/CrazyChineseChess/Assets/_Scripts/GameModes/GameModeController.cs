// File: _Scripts/GameModes/GameModeController.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������Ϸģʽ�������ĳ������ (Strategy Pattern)��
/// ������������ģʽ��������Ӧ��ͨ������ӿڣ����ṩ�˹����ܡ�
/// </summary>
public abstract class GameModeController
{
    // --- �������� (�����๹�캯��ע��) ---
    protected BoardState boardState;
    protected BoardRenderer boardRenderer;
    protected GameManager gameManager;

    public GameModeController(GameManager manager, BoardState state, BoardRenderer renderer)
    {
        this.gameManager = manager;
        this.boardState = state;
        this.boardRenderer = renderer;
    }

}