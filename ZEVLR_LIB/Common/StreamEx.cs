using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ZEVLR_LIB.Common
{

    public class MemDataReader : IDisposable
    {
        BinaryReader br;
        byte[] buf;
        public int Offset { get; private set; }

        public MemDataReader(string path)
        {
            br = new BinaryReader(File.OpenRead(path));
        }
        public MemDataReader(Stream stream)
        {
            br = new BinaryReader(stream);
        }

        public void Dispose()
        {
            br?.Dispose();
        }

        public void Reset(Stream stream)
        {
            Dispose();
            br = new BinaryReader(stream);
        }

        void EnsureBuf(int size)
        {
            if (buf == null || buf.Length < size)
                buf = new byte[size];
        }

        public void Read<T>(out T value) where T : struct
        {
            value = Read<T>();
        }

        public T Read<T>() where T : struct
        {
            var size = SizeOf<T>.Value;
            EnsureBuf(size);
            br.Read(buf, 0, size);
            Offset += size;
            return MemoryMarshal.Cast<byte, T>(new ReadOnlySpan<byte>(buf, 0, size))[0];
        }
        public void ReadString(out string value)
        {
            value = ReadString();
        }
        public string ReadString()
        {
            EnsureBuf(SizeOf<int>.Value);
            Read<int>(out var len);

            EnsureBuf(len);
            br.Read(buf, 0, len);

            Offset += len;
            return Encoding.UTF8.GetString(buf, 0, len);
        }

        public void ReadBytes(byte[] bytes)
        {
            br.Read(bytes);
            Offset += bytes.Length;
        }
    }
}
