using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    public static class ZECrypt
    {
        public static uint nonary_crypt(ResizableBuffer buf, uint key, uint relative_offset)
        {
            var data = buf.Bytes;
            var size = data.Length;
            uint eax,
                ecx,
                edx,
                edi,
                esi;

            eax = relative_offset;
            esi = (relative_offset + 3) << 0x18;
            ecx = (relative_offset + 2) << 0x10;
            edi = (relative_offset + 1) << 0x8;

            Int64 i;
            for (i = 0; i < ((size / 4) * 4); i += 4)
            {
                edx = (ecx & 0xff0000) | (edi & 0xff00) | (esi & 0xff000000) | (eax & 0xff);
                eax += 0x4;
                edi += 0x400;
                ecx += 0x40000;
                esi += 0x4000000;
                BitOperationEx.ToValue(data, i, out var v);
                v ^= edx ^ key;
                BitOperationEx.SetValue(data, v, i);
            }
            for (; i < size; i++)
            {
                uint v = data[i];
                v ^= eax ^ key;
                data[i] = (byte)v;
                eax++;
                key >>= 8;
            }
            return eax;
        }

        public const uint ze1_key_header = 0xfabaceda;
        public static uint GetMainKey(string filename)
        {
            if (filename == "ze1_data")
            {
                return ze1_key_header;
            }
            else
            {
                return nonary_calculate_key("ZeroEscapeTNG");
            }
        }

        public static uint nonary_calculate_key(string name)
        {
            int i,
                size;
            uint eax = 0,
                esi = 0,
                edx = 0;

            size = name.Length;

            for (i = 0; i < ((size / 2) * 2); i += 2)
            {
                eax += (uint)name[i] + (uint)name[i + 1];
                edx = (uint)(name[i] & 0xdf) + (esi * 0x83);
                esi = (uint)(name[i + 1] & 0xdf) + (edx * 0x83);
            }
            for (; i < size; i++)
            {
                eax += name[i];
                esi = (uint)(name[i] & 0xdf) + (esi * 0x83);
            }
            return (eax & 0xf) | ((esi & 0x07FFFFFF) << 4);
        }

        public static void XOR(uint key, Int64 offset, ResizableBuffer data)
        {
            var xor_key = new ResizableBuffer(data.Length);
            nonary_crypt(xor_key, key, (uint)offset);
            var xor_bytes = xor_key.Bytes;
            var key_len = xor_key.Length;
            for (Int64 i = 0; i < data.Length; i++)
            {
                data.Bytes[i] ^= xor_bytes[i % key_len];
            }
        }

        public static void XOR(uint key, FileStream stream, Int64 offset, Int64 size)
        {
            var xor_key = new ResizableBuffer(size);
            nonary_crypt(xor_key, key, (uint)offset);
            var xor_bytes = xor_key.Bytes;
            var key_len = xor_key.Length;
            for (Int64 i = 0; i < size; i++)
            {
                var v = (byte)stream.ReadByte();
                v ^= xor_bytes[i % key_len];

                stream.Seek(-1, SeekOrigin.Current);
                stream.WriteByte(v);
            }
        }
    }
}
