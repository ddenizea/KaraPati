using UnityEditor;
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

        [MenuItem("KaraPati/Kayıtlı Oyunu Sil")]
        static void ClearSave()
        {
            PlayerPrefs.DeleteKey("karapati_save_v1");
            PlayerPrefs.Save();
            Debug.Log("KaraPati: kayıt silindi. Bir sonraki Play yeni oyunla başlar.");
        }
    }
}
