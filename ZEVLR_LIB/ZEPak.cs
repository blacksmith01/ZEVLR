using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    public class ZEPak : IDisposable
    {
        public static readonly byte[] Sig = Encoding.UTF8.GetBytes("bin.");

        FileStream fStream;
        public string FilePath { get; private set; }
        public Int64 FileSize { get; private set; }

        public Int64 OffsetHeader1 { get; private set; }
        public Int64 OffsetHeader2 { get; private set; }
        public Int64 OffsetHeader3 { get; private set; }
        public Int64 OffsetHeader4 { get; private set; }
        public UInt32 OffsetNode { get; private set; }
        public Int64 OffsetFooter { get; private set; }
        public UInt32 MainKey { get; private set; }
        public Int64 StructureDataSize() { return OffsetHeader4 - OffsetHeader2; }

        public class Node
        {
            public const int PackSize = 8 + 4 * 6;

            public Int64 offset;
            public uint key;
            public uint size;
            public uint xsize;
            public uint id;
            public uint flags;
            public uint dummy;

            public void Set(Node other)
            {
                offset = other.offset;
                key = other.key;
                size = other.size;
                xsize = other.xsize;
                id = other.id;
                flags = other.flags;
                dummy = other.dummy;
            }
        };
        List<Node> nodes = new();
        public int NodeCount => nodes.Count;

        public ZEPak(string filePath)
        {
            this.FilePath = filePath;
            fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            FileSize = fStream.Length;
            MainKey = ZECrypt.GetMainKey(Path.GetFileNameWithoutExtension(filePath));
            ReadFile();
        }

        void ReadFile()
        {
            var tempBuf = new ResizableBuffer(32);
            Read(0, tempBuf);
            ZECrypt.XOR(MainKey, 0, tempBuf);

            if (!new ReadOnlySpan<byte>(Sig).SequenceEqual(new ReadOnlySpan<byte>(tempBuf.Bytes, 0, 4)))
            {
                return;
            }

            using var dr = new MemDataReader(new MemoryStream(tempBuf.Bytes, 4, 32 - 4));
            {
                dr.Read(out UInt32 header_offset1); OffsetHeader1 = header_offset1;
                dr.Read(out UInt32 header_offset2); OffsetHeader2 = header_offset2;
                dr.Read(out UInt64 header_offset3); OffsetHeader3 = (Int64)header_offset3;
                dr.Read(out UInt64 header_offset4); OffsetHeader4 = (Int64)header_offset4;
            }

            tempBuf.Set(StructureDataSize());

            Read(OffsetHeader2, tempBuf);
            ZECrypt.XOR(MainKey, OffsetHeader2, tempBuf);

            dr.Reset(new MemoryStream(tempBuf.Bytes, 0, (int)tempBuf.Length));

            OffsetNode = dr.Read<uint>();
            var node_count = dr.Read<uint>();

            dr.Reset(new MemoryStream(tempBuf.Bytes, (int)OffsetNode, (int)(tempBuf.Length - OffsetNode)));
            for (int i = 0; i < node_count; i++)
            {
                var n = new Node();
                dr.Read(out n.offset);
                dr.Read(out n.key);
                dr.Read(out n.size);
                dr.Read(out n.xsize);
                dr.Read(out n.id);
                dr.Read(out n.flags);
                dr.Read(out n.dummy);
                nodes.Add(n);
            }

            if (nodes.Count != 0)
            {
                OffsetFooter = GetNextNodeOffset(nodes.Last().offset, nodes.Last().size);
            }
            else
            {
                OffsetFooter = OffsetHeader4;
            }
        }

        public void Read(Int64 offset, ResizableBuffer buf)
        {
            Read(offset, new Span<byte>(buf.Bytes, 0, (int)buf.Length));
        }

        public void Read(Int64 offset, Span<byte> buf)
        {
            fStream.Seek(offset, SeekOrigin.Begin);
            fStream.Read(buf);
        }

        public bool RetrieveNode(int idx, Node n)
        {
            if (idx >= nodes.Count)
                return false;

            n.Set(nodes[idx]);

            return true;
        }

        public Int64 GetNextNodeOffset(Int64 offset, Int64 size)
        {
            return offset + size + GetNodePaddingSize(offset, size);
        }
        public Int64 GetNodePaddingSize(Int64 offset, Int64 size)
        {
            return (16 - ((offset + size) & 0x0F)) & 0x0F;
        }

        public void Dispose()
        {
            fStream?.Dispose();
        }

        public Int64 ComparePatchFileSizes(List<string> mod_file_paths, List<uint> mod_file_ids)
        {
            Int64 diff_size = 0;

            Int64 org_offset = 0;
            Int64 mod_offset = 0;
            foreach (var n in nodes)
            {
                Int64 add_size = n.size;
                org_offset = (int)GetNextNodeOffset(org_offset, add_size);

                var idIdx = mod_file_ids.FindIndex(x => x == n.id);
                if (idIdx >= 0)
                {
                    add_size = (uint)new FileInfo(mod_file_paths[idIdx]).Length;
                }
                mod_offset = (int)GetNextNodeOffset(mod_offset, add_size);
            }

            if (org_offset >= mod_offset)
            {
                diff_size = -(org_offset - mod_offset);
            }
            else
            {
                diff_size = (mod_offset - org_offset);
            }

            return diff_size;
        }
    }
}
