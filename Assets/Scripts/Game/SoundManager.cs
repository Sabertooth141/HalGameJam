using System;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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