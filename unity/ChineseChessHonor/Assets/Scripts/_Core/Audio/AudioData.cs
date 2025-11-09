using System;
using System.Collections.Generic;
using UnityEngine;

// 这个类用于在Inspector中方便地组织数据
[Serializable]
public class AudioEntry
{
    public string key;
    public AudioClip clip;
}

// 使用CreateAssetMenu特性，让我们可以直接在Project窗口创建这个配置的实例
[CreateAssetMenu(fileName = "AudioData", menuName = "ChessHonor/Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    public List<AudioEntry> bgmClips;
    public List<AudioEntry> sfxClips;
}