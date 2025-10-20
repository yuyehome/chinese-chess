// File: _Scripts/GameManager.cs

using UnityEngine;
using System; // <--- 必须有这一行

/// <summary>
/// 游戏总管理器，协调所有核心组件。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }
    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("场景中找不到 BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem();

        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("游戏开始，已进入【传统回合制】模式。");
                break;
            case GameModeType.RealTime:
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("游戏开始，已进入【实时对战】模式。");
                break;
            default:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        boardRenderer.RenderBoard(CurrentBoardState);

        // 然后，如果当前是实时模式，我们再进行实时状态的初始化
        if (CurrentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }

    }

    private void Update()
    {
        if (isGameEnded) return;

        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (CurrentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    // 【新增】创建一个不带回调的重载版本，它会调用带回调的版本，但传递null
    // 这样回合制模式的代码就无需任何改动
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        ExecuteMove(from, to, null, null);
    }


    public void ExecuteMove(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        if (isGameEnded) return;

        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPiece.Type != PieceType.None;

        if (isCapture)
        {
            boardRenderer.RemovePieceAt(to);
            if (targetPiece.Type == PieceType.General)
            {
                GameStatus status = (targetPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                CurrentBoardState.MovePiece(from, to);
                boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture, onProgressUpdate, onComplete);
                HandleEndGame(status);
                return;
            }
        }

        CurrentBoardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture, onProgressUpdate, onComplete);
        CheckForCheck();
    }

    private void CheckForCheck()
    {
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"将军！{nextPlayer} 方的王正被攻击！");
            }
        }
    }

    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true;
        Debug.Log($"游戏结束！结果: {status}");
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}