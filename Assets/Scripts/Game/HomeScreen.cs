using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KaraPati
{
    /// <summary>Main screen: the witch's shop. Spend Pati Mührü on story tasks that restore it piece by piece.</summary>
    public class HomeScreen : MonoBehaviour
    {
        GameManager GM => GameManager.I;

        Text chapterTitle, progressText;
        RectTransform progressFill;
        const float ProgressW = 640f;

        RectTransform scene, ghostLayer;
        Image dim;
        GameObject webs, dirt, rug, bottles, counterStuff, bed;
        Image cauldron, cauldronGlow, lanternA, lanternB, glowA, glowB, plankTop, plankBottom, counterTop, counterBody, karaPati;
        RectTransform taskList;
        Image albumBadge;
        readonly List<RectTransform> floatingGhosts = new List<RectTransform>();
        int shownDone = -1;

        static readonly string[] catLines =
        {
            "Miyav. Mühürleri sen topla, dükkânı ben yönetirim.",
            "Cadı bir yerlerde... bıyıklarımda hissediyorum.",
            "Mırr. Kulağımın arkasını kaşı, sonra işe dön.",
            "Hayaletlerden korkma, çoğu sadece sohbet etmek istiyor.",
        };

        // ------------------------------------------------------------------ build
        public void Build(RectTransform root)
        {
            // header
            var header = UIKit.Section(root, "Header", 140f, 120f);
            chapterTitle = UIKit.Txt("Chapter", header, Story.ChapterName, 42, UIKit.Orange);
            chapterTitle.fontStyle = FontStyle.Bold;
            chapterTitle.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(0f, -4f), new Vector2(1000f, 56f));
            var barBg = UIKit.Img("Bar", header, ProcArt.Rounded, UIKit.PanelDark, true);
            barBg.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(-50f, -74f), new Vector2(ProgressW + 12f, 36f));
            var fill = UIKit.Img("Fill", barBg.transform, ProcArt.Rounded, UIKit.Orange, true);
            progressFill = fill.rectTransform;
            progressFill.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(6f, 0f), new Vector2(40f, 24f));
            progressText = UIKit.Txt("Count", header, "0/8", 32, UIKit.Cream);
            progressText.rectTransform.Anchor(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f)).At(new Vector2(345f, -74f), new Vector2(120f, 40f));

            BuildScene(UIKit.Section(root, "Shop", 272f, 940f));

            // tasks
            var tasks = UIKit.Section(root, "Tasks", 1228f, 515f);
            var panel = UIKit.Img("Panel", tasks, ProcArt.Rounded, UIKit.Panel, true);
            UIKit.Stretch(panel.rectTransform, 30f, 30f);
            var t = UIKit.Txt("Title", panel.transform, "Görevler", 36, UIKit.Orange, TextAnchor.MiddleLeft);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Anchor(new Vector2(0f, 1f), new Vector2(0f, 1f)).At(new Vector2(30f, -10f), new Vector2(400f, 56f));
            taskList = UIKit.Rect("List", panel.transform);
            UIKit.Stretch(taskList, 20f, 20f, 70f, 10f);

            // bottom buttons
            var bottom = UIKit.Section(root, "Bottom", 1765f, 170f);
            var album = UIKit.Btn("Album", bottom, "", UIKit.Panel, () => GM.OpenAlbum());
            ((RectTransform)album.transform).Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 0f), new Vector2(320f, 150f));
            var aIcon = UIKit.Img("Icon", album.transform, ProcArt.AlbumIcon, Color.white);
            aIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(18f, 0f), new Vector2(100f, 100f));
            var aLabel = album.Label();
            aLabel.text = "Albüm";
            aLabel.fontSize = 40;
            aLabel.rectTransform.offsetMin = new Vector2(110f, 4f);
            albumBadge = UIKit.Img("Badge", album.transform, ProcArt.Circle, new Color(0.9f, 0.3f, 0.3f));
            albumBadge.rectTransform.Anchor(new Vector2(1f, 1f), new Vector2(0.5f, 0.5f)).At(new Vector2(-14f, -14f), new Vector2(46f, 46f));
            var bt = UIKit.Txt("!", albumBadge.transform, "!", 32, Color.white);
            UIKit.Stretch(bt.rectTransform);

            var play = UIKit.Btn("Play", bottom, "Dükkânı Aç", UIKit.Green, () => GM.GoBoard(), 52);
            ((RectTransform)play.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-30f, 0f), new Vector2(660f, 150f));
            play.Label().fontStyle = FontStyle.Bold;
            play.Label().rectTransform.offsetMin = new Vector2(120f, 4f);
            var pIcon = UIKit.Img("Icon", play.transform, ProcArt.Item(Chains.Kazan, 1), Color.white);
            pIcon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(30f, 4f), new Vector2(110f, 110f));
        }

        Image Part(string name, Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var img = UIKit.Img(name, parent, s, c, sliced);
            img.rectTransform.At(pos, size);
            return img;
        }

        void BuildScene(RectTransform area)
        {
            scene = UIKit.Rect("Scene", area).At(Vector2.zero, new Vector2(1020f, 940f));
            var wood = new Color(0.45f, 0.3f, 0.2f);

            // wall + floor
            Part("Wall", scene, ProcArt.Rounded, new Color(0.3f, 0.21f, 0.36f), Vector2.zero, new Vector2(1020f, 940f), true);
            for (int i = 0; i < 9; i++)
                Part("Stripe", scene, null, new Color(1f, 0.95f, 0.85f, 0.05f), new Vector2(-440f + i * 110f, 120f), new Vector2(20f, 660f));
            Part("Floor", scene, ProcArt.Rounded, new Color(0.33f, 0.22f, 0.17f), new Vector2(0f, -330f), new Vector2(1020f, 280f), true);
            for (int i = 0; i < 3; i++)
                Part("Plank", scene, null, new Color(0f, 0f, 0f, 0.15f), new Vector2(0f, -250f - i * 75f), new Vector2(1000f, 4f));

            // window
            Part("WinFrame", scene, ProcArt.Rounded, new Color(0.35f, 0.22f, 0.15f), new Vector2(-290f, 190f), new Vector2(260f, 290f), true);
            Part("Glass", scene, ProcArt.Rounded, new Color(0.1f, 0.12f, 0.3f), new Vector2(-290f, 190f), new Vector2(214f, 244f), true);
            Part("Moon", scene, ProcArt.Moon, Color.white, new Vector2(-320f, 225f), new Vector2(110f, 110f));
            Part("Star", scene, ProcArt.Sparkle, new Color(1f, 1f, 0.85f), new Vector2(-230f, 270f), new Vector2(26f, 26f));
            Part("Star", scene, ProcArt.Sparkle, new Color(1f, 1f, 0.85f), new Vector2(-250f, 140f), new Vector2(18f, 18f));
            Part("Star", scene, ProcArt.Sparkle, new Color(1f, 1f, 0.85f), new Vector2(-350f, 120f), new Vector2(22f, 22f));
            dirt = UIKit.Rect("Dirt", scene).At(new Vector2(-290f, 190f), new Vector2(214f, 244f)).gameObject;
            Part("Grime", dirt.transform, ProcArt.Rounded, new Color(0.45f, 0.38f, 0.3f, 0.88f), Vector2.zero, new Vector2(214f, 244f), true);
            Part("Smudge", dirt.transform, ProcArt.Circle, new Color(0.3f, 0.25f, 0.2f, 0.5f), new Vector2(-40f, 40f), new Vector2(90f, 70f));
            Part("Smudge", dirt.transform, ProcArt.Circle, new Color(0.3f, 0.25f, 0.2f, 0.4f), new Vector2(50f, -50f), new Vector2(110f, 80f));
            Part("BarV", scene, null, new Color(0.35f, 0.22f, 0.15f), new Vector2(-290f, 190f), new Vector2(12f, 244f));
            Part("BarH", scene, null, new Color(0.35f, 0.22f, 0.15f), new Vector2(-290f, 190f), new Vector2(214f, 12f));

            // shelves + bottles
            plankTop = Part("ShelfTop", scene, ProcArt.Rounded, wood, new Vector2(250f, 280f), new Vector2(360f, 28f), true);
            plankBottom = Part("ShelfBottom", scene, ProcArt.Rounded, wood, new Vector2(250f, 140f), new Vector2(360f, 28f), true);
            bottles = UIKit.Rect("Bottles", scene).gameObject;
            for (int i = 0; i < 4; i++)
                Part("Bottle", bottles.transform, ProcArt.Bottle(i), Color.white, new Vector2(125f + i * 82f, 332f), new Vector2(80f, 80f));
            Part("Bottle", bottles.transform, ProcArt.Bottle(4), Color.white, new Vector2(140f, 192f), new Vector2(80f, 80f));
            Part("Bottle", bottles.transform, ProcArt.Bottle(1), Color.white, new Vector2(220f, 192f), new Vector2(80f, 80f));
            Part("Book", bottles.transform, ProcArt.WitchItem("w_kitap"), Color.white, new Vector2(330f, 196f), new Vector2(84f, 84f));

            // lanterns
            Part("String", scene, null, new Color(0.2f, 0.15f, 0.2f), new Vector2(-90f, 425f), new Vector2(4f, 90f));
            Part("String", scene, null, new Color(0.2f, 0.15f, 0.2f), new Vector2(110f, 425f), new Vector2(4f, 90f));
            glowA = Part("GlowA", scene, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.45f), new Vector2(-90f, 345f), new Vector2(320f, 320f));
            glowB = Part("GlowB", scene, ProcArt.GlowSprite, new Color(1f, 0.8f, 0.4f, 0.45f), new Vector2(110f, 345f), new Vector2(320f, 320f));
            lanternA = Part("LanternA", scene, ProcArt.Lantern(false), Color.white, new Vector2(-90f, 345f), new Vector2(100f, 110f));
            lanternB = Part("LanternB", scene, ProcArt.Lantern(false), Color.white, new Vector2(110f, 345f), new Vector2(100f, 110f));

            // rug + cauldron
            rug = UIKit.Rect("Rug", scene).gameObject;
            Part("RugOuter", rug.transform, ProcArt.Circle, new Color(0.6f, 0.2f, 0.3f), new Vector2(0f, -365f), new Vector2(620f, 170f));
            Part("RugMid", rug.transform, ProcArt.Circle, new Color(0.8f, 0.4f, 0.42f), new Vector2(0f, -365f), new Vector2(540f, 130f));
            Part("RugIn", rug.transform, ProcArt.Circle, new Color(0.6f, 0.2f, 0.3f), new Vector2(0f, -365f), new Vector2(300f, 70f));
            cauldronGlow = Part("CauldronGlow", scene, ProcArt.GlowSprite, new Color(0.4f, 1f, 0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(480f, 480f));
            cauldron = Part("Cauldron", scene, ProcArt.Item(Chains.Kazan, 1), Color.white, new Vector2(0f, -230f), new Vector2(300f, 300f));

            // counter
            counterBody = Part("CounterBody", scene, ProcArt.Rounded, new Color(0.42f, 0.28f, 0.2f), new Vector2(340f, -255f), new Vector2(260f, 180f), true);
            counterTop = Part("CounterTop", scene, ProcArt.Rounded, wood, new Vector2(340f, -155f), new Vector2(310f, 32f), true);
            counterStuff = UIKit.Rect("CounterStuff", scene).gameObject;
            Part("Ball", counterStuff.transform, ProcArt.WitchItem("w_kure"), Color.white, new Vector2(300f, -95f), new Vector2(100f, 100f));
            Part("Letter", counterStuff.transform, ProcArt.WitchItem("w_mektup"), Color.white, new Vector2(400f, -118f), new Vector2(70f, 70f));

            // cobwebs
            webs = UIKit.Rect("Webs", scene).gameObject;
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.85f), new Vector2(-430f, 390f), new Vector2(200f, 200f));
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.85f), new Vector2(430f, 390f), new Vector2(200f, 200f));
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.7f), new Vector2(-30f, 420f), new Vector2(130f, 130f));
            Part("Web", webs.transform, ProcArt.Item(Chains.Ag, 1), new Color(1f, 1f, 1f, 0.7f), new Vector2(420f, -100f), new Vector2(120f, 120f));

            // Kara Pati + bed
            bed = UIKit.Rect("Bed", scene).gameObject;
            Part("BedBase", bed.transform, ProcArt.Circle, new Color(0.7f, 0.25f, 0.3f), new Vector2(-330f, -350f), new Vector2(280f, 120f));
            Part("Cushion", bed.transform, ProcArt.Circle, new Color(0.95f, 0.65f, 0.6f), new Vector2(-330f, -345f), new Vector2(230f, 80f));
            karaPati = Part("KaraPati", scene, ProcArt.SideCat(0, 4), Color.white, new Vector2(-330f, -290f), new Vector2(190f, 190f));
            karaPati.raycastTarget = true;
            var catBtn = karaPati.gameObject.AddComponent<Button>();
            catBtn.transition = Selectable.Transition.None;
            catBtn.onClick.AddListener(() =>
            {
                Tween.Punch(karaPati.transform, 0.15f, 0.3f);
                GM.Toast("Kara Pati: \"" + catLines[Random.Range(0, catLines.Length)] + "\"");
            });

            ghostLayer = UIKit.Rect("Ghosts", scene).At(Vector2.zero, scene.sizeDelta);

            // everything gets brighter as the shop is restored
            dim = Part("Dust", scene, ProcArt.Rounded, new Color(0.05f, 0.03f, 0.08f, 0.45f), Vector2.zero, new Vector2(1020f, 940f), true);
        }

        // ------------------------------------------------------------------ state
        public void Refresh(bool animate)
        {
            int done = GM.TasksDone;
            int total = Story.Tasks.Length;

            progressText.text = done + "/" + total;
            float fw = Mathf.Max(24f, ProgressW * done / (float)total);
            if (animate)
            {
                float from = progressFill.sizeDelta.x;
                var pf = progressFill;
                Tween.Run(0.5f, t => { if (pf) pf.sizeDelta = new Vector2(Mathf.Lerp(from, fw, Tween.OutCubic(t)), 24f); });
            }
            else progressFill.sizeDelta = new Vector2(fw, 24f);

            ApplyScene(done, animate && shownDone >= 0 && done > shownDone);
            shownDone = done;
            BuildTasks();
            RefreshGhosts();
            albumBadge.gameObject.SetActive(GM.AnyClaimableSet);
        }

        bool Done(string id, int done)
        {
            for (int i = 0; i < done && i < Story.Tasks.Length; i++)
                if (Story.Tasks[i].id == id) return true;
            return false;
        }

        void ApplyScene(int done, bool animate)
        {
            webs.SetActive(!Done("ag", done));
            dirt.SetActive(!Done("pencere", done));

            bool shelves = Done("raf", done);
            plankTop.rectTransform.localEulerAngles = new Vector3(0, 0, shelves ? 0f : -14f);
            plankBottom.rectTransform.localEulerAngles = new Vector3(0, 0, shelves ? 0f : 10f);
            bottles.SetActive(shelves);

            bool fire = Done("kazan", done);
            cauldron.color = fire ? Color.white : new Color(0.5f, 0.5f, 0.56f);
            cauldronGlow.gameObject.SetActive(fire);

            rug.SetActive(Done("hali", done));

            bool lit = Done("fener", done);
            lanternA.sprite = ProcArt.Lantern(lit);
            lanternB.sprite = ProcArt.Lantern(lit);
            glowA.gameObject.SetActive(lit);
            glowB.gameObject.SetActive(lit);

            bool counter = Done("tezgah", done);
            counterTop.rectTransform.localEulerAngles = new Vector3(0, 0, counter ? 0f : 12f);
            counterBody.color = counter ? new Color(0.42f, 0.28f, 0.2f) : new Color(0.3f, 0.2f, 0.16f);
            counterStuff.SetActive(counter);

            bool hasBed = Done("yatak", done);
            bed.SetActive(hasBed);
            karaPati.sprite = ProcArt.SideCat(0, hasBed ? 5 : 4);
            karaPati.rectTransform.anchoredPosition = hasBed ? new Vector2(-330f, -318f) : new Vector2(-330f, -290f);

            float a = 0.45f * (1f - done / (float)Story.Tasks.Length);
            dim.color = new Color(0.05f, 0.03f, 0.08f, a);

            if (!animate || done <= 0) return;
            var target = TaskTarget(Story.Tasks[done - 1].id);
            Vector2 local = new Vector2(0f, 380f);
            if (target)
            {
                Tween.Pop(target, 0.4f);
                local = scene.InverseTransformPoint(target.position);
            }
            FX.Burst(scene, local, UIKit.Orange, 12);
        }

        Transform TaskTarget(string id)
        {
            switch (id)
            {
                case "ag": return null;
                case "pencere": return scene.Find("Moon");
                case "raf": return plankTop.transform;
                case "kazan": return cauldron.transform;
                case "hali": return rug.transform.GetChild(0);
                case "fener": return lanternA.transform;
                case "tezgah": return counterTop.transform;
                case "yatak": return karaPati.transform;
                default: return null;
            }
        }

        static Sprite TaskIcon(string id)
        {
            switch (id)
            {
                case "ag": return ProcArt.Item(Chains.Ag, 1);
                case "pencere": return ProcArt.Moon;
                case "raf": return ProcArt.Bottle(0);
                case "kazan": return ProcArt.Item(Chains.Kazan, 1);
                case "hali": return ProcArt.Item(Chains.Ag, 4);
                case "fener": return ProcArt.Lantern(true);
                case "tezgah": return ProcArt.WitchItem("w_kure");
                default: return ProcArt.SideCat(0, 5);
            }
        }

        void BuildTasks()
        {
            for (int i = taskList.childCount - 1; i >= 0; i--) Destroy(taskList.GetChild(i).gameObject);
            int done = GM.TasksDone;

            if (done >= Story.Tasks.Length)
            {
                var row = Row(0);
                var txt = UIKit.Txt("Done", row, "Bölüm tamamlandı!\nBölüm 2: Mahzen - yakında", 34, UIKit.Cream);
                UIKit.Stretch(txt.rectTransform, 20f, 20f);
                return;
            }

            for (int k = 0; k < 3 && done + k < Story.Tasks.Length; k++)
            {
                int idx = done + k;
                var task = Story.Tasks[idx];
                bool current = k == 0;
                bool affordable = GM.Stamps >= task.cost;
                var row = Row(k);
                if (!current) row.GetComponent<Image>().color = new Color(0.13f, 0.09f, 0.19f, 0.6f);

                var icon = UIKit.Img("Icon", row, TaskIcon(task.id), current ? Color.white : new Color(1f, 1f, 1f, 0.45f));
                icon.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(14f, 0f), new Vector2(94f, 94f));

                var title = UIKit.Txt("Title", row, task.title, 32, current ? UIKit.Cream : new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleLeft);
                title.rectTransform.Anchor(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f)).At(new Vector2(124f, 0f), new Vector2(480f, 100f));

                var stamp = UIKit.Img("Stamp", row, ProcArt.Stamp, current ? Color.white : new Color(1f, 1f, 1f, 0.5f));
                stamp.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-268f, 0f), new Vector2(56f, 56f));
                var cost = UIKit.Txt("Cost", row, task.cost.ToString(), 36, current ? UIKit.Cream : new Color(1f, 1f, 1f, 0.5f));
                cost.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-222f, 0f), new Vector2(50f, 50f));

                if (current)
                {
                    var btn = UIKit.Btn("Do", row, "Yap", affordable ? UIKit.Green : UIKit.Disabled, () => GM.DoTask(idx), 38);
                    ((RectTransform)btn.transform).Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(180f, 90f));
                }
                else
                {
                    var wait = UIKit.Txt("Next", row, "Sırada", 28, new Color(1f, 1f, 1f, 0.45f));
                    wait.rectTransform.Anchor(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f)).At(new Vector2(-14f, 0f), new Vector2(180f, 90f));
                }
            }
        }

        RectTransform Row(int k)
        {
            var img = UIKit.Img("Row", taskList, ProcArt.Rounded, UIKit.PanelDark, true);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -k * 142f);
            rt.sizeDelta = new Vector2(0f, 128f);
            return rt;
        }

        // ghosts you collected as stickers come to visit the shop
        void RefreshGhosts()
        {
            foreach (var g in floatingGhosts) if (g) Destroy(g.gameObject);
            floatingGhosts.Clear();
            for (int i = 0; i < Ghosts.Names.Length; i++)
            {
                if (!GM.HasSticker("g" + i)) continue;
                var img = UIKit.Img("Ghost", ghostLayer, ProcArt.Ghost(i), new Color(1f, 1f, 1f, 0.8f));
                var rt = img.rectTransform;
                rt.At(new Vector2(-420f + (i % 4) * 240f + Random.Range(-30f, 30f), (i < 4 ? 40f : -80f) + Random.Range(-20f, 20f)), new Vector2(70f, 70f));
                floatingGhosts.Add(rt);
            }
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < floatingGhosts.Count; i++)
            {
                var g = floatingGhosts[i];
                if (!g) continue;
                g.anchoredPosition += new Vector2(Mathf.Sin(t * 0.7f + i) * 0.4f, Mathf.Sin(t * 2f + i * 1.3f) * 0.35f);
            }
            if (cauldronGlow && cauldronGlow.gameObject.activeSelf)
                cauldronGlow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.05f);
            if (karaPati)
            {
                bool sleeping = GM != null && GM.TasksDone >= Story.Tasks.Length;
                if (sleeping) karaPati.rectTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);
            }
        }
    }
}
