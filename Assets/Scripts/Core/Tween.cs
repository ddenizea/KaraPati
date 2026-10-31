using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KaraPati
{
    /// <summary>Minimal coroutine tweening. Tweens sharing a key replace each other.</summary>
    public class Tween : MonoBehaviour
    {
        static Tween inst;
        static readonly Dictionary<string, Coroutine> running = new Dictionary<string, Coroutine>();

        static Tween I
        {
            get
            {
                if (!inst)
                {
                    running.Clear();
                    var go = new GameObject("Tweens");
                    DontDestroyOnLoad(go);
                    inst = go.AddComponent<Tween>();
                }
                return inst;
            }
        }

        public static void Run(float dur, Action<float> step, Action done = null, string key = null)
        {
            var t = I;
            if (key != null) Stop(key);
            var co = t.StartCoroutine(Co(dur, step, done, key));
            if (key != null) running[key] = co;
        }

        public static void Stop(string key)
        {
            if (key == null || !inst) return;
            if (running.TryGetValue(key, out var co))
            {
                if (co != null) inst.StopCoroutine(co);
                running.Remove(key);
            }
        }

        static IEnumerator Co(float dur, Action<float> step, Action done, string key)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / dur));
                yield return null;
            }
            step(1f);
            if (key != null) running.Remove(key);
            done?.Invoke();
        }

        public static string Key(UnityEngine.Object o, string ch) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o) + ch;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static void Pop(Transform tr, float dur = 0.3f)
        {
            if (!tr) return;
            Run(dur, t => { if (tr) tr.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, OutBack(t)); }, null, Key(tr, "s"));
        }

        public static void Punch(Transform tr, float amount = 0.18f, float dur = 0.3f)
        {
            if (!tr) return;
            Run(dur, t => { if (tr) tr.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * amount); }, null, Key(tr, "s"));
        }

        public static void Move(RectTransform rt, Vector2 to, float dur, Action done = null)
        {
            if (!rt) return;
            var from = rt.anchoredPosition;
            Run(dur, t => { if (rt) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, OutCubic(t)); }, done, Key(rt, "m"));
        }

        public static void Spin(Transform tr, float dur)
        {
            if (!tr) return;
            Run(dur, t => { if (tr) tr.localEulerAngles = new Vector3(0, 0, 360f * OutCubic(t)); },
                () => { if (tr) tr.localEulerAngles = Vector3.zero; }, Key(tr, "r"));
        }
    }
}
