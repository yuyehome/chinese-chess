# **《魔改中国象棋》技术交接文档 (v2.0)**

### **1. 项目概述**

#### **1.1 核心理念**

本项目是一款将中国象棋的经典策略与即时战略(RTS)相结合的1v1在线竞技游戏。其核心是打破传统回合制，引入**实时行动点系统**和**动态棋子状态模型**，并支持**可插拔的AI策略**，为未来的网络对战和技能系统打下坚实基础。

#### **1.2 技术目标**

构建一个高性能、可扩展、易于维护的游戏框架，其特点是：

- **逻辑与表现分离：** 核心游戏规则与Unity的视觉表现（MonoBehaviour）严格解耦。
    
- **事件驱动：** 关键的游戏事件（如棋子死亡）通过事件广播，降低模块间的耦合。
    
- **策略模式驱动：** 游戏模式（回合制/实时）和AI难度（简单/困难/极难）都采用策略模式，易于扩展。
    
- **清晰的控制流：** 明确划分了**输入/决策层**、**游戏管理层**和**逻辑执行层**，为网络同步做好了准备。
    

---

### **2. 核心架构设计**

项目采用**模型-视图-控制器 (MVC)** 架构模式，并在此基础上扩展出了一个独立的**输入/决策控制器层 (Controller Layer)**。

#### **架构关系图**

codeCode

- **模型 (Model):** 纯粹的游戏数据和规则，与Unity引擎解耦。
    
    - BoardState: 存储**静止**棋子的二维数组，是棋盘逻辑状态的基础。
        
    - PieceData: 定义棋子“身份”（类型、颜色）的纯数据结构体。
        
    - RealTimePieceState: 存储棋子在实时对战中的动态状态（是否移动、逻辑位置等）。
        
    - RuleEngine: 纯静态的中国象棋基础移动规则计算器。
        
- **视图 (View):** 负责将Model中的数据可视化为场景中的GameObject。
    
    - BoardRenderer: 唯一的视觉渲染核心。负责创建/销毁棋子、播放动画、显示高亮。它只执行指令，不包含任何游戏逻辑。
        
- **控制器 (Controller - 分为三层):**
    
    1. **输入/决策层 (IPlayerController):** 游戏的“玩家”或“代理”。
        
        - PlayerInputController: 监听鼠标点击，管理玩家的选择/高亮，并向GameManager**请求**移动。
            
        - AIController: AI的“身体”，管理决策时机，并将AI“大脑”（IAIStrategy）计算出的移动向GameManager**请求**移动。
            
        - IAIStrategy (Easy, Hard, VeryHard): AI的“大脑”，负责具体的决策算法，与Unity API完全解耦，可在后台线程运行。
            
    2. **游戏管理层 (GameManager):** 游戏的总指挥官和中央枢纽。
        
        - 作为单例存在，初始化所有系统。
            
        - 持有并管理所有IPlayerController。
            
        - 作为所有移动请求的**唯一入口 (RequestMove)**，负责校验（如能量）和分发。
            
        - 监听全局事件（如OnPieceKilled），并协调模型和视图进行更新。
            
    3. **游戏模式层 (GameModeController - 策略模式):** 定义特定模式下的“物理法则”。
        
        - RealTimeModeController: 接收GameManager的执行指令，驱动棋子的状态变化、碰撞检测，是所有实时逻辑的**最终执行者**。
            
        - TurnBasedModeController: 实现了传统回合制的轮转逻辑。
            

---

### **3. 关键数据流详解 (以极难AI执行一次移动为例)**

1. **决策时机 (AIController.Update):**
    
    - AIController内部计时器触发，调用MakeDecisionAsync方法。
        
    - isThinking标志设为true，防止重复决策。
        
2. **主线程预处理 (MakeDecisionAsync - 主线程部分):**
    
    - AIController首先检查开局库 (vhStrategy.TryGetOpeningBookMove)。这是一个快速、需要在主线程执行的操作，因为它需要访问BoardRenderer。
        
    - 如果开局库未命中，AIController会从GameManager安全地获取所有纯数据（BoardState、SimulatedPiece列表）。
        
3. **后台线程深度思考 (MakeDecisionAsync - 后台线程部分):**
    
    - AIController调用await Task.Run(...)，将**纯数据**和FindBestMove的计算任务抛到后台线程。
        
    - VeryHardAIStrategy.FindBestMove在后台线程中执行耗时的**Minimax算法**，期间主线程**完全流畅**。
        
4. **返回结果 (MakeDecisionAsync - 主线程部分):**
    
    - await执行完毕，代码回到主线程。
        
    - AIController获得了后台计算出的最佳移动方案 bestMove (一个MovePlan对象)。
        
5. **提交请求 (AIController -> GameManager):**
    
    - AIController调用gameManager.RequestMove(color, bestMove.From, bestMove.To)。
        
6. **验证与执行 (GameManager -> RealTimeModeController):**
    
    - GameManager.RequestMove检查能量，如果足够则消耗能量，并调用realTimeModeController.ExecuteMoveCommand(from, to)。
        
7. **逻辑与动画 (RealTimeModeController -> Model & View):**
    
    - RealTimeModeController命令BoardState将棋子“提起”，并命令BoardRenderer开始播放移动动画。
        
    - 动画播放期间，BoardRenderer通过回调函数将进度报告给RealTimeModeController，后者实时更新棋子的RTState。
        
8. **战斗与死亡 (事件驱动):**
    
    - RealTimeModeController的Tick方法驱动CombatManager进行碰撞检测。
        
    - 如果发生击杀，CombatManager触发OnPieceKilled事件。
        
    - GameManager监听到此事件，并分别命令BoardState和BoardRenderer移除死亡棋子的数据和对象。
        

---

### **4. 文件结构与职责说明**

|   |   |
|---|---|
|文件夹/文件名|核心职责|
|**_Scripts/Core/**|**存放游戏核心数据结构与逻辑模块**|
|BoardState.cs|**模型(M):** 存储和管理棋盘上**静止**棋子的逻辑数据。|
|CombatManager.cs|**逻辑:** 实时模式下处理所有战斗碰撞，判定胜负，并触发OnPieceKilled事件。|
|EnergySystem.cs|**逻辑:** 管理双方玩家的行动点（能量）。|
|GameModeSelector.cs|在场景间传递玩家选择的游戏模式和AI难度。|
|PieceData.cs|**模型(M):** 定义Piece结构体和相关枚举（颜色、类型）。|
|PieceValue.cs|提供不同棋子类型的价值，用于AI评估。|
|RealTimePieceState.cs|**模型(M):** 定义棋子在实时对战中的所有动态状态。|
|RuleEngine.cs|纯静态类，计算中国象棋的基础移动和攻击规则。|
|**_Scripts/GameModes/**|**存放不同游戏模式的具体实现 (策略模式)**|
|GameModeController.cs|所有游戏模式控制器的抽象基类。|
|RealTimeModeController.cs|**控制器(C-执行层):** 实时对战模式的**逻辑执行者**，驱动所有实时状态变化。|
|TurnBasedModeController.cs|传统回合制模式的控制器。|
|**_Scripts/Controllers/**|**存放输入/决策代理 (策略模式)**|
|IPlayerController.cs|**接口:** 定义了所有“玩家”代理（人、AI、网络）的统一初始化规范。|
|PlayerInputController.cs|**控制器(C-输入层):** 处理本地玩家的鼠标输入、选择高亮，并向GameManager**请求**移动。|
|TurnBasedInputController.cs|回合制模式的专属输入处理器。|
|AIController.cs|**控制器(C-AI代理):** AI的“身体”。负责管理决策时机，启动后台思考，并将AI的决策**请求**移动。|
|**_Scripts/Controllers/AI/**|**存放AI的“大脑” (策略模式)**|
|IAIStrategy.cs|**接口:** 定义了所有AI难度“大脑”的统一规范，核心是FindBestMove方法。|
|EasyAIStrategy.cs|**AI策略:** 实现了基于概率（进攻/躲避/随机）的简单AI。|
|HardAIStrategy.cs|**AI策略:** 继承自EasyAIStrategy，实现了基于单步评估函数（考虑威胁、位置等）的困难AI。|
|VeryHardAIStrategy.cs|**AI策略:** 继承自HardAIStrategy，实现了开局库和Minimax浅层搜索的极难AI。|
|**_Scripts/UI/**|**存放所有UI相关的控制脚本**|
|EnergyBarSegmentsUI.cs|控制分段式能量条的视觉表现。|
|GameUIManager.cs|管理游戏内UI的创建、布局和数据更新。|
|MainMenuController.cs|主菜单场景的UI交互逻辑。|
|**_Scripts/ (根目录)**|**存放与场景对象直接交互的组件**|
|BoardRenderer.cs|**视图(V):** 视觉渲染核心，管理棋盘上所有GameObject。|
|GameManager.cs|**控制器(C-管理层):** 游戏总管理器(Singleton)，协调所有模块，是移动请求的唯一入口。|
|MoveMarkerComponent.cs|挂载在“移动标记”Prefab上的数据组件。|
|PieceComponent.cs|挂载在棋子Prefab上的“身份证”，连接视觉与逻辑。|

---

### **5. 后续开发建议**

#### **5.1 技能系统开发**

- **数据层:** 创建一个 SkillData.cs 并使用 ScriptableObject 来定义技能（名称、效果、冷却、消耗等），方便策划配置。
    
- **逻辑层:**
    
    1. 创建一个 SkillManager.cs 单例，用于处理技能的释放和冷却。
        
    2. 创建一个 BuffSystem.cs，用于在 PieceComponent 或 RealTimePieceState 上附加/移除状态效果（如无敌、眩晕、加速）。
        
- **集成点:**
    
    1. 在 PieceComponent 中添加 List<SkillData> skills。
        
    2. PlayerInputController 检测到玩家选中带技能的棋子时，在UI上显示技能按钮。
        
    3. 点击技能按钮后，调用 SkillManager.ActivateSkill(piece, skill)。
        
    4. SkillManager 进而调用 BuffSystem，修改棋子的 RealTimePieceState（例如，rtState.IsVulnerable = false）。RealTimeModeController 和 CombatManager 会自然地响应这些状态变化，无需大的改动。
        

#### **5.2 网络功能开发 (Fish-Net)**

当前架构对网络同步非常友好，因为**请求**和**执行**是分离的。

- **核心原则:** **服务器权威**。客户端只发送输入请求，不执行任何游戏逻辑。
    
- **实现步骤:**
    
    1. **改造控制器:**
        
        - 在客户端，PlayerInputController 的 gameManager.RequestMove 调用不再直接执行逻辑，而是调用一个 [ServerRpc] 方法，将移动指令 {from, to} 发送给服务器。
            
        - 在服务器上，为非本地玩家创建一个 NetworkController，它负责接收来自远程客户端的数据包，并调用服务器上的 GameManager.RequestMove。
            
    2. **服务器执行:** 服务器上的 GameManager 和 RealTimeModeController 像单机模式一样正常执行所有游戏逻辑。
        
    3. **状态同步:**
        
        - 服务器上的 BoardState 和所有棋子的 RealTimePieceState 是权威数据。
            
        - 将这些核心状态（特别是棋子的位置、IsMoving、IsDead等）通过Fish-Net的同步机制（SyncVar, SyncList, 或自定义序列化）广播给所有客户端。
            
        - 全局事件，如 OnPieceKilled，也应该由服务器触发一个 [ObserversRpc]，通知所有客户端某个棋子死亡。
            
    4. **客户端表现:**
        
        - 客户端的 RealTimeModeController 和 CombatManager **不运行**任何逻辑计算。
            
        - 客户端的 BoardRenderer 和其他视觉组件，完全根据从服务器同步来的状态数据来更新画面。例如，当一个棋子的同步状态变为 IsMoving=true 并且其目标位置更新时，客户端的 BoardRenderer 就播放相应的移动动画。客户端是一个忠实的“播放器”。