using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : PersistentSingleton<AudioManager>
{
    [Header("配置")]
    [SerializeField] private AudioData audioData;
    [SerializeField] private AudioMixer mainMixer;

    [Header("设置")]
    [SerializeField] private int sfxPoolSize = 15;

    // Mixer中暴露出的参数名称
    private const string BGM_VOLUME_PARAM = "BGMVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";

    private AudioSource _bgmSource;
    private List<AudioSource> _sfxPool;
    private int _sfxPoolIndex = 0;

    // 为了快速查找，将List转换为Dictionary
    private Dictionary<string, AudioClip> _bgmClips;
    private Dictionary<string, AudioClip> _sfxClips;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        // 1. 数据预处理：将List转为Dictionary，O(1)查找速度
        _bgmClips = audioData.bgmClips.ToDictionary(entry => entry.key, entry => entry.clip);
        _sfxClips = audioData.sfxClips.ToDictionary(entry => entry.key, entry => entry.clip);

        // 2. 创建BGM播放源
        GameObject bgmObject = new GameObject("BGM_Source");
        bgmObject.transform.SetParent(this.transform);
        _bgmSource = bgmObject.AddComponent<AudioSource>();
        _bgmSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("BGM")[0];
        _bgmSource.loop = true;

        // 3. 创建SFX对象池
        _sfxPool = new List<AudioSource>();
        GameObject sfxPoolObject = new GameObject("SFX_Pool");
        sfxPoolObject.transform.SetParent(this.transform);
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxObject = new GameObject($"SFX_Source_{i}");
            sfxObject.transform.SetParent(sfxPoolObject.transform);
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mainMixer.FindMatchingGroups("SFX")[0];
            _sfxPool.Add(source);
        }
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(string key)
    {
        if (_bgmClips.TryGetValue(key, out AudioClip clip))
        {
            if (_bgmSource.isPlaying && _bgmSource.clip == clip)
            {
                return; // 已经在播放同一首BGM
            }
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] BGM key not found: {key}");
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(string key)
    {
        if (_sfxClips.TryGetValue(key, out AudioClip clip))
        {
            // 从对象池中获取一个可用的AudioSource
            AudioSource source = GetAvailableSfxSource();
            // PlayOneShot允许在同一个AudioSource上播放重叠的音效，非常适合SFX
            source.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX key not found: {key}");
        }
    }

    // 简单的轮询方式获取播放源，比查找更高效
    private AudioSource GetAvailableSfxSource()
    {
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
        return _sfxPool[_sfxPoolIndex];
    }

    /// <summary>
    /// 设置BGM音量
    /// </summary>
    /// <param name="volume">音量值 (0.0 to 1.0)</param>
    public void SetBGMVolume(float volume)
    {
        // AudioMixer使用分贝（dB）单位，需要将线性值转换为对数值
        // 0.0001f 是为了避免log(0)导致无穷小
        mainMixer.SetFloat(BGM_VOLUME_PARAM, Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
    }

    /// <summary>
    /// 设置SFX音量
    /// </summary>
    /// <param name="volume">音量值 (0.0 to 1.0)</param>
    public void SetSFXVolume(float volume)
    {
        mainMixer.SetFloat(SFX_VOLUME_PARAM, Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20);
    }
}