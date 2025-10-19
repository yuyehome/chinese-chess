### **《魔改中国象棋》 - 技术设计概要 (重构后)**

#### **1. 项目概述与核心理念**

本游戏是一款将中国象棋与即时战略(RTS)相结合的1v1在线竞技游戏。核心技术目标是实现一个高性能、可扩展、易于维护的游戏框架，支持实时对战、技能系统，并兼容传统的回合制玩法。**当前版本(V0.3)已完成关键的架构重构，以支持多游戏模式。**

#### **2. 核心架构：模型-视图-控制器 (MVC) 与 策略模式 (Strategy Pattern)**

项目遵循关注点分离(Separation of Concerns)的原则，并引入了策略模式来管理不同的游戏玩法逻辑。

- **Model (模型层):** (职责不变) 纯粹的游戏数据和规则。
    
    - **BoardState.cs**: 游戏状态的**唯一真实来源(Single Source of Truth)**。
        
    - **PieceData.cs**: 数据的基本单元。
        
    - **RuleEngine.cs**: 纯静态规则计算工具。
        
- **View (视图层):** (职责不变) 负责将Model中的数据可视化。
    
    - **BoardRenderer.cs**: 核心视觉渲染器。读取BoardState并管理场景中的GameObject。
        
- **Controller (控制层):** (经过重构)
    
    - **GameManager.cs**: 游戏的总指挥官和协调者。
        
        - 持有权威的CurrentBoardState实例。
            
        - **【新增】持有并管理当前激活的GameModeController实例。**
            
        - 提供原子性的游戏行为接口（如ExecuteMove）。
            
    - **PlayerInput.cs**: **【职责简化】** 纯粹的**输入转发器**。
        
        - 只负责监听鼠标点击，并将原始输入事件（点击了棋子、标记或棋盘）转发给GameManager中当前激活的GameModeController。**自身不再包含任何游戏流程逻辑。**
            
    - **GameModeController (策略模式核心):**
        
        - **GameModeController.cs (抽象基类):** 定义了所有游戏模式必须响应的通用输入接口（OnPieceClicked, OnMarkerClicked等）和共享功能（SelectPiece, ClearSelection）。
            
        - **TurnBasedModeController.cs (具体策略):** 实现了传统回合制的完整逻辑，包括回合切换、己方棋子判断等。
            
        - **RealTimeModeController.cs (具体策略):** 为未来的实时行动点模式预留的实现类。
            

#### **3. 关键组件与数据流**

- **PieceComponent.cs**: (职责不变) 挂载在棋子Prefab上的“身份证”，连接View和Model。
    
- **MoveMarkerComponent.cs**: 【新增】挂载在移动标记Prefab上的组件，存储其对应的棋盘坐标，用于精确点击判定。
    

1. **输入(Input):** PlayerInput.cs 检测到玩家点击，并将事件（如OnPieceClicked(piece))转发给 GameManager.Instance.CurrentGameMode。
    
2. **逻辑处理(Controller - Strategy):** 当前的GameModeController实例（即TurnBasedModeController）接收到事件。
    
    - 它根据**自身的状态**（如currentPlayerTurn）和规则判断玩家的意图（选择、攻击、无效操作）。
        
3. **请求规则(Controller -> Model):** 如果是“选择”意图，TurnBasedModeController会调用RuleEngine.cs来获取合法移动点。
    
4. **显示反馈(Controller -> View):** TurnBasedModeController调用BoardRenderer.cs的ShowValidMoves()方法来高亮。
    
5. **执行操作(Input -> Controller - Strategy):** 玩家再次点击（棋子或标记），事件被再次转发到TurnBasedModeController。
    
6. TurnBasedModeController判断这是一个合法的移动/攻击，于是调用GameManager.Instance.ExecuteMove()。
    
7. **更新状态(GameManager -> Model & View):** GameManager.ExecuteMove方法：
    
    - 首先更新CurrentBoardState (Model)。
        
    - 然后调用BoardRenderer的方法来更新场景 (View)。
        
8. **流程推进(Controller - Strategy):** TurnBasedModeController在ExecuteMove调用成功后，执行SwitchTurn()方法，改变自身状态，推进游戏流程。
    

#### **4. 美术资源工作流**

(无变化) 使用单一模型 + 贴图集 + MaterialPropertyBlock 的高性能方案。

---

### **后续开发计划蓝图**

根据你提供的计划，我将其整合到我们的开发阶段中。

#### **阶段 1 (收尾): 完善单机核心玩法**

- **当前位置 ->** **任务 1.4: 实现将军和将死判断。**
    
    - 在RuleEngine中增加一个IsKingInCheck(color, boardState)的方法。
        
    - 在GameManager.ExecuteMove()的末尾调用此方法，进行将军提示。
        
    - 在将军状态下，GetValidMoves需要过滤掉那些无法解除将军的“非法”移动。
        
    - 判断将死（Checkmate）和逼和（Stalemate）。
        

#### **阶段 2: 引入实时模式与UI**

- **任务 2.1: 实现主菜单与模式选择。**
    
    - 创建 MainMenu 场景。
        
    - 创建UI按钮对应开发计划中的各个入口。
        
    - 点击按钮加载 Game 场景，并通过一个静态变量或ScriptableObject将选择的模式（如 "TurnBased_Vs_AI", "RealTime_Local"）传递给GameManager。
        
    - GameManager.Start()方法根据传入的模式，实例化对应的GameModeController。
        
- **任务 2.2: 实现实时行动点系统 (RealTimeModeController)**
    
    - 创建EnergySystem.cs，负责管理双方玩家的行动点恢复、上限和消耗。
        
    - RealTimeModeController将持有EnergySystem的实例。
        
    - 在OnPieceClicked中，检查对应玩家的行动点是否足够。
        
    - 在执行移动后，通过EnergySystem消耗行动点。
        
- **任务 2.3: 创建游戏UI**
    
    - 为双方玩家创建行动点进度条。
        
    - 创建将军提示、游戏结束面板等UI元素。
        

#### **阶段 3: 实现AI对手**

- **任务 3.1: 创建AI控制器。**
    
    - 可以创建一个AIModeController继承自TurnBasedModeController或RealTimeModeController。
        
    - 在AI的回合/行动时机，调用AI算法。
        
- **任务 3.2: 实现AI算法。**
    
    - 初期可以实现一个简单的AI，例如：随机选择一个可移动的棋子，并随机执行一个合法移动。
        
    - 进阶可采用**Minimax算法**或**Alpha-Beta剪枝**来进行决策。
        

#### **阶段 4: 集成Fish-Net网络**

- **任务 4.1: 搭建网络基础。**
    
    - 创建NetworkManager预制件，并配置Fish-Net。
        
    - 在主菜单UI中加入“创建房间”、“加入房间”的功能，调用NetworkManager的API。
        
- **任务 4.2: 状态同步与权威服务器。**
    
    - 修改GameManager，使其在网络模式下成为NetworkBehaviour。
        
    - BoardState将不再是简单的 new BoardState()，而是需要在服务器上创建，并将其状态通过网络变量（如SyncVar）或RPC同步给客户端。
        
    - **重构输入流程:**
        
        - 客户端的GameModeController在玩家执行操作时，不再直接调用GameManager.ExecuteMove()。
            
        - 而是调用一个[ServerRpc]方法，将移动指令（from, to）发送给服务器（房主）。
            
        - 服务器端的GameManager在[ServerRpc]方法中接收到指令，验证其合法性，然后调用权威的ExecuteMove方法。
            
- **任务 4.3: 客户端播放。**
    
    - 客户端的BoardState将通过网络同步自动更新。
        
    - 我们需要一个机制（例如SyncVar的OnChange钩子函数或RPC），当客户端的BoardState数据发生变化时，自动触发BoardRenderer进行重绘或更新。客户端本身不进行任何主动的逻辑状态变更。
        

---