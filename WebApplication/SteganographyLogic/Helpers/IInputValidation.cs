using System.Drawing;

namespace SteganographyLogic.Helpers
{
    public interface IInputValidation
    {
        bool IsAudioValid(byte[] audio, byte[] message, string fileName = null);
        bool IsImageVaild(byte[] image, byte[] message, string fileName = null);
    }
}