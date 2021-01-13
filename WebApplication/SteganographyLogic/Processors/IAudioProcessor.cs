namespace SteganographyLogic.Processors
{
    public interface IAudioProcessor
    {
        bool CheckIfFileIsWaveType(byte[] fullSong);
        byte[] GetMessage(byte[] fullSong, out string fileName, out bool containsMessage);
        byte[] HideMessage(byte[] fullSong, byte[] message, string fileName = null);
    }
}