using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>A home-screen scene for chapters 3+. Each story task toggles one piece of it.</summary>
    public class ChapterScene
    {
        public RectTransform root;
        public Image dim;
        public Color dimColor = new Color(0.03f, 0.02f, 0.08f, 1f);
        public float dimMax = 0.5f, dimMin = 0.08f;
        public readonly Dictionary<string, Action<bool>> apply = new Dictionary<string, Action<bool>>();
        public readonly Dictionary<string, Transform> targets = new Dictionary<string, Transform>();
        public readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        public Action<float> tick;

        public void Apply(int done, int chapterIndex)
        {
            foreach (var kv in apply) kv.Value(Story.IsDone(kv.Key, done));
            var ch = Story.Chapters[chapterIndex];
            int start = Story.ChapterStart(chapterIndex);
            float prog = Mathf.Clamp01((done - start) / (float)ch.tasks.Length);
            dim.color = new Color(dimColor.r, dimColor.g, dimColor.b, Mathf.Lerp(dimMax, dimMin, prog));
        }
    }

    /// <summary>Builds the chapter 3-6 scenes from code (garden, ghost town, tower, mirror realm).</summary>
    public static class ChapterScenes
    {
        static readonly Vector2 SceneSize = new Vector2(1020f, 940f);

        static Image P(string name, Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, bool sliced = false, float rot = 0f)
        {
            var img = UIKit.ImgArt(name, parent, s, c, sliced);
            img.rectTransform.At(pos, size);
            if (rot != 0f) img.rectTransform.localEulerAngles = new Vector3(0f, 0f, rot);
            return img;
        }

        static RectTransform Group(string name, Transform parent) => UIKit.Rect(name, parent).At(Vector2.zero, SceneSize);

        static void Stars(Transform parent, int count, float yMin, Color c)
        {
            for (int i = 0; i < count; i++)
            {
                float x = -470f + ((i * 137) % 940);
                float y = yMin + ((i * 89) % (int)(450f - yMin));
                float sz = 14f + (i * 7) % 16;
                P("Star", parent, ProcArt.Sparkle, new Color(c.r, c.g, c.b, 0.35f + (i % 3) * 0.2f), new Vector2(x, y), new Vector2(sz, sz));
            }
        }

        public static ChapterScene Build(int chapter, RectTransform root, Action<Image> catHook)
        {
            var cs = new ChapterScene { root = root };
            switch (chapter)
            {
                case 2: Garden(cs, catHook); break;
                case 3: Town(cs, catHook); break;
                case 4: Tower(cs, catHook); break;
                default: Realm(cs, catHook); break;
            }
            cs.dim = P("Dim", root, ProcArt.Rounded, cs.dimColor, Vector2.zero, SceneSize, true);
            return cs;
        }

        static void Cat(ChapterScene cs, Action<Image> hook, Vector2 pos)
        {
            var cat = P("KaraPati", cs.root, ProcArt.SideCat(0, 4), Color.white, pos, new Vector2(150f, 150f));
            hook(cat);
        }

        // ------------------------------------------------------------------ chapter 3: moonlit garden
        static void Garden(ChapterScene cs, Action<Image> hook)
        {
            var r = cs.root;
            var wood = new Color(0.32f, 0.22f, 0.16f);
            P("Sky", r, ProcArt.Rounded, new Color(0.09f, 0.11f, 0.25f), Vector2.zero, SceneSize, true);
            Stars(r, 14, 120f, new Color(1f, 1f, 0.85f));
            var moonGlow = P("MoonGlow", r, ProcArt.GlowSprite, new Color(1f, 0.95f, 0.7f, 0.3f), new Vector2(300f, 300f), new Vector2(420f, 420f));
            P("Moon", r, ProcArt.FullMoon, Color.white, new Vector2(300f, 300f), new Vector2(260f, 260f));
            for (int i = 0; i < 7; i++)
                P("Hedge", r, ProcArt.Bush, new Color(0.11f, 0.22f, 0.16f), new Vector2(-480f + i * 160f, 30f), new Vector2(250f, 170f));
            var eyes = Group("Eyes", r);
            P("EyeL", eyes, ProcArt.Circle, new Color(1f, 0.85f, 0.2f), new Vector2(-110f, 50f), new Vector2(18f, 12f));
            P("EyeR", eyes, ProcArt.Circle, new Color(1f, 0.85f, 0.2f), new Vector2(-75f, 50f), new Vector2(18f, 12f));
            P("Ground", r, ProcArt.Rounded, new Color(0.13f, 0.2f, 0.15f), new Vector2(0f, -250f), new Vector2(1020f, 440f), true);
            P("Path", r, ProcArt.Circle, new Color(0.24f, 0.24f, 0.2f, 0.7f), new Vector2(0f, -430f), new Vector2(380f, 110f));

            // gate
            P("PostL", r, null, wood, new Vector2(-470f, -40f), new Vector2(24f, 220f));
            P("PostR", r, null, wood, new Vector2(-330f, -40f), new Vector2(24f, 220f));
            var gate = UIKit.Rect("Gate", r).At(new Vector2(-457f, -40f), new Vector2(120f, 190f));
            gate.pivot = new Vector2(0f, 0.5f);
            gate.anchoredPosition = new Vector2(-457f, -40f);
            for (int i = 0; i < 5; i++) P("Bar", gate, null, new Color(0.22f, 0.18f, 0.16f), new Vector2(-50f + i * 25f, 0f), new Vector2(8f, 180f));
            P("Rail", gate, null, new Color(0.22f, 0.18f, 0.16f), new Vector2(0f, 50f), new Vector2(120f, 10f));
            P("Rail", gate, null, new Color(0.22f, 0.18f, 0.16f), new Vector2(0f, -50f), new Vector2(120f, 10f));

            // weeds
            var weeds = Group("Weeds", r);
            var wp = new[] { new Vector2(-260f, -260f), new Vector2(-120f, -420f), new Vector2(150f, -240f), new Vector2(250f, -420f), new Vector2(-380f, -330f), new Vector2(420f, -300f), new Vector2(60f, -420f) };
            foreach (var w in wp) P("Weed", weeds, ProcArt.Classic(Chains.Ot, 2), new Color(0.35f, 0.5f, 0.3f), w, new Vector2(110f, 110f));

            // fountain + water
            var fountain = P("Fountain", r, ProcArt.Fountain, Color.white, new Vector2(0f, -150f), new Vector2(260f, 260f));
            var water = Group("Water", r);
            P("WaterGlow", water, ProcArt.GlowSprite, new Color(0.5f, 0.8f, 1f, 0.4f), new Vector2(0f, -150f), new Vector2(320f, 320f));
            P("Pool", water, ProcArt.Circle, new Color(0.55f, 0.82f, 1f, 0.7f), new Vector2(0f, -212f), new Vector2(190f, 36f));
            P("Jet", water, ProcArt.Circle, new Color(0.7f, 0.9f, 1f, 0.55f), new Vector2(0f, -60f), new Vector2(28f, 130f));

            // flower bed
            P("Bed", r, ProcArt.Rounded, new Color(0.3f, 0.2f, 0.14f), new Vector2(340f, -370f), new Vector2(300f, 80f), true);
            var flowers = Group("Flowers", r);
            for (int i = 0; i < 4; i++) P("MoonFlower", flowers, ProcArt.Classic(Chains.Ot, 5), Color.white, new Vector2(240f + i * 68f, -330f + (i % 2) * 20f), new Vector2(100f, 100f));

            // scarecrow
            var crow = P("Scarecrow", r, ProcArt.Scarecrow, Color.white, new Vector2(-210f, -130f), new Vector2(260f, 260f));
            var hat = P("Hat", r, ProcArt.WitchItem("w_sapka"), Color.white, new Vector2(-210f, 20f), new Vector2(120f, 120f));

            // pumpkin patch
            var vines = Group("Vines", r);
            for (int i = 0; i < 4; i++) P("Vine", vines, null, new Color(0.35f, 0.3f, 0.15f), new Vector2(-90f + i * 70f, -410f), new Vector2(60f, 6f), false, i * 25f - 30f);
            var pumpkins = Group("Pumpkins", r);
            for (int i = 0; i < 4; i++) P("Pumpkin", pumpkins, ProcArt.Pumpkin, Color.white, new Vector2(-110f + i * 75f, -405f + (i % 2) * 18f), new Vector2(90f, 90f));

            // lantern string with fireflies
            P("String", r, null, new Color(0.2f, 0.15f, 0.1f), new Vector2(-150f, 230f), new Vector2(620f, 4f));
            var lanterns = new List<Image>();
            var glows = new List<Image>();
            foreach (var x in new[] { -400f, -230f, -60f, 110f })
            {
                glows.Add(P("Glow", r, ProcArt.GlowSprite, new Color(0.9f, 1f, 0.5f, 0.45f), new Vector2(x, 190f), new Vector2(240f, 240f)));
                lanterns.Add(P("Lantern", r, ProcArt.Lantern(false), Color.white, new Vector2(x, 190f), new Vector2(70f, 80f)));
            }
            var flies = Group("Fireflies", r);
            var flyList = new List<RectTransform>();
            for (int i = 0; i < 10; i++)
                flyList.Add(P("Fly", flies, ProcArt.Circle, new Color(0.85f, 1f, 0.5f, 0.9f), new Vector2(-420f + i * 95f, 80f + (i % 3) * 70f), new Vector2(12f, 12f)).rectTransform);

            // moon dial
            P("Pedestal", r, ProcArt.Rounded, new Color(0.5f, 0.5f, 0.58f), new Vector2(370f, -150f), new Vector2(100f, 170f), true);
            P("Top", r, ProcArt.Rounded, new Color(0.58f, 0.58f, 0.66f), new Vector2(370f, -62f), new Vector2(170f, 30f), true);
            var dialGlow = P("DialGlow", r, ProcArt.GlowSprite, new Color(1f, 0.9f, 0.5f, 0.6f), new Vector2(370f, -35f), new Vector2(260f, 200f));
            var dial = P("Dial", r, ProcArt.MagicCircle, new Color(0.7f, 0.7f, 0.75f, 0.4f), new Vector2(370f, -38f), new Vector2(150f, 56f));

            Cat(cs, hook, new Vector2(-430f, -360f));

            cs.apply["b_kapi"] = on => gate.localScale = new Vector3(on ? 0.22f : 1f, 1f, 1f);
            cs.apply["b_ot"] = on => weeds.gameObject.SetActive(!on);
            cs.apply["b_cesme"] = on => water.gameObject.SetActive(on);
            cs.apply["b_cicek"] = on => flowers.gameObject.SetActive(on);
            cs.apply["b_korkuluk"] = on => hat.gameObject.SetActive(on);
            cs.apply["b_bal"] = on => { pumpkins.gameObject.SetActive(on); vines.gameObject.SetActive(!on); };
            cs.apply["b_fener"] = on =>
            {
                foreach (var l in lanterns) l.sprite = ProcArt.Lantern(on);
                foreach (var g in glows) g.gameObject.SetActive(on);
                flies.gameObject.SetActive(on);
                eyes.gameObject.SetActive(on);
            };
            cs.apply["b_kadran"] = on =>
            {
                dial.color = on ? new Color(1f, 0.85f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.75f, 0.4f);
                dialGlow.gameObject.SetActive(on);
                moonGlow.rectTransform.sizeDelta = on ? new Vector2(620f, 620f) : new Vector2(420f, 420f);
            };

            cs.targets["b_kapi"] = gate; cs.targets["b_ot"] = fountain.transform; cs.targets["b_cesme"] = fountain.transform;
            cs.targets["b_cicek"] = flowers.GetChild(1); cs.targets["b_korkuluk"] = hat.transform; cs.targets["b_bal"] = pumpkins.GetChild(1);
            cs.targets["b_fener"] = lanterns[1].transform; cs.targets["b_kadran"] = dial.transform;

            cs.icons["b_kapi"] = ProcArt.Lock; cs.icons["b_ot"] = ProcArt.Classic(Chains.Ot, 2); cs.icons["b_cesme"] = ProcArt.Fountain;
            cs.icons["b_cicek"] = ProcArt.Classic(Chains.Ot, 5); cs.icons["b_korkuluk"] = ProcArt.Scarecrow; cs.icons["b_bal"] = ProcArt.Pumpkin;
            cs.icons["b_fener"] = ProcArt.Lantern(true); cs.icons["b_kadran"] = ProcArt.FullMoon;

            cs.tick = t =>
            {
                for (int i = 0; i < flyList.Count; i++)
                    flyList[i].anchoredPosition = new Vector2(-420f + i * 95f + Mathf.Sin(t * 0.8f + i) * 30f, 80f + (i % 3) * 70f + Mathf.Sin(t * 1.3f + i * 2f) * 20f);
                float blink = Mathf.Repeat(t, 4f) < 0.15f ? 0.1f : 1f;
                eyes.localScale = new Vector3(1f, blink, 1f);
            };
        }

        // ------------------------------------------------------------------ chapter 4: ghost town
        static void Town(ChapterScene cs, Action<Image> hook)
        {
            var r = cs.root;
            cs.dimColor = new Color(0.06f, 0.03f, 0.1f, 1f);
            var wood = new Color(0.42f, 0.28f, 0.2f);
            P("Sky", r, ProcArt.Rounded, new Color(0.17f, 0.13f, 0.27f), Vector2.zero, SceneSize, true);
            Stars(r, 10, 200f, new Color(0.9f, 0.9f, 1f));
            P("Moon", r, ProcArt.Moon, Color.white, new Vector2(-390f, 370f), new Vector2(120f, 120f));

            var windows = new List<Image>();
            foreach (var x in new[] { -410f, -230f, 230f, 410f })
            {
                P("House", r, ProcArt.House, new Color(0.3f, 0.24f, 0.4f), new Vector2(x, 140f), new Vector2(200f, 200f));
                windows.Add(P("Win", r, null, new Color(0.14f, 0.11f, 0.2f), new Vector2(x - 34f, 110f), new Vector2(30f, 34f)));
                windows.Add(P("Win", r, null, new Color(0.14f, 0.11f, 0.2f), new Vector2(x + 34f, 110f), new Vector2(30f, 34f)));
            }

            // clock tower
            P("Tower", r, ProcArt.Rounded, new Color(0.3f, 0.25f, 0.38f), new Vector2(0f, 150f), new Vector2(180f, 420f), true);
            P("Roof", r, ProcArt.Pennant, new Color(0.38f, 0.2f, 0.42f), new Vector2(0f, 405f), new Vector2(230f, 100f), false, 180f);
            P("Belfry", r, ProcArt.Rounded, new Color(0.12f, 0.09f, 0.16f), new Vector2(0f, 315f), new Vector2(110f, 80f), true);
            var bellGlow = P("BellGlow", r, ProcArt.GlowSprite, new Color(1f, 0.85f, 0.4f, 0.6f), new Vector2(0f, 315f), new Vector2(220f, 220f));
            var bell = P("Bell", r, ProcArt.Bell, new Color(0.5f, 0.5f, 0.56f), new Vector2(0f, 315f), new Vector2(64f, 64f));
            var clockGlow = P("ClockGlow", r, ProcArt.GlowSprite, new Color(1f, 0.95f, 0.7f, 0.5f), new Vector2(0f, 200f), new Vector2(260f, 260f));
            var face = P("Clock", r, ProcArt.ClockFace, Color.white, new Vector2(0f, 200f), new Vector2(140f, 140f));
            var hour = P("Hour", r, null, new Color(0.25f, 0.18f, 0.15f), new Vector2(0f, 200f), new Vector2(9f, 36f));
            hour.rectTransform.pivot = new Vector2(0.5f, 0f);
            hour.rectTransform.anchoredPosition = new Vector2(0f, 200f);
            var minute = P("Minute", r, null, new Color(0.25f, 0.18f, 0.15f), new Vector2(0f, 200f), new Vector2(6f, 52f));
            minute.rectTransform.pivot = new Vector2(0.5f, 0f);
            minute.rectTransform.anchoredPosition = new Vector2(0f, 200f);

            // bunting
            P("Line", r, null, new Color(0.2f, 0.15f, 0.2f), new Vector2(0f, 50f), new Vector2(940f, 3f));
            var flags = Group("Flags", r);
            var fc = new[] { new Color(1f, 0.55f, 0.2f), new Color(0.6f, 0.4f, 0.9f), new Color(0.4f, 0.85f, 0.5f) };
            for (int i = 0; i < 9; i++) P("Flag", flags, ProcArt.Pennant, fc[i % 3], new Vector2(-420f + i * 105f, 25f), new Vector2(50f, 56f));

            P("Ground", r, ProcArt.Rounded, new Color(0.2f, 0.18f, 0.26f), new Vector2(0f, -290f), new Vector2(1020f, 360f), true);
            for (int i = 0; i < 4; i++) P("Cobble", r, null, new Color(0f, 0f, 0f, 0.15f), new Vector2(0f, -170f - i * 80f), new Vector2(1000f, 3f));

            // bridge
            P("Mist", r, ProcArt.Circle, new Color(0.8f, 0.85f, 1f, 0.25f), new Vector2(-330f, -320f), new Vector2(340f, 100f));
            P("Rail", r, null, new Color(0.3f, 0.2f, 0.14f), new Vector2(-337f, -222f), new Vector2(280f, 8f));
            P("Rail", r, null, new Color(0.3f, 0.2f, 0.14f), new Vector2(-337f, -318f), new Vector2(280f, 8f));
            var planks = new List<Image>();
            for (int i = 0; i < 6; i++) planks.Add(P("Plank", r, ProcArt.Rounded, wood, new Vector2(-450f + i * 45f, -270f), new Vector2(38f, 100f), true));

            // lamps
            var lamps = new List<Image>();
            var lampGlows = new List<Image>();
            foreach (var x in new[] { -160f, 160f, 440f })
            {
                P("Post", r, null, new Color(0.15f, 0.12f, 0.18f), new Vector2(x, -200f), new Vector2(14f, 170f));
                lampGlows.Add(P("Glow", r, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.5f), new Vector2(x, -105f), new Vector2(260f, 260f)));
                lamps.Add(P("Lamp", r, ProcArt.Lantern(false), Color.white, new Vector2(x, -105f), new Vector2(70f, 80f)));
            }

            // market stall
            var stall = Group("Stall", r);
            for (int i = 0; i < 5; i++) P("Awning", stall, null, i % 2 == 0 ? new Color(0.85f, 0.3f, 0.35f) : new Color(1f, 0.92f, 0.8f), new Vector2(250f + i * 40f, -190f), new Vector2(40f, 50f));
            P("Counter", stall, ProcArt.Rounded, wood, new Vector2(330f, -300f), new Vector2(220f, 80f), true);
            P("Goods", stall, ProcArt.Pumpkin, Color.white, new Vector2(280f, -240f), new Vector2(70f, 70f));
            P("Goods", stall, ProcArt.Bottle(2), Color.white, new Vector2(350f, -240f), new Vector2(60f, 60f));
            P("Goods", stall, ProcArt.Classic(Chains.Mantar, 2), Color.white, new Vector2(400f, -240f), new Vector2(60f, 60f));

            // fountain
            var fountain = P("Fountain", r, ProcArt.Fountain, Color.white, new Vector2(0f, -240f), new Vector2(230f, 230f));
            var water = Group("Water", r);
            P("Pool", water, ProcArt.Circle, new Color(0.55f, 0.82f, 1f, 0.7f), new Vector2(0f, -294f), new Vector2(170f, 32f));
            P("Jet", water, ProcArt.Circle, new Color(0.7f, 0.9f, 1f, 0.55f), new Vector2(0f, -160f), new Vector2(26f, 110f));

            // a few residents
            var ghosts = new List<RectTransform>();
            int gi = 0;
            foreach (var gp in new[] { new Vector2(-260f, -60f), new Vector2(260f, -40f), new Vector2(90f, -110f) })
                ghosts.Add(P("Resident", r, ProcArt.Ghost(gi++ * 3 % 8), new Color(1f, 1f, 1f, 0.6f), gp, new Vector2(80f, 80f)).rectTransform);

            Cat(cs, hook, new Vector2(-430f, -380f));

            cs.apply["k_kopru"] = on =>
            {
                for (int i = 0; i < planks.Count; i++)
                {
                    planks[i].gameObject.SetActive(on || i % 2 == 0);
                    planks[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, on ? 0f : (i * 17 % 30) - 15f);
                }
            };
            cs.apply["k_lamba"] = on =>
            {
                foreach (var l in lamps) l.sprite = ProcArt.Lantern(on);
                foreach (var g in lampGlows) g.gameObject.SetActive(on);
            };
            cs.apply["k_tezgah"] = on => stall.gameObject.SetActive(on);
            cs.apply["k_cesme"] = on => { fountain.color = on ? Color.white : new Color(0.5f, 0.5f, 0.45f); water.gameObject.SetActive(on); };
            cs.apply["k_ev"] = on => { foreach (var w in windows) w.color = on ? new Color(1f, 0.85f, 0.45f) : new Color(0.14f, 0.11f, 0.2f); };
            cs.apply["k_bayrak"] = on => flags.gameObject.SetActive(on);
            cs.apply["k_saat"] = on =>
            {
                face.color = on ? Color.white : new Color(0.55f, 0.55f, 0.55f);
                clockGlow.gameObject.SetActive(on);
                hour.rectTransform.localEulerAngles = new Vector3(0f, 0f, on ? 0f : 130f);
                minute.rectTransform.localEulerAngles = new Vector3(0f, 0f, on ? 0f : -100f);
            };
            cs.apply["k_can"] = on => { bell.color = on ? new Color(1f, 0.82f, 0.35f) : new Color(0.5f, 0.5f, 0.56f); bellGlow.gameObject.SetActive(on); };

            cs.targets["k_kopru"] = planks[2].transform; cs.targets["k_lamba"] = lamps[0].transform; cs.targets["k_tezgah"] = stall.GetChild(5);
            cs.targets["k_cesme"] = fountain.transform; cs.targets["k_ev"] = windows[2].transform; cs.targets["k_bayrak"] = flags.GetChild(4);
            cs.targets["k_saat"] = face.transform; cs.targets["k_can"] = bell.transform;

            cs.icons["k_kopru"] = ProcArt.Crate; cs.icons["k_lamba"] = ProcArt.Lantern(true); cs.icons["k_tezgah"] = ProcArt.Pumpkin;
            cs.icons["k_cesme"] = ProcArt.Fountain; cs.icons["k_ev"] = ProcArt.House; cs.icons["k_bayrak"] = ProcArt.Pennant;
            cs.icons["k_saat"] = ProcArt.ClockFace; cs.icons["k_can"] = ProcArt.Bell;

            cs.tick = t =>
            {
                for (int i = 0; i < ghosts.Count; i++)
                    ghosts[i].localPosition += new Vector3(Mathf.Sin(t * 0.6f + i) * 0.3f, Mathf.Sin(t * 1.8f + i * 2f) * 0.35f, 0f);
                if (bellGlow.gameObject.activeSelf) bell.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 4f) * 10f);
                if (clockGlow.gameObject.activeSelf) minute.rectTransform.localEulerAngles = new Vector3(0f, 0f, -t * 30f);
            };
        }

        // ------------------------------------------------------------------ chapter 5: the lost tower
        static void Tower(ChapterScene cs, Action<Image> hook)
        {
            var r = cs.root;
            cs.dimColor = new Color(0.02f, 0.02f, 0.08f, 1f);
            var wood = new Color(0.4f, 0.27f, 0.18f);
            P("Wall", r, ProcArt.Rounded, new Color(0.14f, 0.15f, 0.25f), Vector2.zero, SceneSize, true);
            for (int i = 0; i < 6; i++) P("Seam", r, null, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, 400f - i * 120f), new Vector2(1000f, 3f));

            // moon window
            P("WindowArch", r, ProcArt.Circle, new Color(0.07f, 0.09f, 0.22f), new Vector2(0f, 270f), new Vector2(300f, 300f));
            P("WindowBody", r, null, new Color(0.07f, 0.09f, 0.22f), new Vector2(0f, 180f), new Vector2(300f, 180f));
            P("WinMoon", r, ProcArt.FullMoon, Color.white, new Vector2(30f, 280f), new Vector2(180f, 180f));
            var shutters = Group("Shutters", r);
            P("ShutterL", shutters, ProcArt.Rounded, wood, new Vector2(-75f, 230f), new Vector2(150f, 320f), true);
            P("ShutterR", shutters, ProcArt.Rounded, new Color(0.36f, 0.24f, 0.16f), new Vector2(75f, 230f), new Vector2(150f, 320f), true);
            var winLight = P("WinLight", r, ProcArt.GlowSprite, new Color(0.8f, 0.85f, 1f, 0.35f), new Vector2(0f, 120f), new Vector2(520f, 520f));

            // stairs
            var steps = new List<Image>();
            for (int i = 0; i < 7; i++)
                steps.Add(P("Step", r, ProcArt.Rounded, new Color(0.3f, 0.28f, 0.4f), new Vector2(-430f + (i % 2) * 30f, -380f + i * 100f), new Vector2(150f, 30f), true));

            // star chart
            var chart = Group("Chart", r);
            P("Paper", chart, ProcArt.Rounded, new Color(0.9f, 0.85f, 0.7f), new Vector2(-250f, 300f), new Vector2(170f, 140f), true);
            var cpts = new[] { new Vector2(-300f, 330f), new Vector2(-260f, 345f), new Vector2(-215f, 320f), new Vector2(-225f, 270f), new Vector2(-280f, 260f) };
            foreach (var c in cpts) P("Star", chart, ProcArt.Sparkle, new Color(0.3f, 0.3f, 0.6f), c, new Vector2(24f, 24f));

            // bookshelf
            P("Shelf", r, ProcArt.Rounded, new Color(0.3f, 0.2f, 0.14f), new Vector2(370f, 150f), new Vector2(230f, 330f), true);
            for (int i = 0; i < 3; i++) P("Board", r, null, wood, new Vector2(370f, 60f + i * 95f), new Vector2(210f, 10f));
            var books = Group("Books", r);
            var bc = new[] { new Color(0.75f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 0.75f), new Color(0.4f, 0.65f, 0.4f), new Color(0.75f, 0.6f, 0.3f), new Color(0.55f, 0.35f, 0.7f) };
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 6; i++)
                    P("Book", books, null, bc[(i + row) % 5], new Vector2(285f + i * 32f, 100f + row * 95f), new Vector2(24f, 70f));
            var messy = Group("Messy", r);
            for (int i = 0; i < 6; i++) P("Book", messy, null, bc[i % 5], new Vector2(260f + i * 40f, -400f + (i % 2) * 20f), new Vector2(60f, 22f), false, i * 23f - 40f);

            // telescope, crystal ball, cauldron, portal
            var scope = P("Telescope", r, ProcArt.Telescope, Color.white, new Vector2(220f, -130f), new Vector2(260f, 260f));
            P("Pedestal", r, ProcArt.Rounded, new Color(0.35f, 0.3f, 0.42f), new Vector2(-200f, -250f), new Vector2(90f, 130f), true);
            var ballGlow = P("BallGlow", r, ProcArt.GlowSprite, new Color(0.6f, 0.8f, 1f, 0.6f), new Vector2(-200f, -150f), new Vector2(220f, 220f));
            var ball = P("Ball", r, ProcArt.WitchItem("w_kure"), Color.white, new Vector2(-200f, -150f), new Vector2(120f, 120f));
            var potGlow = P("PotGlow", r, ProcArt.GlowSprite, new Color(0.75f, 0.45f, 1f, 0.6f), new Vector2(40f, -260f), new Vector2(300f, 300f));
            var pot = P("Cauldron", r, ProcArt.Classic(Chains.Kazan, 1), Color.white, new Vector2(40f, -290f), new Vector2(200f, 200f));
            var portalGlow = P("PortalGlow", r, ProcArt.GlowSprite, new Color(0.8f, 0.4f, 1f, 0.6f), new Vector2(0f, -410f), new Vector2(620f, 220f));
            var portal = P("Portal", r, ProcArt.MagicCircle, new Color(0.6f, 0.6f, 0.65f, 0.3f), new Vector2(0f, -415f), new Vector2(460f, 120f));

            Cat(cs, hook, new Vector2(-330f, -390f));

            cs.apply["t_merdiven"] = on =>
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    steps[i].gameObject.SetActive(on || (i != 2 && i != 4));
                    steps[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, on ? 0f : (i * 11 % 16) - 8f);
                }
            };
            cs.apply["t_kitap"] = on => { books.gameObject.SetActive(on); messy.gameObject.SetActive(!on); };
            cs.apply["t_harita"] = on => chart.gameObject.SetActive(on);
            cs.apply["t_teleskop"] = on =>
            {
                scope.rectTransform.localEulerAngles = new Vector3(0f, 0f, on ? 25f : -35f);
                scope.color = on ? Color.white : new Color(0.55f, 0.55f, 0.55f);
            };
            cs.apply["t_kure"] = on => { ball.color = on ? Color.white : new Color(0.5f, 0.5f, 0.55f); ballGlow.gameObject.SetActive(on); };
            cs.apply["t_kazan"] = on => { pot.color = on ? new Color(0.9f, 0.7f, 1f) : new Color(0.5f, 0.5f, 0.56f); potGlow.gameObject.SetActive(on); };
            cs.apply["t_pencere"] = on => { shutters.gameObject.SetActive(!on); winLight.gameObject.SetActive(on); };
            cs.apply["t_gecit"] = on => { portal.color = on ? new Color(0.85f, 0.6f, 1f, 1f) : new Color(0.6f, 0.6f, 0.65f, 0.3f); portalGlow.gameObject.SetActive(on); };

            cs.targets["t_merdiven"] = steps[3].transform; cs.targets["t_kitap"] = books.GetChild(8); cs.targets["t_harita"] = chart.GetChild(0);
            cs.targets["t_teleskop"] = scope.transform; cs.targets["t_kure"] = ball.transform; cs.targets["t_kazan"] = pot.transform;
            cs.targets["t_pencere"] = r.Find("WinMoon"); cs.targets["t_gecit"] = portal.transform;

            cs.icons["t_merdiven"] = ProcArt.WitchItem("w_supurge"); cs.icons["t_kitap"] = ProcArt.WitchItem("w_kitap"); cs.icons["t_harita"] = ProcArt.Sparkle;
            cs.icons["t_teleskop"] = ProcArt.Telescope; cs.icons["t_kure"] = ProcArt.WitchItem("w_kure"); cs.icons["t_kazan"] = ProcArt.Classic(Chains.Kazan, 1);
            cs.icons["t_pencere"] = ProcArt.FullMoon; cs.icons["t_gecit"] = ProcArt.MagicCircle;

            cs.tick = t =>
            {
                if (portalGlow.gameObject.activeSelf)
                    portalGlow.color = new Color(0.8f, 0.4f, 1f, 0.45f + Mathf.Sin(t * 2.5f) * 0.15f);
            };
        }

        // ------------------------------------------------------------------ chapter 6: mirror realm (finale)
        static void Realm(ChapterScene cs, Action<Image> hook)
        {
            var r = cs.root;
            cs.dimColor = new Color(0.08f, 0.02f, 0.12f, 1f);
            cs.dimMax = 0.45f;
            cs.dimMin = 0f;
            P("Void", r, ProcArt.Rounded, new Color(0.2f, 0.09f, 0.3f), Vector2.zero, SceneSize, true);
            P("Haze", r, ProcArt.GlowSprite, new Color(1f, 0.5f, 0.85f, 0.3f), new Vector2(-250f, 200f), new Vector2(700f, 700f));
            P("Haze", r, ProcArt.GlowSprite, new Color(0.4f, 0.6f, 1f, 0.3f), new Vector2(300f, -150f), new Vector2(700f, 700f));
            Stars(r, 18, -200f, new Color(1f, 0.9f, 1f));

            // floating mirrors
            var mirrors = new List<Image>();
            var mirrorGlows = new List<Image>();
            var mpos = new[] { new Vector2(-270f, 320f), new Vector2(400f, 280f), new Vector2(-330f, -10f) };
            foreach (var mp in mpos)
            {
                mirrorGlows.Add(P("MirrorGlow", r, ProcArt.GlowSprite, new Color(0.7f, 0.6f, 1f, 0.5f), mp, new Vector2(240f, 280f)));
                mirrors.Add(P("Mirror", r, ProcArt.Mirror(false), Color.white, mp, new Vector2(120f, 170f), false, mp.x > 0 ? -10f : 10f));
            }

            // cage with the witch
            var exitGlow = P("ExitGlow", r, ProcArt.GlowSprite, new Color(1f, 0.85f, 0.5f, 0.7f), new Vector2(0f, 260f), new Vector2(520f, 520f));
            var exitRing = P("ExitRing", r, ProcArt.MagicCircle, new Color(1f, 0.85f, 0.4f), new Vector2(0f, 260f), new Vector2(320f, 320f));
            var witchGlow = P("WitchGlow", r, ProcArt.GlowSprite, new Color(1f, 0.8f, 1f, 0.5f), new Vector2(0f, 250f), new Vector2(280f, 280f));
            var witch = P("Witch", r, ProcArt.Witch, Color.white, new Vector2(0f, 240f), new Vector2(170f, 170f));
            var cage = P("Cage", r, ProcArt.Cage, Color.white, new Vector2(0f, 250f), new Vector2(240f, 260f));

            // crystal bridge
            var crystals = new List<Image>();
            for (int i = 0; i < 7; i++)
                crystals.Add(P("Crystal", r, ProcArt.Classic(Chains.Kristal, 3), Color.white, new Vector2(-420f + i * 85f, -110f), new Vector2(90f, 90f), false, 90f));

            // star lanterns
            var lamps = new List<Image>();
            var lampGlows = new List<Image>();
            foreach (var x in new[] { -220f, 0f, 220f })
            {
                P("Pole", r, null, new Color(0.5f, 0.4f, 0.7f), new Vector2(x, 20f), new Vector2(10f, 110f));
                lampGlows.Add(P("Glow", r, ProcArt.GlowSprite, new Color(1f, 0.6f, 0.9f, 0.55f), new Vector2(x, 90f), new Vector2(230f, 230f)));
                lamps.Add(P("Lamp", r, ProcArt.Lantern(false), new Color(1f, 0.85f, 1f), new Vector2(x, 90f), new Vector2(64f, 74f)));
            }

            // paw-print path towards the throne
            var paws = new List<Image>();
            for (int i = 0; i < 6; i++)
                paws.Add(P("Paw", r, ProcArt.PawPrint, new Color(0.6f, 0.9f, 1f, 0.15f), new Vector2(-330f + i * 85f, -330f + (i % 2) * 40f), new Vector2(54f, 54f), false, -80f));

            // throne + Gölge
            P("ThroneBack", r, ProcArt.Rounded, new Color(0.32f, 0.18f, 0.42f), new Vector2(340f, -170f), new Vector2(260f, 320f), true);
            P("ThroneSeat", r, ProcArt.Rounded, new Color(0.45f, 0.27f, 0.55f), new Vector2(340f, -300f), new Vector2(300f, 80f), true);
            var cushion = P("Cushion", r, ProcArt.Circle, new Color(0.95f, 0.55f, 0.65f), new Vector2(340f, -270f), new Vector2(250f, 70f));
            var golge = P("Golge", r, ProcArt.SideCat(0, 4), new Color(0.5f, 0.38f, 0.7f), new Vector2(340f, -170f), new Vector2(300f, 300f));
            golge.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            var yarn = P("Yarn", r, ProcArt.Classic(Chains.Ag, 3), Color.white, new Vector2(200f, -330f), new Vector2(90f, 90f));

            Cat(cs, hook, new Vector2(-430f, -390f));

            cs.apply["d_kopru"] = on => { for (int i = 0; i < crystals.Count; i++) crystals[i].gameObject.SetActive(on || i % 2 == 0); };
            cs.apply["d_fener"] = on =>
            {
                foreach (var l in lamps) l.sprite = ProcArt.Lantern(on);
                foreach (var g in lampGlows) g.gameObject.SetActive(on);
            };
            cs.apply["d_ayna"] = on =>
            {
                foreach (var m in mirrors) m.sprite = ProcArt.Mirror(on);
                foreach (var g in mirrorGlows) g.gameObject.SetActive(on);
            };
            cs.apply["d_yol"] = on => { foreach (var p in paws) p.color = new Color(0.6f, 0.9f, 1f, on ? 0.95f : 0.15f); };
            cs.apply["d_yastik"] = on => cushion.gameObject.SetActive(on);
            cs.apply["d_yumak"] = on =>
            {
                yarn.gameObject.SetActive(on);
                golge.color = on ? new Color(0.62f, 0.5f, 0.82f) : new Color(0.5f, 0.38f, 0.7f);
            };
            cs.apply["d_kafes"] = on => { cage.gameObject.SetActive(!on); witchGlow.gameObject.SetActive(on); };
            cs.apply["d_eve"] = on =>
            {
                exitGlow.gameObject.SetActive(on);
                exitRing.gameObject.SetActive(on);
                golge.sprite = ProcArt.SideCat(0, on ? 5 : 4);
            };

            cs.targets["d_kopru"] = crystals[3].transform; cs.targets["d_fener"] = lamps[1].transform; cs.targets["d_ayna"] = mirrors[0].transform;
            cs.targets["d_yol"] = paws[3].transform; cs.targets["d_yastik"] = cushion.transform; cs.targets["d_yumak"] = yarn.transform;
            cs.targets["d_kafes"] = witch.transform; cs.targets["d_eve"] = exitRing.transform;

            cs.icons["d_kopru"] = ProcArt.Classic(Chains.Kristal, 3); cs.icons["d_fener"] = ProcArt.Lantern(true); cs.icons["d_ayna"] = ProcArt.Mirror(true);
            cs.icons["d_yol"] = ProcArt.PawPrint; cs.icons["d_yastik"] = ProcArt.SideCat(0, 5); cs.icons["d_yumak"] = ProcArt.Classic(Chains.Ag, 3);
            cs.icons["d_kafes"] = ProcArt.Cage; cs.icons["d_eve"] = ProcArt.Witch;

            cs.tick = t =>
            {
                for (int i = 0; i < mirrors.Count; i++)
                {
                    var p = mpos[i] + new Vector2(0f, Mathf.Sin(t * 1.2f + i * 2f) * 10f);
                    mirrors[i].rectTransform.anchoredPosition = p;
                    mirrorGlows[i].rectTransform.anchoredPosition = p;
                }
                if (exitRing.gameObject.activeSelf) exitRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 20f);
                golge.rectTransform.localScale = new Vector3(-1f, 1f + Mathf.Sin(t * 1.5f) * 0.015f, 1f);
            };
        }
    }
}
