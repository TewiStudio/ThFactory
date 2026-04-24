using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewSound", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    public string id;             // 声音ID，如 "Jump", "Explosion"
    public AudioClip[] clips;     // 支持多个素材随机播放（防重复感）
    public AudioMixerGroup outputGroup; // 输出到哪个Mixer组

    [Range(0, 1)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;

    public bool loop = false;
    public bool playOnAwake = false;

    [Range(0, 1)] public float spatialBlend = 0f; // 0是2D(UI), 1是3D

    // 获取随机Clip
    public AudioClip GetClip()
    {
        if (clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}