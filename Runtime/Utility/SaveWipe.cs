using System;
using System.IO;
using UnityEngine;

namespace MagicSave
{
    // Deletes every save once, each time the game raises its generation number
    public static class SaveWipe
    {
        private const string GENERATION_KEY = "MagicSave.SavesGeneration";

        public static void WipeIfOutdated(int generation)
        {
            if (PlayerPrefs.GetInt(GENERATION_KEY, 0) >= generation) return;

            try
            {
                string directory = SavePaths.DefaultSavesDirectory;
                if (Directory.Exists(directory)) Directory.Delete(directory, true);

                Debug.Log($"[MagicSave] Saves wiped, generation {generation}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] Saves could not be wiped: {e.Message}");
                return;
            }

            PlayerPrefs.SetInt(GENERATION_KEY, generation);
            PlayerPrefs.Save();
        }
    }
}