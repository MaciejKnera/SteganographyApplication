namespace SteganographyLogic.Processors
{
    public interface IImageProcessor
    {
        byte[] GetMessage(byte[] imageByte, out string fileName, out bool containsMessage);
        byte[] HideMessage(byte[] imageByte, byte[] message, string fileName = null);
    }
}