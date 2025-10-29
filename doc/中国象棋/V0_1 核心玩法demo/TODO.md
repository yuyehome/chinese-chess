### **《魔改中国象棋》网络化改造技术交接文档 (v2.0)**

#### **1. 核心原则与目标 (无变化)**

本项目网络化改造遵循以下四大核心原则：

1. **服务器权威 (Server Authority):** 所有游戏逻辑**必须**在服务器上执行。客户端只负责发送输入并渲染状态。
    
2. **兼容单机模式 (PVE/PVP Coexistence):** 网络化改造后，原有的单机AI对战功能必须完整保留。GameManager 会在启动时自动检测网络状态以区分PVP和PVE模式。
    
3. **状态同步为主 (State Synchronization):** 优先使用FishNet的状态同步机制（SyncList, SyncVar）来同步游戏状态，而非依赖大量的RPC调用。
    
4. **职责清晰 (Clear Separation):** 严格区分服务器、客户端及共享代码的职责。
    

---

#### **2. 核心网络架构**

本项目的网络架构围绕一个核心理念：**由LobbyManager在游戏开始前启动网络会话，并在Game场景中通过一个权威的GameNetworkManager来同步所有游戏状态。**

1. **房间与连接 (LobbyManager):**
    
    - LobbyManager (位于MainMenu场景) 负责通过Steamworks处理大厅的创建、加入和玩家连接。
        
    - 当房主（Host）点击"开始游戏"，LobbyManager验证所有玩家到齐后，**不会**生成任何持久化对象。
        
    - 它只负责调用 NetworkManager.SceneManager.LoadGlobalScenes()，命令所有客户端同步加载Game场景。
        
2. **游戏状态同步枢纽 (GameNetworkManager):**
    
    - GameNetworkManager 是一个**场景对象 (Scene Object)**，唯一存在于Game.unity场景中。它继承自NetworkBehaviour。
        
    - **职责:**
        
        - 作为所有**核心游戏状态**的权威同步容器。
            
        - 通过 SyncList<NetworkPieceData> AllPieces 同步整个棋盘的布局。
            
        - 通过 SyncDictionary<int, PlayerNetData> AllPlayers 同步所有玩家的身份信息（SteamID、阵营颜色等）。
            
        - 提供 OnInstanceReady 静态事件，这是**整个PVP初始化流程的关键信号**。它在 OnStartServer 和 OnStartClient 中触发，确保只有当该对象的网络功能完全激活时，其他脚本才能开始执行网络相关操作。
            
3. **游戏逻辑与模式切换 (GameManager):**
    
    - GameManager 在Start()方法中检测网络状态 (InstanceFinder.IsClient/IsServer)。
        
    - **PVP模式下:** 它**订阅** GameNetworkManager.OnInstanceReady 事件。在事件触发前，它处于“等待”状态。
        
    - **事件触发后 (HandlePVPInitialization):**
        
        - **服务器 (Server):** 初始化所有游戏逻辑模块 (RealTimeModeController, EnergySystem等)，并调用GameNetworkManager.Server_InitializeBoard()，将初始棋盘状态填充到AllPieces同步列表中。
            
        - **客户端 (Client):** 初始化一个**不执行逻辑**的 RealTimeModeController 实例（用于访问方法），然后订阅GameNetworkManager.AllPieces.OnChange事件，等待服务器的数据。
            
4. **客户端渲染 (GameManager & BoardRenderer):**
    
    - 当客户端的 GameNetworkManager.AllPieces 列表第一次从服务器接收到完整的棋盘数据时，会触发 OnChange 事件（带有Complete操作）。
        
    - GameManager 的 OnNetworkBoardStateChanged 回调方法被调用。
        
    - 该方法根据收到的网络数据 (List<NetworkPieceData>)，构建一个临时的 BoardState 对象。
        
    - 最后，调用 BoardRenderer.RenderBoard()，使用这个临时BoardState来**一次性渲染**出完整的棋盘视觉效果。
        


---

#### **3. 最新文件结构与职责说明 (新增/修改)**

|   |   |   |
|---|---|---|
|文件夹/文件名|核心职责|备注|
|**_Scripts/Network/**|**存放所有网络核心逻辑与数据结构**||
|GameNetworkManager.cs|**[核心]** 游戏状态同步枢纽。作为场景对象存在于Game.unity。管理玩家数据和棋盘状态的同步列表，并提供OnInstanceReady事件作为PVP初始化信号。|继承自NetworkBehaviour。|
|PlayerNetData.cs|**[数据结构]** 定义了在网络中同步的玩家信息（SteamID, 阵营颜色等）。|纯struct。|
|NetworkPieceData.cs|**[数据结构]** 定义了在网络中同步的棋子信息（ID, 类型, 颜色, 位置）。|纯struct。|
|**_Scripts/ (根目录)**|||
|GameManager.cs|**[核心控制器]** 游戏总管理器。**新增职责**：检测PVE/PVP模式，并根据GameNetworkManager的事件来驱动PVP初始化流程。在客户端，它负责响应网络数据并调用渲染。|修改巨大，是网络化改造的中心。|
|**_Scripts/Network/**|||
|LobbyManager.cs|**[网络引导]** 大厅管理器。**职责简化**：现在只负责处理Steam Lobby和网络连接，并在游戏开始时**仅加载场景**，不再负责生成任何游戏对象。|继承自MonoBehaviour。|

---

#### **4. 后续开发规划 (Milestone 3 及以后)**

当前我们已成功完成Milestone 2，为后续开发奠定了坚实的基础。

1. **创建 NetworkPlayerController.cs:**
    
    - 这是一个新的 NetworkBehaviour Prefab。
        
    - **职责:** 代表一个网络中的“玩家实体”。每个客户端连接成功后，服务器会为其生成一个NetworkPlayerController实例，并将所有权（Ownership）交给他。
        
    - 它将包含一个 [ServerRpc] 方法，例如 CmdRequestMove(Vector2Int from, Vector2Int to)。
        
2. **改造 PlayerInputController.cs:**
    
    - 不再是MonoBehaviour，而是一个纯C#类，或者保持MonoBehaviour但需要被动态添加到玩家对象上。
        
    - 当检测到合法的鼠标点击时，它不再调用本地的GameManager.RequestMove()。
        
    - 而是找到属于本地玩家的 NetworkPlayerController 实例，并调用其 CmdRequestMove 方法，将移动指令发送给服务器。
        
3. **服务器响应:**
    
    - 服务器上的 NetworkPlayerController 收到 CmdRequestMove RPC后，会验证该移动（例如，该玩家是否是棋子的主人）。
        
    - 验证通过后，调用服务器上权威的 GameManager.RequestMove() 来执行游戏逻辑。
        
4. **状态同步:**
    
    - 服务器的 RealTimeModeController 在执行移动后，需要更新棋子的 RealTimePieceState。
        
    - 创建一个新的同步机制（例如，修改NetworkGameState中的AllPieces列表或创建一个新的SyncList用于移动状态），将棋子的“正在移动”状态、起点和终点同步给所有客户端。
        
    - 客户端的 BoardRenderer 订阅这些移动状态的变化，并播放相应的移动动画。
        

**此阶段完成后，玩家将能够真正在网络上移动棋子。**

---