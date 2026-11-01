using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// Plays sound effects and the looping music. Clips live in Assets/Resources/Audio (made by Tools/make_sounds.py).
    /// Mode: 0 = everything on, 1 = music off, 2 = all sound off. Saved in PlayerPrefs.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const string PrefKey = "karapati_sound_mode";
        const int Voices = 8;

        static Sfx inst;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        AudioSource music;
        AudioSource[] voices;
        int next;

        public static int Mode { get; private set; }
        public static bool EffectsOn => Mode < 2;
        public static bool MusicOn => Mode == 0;

        // per-sound loudness so nothing sticks out
        static readonly Dictionary<string, float> volume = new Dictionary<string, float>
        {
            { "click", 0.35f }, { "pop", 0.6f }, { "spawn", 0.5f }, { "coin", 0.6f }, { "stamp", 0.7f },
            { "brew", 0.55f }, { "ready", 0.6f }, { "gift", 0.7f }, { "levelup", 0.75f }, { "paw", 0.55f },
            { "bubble", 0.55f }, { "error", 0.5f }, { "whoosh", 0.4f }, { "page", 0.45f },
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
            Mode = Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, 2);
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

        /// <summary>Cycle on -> music off -> silent. Returns the new mode.</summary>
        public static int CycleMode()
        {
            Mode = (Mode + 1) % 3;
            PlayerPrefs.SetInt(PrefKey, Mode);
            PlayerPrefs.Save();
            if (inst) inst.ApplyMusic();
            Play("click");
            return Mode;
        }

        void ApplyMusic()
        {
            if (!music || !music.clip) return;
            if (MusicOn && !music.isPlaying) music.Play();
            else if (!MusicOn && music.isPlaying) music.Stop();
        }
    }
}
