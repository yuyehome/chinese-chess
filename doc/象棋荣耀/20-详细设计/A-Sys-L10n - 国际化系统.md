
### **模块 1: A-Sys-L10n - 国际化系统**

#### **1.1. 模块目标 (Goal)**

提供一个中心化的服务，使得游戏内所有面向玩家的文本和资源（图片、模型等）都能根据当前选择的语言，自动切换为对应版本。

#### **1.2. 核心类/结构设计 (Code Design)**

1. **LocalizationManager (核心单例服务)**
    
    - **职责**:
        
        - 管理当前语言 (CurrentLanguage)。
            
        - 加载和缓存所有语言的本地化数据。
            
        - 提供获取本地化文本和资源的接口。
            
        - 提供语言切换功能，并广播语言切换事件。
            
    - **关键属性/方法**:
        
        - public static LocalizationManager Instance { get; }：单例访问点。
            
        - public Language CurrentLanguage { get; private set; }：只读的当前语言。
            
        - public event Action OnLanguageChanged;：语言切换时触发的事件。
            
        - public void SetLanguage(Language language)：切换语言的方法。内部会加载新语言数据并触发OnLanguageChanged事件。
            
        - public string GetText(string key)：**核心方法**。根据一个唯一的key（如 "main_menu_start_button"）获取当前语言的文本。
            
        - public T GetAsset<T>(string key) where T : UnityEngine.Object：**核心方法**。根据一个key（如 "piece_chariot_texture"）获取当前语言对应的资源（Texture, Sprite, Material, Prefab等）。
            
2. **Language (枚举)**
    
    - **职责**: 定义所有支持的语言。
        
    - **示例**: public enum Language { ZH_CN, EN_US, ... }
        
3. **数据存储结构 (使用 ScriptableObject 进行配置)**
    
    - LanguageTextData (ScriptableObject)
        
        - **职责**: 存储**一种语言**的所有文本。
            
        - **字段**: public Language language;, public List<TextEntry> texts;
            
        - TextEntry (Serializable Class): public string key;, public string value;
            
    - LanguageAssetData (ScriptableObject)
        
        - **职责**: 存储**一种语言**的所有本地化资源映射。
            
        - **字段**: public Language language;, public List<AssetEntry> assets;
            
        - AssetEntry (Serializable Class): public string key;, public UnityEngine.Object asset;
            
4. **LocalizedText (组件)**
    
    - **职责**: 挂载在任何UnityEngine.UI.Text或TextMeshProUGUI组件上，使其文本内容自动本地化。
        
    - **字段**: public string localizationKey;
        
    - **逻辑**: 在Start()时获取文本，并订阅LocalizationManager.OnLanguageChanged事件，在语言切换时自动刷新文本。
        
5. **LocalizedImage (组件)**
    
    - **职责**: 类似LocalizedText，用于UnityEngine.UI.Image，根据key自动切换Sprite。
        
    - **字段**: public string localizationKey;
        

#### **1.3. Unity操作概要 (Unity Workflow)**

1. **创建数据**:
    
    - 在Project窗口右键 -> Create -> ChessHonor -> Localization -> Text Data。为每种语言（如ZH_CN, EN_US）创建一个LanguageTextData实例。
        
    - 在这些ScriptableObject实例中，像填写表格一样填入key-value对。例如，在ZH_CN数据中，key="confirm", value="确认"；在EN_US数据中，key="confirm", value="Confirm"。
        
    - 对需要本地化的资源（如棋子贴图），创建LanguageAssetData实例并配置。
        
2. **配置管理器**:
    
    - 创建一个名为LocalizationManager的GameObject，挂上LocalizationManager.cs脚本。
        
    - 在Inspector中，将所有创建的LanguageTextData和LanguageAssetData文件拖拽到LocalizationManager的对应列表中。
        
3. **使用**:
    
    - 对于UI文本，在Text组件旁边添加LocalizedText组件，并填入对应的key。
        
    - 对于代码中需要动态生成的文本（如 "玩家 {0} 获胜！"），通过调用 string.Format(LocalizationManager.Instance.GetText("player_win_format"), playerName) 来实现。
        
    - 对于棋子视图PieceView，在需要设置棋子材质时，调用 var material = LocalizationManager.Instance.GetAsset<Material>("piece_rook_material_" + pieceColor);。
        

---
