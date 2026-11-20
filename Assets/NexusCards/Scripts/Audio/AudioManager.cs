using System.Collections;
using UnityEngine;

namespace Nexus
{
    public enum Sfx { Draw, Play, Attack, Hit, Block, Gate, Combo, Turret, Click, Death, Victory, Defeat }
    public enum MusicTrack { None, Menu, Battle }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioClip menuMusic;
        public AudioClip battleMusic;
        [Tooltip("Um clip por item do enum Sfx, na mesma ordem.")]
        public AudioClip[] effects;
        [Range(0f, 1f)] public float musicVolume = 0.42f;
        [Range(0f, 1f)] public float effectsVolume = 0.9f;

        AudioSource music;
        AudioSource[] pool;
        int nextVoice;
        MusicTrack current = MusicTrack.None;
        bool muted;
        Coroutine fade;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = 0f;
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].spatialBlend = 0f;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Fire(Sfx s, float volume = 1f)
        {
            if (Instance != null) Instance.Play(s, volume);
        }

        public static void Music(MusicTrack t)
        {
            if (Instance != null) Instance.PlayMusic(t);
        }

        public void Play(Sfx s, float volume = 1f)
        {
            int i = (int)s;
            if (muted || effects == null || i >= effects.Length || effects[i] == null) return;
            var src = pool[nextVoice];
            nextVoice = (nextVoice + 1) % pool.Length;
            src.pitch = Random.Range(0.95f, 1.05f);
            src.PlayOneShot(effects[i], effectsVolume * volume);
        }

        public void PlayMusic(MusicTrack t)
        {
            if (t == current) return;
            current = t;
            var clip = t == MusicTrack.Menu ? menuMusic : t == MusicTrack.Battle ? battleMusic : null;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Crossfade(clip));
        }

        public void DuckMusic(float target, float seconds)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeTo(target, seconds));
        }

        public void ToggleMute()
        {
            muted = !muted;
            music.mute = muted;
        }

        IEnumerator Crossfade(AudioClip clip)
        {
            yield return FadeTo(0f, 0.5f);
            music.Stop();
            music.clip = clip;
            if (clip == null) yield break;
            music.Play();
            yield return FadeTo(musicVolume, 1.2f);
        }

        IEnumerator FadeTo(float target, float seconds)
        {
            float start = music.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                music.volume = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            music.volume = target;
        }
    }
}
