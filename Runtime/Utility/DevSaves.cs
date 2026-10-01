using System.IO;
using UnityEngine;

namespace MagicSave
{
    public static class DevSaves
    {
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("UseDevSaves", 0) == 1;
            set => PlayerPrefs.SetInt("UseDevSaves", value ? 1 : 0);
        }

        public static string DirectoryPath => Path.Combine(Application.dataPath, "..", "DevSaves");
    }
}
