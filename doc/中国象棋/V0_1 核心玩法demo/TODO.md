### **《魔改中国象棋》网络化改造技术设计文档**

#### **1. 核心原则与目标**

1. **服务器权威 (Server Authority):** 这是多人在线游戏的黄金法则。**所有**游戏逻辑（移动、碰撞、战斗、能量增减、技能效果）**必须**在服务器上计算和执行。客户端只负责发送玩家输入，并根据服务器广播的状态来“播放”游戏画面。这能从根本上杜绝作弊。
    
2. **保留单机模式:** 改造必须兼容现有的单机AI对战模式。GameManager 将根据启动模式（PVP或PVE）来决定加载不同的控制器组合。
    
3. **状态同步而非RPC轰炸:** 优先使用FishNet的状态同步机制（如 SyncVar, SyncObject）来同步游戏状态，而不是频繁地通过RPC（远程过程调用）来传递每一个小变化。这更高效，且能更好地处理新加入的观战者或断线重连。
    
4. **清晰的职责划分:** 严格区分服务器逻辑、客户端逻辑和共享逻辑。
    

#### **2. 需要的代码文件**

根据您的文件列表和我的分析，为了完成核心PVP玩法的网络化，我需要您提供以下所有代码文件的当前版本。这些文件构成了游戏的核心循环，每一个都与网络化改造息息相关。

- **_Scripts/Core/**
    
    - BoardState.cs
        
    - CombatManager.cs
        
    - EnergySystem.cs
        
    - GameModeSelector.cs
        
    - PieceData.cs
        
    - RealTimePieceState.cs
        
    - RuleEngine.cs
        
- **_Scripts/GameModes/**
    
    - GameModeController.cs
        
    - RealTimeModeController.cs
        
- **_Scripts/Controllers/**
    
    - IPlayerController.cs
        
    - PlayerInputController.cs
        
    - AIController.cs
        
- **_Scripts/UI/**
    
    - GameUIManager.cs
        
- **_Scripts/ (根目录)**
    
    - BoardRenderer.cs
        
    - GameManager.cs
        
    - PieceComponent.cs
        

请将这些文件提供给我，以便进行后续更具体的代码设计。

---

#### **3. 改造方案概要设计**

这是我们首先要实现的目标。

1. **LobbyManager.StartGame() 的改造:**
    
    - 当房主点击“开始游戏”时，StartGame() 方法被调用。
        
    - **服务器逻辑:**
        
        - 该方法将调用FishNet的 NetworkManager.SceneManager.LoadGlobalScenes() 来加载 Game.unity 场景。LoadGlobalScenes 会自动通知所有已连接的客户端同步加载该场景。
            
        - 在加载前，服务器需要决定并存储双方玩家的阵营信息。例如，创建一个Dictionary<int, PlayerNetData>，其中 int 是客户端的Connection ID，PlayerNetData 是一个结构体，包含SteamID, PlayerColor (红/黑)等信息。房主（Host）的连接ID总是1，可以默认为红方。
            
        - 这个玩家数据可以通过一个NetworkBehaviour脚本同步给所有客户端。
            
2. **GameManager.Start() 的改造:**
    
    - 当 Game.unity 场景加载后，GameManager 的 Start() 方法（或 Awake）会被调用。
        
    - **关键改动:** GameManager 需要判断当前是PVP模式还是PVE模式。
        
        - **PVP模式判断:** if (InstanceFinder.IsServer || InstanceFinder.IsClient)，只要网络是激活的，就是PVP模式。
            
        - **PVE模式判断:** else，网络未激活，就是单机模式。
            
    - **PVP模式下的初始化:**
        
        - **服务器:** GameManager 将创建和初始化所有服务器端的逻辑模块，如 BoardState, RealTimeModeController, EnergySystem 等。然后，它需要为每个连接的玩家（包括自己）生成一个“玩家代理”。对于本地玩家（房主），它实例化一个 PlayerInputController；对于远程玩家，它实例化一个新的 NetworkPlayerController (我们稍后会创建它)。
            
        - **客户端:** 客户端的 GameManager 将会“等待”服务器同步游戏状态。它会禁用本地的所有逻辑计算模块（如RealTimeModeController），只保留表现层模块（如BoardRenderer）。它只会为自己实例化一个PlayerInputController。
            

这是网络化的心脏。我们需要将游戏的核心状态变为可同步的。

1. **创建 NetworkGameState.cs:**
    
    - 这是一个新的 NetworkBehaviour 脚本，场景中只需要一个实例（可以挂在 GameManager 上）。
        
    - **职责:** 作为所有核心游戏状态的“同步容器”。
        
    - **伪代码:**
        
        codeC#
        
2. **改造 BoardState 和 RealTimePieceState:**
    
    - 在服务器上，BoardState 和 RealTimePieceState 依然是核心的逻辑数据源。
        
    - 服务器的 GameManager 或 RealTimeModeController 在每次逻辑更新后，需要将 BoardState 的数据**翻译**并更新到 NetworkGameState 的 AllPieces 列表中。
        
    - FishNet会自动检测到 AllPieces 的变化，并将其高效地广播给所有客户端。
        
3. **客户端的响应 (BoardRenderer.cs)**:
    
    - 客户端的 BoardRenderer 将不再监听本地的 GameManager 事件。
        
    - 取而代之，它将引用 NetworkGameState。它会订阅 AllPieces.OnChange 事件。
        
    - 当 AllPieces 列表发生变化时（棋子移动、死亡等），OnChange 事件被触发，BoardRenderer 就根据新的数据列表来更新棋子的视觉表现（移动GameObject，播放死亡特效等）。
        

这是将玩家操作网络化的关键。

1. **改造 PlayerInputController.cs:**
    
    - 当玩家点击棋盘并选择一个合法的移动时，PlayerInputController 不再调用 GameManager.RequestMove()。
        
    - 取而代之，它将调用一个在服务器上执行的RPC方法。
        
    - **伪代码:**
        
        codeC#
        
2. **创建 NetworkPlayer.cs:**
    
    - 这是一个新的 NetworkBehaviour，代表一个网络中的玩家实体。当一个玩家连接到服务器时，服务器应该为他生成一个 NetworkPlayer 对象。
        
    - **职责:** 作为客户端向服务器发送指令的唯一通道。
        
    - **伪代码:**
        
        codeC#
        
    - 这样，点击操作就形成了一个闭环：**客户端输入 -> [ServerRpc] -> 服务器执行逻辑 -> 服务器更新 NetworkGameState -> 所有客户端同步状态 -> 客户端 BoardRenderer 刷新画面**。
        

处理好 “自己总在下方” 的体验。

1. **阵营分配:**
    
    - 如阶段一所述，服务器在加载游戏场景前，就为每个连接分配好阵营（红/黑）。这个信息需要同步给所有客户端。
        
2. **视角相机控制:**
    
    - 在 Game.unity 场景中，创建一个 CameraRig 或 CameraController 脚本。
        
    - 在 Start() 方法中，它会获取本地玩家的阵营信息。
        
    - 如果本地玩家是红方（默认在下方），相机保持默认旋转。
        
    - 如果本地玩家是黑方（默认在上方），该脚本需要将相机绕棋盘中心旋转180度。这样，黑方玩家看到的棋盘就是“正”的，自己的棋子在下方。
        
3. **棋子文字朝向:**
    
    - 棋子上的文字（帅、車、馬...）通常是3D Text或TextMeshPro组件。
        
    - 默认情况下，它们会跟随棋子一起旋转。当相机旋转180度后，黑方玩家会看到自己的棋子文字是倒的。
        
    - **解决方案:** 创建一个 Billboard.cs 脚本，挂在每个棋子的文字组件上。
        
        - **伪代码:**
            
            codeC#
            
        - 这样，无论棋盘怎么转，文字总是正对着玩家的屏幕。
            

---

#### **4. 开发路线图 (Roadmap)**

我建议将整个改造过程分为以下几个可交付的步骤：

1. **[Milestone 1] 联网启动与场景同步:**
    
    - **任务:** 实现 LobbyManager.StartGame()，让房主点击后，所有玩家能一同加载到 Game.unity 场景。
        
    - **验证:** 两个客户端都能进入游戏场景，并打印日志 "PVP mode started on [Server/Client]"。
        
2. **[Milestone 2] 静态棋盘同步:**
    
    - **任务:** 实现 NetworkGameState 和 NetworkPieceData。服务器在 Start 时初始化 BoardState，并将其状态同步到 NetworkGameState。客户端的 BoardRenderer 根据 NetworkGameState 来生成初始棋盘。
        
    - **验证:** 游戏开始时，双方客户端都能看到一模一样的初始棋盘布局。
        
3. **[Milestone 3] 玩家输入与移动同步:**
    
    - **任务:** 实现 NetworkPlayer 和 PlayerInputController 的 CmdRequestMove 逻辑。完成从客户端输入到服务器执行，再到状态同步回所有客户端的完整流程。
        
    - **验证:** 房主移动一个棋子，另一个客户端能平滑地看到该棋子移动到目标位置。反之亦然。
        
4. **[Milestone 4] 核心逻辑（能量、战斗）同步:**
    
    - **任务:** 将 EnergySystem 和 CombatManager 的逻辑完全放到服务器上。将能量值通过 SyncVar 同步。战斗结果（棋子死亡）通过更新 NetworkGameState 中的 IsDead 标志来同步。
        
    - **验证:** 玩家移动消耗能量，双方UI同步显示。棋子发生碰撞，在双方屏幕上同时消失。
        
5. **[Milestone 5] 视角与游戏结束处理:**
    
    - **任务:** 实现相机旋转和文字Billboard。实现游戏结束逻辑的判断（服务器判断）和同步。
        
    - **验证:** 黑方玩家进入游戏后，视角自动翻转，棋子文字正常。一方将死另一方后，双方都弹出胜负结算界面。