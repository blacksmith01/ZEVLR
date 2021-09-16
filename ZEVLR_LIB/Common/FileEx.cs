using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZEVLR_LIB.Common
{
    public static class FileEx
    {
        public static void Read(string filePath, ResizableBuffer buffer)
        {
            using var fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fStream.Read(buffer.Set(fStream.Length));
        }
        public static void Read(string filePath, Span<byte> buffer)
        {
            using var fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fStream.Read(buffer);
        }

        public static void Write(this BinaryWriter writer, ResizableBuffer buffer)
        {
            writer.Write(buffer.Bytes, 0, (int)buffer.Length);
        }

        public static void Read(this FileStream fstream, ResizableBuffer buffer)
        {
            fstream.Read(buffer.Bytes, 0, (int)buffer.Length);
        }
    }
}
