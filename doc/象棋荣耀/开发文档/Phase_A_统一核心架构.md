  《Phase_A_统一核心架构》
**阶段目标**: 搭建一个健壮、可扩展、模式兼容的“空壳”游戏框架。此阶段结束时，项目可运行，但无具体玩法。

|       |            |                        |                                                 |                                                                                                                                |                              |
| ----- | ---------- | ---------------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | ---------------------------- |
| 任务ID  | 冲刺(Sprint) | 任务名称                   | 需求描述                                            | 验收标准 (AC)                                                                                                                      | 涉及模块                         |
| A-1.1 | Sprint A.1 | 基础层与核心数据结构             | 定义游戏中最基础的数据结构，如棋子、玩家、游戏配置等，为所有上层逻辑提供统一的数据模型。    | 1. PieceData类创建，包含ID、类型、归属、位置、状态等字段。<br>2. PlayerProfile类创建。<br>3. GameConfig ScriptableObject创建，可配置行动点恢复速度等。                  | Core.Foundation, Core.Data   |
| A-2.1 | Sprint A.2 | 指令系统 (Command Pattern) | 设计并实现一套指令系统，所有改变游戏状态的操作都必须通过指令完成。               | 1. ICommand接口定义完成。<br>2. MoveCommand, SkillCommand等基础指令类创建并可序列化。<br>3. CommandProcessor可以接收并执行一个MoveCommand来更新GameState中的棋子位置。 | Core.Command, Core.GameState |
| A-3.1 | Sprint A.3 | 模块化游戏模式管理器             | 创建一个管理器，能根据配置在游戏开始时加载不同的游戏规则逻辑“插件”。             | 1. IGameModeLogic接口定义完成。<br>2. GameModeManager能根据传入的枚举（GameMode.TB, GameMode.RT）实例化对应的空逻辑类。                                    | Core.GameModes               |
| A-4.1 | Sprint A.4 | 视图-逻辑分离框架              | 建立表现层(View)与数据层(GameState)的单向依赖关系。视图层只负责响应数据变化。 | 1. PieceView脚本能根据PieceData的变化来更新自身在场景中的位置。<br>2. InputController能将鼠标点击转换为MoveCommand并发送。                                       | Core.View, Core.Controller   |
| A-5.1 | Sprint A.5 | 网络层抽象与封装               | 封装Fish-Networking，提供统一的网络服务接口，支持无缝切换单机/联机模式。    | 1. INetworkService接口定义。<br>2. FishNetService和OfflineService实现该接口。<br>3. 项目可以在不修改上层代码的情况下，通过切换服务实现单机或联机启动。                      | Core.Networking              |
