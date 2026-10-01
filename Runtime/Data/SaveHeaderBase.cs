namespace MagicSave
{
    [System.Serializable]
    public class SaveHeaderBase
    {
        public int saveVersion;
        public string appVersion;
        public long createdAtUtc;
        public long lastSavedAtUtc;
        public double totalPlayTimeSeconds;
    }
}
