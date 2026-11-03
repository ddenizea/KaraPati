using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KaraPati.EditorTools
{
    /// <summary>One-time project tweaks for a portrait mobile game, plus a few handy menu items.</summary>
    [InitializeOnLoad]
    static class KaraPatiSetup
    {
        static KaraPatiSetup()
        {
            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                Debug.Log("KaraPati: ekran yönü Dikey (Portrait) olarak ayarlandı.");
            }
            // Script changes while playing: recompile only after Play stops. Recompiling mid-play wipes the
            // game's in-memory state, which could then be saved over a good save.
            if (EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0) != 1)
            {
                EditorPrefs.SetInt("ScriptCompilationDuringPlay", 1);
                Debug.Log("KaraPati: oyun sırasında kod değişirse derleme Play bitince yapılacak.");
            }
        }

        [MenuItem("KaraPati/Test/+30 Pati Mührü (Play modunda)")]
        static void GiveStamps()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.AddStamps(30);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/+500 Altın (Play modunda)")]
        static void GiveGold()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.AddGold(500);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/+50 XP (Play modunda)")]
        static void GiveXp()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.AddXp(50);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Baloncuk çıkar (Play modunda)")]
        static void Bubble()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Board.DebugSpawnBubble();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/+10 Balkabağı (Play modunda)")]
        static void Pumpkins()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.AddPumpkins(10);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Günlük ödülü sıfırla (Play modunda)")]
        static void ResetDaily()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.DebugResetDaily();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Rehberi Baştan Başlat (Play modunda)")]
        static void RestartTutorial()
        {
            if (Application.isPlaying && GameManager.I != null && GameManager.I.Tut != null) { GameManager.I.Tut.Step = 1; GameManager.I.MarkDirty(); }
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Sonraki Bölüme Atla (Play modunda)")]
        static void SkipChapter()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.DebugSkipChapter();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Yavru Çağır (Play modunda)")]
        static void CallKitten()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugSpawn(1);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Çırak Kedi Ekle (Play modunda)")]
        static void AddApprentice()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugSpawn(2);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Çırak'ı Kazana Koy (Play modunda)")]
        static void ApprenticeToPot()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugStir();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Kedi Evine Yavru Koy (Play modunda)")]
        static void HouseKitten()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugHouseKitten();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Minik Çırak Ekle (Play modunda)")]
        static void AddMini()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugMini();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Eğitimi Bitir (Play modunda)")]
        static void FinishTraining()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugFinishTraining();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Usta Kedi Ekle (Play modunda)")]
        static void AddMaster()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugSpawn(3);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Usta'nın Keşfini Bitir (Play modunda)")]
        static void FinishTrip()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugFinishTrip();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Usta Kedim Penceresi (Play modunda)")]
        static void UstaColor()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.OpenUstaPanel();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Müşteri İksir İstesin (Play modunda)")]
        static void PotionOrder()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Orders.DebugPotionOrder();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Eğitimi Başlat (Play modunda)")]
        static void StartTraining()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Cats.DebugStartTraining();
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        [MenuItem("KaraPati/Test/Müşteri Dostluğu +3 (Play modunda)")]
        static void FriendPlus()
        {
            if (Application.isPlaying && GameManager.I != null) GameManager.I.Orders.DebugFriendship(3);
            else Debug.Log("KaraPati: önce Play'e bas.");
        }

        // ------------------------------------------------------------ save backup for testing
        // Test on the real save, then put it back exactly as it was. The file lives in UserSettings/,
        // which Git ignores, so it never ends up on GitHub.
        const string SaveKey = "karapati_save_v1", BackupKey = "karapati_save_v1_backup";
        const string BackupFile = "UserSettings/karapati_kayit_yedegi.json";

        [MenuItem("KaraPati/Kayıt/Kaydı Dosyaya Yedekle")]
        static void BackupSaveToFile()
        {
            if (Application.isPlaying) { Debug.LogWarning("KaraPati: önce Play modundan çık."); return; }
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) { Debug.LogWarning("KaraPati: yedeklenecek kayıt yok."); return; }
            Directory.CreateDirectory("UserSettings");
            File.WriteAllText(BackupFile, json);
            Debug.Log("KaraPati: kayıt yedeklendi -> " + BackupFile + " (" + json.Length + " karakter)");
        }

        [MenuItem("KaraPati/Kayıt/Yedekten Geri Yükle")]
        static void RestoreSaveFromFile()
        {
            if (Application.isPlaying) { Debug.LogWarning("KaraPati: önce Play modundan çık."); return; }
            if (!File.Exists(BackupFile)) { Debug.LogWarning("KaraPati: yedek dosyası yok."); return; }
            string json = File.ReadAllText(BackupFile);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.SetString(BackupKey, json);
            PlayerPrefs.Save();
            Debug.Log("KaraPati: kayıt yedekten geri yüklendi (" + json.Length + " karakter).");
        }

        const string ApkPath = "Builds/KaraPati.apk";

        [MenuItem("KaraPati/Android APK Oluştur")]
        static void BuildApk()
        {
            if (Application.isPlaying) { Debug.LogWarning("KaraPati: önce Play modundan çık."); return; }

            // 1) app icons drawn by the game's own procedural painter
            var full = SaveIcon("Assets/Art/AppIcon.png", ProcArt.AppIcon(512, 0));
            var back = SaveIcon("Assets/Art/AppIconBack.png", ProcArt.AppIcon(432, 1));
            var fore = SaveIcon("Assets/Art/AppIconFore.png", ProcArt.AppIcon(432, 2));
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);
            var android = NamedBuildTarget.Android;
            // Unity 6.6 only keeps adaptive icons (background + foreground layer) for Android
            var adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
            foreach (var ic in adaptive) { ic.SetTexture(back, 0); ic.SetTexture(fore, 1); }
            PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, adaptive);

            // 2) identity + 64-bit Android (required by modern phones)
            PlayerSettings.companyName = "KaraPati";
            PlayerSettings.productName = "Kara Pati";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.karapati.iksirdukkani");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;

            // 3) scenes
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };

            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });
            var sum = report.summary;
            if (sum.result == BuildResult.Succeeded)
            {
                Debug.Log("KaraPati: APK hazır -> " + Path.GetFullPath(ApkPath) + " (" + (sum.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + sum.totalTime.TotalMinutes.ToString("0.0") + " dk)");
                EditorUtility.RevealInFinder(ApkPath);
            }
            else Debug.LogError("KaraPati: APK derlemesi başarısız: " + sum.result + " (" + sum.totalErrors + " hata). Ayrıntı için Console'a bak.");
        }

        static Texture2D SaveIcon(string path, Texture2D tex)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        [MenuItem("KaraPati/Kayıtlı Oyunu Sil")]
        static void ClearSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(BackupKey);   // otherwise the game would load the automatic backup copy
            PlayerPrefs.Save();
            Debug.Log("KaraPati: kayıt silindi. Bir sonraki Play yeni oyunla başlar.");
        }
    }
}
