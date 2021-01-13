using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;

namespace SteganographyLogic.Helpers
{
    public class InputValidation : IInputValidation
    {

        // + 3 because information about length of file name's lengh and extension's length is stored in 3 bytes.
        public bool IsImageVaild(byte[] image, byte[] message, string fileName = null)
        {

            if (fileName == null)
            {
                if ((image.Length * 3 / 8) <= message.Length)
                {
                    return false;
                }
            }
            else
            {
                if ((image.Length * 3 / 8) <= (message.Length + fileName.Length + 3))
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool IsAudioValid(byte[] audio, byte[] message, string fileName = null)
        {
            if (fileName == null)
            {
                if (audio.Length / 8 <= message.Length)
                {
                    return false;
                }
            }
            else
            {
                if (audio.Length / 8 <= (message.Length + fileName.Length + 3))
                {
                    return false;
                }
            }

            return true;
        }




    }
}
