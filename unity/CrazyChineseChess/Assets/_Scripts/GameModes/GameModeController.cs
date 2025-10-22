// File: _Scripts/GameModes/GameModeController.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有游戏模式控制器的抽象基类 (Strategy Pattern)。
/// 它定义了所有模式都必须响应的通用输入接口，并提供了共享功能。
/// </summary>
public abstract class GameModeController
{
    // --- 依赖引用 (由子类构造函数注入) ---
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