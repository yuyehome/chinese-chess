
## **《魔改中国象棋》技术交接文档**


### **1. 项目概述**

#### **1.1 核心理念**

本项目是一款将中国象棋的经典策略与即时战略(RTS)相结合的1v1在线竞技游戏。其核心是打破传统回合制，引入**实时行动点系统**和**动态的棋子状态模型**，创造出充满微操、博弈和紧张感的全新对战体验。

#### **1.2 技术目标**

构建一个高性能、可扩展、易于维护的游戏框架，能够同时支持**传统回合制**和**实时对战**两种核心玩法，并为未来的技能系统、AI对手和网络对战功能打下坚实的基础。

---

### **2. 核心架构设计**

项目采用**模型-视图-控制器 (MVC)** 架构模式，并巧妙地结合了**策略模式 (Strategy Pattern)** 来管理不同的游戏玩法。

(这是一个简化的概念图，帮助理解模块关系)

- **Model (模型层):** 纯粹的游戏数据和规则，与Unity引擎解耦。
    
    - BoardState.cs: 存储**静止**棋子的二维数组，是棋盘逻辑状态的基础。
        
    - PieceData.cs: 定义了棋子“身份”（类型、颜色）的纯数据结构体。
        
    - RuleEngine.cs: 纯静态的中国象棋基础移动规则计算器。
        
    - RealTimePieceState.cs: 【实时模式专属】存储棋子在实时对战中的动态状态（是否移动、是否无敌、逻辑位置等）。
        
- **View (视图层):** 负责将Model中的数据可视化为场景中的3D对象。
    
    - BoardRenderer.cs: 核心视觉渲染器。负责创建/销毁棋子GameObject、播放移动动画、显示高亮和标记。它通过一个内部的 pieceObjects 数组来管理视觉对象。
        
- **Controller (控制层):** 协调Model和View，处理玩家输入和游戏流程。
    
    - GameManager.cs: **游戏总指挥官**。作为单例存在，负责初始化游戏、协调各模块、执行核心操作（如 ExecuteMove）。
        
    - PlayerInput.cs: **纯粹的输入转发器**。只负责监听鼠标点击，并将事件（如“点击了哪个棋子”）转发给当前激活的游戏模式控制器。
        
    - **策略模式核心 (GameModeController):**
        
        - GameModeController.cs: **抽象基类**。定义了所有游戏模式都必须响应的通用输入接口 (OnPieceClicked, OnMarkerClicked 等)。
            
        - TurnBasedModeController.cs: **具体策略**。实现了传统回合制的完整逻辑。
            
        - RealTimeModeController.cs: **具体策略**。实现了实时对战的复杂逻辑，包括状态更新、动态高亮和与战斗系统的交互。
            

---

### **3. 文件结构与职责说明**

项目脚本主要分布在 _Scripts 文件夹下的四个子目录中：

|   |   |
|---|---|
|文件夹/文件名|核心职责|
|**_Scripts/Core/**|**存放游戏核心数据结构与逻辑模块**|
|BoardState.cs|存储和管理棋盘上**静止**棋子的逻辑数据。|
|CombatManager.cs|【实时模式】处理所有战斗碰撞、伤害判定和棋子死亡逻辑。|
|EnergySystem.cs|【实时模式】管理双方玩家的行动点（能量）。|
|GameModeSelector.cs|在场景间传递玩家选择的游戏模式（回合制/实时）。|
|PieceData.cs|定义 Piece 结构体和相关枚举（颜色、类型）。|
|PieceValue.cs|【实时模式】定义不同棋子的价值，用于友军碰撞裁决。|
|RealTimePieceState.cs|【实时模式】定义棋子在实时对战中的所有动态状态。|
|RuleEngine.cs|纯静态类，计算中国象棋的基础移动规则。|
|**_Scripts/GameModes/**|**存放不同游戏模式的具体实现 (策略模式)**|
|GameModeController.cs|所有游戏模式控制器的抽象基类。|
|RealTimeModeController.cs|实时对战模式的核心控制器，驱动所有实时逻辑。|
|TurnBasedModeController.cs|传统回合制模式的控制器。|
|**_Scripts/UI/**|**存放所有UI相关的控制脚本**|
|EnergyBarSegmentsUI.cs|控制单个分段式能量条的视觉表现。|
|GameUIManager.cs|管理游戏内UI的创建、布局适配和数据更新。|
|MainMenuController.cs|主菜单场景的UI交互逻辑。|
|**_Scripts/ (根目录)**|**存放与场景对象直接交互的组件**|
|BoardRenderer.cs|视觉渲染核心，管理棋盘上所有GameObject。|
|GameManager.cs|游戏总管理器，协调所有模块。|
|MoveMarkerComponent.cs|挂载在“移动标记”Prefab上的数据组件。|
|PieceComponent.cs|挂载在棋子Prefab上的“身份证”，连接视觉与逻辑。|
|PlayerInput.cs|监听并转发玩家的鼠标点击输入。|

---

### **4. 核心数据流详解（以实时模式为例）**

一次典型的“玩家操作棋子移动”的数据流如下：

1. **输入 (Input):** PlayerInput.cs 检测到鼠标点击，通过射线检测识别出被点击的是 PieceComponent。
    
2. **转发 (Controller):** PlayerInput 调用 GameManager.Instance.CurrentGameMode.OnPieceClicked(clickedPiece)。此时 CurrentGameMode 是 RealTimeModeController 的实例。
    
3. **选择逻辑 (Controller - Strategy):** RealTimeModeController.OnPieceClicked -> TrySelectPiece
    
    - 检查 EnergySystem 确认行动点是否足够。
        
    - 若足够，调用基类的 SelectPiece 方法，将该棋子设为 selectedPiece。
        
    - SelectPiece 调用 RuleEngine 初步计算合法移动点，并调用 BoardRenderer 显示高亮和选择标记。
        
4. **动态高亮 (Controller - Strategy):**
    
    - 在 RealTimeModeController.Tick() -> UpdateSelectionHighlights() 中：
        
    - 每帧动态构建一个包含移动中棋子位置的“虚拟棋盘” (GetLogicalBoardState)。
        
    - 用这个虚拟棋盘重新计算 selectedPiece 的合法移动点。
        
    - 如果列表有变，则调用 BoardRenderer.ShowValidMoves 刷新高亮。
        
5. **执行移动 (Input -> Controller):** 玩家点击移动标记，PlayerInput 转发事件至 RealTimeModeController.OnMarkerClicked。
    
6. **下达指令 (Controller -> Manager):** OnMarkerClicked -> PerformMove
    
    - 设置棋子的 RTState 为移动状态，并将其加入 movingPieces 列表。
        
    - 调用 GameManager.Instance.ExecuteMove(from, to, onProgress, onComplete)，并传入两个**回调函数**。
        
7. **启动移动 (Manager -> View & Model):** GameManager.ExecuteMove
    
    - 在 BoardState (模型) 中将 from 位置的棋子移除（提起）。
        
    - 调用 BoardRenderer (视图) 的 MovePiece 方法，启动视觉移动动画，并将回调函数传递过去。
        
8. **动画与状态更新 (View & Controller):**
    
    - BoardRenderer.MovePieceCoroutine 每帧播放动画。
        
    - 同时，它调用 onProgress 回调，将动画进度传回 RealTimeModeController。
        
    - RealTimeModeController 在 UpdateAllPieceStates 中根据进度更新棋子的**攻防状态**和**逻辑位置**。
        
9. **碰撞判定 (Controller -> Combat):**
    
    - RealTimeModeController.Tick() 调用 CombatManager.ProcessCombat。
        
    - CombatManager 根据所有棋子的实时状态和位置（通过 localPosition 计算距离）进行碰撞检测。
        
    - 如果发生击杀，CombatManager.Kill 方法会直接销毁棋子的 GameObject，并更新 BoardState。
        
10. **移动完成 (View -> Controller -> Model):**
    
    - 若棋子未被中途击杀，动画播放完毕，onComplete 回调被触发。
        
    - RealTimeModeController 在回调中将棋子在 BoardState (模型) 的 to 位置“落座”，并重置其 RTState。
        

---

### **5. 后续开发工作建议**

#### **5.1 遗留小任务 (美术 & 音效)**

- **实现思路:** 创建一个 SoundManager 和/或 EffectManager 单例。
    
- 在关键逻辑点（如 CombatManager.Kill, RealTimeModeController.SelectPiece, GameManager.HandleEndGame）调用这些管理器的公共方法（如 PlaySound(SoundType.Capture)）。
    
- 王见王的激光可以作为一个独立的系统，在 GameManager 或 RealTimeModeController 的 Tick 中持续检测触发条件，满足则创建并驱动一个“激光”GameObject。
    

#### **5.2 实现AI对手**

- **架构建议:** 创建一个新的 AIModeController，它继承自 RealTimeModeController。
    
- **AI输入:** 在 GameManager 中增加一个 PlayerController 和 AIController 的概念。AIModeController 会忽略 PlayerInput 的事件，而是由一个内部的 AIPlayer 类来驱动。
    
- **AI算法 (AIPlayer.cs):**
    
    - **简单AI:** 在 Update 中使用计时器，每隔随机时间（模拟反应），执行一次“随机合法移动”。从所有己方棋子中随机选一个，再从其合法移动中随机选一个执行。
        
    - **中等/困难AI:** 实现**局面评估函数 (Evaluation Function)**，能给一个棋盘状态打分（例如，车=9分，兵=1分，考虑位置优势等）。然后使用 **Minimax** 或 **Alpha-Beta剪枝** 算法来预测几步之后的最佳走法。对于实时制，AI可以在每个决策点（例如每秒）运行一次搜索，选择当前最优的移动。
        

#### **5.3 集成Fish-Net网络**

当前架构对网络同步非常友好，因为逻辑和表现是分离的。

- **核心原则:** **服务器（或房主）权威**。客户端只发送输入，不执行任何游戏逻辑。
    
- **步骤 4.1 & 4.2 (搭建与同步):**
    
    - 将 GameManager 改造为 NetworkBehaviour。
        
    - 将 BoardState 和所有棋子的 RealTimePieceState 作为服务器上的权威数据。可以使用 Fish-Net 的 SyncVar 或自定义的结构体同步。
        
    - 玩家输入不再直接调用 PerformMove。而是调用一个 [ServerRpc] 方法，将移动指令（如 MoveRequest(from, to)）发送给服务器。
        
    - 服务器在 [ServerRpc] 方法中接收到请求，验证其合法性（能量、规则等），然后执行权威的移动逻辑。
        
    - 服务器状态的改变（BoardState 更新、棋子死亡）会自动通过网络同步到所有客户端。
        
- **步骤 4.3 (客户端播放):**
    
    - 客户端的 BoardRenderer 和 RealTimeModeController 需要监听网络状态的变化。
        
    - 当客户端接收到服务器的状态更新时（例如，一个棋子开始移动），客户端的 RealTimeModeController 会触发 BoardRenderer 播放对应的动画。
        
    - 客户端**不进行任何碰撞检测或逻辑计算**，它只是一个忠实的“播放器”，完全根据服务器同步来的数据来渲染画面。
        

---


