using SteganographyLogic.Enums;
using SteganographyLogic.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SteganographyLogic.Processors
{
    public class AudioProcessor : IAudioProcessor
    {
        public byte[] HideMessage(byte[] fullSong, byte[] message, string fileName = null)
        {
            int dataIndex = GetDataChunkIndex(fullSong);
            int dataCapacity;
            int headerPartOneCapacity;
            (dataCapacity, headerPartOneCapacity) = GetHeadersAndDataLengths(fullSong, dataIndex);


            byte[] data;
            byte[] headerPartOne;
            byte[] headerPartTwo;
            (data, headerPartOne, headerPartTwo) = ExtractHeadersAndData(fullSong, dataCapacity, headerPartOneCapacity, dataIndex);

            string messageBinary;
            if (fileName != null)
            {
                messageBinary = StaticData.EmbedFileNameIntoMsgAndConvertToBin(message, fileName);
            }
            else
            {
                messageBinary = StaticData.AddMsgDelimAndConvertMsgToBin(message);
            }

            int index;
            int offset;
            FindBitOffsetAndSaveItInCarrierAudio(data, out index, messageBinary.Length, out offset);

            StringBuilder byteElement = new StringBuilder();
            foreach (char item in messageBinary)
            {
                byteElement.Append(Convert.ToString(data[index], 2).PadLeft(8, '0'));
                byteElement.Length--;
                byteElement.Insert(byteElement.Length, item);

                data[index] = Convert.ToByte(byteElement.ToString(), 2);
                byteElement.Clear();
                index += offset;
            }

            byte[] newSong;
            if (headerPartTwo == null)
            {
                newSong = headerPartOne.Concat(data).ToArray();
            }
            else
            {
                newSong = headerPartOne.Concat(data).Concat(headerPartTwo).ToArray();
            }

            return newSong;
        }

        public byte[] GetMessage(byte[] fullSong, out string fileName, out bool containsMessage)
        {
            byte[] data = GetDataFromAudioFile(fullSong);
            int offset = RetrieveOffsetFromData(data);

            byte[] lsbArray = RetrieveAllLeastSignificantBits(data, offset);
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

        private (int, int) GetHeadersAndDataLengths(byte[] fullSong, int dataChunkStartId)
        {
            //Info about data length in .wav is set in 4 bytes after data id which is word "data".
            byte[] dataCapacityInBytes = new byte[4];
            Array.Copy(fullSong, dataChunkStartId + 4, dataCapacityInBytes, 0, 4);
            //Data length info is written in little endian manner, it must be reversed to get proper value
            dataCapacityInBytes = dataCapacityInBytes.Reverse().ToArray();
            string byteToHex = BitConverter.ToString(dataCapacityInBytes).Replace("-", String.Empty);
            int dataCapacity = Convert.ToInt32(byteToHex, 16);

            int headerPartOneCapacity = dataChunkStartId + 8;

            return (dataCapacity, headerPartOneCapacity);
        }

        private int GetDataChunkIndex(byte[] fullSong)
        {
            byte[] dataChunkId = new byte[4];
            StringBuilder dataChunkIdString = new StringBuilder();

            for (int i = 0; i < fullSong.Length; i++)
            {
                if ((char)fullSong[i] == 'd' || (char)fullSong[i] == 'D')
                {
                    Array.Copy(fullSong, i, dataChunkId, 0, 4);

                    foreach (byte character in dataChunkId)
                    {
                        dataChunkIdString.Append((char)character);
                    }

                    if (dataChunkIdString.ToString().ToLowerInvariant() == "data")
                    {
                        return i;
                    }

                    dataChunkIdString.Clear();
                    Array.Clear(dataChunkId, 0, dataChunkId.Length);
                }
            }

            return 0;
        }

        public bool CheckIfFileIsWaveType(byte[] fullSong)
        {
            if (GetDataChunkIndex(fullSong) <= 0)
            {
                return false;
            }

            return true;
        }

        private (byte[], byte[], byte[]) ExtractHeadersAndData(byte[] fullSong, int dataCapacity, int headerPartOneCapacity, int dataChunkStartId)
        {
            byte[] data = new byte[dataCapacity];
            // + 8 because metadata about data chunk takes spaces of 8 bytes.
            Array.Copy(fullSong, dataChunkStartId + 8, data, 0, dataCapacity);

            byte[] headerPartOne = new byte[headerPartOneCapacity];
            Array.Copy(fullSong, 0, headerPartOne, 0, headerPartOneCapacity);

            int headerPartTwoLength = fullSong.Length - (data.Length + headerPartOne.Length);
            byte[] headerPartTwo = null;
            if (headerPartTwoLength > 0)
            {
                headerPartTwo = new byte[headerPartTwoLength];
                Array.Copy(fullSong, headerPartOne.Length + data.Length, headerPartTwo, 0, headerPartTwoLength);
            }

            return (data, headerPartOne, headerPartTwo);
        }
        private void FindBitOffsetAndSaveItInCarrierAudio(byte[] data, out int index, long messageLenght, out int offset)
        {
            // Offset is the longest distance between next bytes in carrier file on which we can save next bits of file intended to hide.
            // - 32 because 32 bytes from data are needed to store offset length.
            // First / 8 because we get number of bytes from string of bits
            // Second / 8 because we can save only one bit on every bite of carrier file.
            offset = (int)((data.Length - 32) / (messageLenght / 8)) / 8;
            index = 0;
            string offsetBits = Convert.ToString(offset, 2).PadLeft(32, '0');
            StringBuilder byteElement = new StringBuilder();
            for (int i = 0; i < offsetBits.Length; i++)
            {
                byteElement.Append(Convert.ToString(data[index], 2).PadLeft(8, '0'));
                byteElement.Length--;
                byteElement.Insert(byteElement.Length, offsetBits[i]);

                data[index] = Convert.ToByte(byteElement.ToString(), 2);
                byteElement.Clear();
                index += 1;
            }
        }
        private byte[] GetDataFromAudioFile(byte[] fullSong)
        {
            int dataIndex = GetDataChunkIndex(fullSong);
            (int dataLength, _) = GetHeadersAndDataLengths(fullSong, dataIndex);

            byte[] data = new byte[dataLength];
            Array.Copy(fullSong, dataIndex + 8, data, 0, dataLength);

            return data;
        }
        private int RetrieveOffsetFromData(byte[] data)
        {
            string offsetBitArray = String.Empty;
            for (int i = 0; i < 32; i++)
            {
                offsetBitArray += Convert.ToString(data[i], 2).Last();
            }
            int offset = Convert.ToInt32(offsetBitArray, 2);

            return offset;
        }
        private byte[] RetrieveAllLeastSignificantBits(byte[] data, int offset)
        {
            StringBuilder messageBits = new StringBuilder();

            // 32 because offset is stored in first 32 bytes (one bit on each byte)
            for (int i = 32; i < data.Length; i += offset)
            {
                messageBits.Append(Convert.ToString(data[i], 2).Last());
            }

            int numberOfBytes = messageBits.Length / 8;
            byte[] bytes = new byte[numberOfBytes];
            string binaryDataAsString = messageBits.ToString();

            for (int i = 0; i < numberOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(binaryDataAsString.Substring(8 * i, 8), 2);
            }

            return bytes;
        }
    }
}
