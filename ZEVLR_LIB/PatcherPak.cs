using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    public class PakPatcher
    {
        static public string UnpackResources(ZEPak pak, string dstFilePath)
        {
            uint[] unpacked = new uint[7]; // dds, lua, avi, ogg, dat
            uint zerosize = 0;

            List<string> exts = new() { "dds", "fnt", "fnt", "luac", "avi", "ogg", "dat" };
            List<byte[]> sigs = new();
            sigs.Add(Encoding.UTF8.GetBytes("DDS"));
            sigs.Add(new byte[] { 0x39, 0x00, 0x00, 0x00 });
            sigs.Add(new byte[] { 0x79, 0x00, 0x00, 0x00 });
            sigs.Add(ZEResLuaBinFile.Sig);
            sigs.Add(Encoding.UTF8.GetBytes("RIFF"));
            sigs.Add(Encoding.UTF8.GetBytes("OggS"));
            sigs.Add(Encoding.UTF8.GetBytes(""));

            if (!Directory.Exists(dstFilePath))
                Directory.CreateDirectory(dstFilePath);

            foreach (var ext in exts)
                Directory.CreateDirectory(Path.Combine(dstFilePath, ext));

            var taskArr = new Task[Environment.ProcessorCount];
            for (int i = 0; i < taskArr.Length; i++)
            {
                int idx = i;
                taskArr[i] = Task.Run(() =>
                {
                    var fsPak = new FileStream(pak.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var buffer = new ResizableBuffer(1024 * 1024 * 10);
                    var n = new ZEPak.Node();
                    for (int inode = idx; inode < pak.NodeCount; inode += taskArr.Length)
                    {
                        if (!pak.RetrieveNode(inode, n))
                        {
                            throw new Exception($"!node, {inode}");
                        }

                        int ext_idx = 5;

                        if (n.size > 0)
                        {

                            //static void ReadNodeMT(Stream stream, Node n, Span<byte> buffer)
                            //{
                            //    stream.Read(buffer);
                            //    ZECrypt.XOR(n.key, 0, buffer);
                            //}
                            buffer.Set(n.size);
                            fsPak.Seek(pak.OffsetHeader4 + n.offset, SeekOrigin.Begin);
                            fsPak.Read(buffer.Bytes);
                            ZECrypt.XOR(n.key, 0, buffer);

                            for (int iext = 0; iext < 5; iext++)
                            {
                                var sig = sigs[iext];
                                if (ArrayEx.ByteCompare(buffer.Bytes, 0, sig, 0, sig.Length))
                                {
                                    ext_idx = iext;
                                    break;
                                }
                            }

                            var nFilePath = Path.Combine(dstFilePath, exts[ext_idx], $"{n.id:x8}." + exts[ext_idx]);
                            using var fsNode = new FileStream(nFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                            fsNode.Write(buffer.Bytes, 0, (int)buffer.Length);

                            Interlocked.Increment(ref unpacked[ext_idx]);
                        }
                        else
                        {
                            Interlocked.Increment(ref zerosize);
                        }
                    }
                });
            }
            Task.WaitAll(taskArr);

            return $"Unpacked {unpacked[0]} dds, {unpacked[1] + unpacked[2]} fnt, {unpacked[3]} luac, {unpacked[4]} avi, {unpacked[5]} ogg, {unpacked[6]} dat.";
        }

        static public string UpdateFiles(ZEPak pak, List<string> mod_file_paths, string dstFilePath)
        {
            var mod_file_ids = new List<uint>(mod_file_paths.Count);
            {
                var idarr = new byte[4];
                foreach (var p in mod_file_paths)
                {
                    var filename = Path.GetFileNameWithoutExtension(p);
                    var bytes = StringEx.HexStringToBytes(filename);
                    ArrayEx.Clear(idarr);
                    var len = Math.Min(4, bytes.Length);
                    for (int i = 0; i < len; i++)
                    {
                        idarr[i] = bytes[3 - i];
                    }
                    mod_file_ids.Add(BitConverter.ToUInt32(idarr));
                }
            }

            Int64 diff_size = pak.ComparePatchFileSizes(mod_file_paths, mod_file_ids);

            int patched_count = 0;
            File.Delete(dstFilePath);
            using var fWriter = new BinaryWriter(new FileStream(dstFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));

            var tempBuf = new ResizableBuffer(pak.OffsetHeader2);
            pak.Read(0, tempBuf);
            fWriter.Write(tempBuf);

            pak.Read(pak.OffsetHeader2, tempBuf.Set(pak.StructureDataSize()));
            ZECrypt.XOR(pak.MainKey, pak.OffsetHeader2, tempBuf);
            fWriter.Write(tempBuf);

            fWriter.BaseStream.Seek(pak.OffsetHeader4, SeekOrigin.Begin);

            tempBuf.Set(1024 * 1024 * 10);
            var nodeCount = pak.NodeCount;
            Int64 ndoe_offset_alloc = 0;
            var node = new ZEPak.Node();
            for (int i = 0; i < nodeCount; i++)
            {
                if (!pak.RetrieveNode(i, node))
                {
                    throw new Exception($"!Node, {i}");
                }

                var idx = mod_file_ids.FindIndex(x => x == node.id);
                if (idx >= 0)
                {
                    FileEx.Read(mod_file_paths[idx], tempBuf);
                    ZECrypt.XOR(node.key, 0, tempBuf);
                    node.size = (uint)tempBuf.Length;
                    patched_count++;
                }
                else
                {
                    tempBuf.Set(node.size);
                    pak.Read(node.offset + pak.OffsetHeader4, tempBuf);
                }

                fWriter.BaseStream.Seek(ndoe_offset_alloc + pak.OffsetHeader4, SeekOrigin.Begin);
                fWriter.Write(tempBuf);

                node.offset = ndoe_offset_alloc;

                ndoe_offset_alloc += node.size;
                var padding = pak.GetNodePaddingSize(node.offset, node.size);
                if (padding > 0)
                {
                    fWriter.BaseStream.Seek(padding, SeekOrigin.Current);
                }
                ndoe_offset_alloc += padding;

                fWriter.BaseStream.Seek(pak.OffsetHeader2 + pak.OffsetNode + (i * ZEPak.Node.PackSize), SeekOrigin.Begin);
                fWriter.Write(node.offset);
                fWriter.Write(node.key);
                fWriter.Write(node.size);
                fWriter.Write(node.xsize);
                fWriter.Write(node.id);
                fWriter.Write(node.flags);
                fWriter.Write(node.dummy);
            }

            fWriter.BaseStream.Seek(pak.OffsetHeader2, SeekOrigin.Begin);
            ZECrypt.XOR(pak.MainKey, fWriter.BaseStream as FileStream, pak.OffsetHeader2, (int)pak.StructureDataSize());

            pak.Read(pak.OffsetHeader4 + pak.OffsetFooter, tempBuf.Set(pak.FileSize - pak.OffsetHeader4 - pak.OffsetFooter));
            fWriter.BaseStream.Seek(pak.OffsetHeader4 + ndoe_offset_alloc, SeekOrigin.Begin);
            fWriter.Write(tempBuf);

            return $"patched {patched_count} files.";
        }

    }
}
