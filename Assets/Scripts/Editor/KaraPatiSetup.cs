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

        [MenuItem("KaraPati/Kayıtlı Oyunu Sil")]
        static void ClearSave()
        {
            PlayerPrefs.DeleteKey("karapati_save_v1");
            PlayerPrefs.Save();
            Debug.Log("KaraPati: kayıt silindi. Bir sonraki Play yeni oyunla başlar.");
        }
    }
}
