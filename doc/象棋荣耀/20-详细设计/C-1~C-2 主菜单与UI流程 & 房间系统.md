### **详细设计文档: C-1.1, C-1.2, C-2.1**

**文档版本**: 1.0  
**设计目标**:

1. **(C-1.1)** 构建符合“新中式-科技感”风格、采用“中心辐射式”布局的主菜单，并实现以“单机闯关”为引导、兼顾PVP的聚合式玩法流程。
    
2. **(C-2.1)** 设计一套完整的房间与大厅系统，支持自定义游戏，并集成“邀请单机玩家”的冷启动机制。
    
3. **(C-1.2)** 设计清晰、高效的对战界面(HUD)，能够准确传达实时对战中的核心信息（行动点、技能CD等）。
    

---

### **第一部分：核心功能与逻辑说明 (Functional & Logic Specification)**

1. **主流程引导**: 游戏启动后，主菜单的视觉重心和默认引导将指向【**闯关模式**】。巨大的【**对战**】按钮虽然也很显眼，但在视觉上略次于闯关入口，鼓励新玩家先通过PVE熟悉游戏。
    
2. **玩法聚合**: 所有玩法入口都清晰地呈现在主菜单或其下一级菜单中。玩家无需在复杂的菜单中寻找。
    
3. **无缝邀请 (PVE -> PVP Crossover)**:
    
    - 在PVE（闯关、人机）游戏过程中，系统会根据特定条件（如：有等待时间超过5秒且总等待房间数小于3的PVP房间），在屏幕角落弹出一个**非侵入式**的邀请框。
        
    - 邀请框会显示房间名、模式等基本信息，并提供【加入】和【忽略】按钮。
        
    - 玩家点击【加入】，当前PVE对局会**暂停**（或直接结束，待定），然后无缝切换到PVP房间。
        
4. **房间系统**: 提供创建、加入、密码保护、模式选择、金币对战设置等标准功能。房主拥有管理权限。
    
5. **对战HUD**: 实时显示双方玩家信息、行动点（以能量条+数字形式）、每个棋子独立的技能冷却状态。信息布局遵循“角落原则”，避免遮挡中心棋盘区域。
    

---

### **第二部分：技术架构与代码设计 (Technical & Code Design)**

### **模块 1: App.UI - UI系统 (C-1.1 & C-1.2)**

- **代码路径**: Assets/Script/_App/UI/
    

#### **1.1. 核心UI框架**

- **UIManager** (单例)
    
    - **职责**: 管理所有UI面板(Panel)的生命周期（显示、隐藏、销毁），处理面板间的层级关系和导航。提供一个全局的UI事件系统。
        
    - **关键方法**: ShowPanel<T>(), HidePanel<T>(), ShowPopup(PopupData data)。
        
- **UIPanel** (抽象基类)
    
    - **职责**: 所有UI面板的基类，提供OnShow(), OnHide(), Refresh()等生命周期方法。
        

#### **1.2. 主菜单 (MainMenuPanel)**

- **关联场景**: MainMenuScene
    
- **布局与控件 (对应你的需求文档 9.1 流程一)**:
    
    - **顶部栏**:
        
        - PlayerInfoView: 显示玩家头像、昵称、金币、段位。点击可进入玩家资料页。
            
    - **中央区域**:
        
        - CampaignButton: **视觉上最大、最核心的按钮**，指向闯关模式。
            
        - BattleButton: 稍小的核心按钮，点击后打开BattleModeSelectPanel。
            
    - **左/右侧**:
        
        - SideButtonsGroup: 包含【商城】、【任务】、【武将库】、【排行榜】等次级入口的小按钮。
            
    - **底部栏**:
        
        - 【设置】、【社区】、【退出游戏】等功能按钮。
            

#### **1.3. 对战模式选择 (BattleModeSelectPanel)**

- **类型**: 一个弹出式或全屏的面板，由BattleButton触发。
    
- **布局与控件**:
    
    - **ModeTabs**: 左侧垂直标签页，包含【实时对战】、【传统对战】、【创建房间】。
        
    - **ContentArea**: 右侧内容区。
        
        - **实时对战页**: 【排位赛(M3)按钮】、【匹配赛(M2)按钮】。点击后调用INetworkService.StartMatchmaking(mode)。
            
        - **传统对战页**: 【匹配赛(M1)按钮】。
            
        - **创建房间页**: 显示创建房间的各种设置（房间名、密码、模式、金币赌注），点击【确认创建】后调用INetworkService.CreateLobby(settings)。
            

#### **1.4. 对战HUD (BattleHUD)**

- **关联场景**: BattleScene
    
- **布局与控件 (对应你的需求文档 9.1 流程四)**:
    
    - **顶部/底部**:
        
        - PlayerInfoHUD_Top: 敌方玩家信息。
            
        - PlayerInfoHUD_Bottom: 我方玩家信息。
            
    - **我方信息区 (左下角)**:
        
        - ActionPointBar: 行动点能量条和数字文本。
            
        - HeroSkillIcons_Grid: 一个网格布局，用于显示**所有上场武将**的技能图标。每个图标都包含一个冷却计时器覆盖层。
            
    - **棋子交互UI**:
        
        - 当**选择**一个拥有技能的棋子时，其在HeroSkillIcons_Grid中对应的技能图标会高亮，并弹出一个【释放技能】的大按钮在屏幕右侧。
            
    - **右上角**:
        
        - 【设置】、【投降】按钮。
            

### **模块 2: App.Lobby - 房间与大厅系统 (C-2.1)**

- **代码路径**: Assets/Script/_App/Lobby/
    

#### **2.1. 房间面板 (RoomPanel)**

- **关联场景**: 作为一个独立的面板，可以在MainMenuScene之上弹出。
    
- **布局与控件 (对应你的需求文档 9.1 流程三)**:
    
    - RoomInfoDisplay: 显示房间ID、模式、设置等。
        
    - PlayerSlot_Red, PlayerSlot_Black: 显示双方玩家的头像、昵称、准备状态。
        
    - ReadyButton: 准备/取消准备按钮。
        
    - StartGameButton: (仅房主可见) 开始游戏按钮。
        
    - InviteButton: (可选) 邀请Steam好友按钮。
        
    - ChatWindow: 房间内聊天窗口。
        
- **逻辑**:
    
    - RoomPanel通过监听INetworkService.OnLobbyStateUpdated事件来实时刷新所有显示内容。
        
    - 点击按钮会调用INetworkService的对应方法，如UpdatePlayerReadyState()。
        

#### **2.2. PVE邀请系统 (PveInviteSystem)**

- **类型**: MonoBehaviour, 单例
    
- **职责**: 在PVE场景中运行，负责轮询和显示PVP邀请。
    
- **关键成员**:
    
    - private float pollTimer;
        
- **关键逻辑 (Update)**:
    
    1. pollTimer += Time.deltaTime;
        
    2. if (pollTimer >= 2.0f):
        
        - pollTimer = 0;
            
        - 调用一个**新的**INetworkService方法 QueryWaitingLobbies()。
            
        - INetworkService会向后端（或Steam Lobby API）查询符合条件的房间列表。
            
        - 如果返回的列表不为空（例如，count < 3 且 oldest.waitTime > 5s），则触发一个全局事件 OnPvpInviteReceived(lobbyInfo)。
            
- **PveInvitePopup**:
    
    - 一个独立的UI面板，监听OnPvpInviteReceived事件。
        
    - 收到事件后，自我显示，并展示邀请信息。
        
    - 点击【加入】按钮，调用INetworkService.JoinLobby(lobbyId)。
        

---

### **Unity工作流 (Unity Workflow)**

1. **UI Prefab制作**:
    
    - 为上述所有Panel (MainMenuPanel, BattleModeSelectPanel, RoomPanel, BattleHUD, PveInvitePopup) 创建对应的Prefab。
        
    - 使用RectTransform的Anchors和Canvas Scaler来确保分辨率自适应。
        
    - 将按钮、文本、图片等控件的引用拖拽到对应Panel脚本的Inspector字段中。
        
2. **UIManager配置**:
    
    - 在UIManager中，创建一个可配置的列表，将所有Panel Prefab注册进去，以便按类型名动态实例化。
        
3. **流程串联**:
    
    - 在MainMenuScene中，默认激活MainMenuPanel。
        
    - MainMenuPanel中的按钮点击事件，会调用UIManager来打开其他面板（如BattleModeSelectPanel），或者调用NetworkServiceProvider来启动游戏流程。
        
    - 在BattleScene中，GameLoopController会负责激活BattleHUD，并为其提供GameState的数据源。
        

---