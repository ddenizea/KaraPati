using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// Plays sound effects and the looping music. Clips live in Assets/Resources/Audio (made by Tools/make_sounds.py).
    /// Music and effects (merges, coins, clicks...) are switched on and off separately. Saved in PlayerPrefs.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const string PrefKey = "karapati_sound_mode";   // old single setting (0 all on, 1 music off, 2 silent)
        const string MusicKey = "karapati_music_on", EffectsKey = "karapati_effects_on";
        const int Voices = 8;

        static Sfx inst;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        AudioSource music;
        AudioSource[] voices;
        int next;

        public static bool EffectsOn { get; private set; } = true;
        public static bool MusicOn { get; private set; } = true;
        /// <summary>For the speaker icon: 0 = all on, 1 = one of them off, 2 = all off.</summary>
        public static int Mode => MusicOn && EffectsOn ? 0 : !MusicOn && !EffectsOn ? 2 : 1;

        // per-sound loudness so nothing sticks out
        static readonly Dictionary<string, float> volume = new Dictionary<string, float>
        {
            { "click", 0.35f }, { "pop", 0.6f }, { "spawn", 0.5f }, { "coin", 0.6f }, { "stamp", 0.7f },
            { "brew", 0.55f }, { "ready", 0.6f }, { "gift", 0.7f }, { "levelup", 0.75f }, { "paw", 0.55f },
            { "bubble", 0.55f }, { "error", 0.5f }, { "whoosh", 0.4f }, { "page", 0.45f }, { "hiss", 0.5f }, { "scurry", 0.45f },
        };

        public static void Init()
        {
            if (inst) return;
            var go = new GameObject("Sfx");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Sfx>();
            inst.Setup();
        }

        void Setup()
        {
            // first start after the update: carry over the old single setting
            int old = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, 2);
            MusicOn = PlayerPrefs.GetInt(MusicKey, old == 0 ? 1 : 0) == 1;
            EffectsOn = PlayerPrefs.GetInt(EffectsKey, old < 2 ? 1 : 0) == 1;
            foreach (var c in Resources.LoadAll<AudioClip>("Audio")) clips[c.name] = c;
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.35f;
            if (clips.TryGetValue("music", out var m)) music.clip = m;
            ApplyMusic();
        }

        /// <summary>Play a sound once. pitch lets one clip cover several situations (e.g. higher merges sound brighter).</summary>
        public static void Play(string name, float pitch = 1f, float vol = 1f)
        {
            if (!inst || !EffectsOn || !inst.clips.TryGetValue(name, out var clip)) return;
            // the same sound twice in the same instant just sounds louder, so skip it
            float now = Time.unscaledTime;
            if (inst.lastPlayed.TryGetValue(name, out var last) && now - last < 0.04f) return;
            inst.lastPlayed[name] = now;

            var src = inst.voices[inst.next];
            inst.next = (inst.next + 1) % Voices;
            src.pitch = pitch * Random.Range(0.97f, 1.03f);   // tiny random pitch so repeats don't feel robotic
            src.volume = vol * (volume.TryGetValue(name, out var v) ? v : 0.6f);
            src.PlayOneShot(clip);
        }

        public static void SetMusic(bool on)
        {
            MusicOn = on;
            PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
            PlayerPrefs.Save();
            if (inst) inst.ApplyMusic();
            Play("click");
        }

        public static void SetEffects(bool on)
        {
            EffectsOn = on;
            PlayerPrefs.SetInt(EffectsKey, on ? 1 : 0);
            PlayerPrefs.Save();
            Play("click");   // heard only when they were just switched on
        }

        void ApplyMusic()
        {
            if (!music || !music.clip) return;
            if (MusicOn && !music.isPlaying) music.Play();
            else if (!MusicOn && music.isPlaying) music.Stop();
        }
    }
}
