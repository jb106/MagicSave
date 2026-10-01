using System.IO;
using UnityEngine;

namespace MagicSave
{
    public static class SavePaths
    {
        private const string SAVES_DATA_DIRECTORY_NAME = "SavesData";

        private const string SAVE_DATA_NAME = "data.json";
        private const string GLOBAL_DATA_NAME = "global.json";
        private const string HEADER_DATA_NAME = "header.json";

        public static string GlobalFile => Path.Combine(Application.persistentDataPath, GLOBAL_DATA_NAME);
        
        public static string DefaultSavesDirectory => Path.Combine(Application.persistentDataPath, SAVES_DATA_DIRECTORY_NAME);
        public static string SavesDirectory => DevSaves.Enabled ? DevSaves.DirectoryPath : Path.Combine(Application.persistentDataPath, SAVES_DATA_DIRECTORY_NAME);
        public static string GameDirectory(string saveName) => string.IsNullOrEmpty(saveName) ? string.Empty : Path.Combine(SavesDirectory, saveName);

        public static string GameDataFile(string saveName) => GameFile(saveName, SAVE_DATA_NAME);
        public static string GameHeaderFile(string saveName) => GameFile(saveName, HEADER_DATA_NAME);
        public static string GameSectionFile(string saveName, string fileName) => GameFile(saveName, fileName + ".json");

        private static string GameFile(string saveName, string fileName)
        {
            if (string.IsNullOrEmpty(saveName)) return string.Empty;

            return Path.Combine(GameDirectory(saveName), fileName);
        }
    }
}