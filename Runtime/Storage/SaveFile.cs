using System;
using System.IO;
using UnityEngine;

namespace MagicSave
{
    public static class SaveFile
    {
        public static bool Exists(string path) => File.Exists(path);

        public static T Read<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;

            try
            {
                return SaveJson.Deserialize<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] '{path}' unreadable: {e.Message}");
                return null;
            }
        }

        public static bool Write(string path, object data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

                string tmp = path + ".tmp";
                File.WriteAllText(tmp, SaveJson.Serialize(data));

                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] '{path}' could not be written: {e.Message}");
                return false;
            }
        }

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}