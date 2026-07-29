using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
    }

    [SerializeField] private Sound[] sounds;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Tooltip("同じSEの最小再生間隔（秒）")]
    [SerializeField] private float sfxCooldown = 0.05f;

    private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this) return;

        Instance = this;
    }

    private void Start()
    {
        PlayMusic(musicSource.clip);
    }

    public void Play(string inName)
    {
        Sound sound = Array.Find(sounds, x => x.name == inName);
        sfxSource.pitch = 1f;

        if (sound == null)
        {
            Debug.LogWarning($"Sound not found {inName}");
            return;
        }

        if (lastPlayed.TryGetValue(inName, out float last) && Time.unscaledTime - last < sfxCooldown)
        {
            return;
        }

        lastPlayed[inName] = Time.unscaledTime;

        sfxSource.pitch = sound.pitch;
        sfxSource.PlayOneShot(sound.clip, sound.volume);
    }

    public void PlayMusic(AudioClip clip, bool restartIfSame = false)
    {
        if (musicSource.clip == clip && musicSource.isPlaying && !restartIfSame)
        {
            return;
        }
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
}