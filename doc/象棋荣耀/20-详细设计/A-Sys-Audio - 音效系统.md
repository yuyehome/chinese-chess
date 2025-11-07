
### **模块 2: A-Sys-Audio - 音效系统**

#### **2.1. 模块目标 (Goal)**

提供一个中心化的服务来管理和播放所有背景音乐(BGM)和音效(SFX)，支持音量控制、混音分组和简单的音效池化以提高性能。

#### **2.2. 核心类/结构设计 (Code Design)**

1. **AudioManager (核心单例服务)**
    
    - **职责**:
        
        - 管理BGM和SFX的播放、暂停、停止。
            
        - 提供全局音量控制。
            
        - 管理AudioSource池，避免频繁创建和销毁。
            
    - **关键属性/方法**:
        
        - public static AudioManager Instance { get; }：单例访问点。
            
        - public void PlayBGM(string key)：播放指定key的背景音乐（会先停止当前的BGM）。
            
        - public void PlaySFX(string key)：从对象池中获取一个AudioSource，播放指定key的音效。
            
        - public void SetBGMVolume(float volume)：设置BGM音量。
            
        - public void SetSFXVolume(float volume)：设置SFX音量。
            
2. **数据存储结构 (使用 ScriptableObject 进行配置)**
    
    - AudioData (ScriptableObject)
        
        - **职责**: 存储所有音频剪辑及其key的映射。
            
        - **字段**: public List<AudioEntry> bgmClips;, public List<AudioEntry> sfxClips;
            
        - AudioEntry (Serializable Class): public string key;, public AudioClip clip;
            
3. **Unity Mixer (混音器)**
    
    - **职责**: 利用Unity内置的Audio Mixer对BGM和SFX进行分组管理，便于统一控制音量和添加效果（如混响）。
        
    - **分组**: 创建"BGM"和"SFX"两个Group。AudioManager中的SetBGMVolume等方法，实际上是去设置Mixer中对应Group的暴露参数。
        

#### **2.3. Unity操作概要 (Unity Workflow)**

1. **创建Audio Mixer**:
    
    - 在Project窗口右键 -> Create -> Audio Mixer，命名为MainMixer。
        
    - 打开MainMixer，创建BGM和SFX两个子Group。
        
    - 在Inspector中，为BGM和SFX Group的Volume属性右键 -> Expose 'Volume' to script。将暴露出的参数重命名为"BGMVolume"和"SFXVolume"。
        
2. **创建数据**:
    
    - 右键 -> Create -> ChessHonor -> Audio -> Audio Data，创建一个AudioData实例。
        
    - 将所有的BGM和SFX音频文件拖入AudioData，并为每个文件分配一个唯一的key（如 "bgm_main_menu", "sfx_piece_move"）。
        
3. **配置管理器**:
    
    - 创建AudioManager GameObject，挂上AudioManager.cs脚本。
        
    - 在Inspector中，将MainMixer和AudioData文件拖拽到AudioManager的对应字段。
        
    - AudioManager内部会自动创建BGM播放源和SFX对象池。
        
4. **使用**:
    
    - **播放BGM**: 在主菜单场景的某个启动脚本中调用 AudioManager.Instance.PlayBGM("bgm_main_menu");。
        
    - **播放SFX**: 当棋子移动动画开始时，调用 AudioManager.Instance.PlaySFX("sfx_piece_move");。当发生碰撞时，调用 AudioManager.Instance.PlaySFX("sfx_piece_collision");。