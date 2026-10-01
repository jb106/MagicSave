namespace MagicSave
{
    public interface ISaveParticipant<TData>
    {
        void Capture(TData data);
    }

    public interface ISaveBlocker
    {
        bool BlocksSave { get; }
    }
    
    // Entities
    public interface ISaveableState
    {
        string SaveKey { get; }

        object CaptureState();
        void RestoreState(object state);
    }

    public interface IOfflineSimulatable
    {
        void Simulate(double elapsedPlayTimeSeconds);
    }
}