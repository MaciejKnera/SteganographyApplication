using SteganographyLogic.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SteganographyLogic.Helpers
{
    public static class StaticData
    {
        public static readonly byte[] messageDelimeter = Encoding.ASCII.GetBytes("!@#$%^");

        public static IDictionary<string, string> mimeTypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            {".xml",  "text/xml"},
            {".css", "text/css"},
            {".htm", "text/html"},
            {".html", "text/html"},
            {".json", "application/json"},
            {".js", "application/x-javascript"},
            {".py", "text/x-python"},
            {".txt", "text/plain"},
            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".odt", "application/vnd.oasis.opendocument.text"},
            {".pdf", "application/pdf"},
            {".rtf", "application/rtf"},
            {".tex", "application/x-tex"},
            {".bat", "application/x-bat" },
            {".bmp", "image/bmp"},
            {".emf", "application/emf" },
            {".gif", "image/gif"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
            {".tiff", "image/tiff"},
            {".svg",  "image/svg+xml"},
            {".ico", "image/x-icon" },
            {".wmf",  "image/x-wmf"},
            {".aif", "audio/x-aiff"},
            {".aiff", "audio/x-aiff"},
            {".aifc", "audio/x-aiff"},
            {".au", "audio/basic"},
            {".avi", "video/x-msvideo"},
            {".m3u", "audio/x-mpegurl"},
            {".m4a", "audio/mp4a-latm"},
            {".m4b", "audio/mp4a-latm"},
            {".m4p", "audio/mp4a-latm"},
            {".m4u", "video/vnd.mpegurl"},
            {".flac", "audio/x-flac" },
            {".mp3", "audio/mpeg"},
            {".mp4", "video/mp4"},
            {".wav", "audio/x-wav"},
            {".wma", "audio/x-ms-wma" },
            {".aac", "audio/aac" },
            {"ogg", "application/ogg"},
        };

        public static string EmbedFileNameIntoMsgAndConvertToBin(byte[] message, string fileName)
        {
            StringBuilder messageToBinary = new StringBuilder();

            message = EncodeExtensionAndFileNameIntoMsg(message, fileName);
            message = message.Concat(StaticData.messageDelimeter).ToArray();

            foreach (byte item in message)
            {
                messageToBinary.Append(Convert.ToString(item, 2).PadLeft(8, '0'));
            }

            return messageToBinary.ToString();
        }

        public static byte[] EncodeExtensionAndFileNameIntoMsg(byte[] message, string fileName)
        {
            string extension = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            byte extensionLength = (byte)extension.Length;
            short nameLenght = (short)name.Length;

            //store extension and one additional byte to indicate extension's length
            byte[] extensionInfo = new byte[extensionLength + 1];
            extensionInfo[0] = extensionLength;
            Array.Copy(Encoding.ASCII.GetBytes(extension), 0, extensionInfo, 1, extensionLength);

            //store file name and two additional bytes to indicate file name's length
            byte[] fileNameInfo = new byte[nameLenght + 2];
            Array.Copy(BitConverter.GetBytes(nameLenght), fileNameInfo, 2);
            Array.Copy(Encoding.ASCII.GetBytes(name), 0, fileNameInfo, 2, nameLenght);


            return extensionInfo.Concat(fileNameInfo).Concat(message).ToArray();
        }

        public static string AddMsgDelimAndConvertMsgToBin(byte[] message)
        {
            StringBuilder messageToBinary = new StringBuilder();

            // 0 at first positions indicates at there is no file hidden, just string message.
            byte[] messageWithMetaData = new byte[message.Length + 1];
            messageWithMetaData[0] = 0;
            Array.Copy(message, 0, messageWithMetaData, 1, message.Length);
            messageWithMetaData = messageWithMetaData.Concat(StaticData.messageDelimeter).ToArray();

            foreach (byte item in messageWithMetaData)
            {
                messageToBinary.Append(Convert.ToString(item, 2).PadLeft(8, '0'));
            }

            return messageToBinary.ToString();
        }

        public static bool CheckIfAnyFileIsHidden(byte[] lsbArray)
        {
            if (lsbArray[0] == 0)
            {
                return false;
            }

            return true;
        }
        public static string DecodeExtensionAndFileNameFromMsg(byte[] message)
        {
            byte extensionLength = message[0];
            byte[] extension = new byte[extensionLength];
            Array.Copy(message, 1, extension, 0, extensionLength);
            string extensionName = Encoding.ASCII.GetString(extension);

            //file name's length is specified after extension name, thus it's position is set to file extension name's length and +1 because 1 byte is occupied to store extension's length.
            short fileNameLength = BitConverter.ToInt16(message, extensionLength + 1);
            byte[] file = new byte[fileNameLength];
            //extensionLength + 3 because starting index is after extension + 1 byte for its length and then + 2 bytes for file name's length
            Array.Copy(message, extensionLength + 3, file, 0, fileNameLength);
            string fileName = Encoding.ASCII.GetString(file);

            return fileName + extensionName;
        }
        public static byte[] DecodeMessage(byte[] byteMessage, short startIndex, out bool containsMessage)
        {
            List<byte> messageByte = new List<byte>();
            StringBuilder messageChar = new StringBuilder();
            containsMessage = false;

            for (int i = startIndex; i < byteMessage.Length; i++)
            {
                messageByte.Add(byteMessage[i]);
                messageChar.Append((char)byteMessage[i]);

                if (messageChar.ToString().Contains(Encoding.ASCII.GetString(StaticData.messageDelimeter)))
                {
                    containsMessage = true;
                    break;
                }
            }

            int outputLength = messageByte.Count - StaticData.messageDelimeter.Length;
            byte[] output = new byte[outputLength];
            Array.Copy(messageByte.ToArray(), 0, output, 0, outputLength);

            return output;
        }

    }

}
