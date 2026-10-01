using UnityEngine;

namespace MagicSave
{
    public class GlobalSave<T> where T : class, new()
    {
        public T Data { get; private set; }

        public void Load()
        {
            string path = SavePaths.GlobalFile;

            Data = SaveFile.Read<T>(path);

            if (Data == null)
            {
                Data = new T();
                Save();
            }
        }

        public void Save()
        {
            if (Data == null)
            {
                Debug.LogError("[MagicSave] GlobalSave.Save called before Load, nothing written");
                return;
            }
            
            SaveFile.Write(SavePaths.GlobalFile, Data);
        }
    }
}
