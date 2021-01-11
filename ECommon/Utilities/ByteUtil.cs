using System;
using System.Collections.Generic;
using System.Text;

namespace ECommon.Utilities
{
    public class ByteUtil
    {
        public static readonly byte[] ZeroLengthBytes = BitConverter.GetBytes(0);
        public static readonly byte[] EmptyBytes = new byte[0];

        //Encode编码
        public static void EncodeString(string data,out byte[] lengthBytes, out byte[] dataBytes)
        {
            if (data != null)
            {
                dataBytes = Encoding.UTF8.GetBytes(data);
                lengthBytes = BitConverter.GetBytes(dataBytes.Length);//将基本数据类型转换为字节数组，将字节数组转换为基本数据类型。
            }
            else
            {
                dataBytes = EmptyBytes;
                lengthBytes = ZeroLengthBytes;
            }
        }

        public static byte[] EncodeDateTime(DateTime data)
        {
            return BitConverter.GetBytes(data.Ticks);
        }

        public static string DecodeString(byte[] sourceBuffer,int startOffset,out int nextStartOffset)
        {
            return Encoding.UTF8.GetString(DecodeBytes(sourceBuffer, startOffset, out nextStartOffset));
        }

        public static short DecodeShort(byte[] sourceBuffer,int startOffset, out int nextStartOffset)
        {
            var 
        }
    }
}
