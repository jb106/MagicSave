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

        // Files can be briefly locked by other processes (antivirus, cloud sync).
        static readonly int[] RetryDelaysMs = { 20, 50, 100, 200 };

        public static bool Write(string path, object data)
        {
            string json;
            try
            {
                json = SaveJson.Serialize(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MagicSave] '{path}' could not be serialized: {e}");
                return false;
            }

            string tmp = path + ".tmp";

            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

                    File.WriteAllText(tmp, json);

                    if (File.Exists(path)) File.Replace(tmp, path, null);
                    else File.Move(tmp, path);

                    if (attempt > 0)
                        Debug.LogWarning($"[MagicSave] '{path}' written after {attempt + 1} attempts.");

                    return true;
                }
                catch (Exception e) when ((e is IOException || e is UnauthorizedAccessException)
                                          && attempt < RetryDelaysMs.Length)
                {
                    System.Threading.Thread.Sleep(RetryDelaysMs[attempt]);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MagicSave] '{path}' could not be written after {attempt + 1} attempt(s): {e}");
                    return false;
                }
            }
        }

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}