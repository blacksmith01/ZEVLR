using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;

namespace ZEVLR_LIB.Common
{
    public static class BitOperationEx
    {
        public static void ToValue(byte[] bytes, Int64 offset, out uint output)
        {
            output = 0;
            output |= ((uint)bytes[offset + 0]);
            output |= ((uint)bytes[offset + 1]) << 0x08;
            output |= ((uint)bytes[offset + 2]) << 0x10;
            output |= ((uint)bytes[offset + 3]) << 0x18;
        }
        public static void SetValue(byte[] bytes, uint value, Int64 offset)
        {
            bytes[offset + 0] = (byte)(value);
            bytes[offset + 1] = (byte)(value >> 0x08);
            bytes[offset + 2] = (byte)(value >> 0x10);
            bytes[offset + 3] = (byte)(value >> 0x18);
        }
    }

    public static class IEnumerableEx
    {
        public static int FindIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> func)
        {
            int idx = 0;
            foreach (var v in enumerable)
            {
                if (func(v))
                {
                    return idx;
                }
                idx++;
            }

            return -1;
        }
    }

    public static class ArrayEx
    {
        public static bool ByteCompare(ReadOnlySpan<byte> arr1, int offset1, ReadOnlySpan<byte> arr2, int offset2, int len)
        {
            return arr1.Slice(offset1, len).SequenceEqual(arr2.Slice(0, len));
        }
        public static bool ByteCompare<T>(ReadOnlySpan<byte> arr1, int offset1, ReadOnlySpan<T> arr2, int offset2, int len) where T : struct
        {
            return arr1.Slice(offset1, len).SequenceEqual(MemoryMarshal.Cast<T, byte>(arr2).Slice(0, len));
        }

        public static void Clear(Array arr)
        {
            Array.Clear(arr, 0, arr.Length);
        }
    }

    public class CharCodes
    {
        public class Kr
        {
            public const uint Min = 0xAC00;
            public const uint Max = 0xD7AF;
        }
    }

    public static class StringEx
    {
        public static byte[] HexStringToBytes(string hex)
        {
            byte[] convertArr = new byte[hex.Length / 2];
            for (int i = 0; i < convertArr.Length; i++)
            {
                convertArr[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return convertArr;
        }
    }

    public static class SizeOf<T> where T : struct
    {
        public static int Value = Marshal.SizeOf(default(T));
    }

    public struct MinMaxVlaue<T> where T : IComparable<T>
    {
        [XmlIgnore] public bool Initialized;
        [XmlAttribute] public T Min;
        [XmlAttribute] public T Max;

        public void Update(T value)
        {
            if (Initialized)
            {
                if (Min.CompareTo(value) > 0)
                    Min = value;
                if (Max.CompareTo(value) < 0)
                    Max = value;
            }
            else
            {
                Min = value;
                Max = value;
                Initialized = true;
            }
        }
    }
}
