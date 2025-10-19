### **《魔改中国象棋》- 技术设计概要 (草案)**

下面就是我为你起草的文档。你可以把它保存下来。当你要开启新窗口与别的AI协作时，可以先把这份文档发给它，这将极大地帮助它理解我们的项目。

---

## **1. 项目概述与核心理念**

本游戏是一款将中国象棋与即时战略(RTS)相结合的1v1在线竞技游戏。核心技术目标是实现一个高性能、可扩展、易于维护的游戏框架，支持实时对战、技能系统，并兼容传统的回合制玩法。

## **2. 核心架构：准MVC (Model-View-Controller) 模式**

项目遵循关注点分离(Separation of Concerns)的原则，将游戏逻辑、视觉表现和玩家输入进行解耦。

- **Model (模型层):** 纯粹的游戏数据和规则。它独立于Unity引擎，理论上可以在任何C#环境下运行。
    
    - **BoardState.cs**: 游戏状态的**唯一真实来源(Single Source of Truth)**。用一个二维数组Piece[,]存储所有棋子的逻辑信息（类型、颜色、位置）。
        
    - **PieceData.cs**: 定义了Piece结构体和相关枚举，是数据的基本单元。
        
    - **RuleEngine.cs**: 纯静态工具类，负责计算所有中国象棋的移动规则。它接收一个BoardState作为输入，输出合法的移动列表。它不修改任何状态。
        
- **View (视图层):** 负责将Model中的数据可视化地呈现给玩家。
    
    - **BoardRenderer.cs**: 核心视觉渲染器。它的**唯一职责**是读取GameManager持有的CurrentBoardState，并在场景中创建、移动、销毁、高亮棋子的GameObject。它不包含任何游戏规则逻辑。
        
- **Controller (控制层):** 接收玩家输入，调用Model层更新状态，并通知View层刷新。
    
    - **GameManager.cs**: 游戏的总指挥官和协调者。它以**单例模式**存在，持有权威的CurrentBoardState实例。它提供了核心行为接口（如ExecuteMove），是所有游戏状态变更的入口。
        
    - **PlayerInput.cs**: 玩家输入的直接处理者。负责监听鼠标点击，通过Unity的射线检测判断玩家意图（点击了棋子、移动标记还是棋盘），然后调用GameManager中相应的方法来执行操作。
        

## **3. 关键组件与数据流**

### 3.1 关键组件职责

- **PieceComponent.cs**: 挂载在棋子Prefab上的“身份证”。它是一个桥梁，让场景中的GameObject（View）能够知道自己对应BoardState（Model）中的哪个逻辑坐标。
    

### 3.2 核心数据流 (玩家移动一次棋子)

1. **输入(Input):** PlayerInput.cs 通过Physics.Raycast检测到玩家点击了一个棋子A。
    
2. **请求规则(Controller -> Model):** PlayerInput请求RuleEngine.cs，传入当前的BoardState和棋子A的位置，获得一个合法移动点列表。
    
3. **显示反馈(Controller -> View):** PlayerInput调用BoardRenderer.cs的ShowValidMoves()方法，高亮所有合法落点。
    
4. **执行操作(Input -> Controller):** 玩家点击了一个高亮标记B。PlayerInput.cs捕获此事件。
    
5. **更新状态(Controller -> Model):** PlayerInput调用GameManager.Instance.ExecuteMove(posA, posB)。
    
6. 在ExecuteMove内部：
    
    - GameManager首先更新CurrentBoardState中的数据（将棋子A从posA移动到posB）。
        
    - 然后GameManager调用BoardRenderer.cs的MovePiece()和RemovePieceAt()方法。
        
7. **刷新视觉(Controller -> View):** BoardRenderer根据指令，在场景中移动棋子A的GameObject，并销毁被吃的棋子B的GameObject。
    

## **4. 美术资源工作流**

- **模型:** 使用Blender制作统一尺寸的低多边形棋子模型(Piece.fbx)，并正确展开UV。
    
- **贴图:** 使用Photoshop制作**贴图集(Atlas)**，一张图片包含所有棋子的文字。红黑双方各一张。
    
- **渲染:** 在Unity中，通过MaterialPropertyBlock动态修改材质的UV偏移(_MainTex_ST)来显示正确的文字，此方法性能极高，能有效利用GPU实例化。
    

## **5. [未来规划] 架构重构目标**

为了支持“技能系统”和“回合制/实时”双模式，当前线性的Controller逻辑需要被重构。

- **目标:** 引入**策略模式(Strategy Pattern)**。
    
- **方案:**
    
    - 创建一个IGameModeController接口。
        
    - 实现RealTimeModeController和TurnBasedModeController两个具体的策略类，分别封装两种模式下的输入处理和游戏流程逻辑。
        
    - PlayerInput不再处理具体游戏逻辑，仅作为输入转发器，将点击事件转发给GameManager中当前激活的IGameModeController实例。