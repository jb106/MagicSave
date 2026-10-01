using System;
using System.IO;
using UnityEngine;

namespace MagicSave
{
    public static class GameSlots
    {
        public static string[] GetAll()
        {
            string root = SavePaths.SavesDirectory;
            if (!Directory.Exists(root)) return Array.Empty<string>();

            var directories = Directory.GetDirectories(root);
            for (int x = 0; x < directories.Length; x++)
            {
                directories[x] = Path.GetFileName(directories[x]);
            }

            return directories;
        }

        public static bool Exists(string saveName) => !string.IsNullOrEmpty(saveName) && SaveFile.Exists(SavePaths.GameDataFile(saveName));

        // -----------------------------------------------------------------------------------------------------------

        public static bool IsNameAvailable(string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName)) return false;
            if (saveName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;

            foreach (var existing in GetAll())
            {
                if (existing.Equals(saveName, StringComparison.OrdinalIgnoreCase)) return false;
            }

            return true;
        }

        public static string GetNextFreeName()
        {
            int x = 1;
            while (!IsNameAvailable($"game_{x:000}")) x++;

            return $"game_{x:000}";
        }

        public static string GetCopyName(string sourceName)
        {
            string baseName = sourceName + "_copy";
            if (IsNameAvailable(baseName)) return baseName;

            int i = 2;
            while (!IsNameAvailable($"{baseName}{i}")) i++;

            return $"{baseName}{i}";
        }

        // -----------------------------------------------------------------------------------------------------------

        public static bool Delete(string saveName)
        {
            string directory = SavePaths.GameDirectory(saveName);
            if (!Directory.Exists(directory)) return false;

            try
            {
                Directory.Delete(directory, true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] '{saveName}' could not be deleted: {e.Message}");
                return false;
            }
        }

        public static bool Rename(string oldName, string newName)
        {
            newName = newName?.Trim();

            if (!Exists(oldName)) return false;
            if (!IsNameAvailable(newName)) return false;

            try
            {
                Directory.Move(SavePaths.GameDirectory(oldName), SavePaths.GameDirectory(newName));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] '{oldName}' could not be renamed: {e.Message}");
                return false;
            }
        }

        public static string Duplicate(string sourceName)
        {
            if (!Exists(sourceName)) return null;

            string newName = GetCopyName(sourceName);
            string source = SavePaths.GameDirectory(sourceName);
            string destination = SavePaths.GameDirectory(newName);

            try
            {
                Directory.CreateDirectory(destination);

                foreach (var file in Directory.GetFiles(source))
                {
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
                }

                return newName;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] '{sourceName}' could not be duplicated: {e.Message}");
                return null;
            }
        }
    }
}