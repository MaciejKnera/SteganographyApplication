using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using SteganographyLogic.Helpers;
using System.IO;
using SteganographyLogic.Enums;

namespace SteganographyLogic.Processors
{
    public class ImageProcessor : IImageProcessor
    {
        public byte[] HideMessage(byte[] imageByte, byte[] message, string fileName = null)
        {
            Bitmap image = ConvertBitArrayToBitmap(imageByte);
            int imageWidth = image.Width, imageHeight = image.Height, messageLength;
            string messageBinary = string.Empty;

            if (fileName != null)
            {
                messageBinary = StaticData.EmbedFileNameIntoMsgAndConvertToBin(message, fileName);
            }
            else
            {
                messageBinary = StaticData.AddMsgDelimAndConvertMsgToBin(message);
            }


            StringBuilder pixelElement = new StringBuilder();
            messageLength = messageBinary.Length;
            int index = 0;
            bool breakLoop = false;

            for (int i = 0; i < imageWidth; i++)
            {
                if (breakLoop)
                {
                    break;
                }
                for (int j = 0; j < imageHeight; j++)
                {
                    Color pixel;

                    if (index < messageLength)
                    {
                        pixel = image.GetPixel(i, j);
                        pixelElement.Append(Convert.ToString(pixel.R, 2).PadLeft(8, '0'));
                        pixelElement.Length--;
                        pixelElement.Insert(pixelElement.Length, messageBinary[index]);
                        image.SetPixel(i, j, Color.FromArgb(Convert.ToByte(pixelElement.ToString(), 2), pixel.G, pixel.B));
                        pixelElement.Clear();
                        index += 1;
                    }
                    if (index < messageLength)
                    {
                        pixel = image.GetPixel(i, j);
                        pixelElement.Append(Convert.ToString(pixel.G, 2).PadLeft(8, '0'));
                        pixelElement.Length--;
                        pixelElement.Insert(pixelElement.Length, messageBinary[index]);
                        image.SetPixel(i, j, Color.FromArgb(pixel.R, Convert.ToByte(pixelElement.ToString(), 2), pixel.B));
                        pixelElement.Clear();
                        index += 1;
                    }
                    if (index < messageLength)
                    {
                        pixel = image.GetPixel(i, j);
                        pixelElement.Append(Convert.ToString(pixel.B, 2).PadLeft(8, '0'));
                        pixelElement.Length--;
                        pixelElement.Insert(pixelElement.Length, messageBinary[index]);
                        image.SetPixel(i, j, Color.FromArgb(pixel.R, pixel.G, Convert.ToByte(pixelElement.ToString(), 2)));
                        pixelElement.Clear();
                        index += 1;
                    }
                    if (index >= messageLength)
                    {
                        breakLoop = true;
                        break;
                    }
                }
            }

            return ConvertBitmapToByteArray(image);
        }

        public byte[] GetMessage(byte[] imageByte, out string fileName, out bool containsMessage)
        {
            Bitmap image = ConvertBitArrayToBitmap(imageByte);

            byte[] lsbArray = RetrieveAllLeastSignificantBits(image);
            image.Dispose();
            byte[] decodedMessage;
            short startIndex;

            if (StaticData.CheckIfAnyFileIsHidden(lsbArray))
            {
                fileName = StaticData.DecodeExtensionAndFileNameFromMsg(lsbArray);
                // + 3 because metadata about extension and name lenghts take 3 bytes.
                startIndex = (short)(fileName.Length + 3);
                decodedMessage = StaticData.DecodeMessage(lsbArray, startIndex, out containsMessage);
            }
            else
            {
                fileName = null;

                startIndex = 1;
                decodedMessage = StaticData.DecodeMessage(lsbArray, startIndex, out containsMessage);
            }


            return decodedMessage;
        }

        private byte[] ConvertBitmapToByteArray(Bitmap image)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, ImageFormat.Png);
                return memoryStream.ToArray();
            }
        }
        private Bitmap ConvertBitArrayToBitmap(byte[] imageByte)
        {
            Bitmap bitmap;
            using (var memoryStream = new MemoryStream(imageByte))
            {
                bitmap = new Bitmap(memoryStream);
            }

            return bitmap;
        }
        private byte[] RetrieveAllLeastSignificantBits(Bitmap image)
        {
            StringBuilder binaryData = new StringBuilder();
            int imageWidth = image.Width;
            int imageHeight = image.Height;

            for (int i = 0; i < imageWidth; i++)
            {
                for (int j = 0; j < imageHeight; j++)
                {
                    Color pixel = image.GetPixel(i, j);

                    binaryData.Append(Convert.ToString(pixel.R, 2).Last());
                    binaryData.Append(Convert.ToString(pixel.G, 2).Last());
                    binaryData.Append(Convert.ToString(pixel.B, 2).Last());
                }
            }

            int numberOfBytes = binaryData.Length / 8;
            byte[] bytes = new byte[numberOfBytes];
            string binaryDataAsString = binaryData.ToString();

            for (int i = 0; i < numberOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(binaryDataAsString.Substring(8 * i, 8), 2);
            }

            return bytes;
        }
       
    }
}
