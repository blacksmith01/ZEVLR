using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    public partial class ZEResFontRawData
    {
        public static readonly List<string> FontInfoFileNames = new()
        {
            "000003c8"
        };
        public static readonly List<string> FontDataFileNames = new()
        {
            "00000608",
            "00000609",
            "0000060a",
            "0000060b",
            "0000060c",
            "0000060d",
            "0000060e",
            "0000060f",
            "00000610",
            "00000611",
            "00000612",
            "00000613",
            "00000614",
            "00000615",
            "00000616",
            "00000617",
            "00000618",
        };

        public class CharNo
        {
            [XmlAttribute] public uint No;
            [XmlAttribute] public uint Code;
        }
        public class CharData
        {
            [XmlAttribute] public sbyte Left;
            [XmlAttribute] public sbyte Top;
            [XmlAttribute] public sbyte H;
            [XmlAttribute] public sbyte Right;
            [XmlAttribute] public sbyte W;
            [XmlAttribute] public ushort DataLen;
            [XmlAttribute(DataType = "hexBinary")] public byte[] Unknown = new byte[11];
            [XmlAttribute(DataType = "hexBinary")] public byte[] Data;
        }

        public uint FontType;
        public int Unknown01;
        public string Name1;
        public string Name2;
        public int CharCount;
        public List<CharNo> CharNos = new();
        public int Unknown02;
        public int Unknown03;
        public int Unknown04;
        public int Unknown05;
        [XmlElement(DataType = "hexBinary")] public byte[] Unknown06 = new byte[19];
        [XmlElement(DataType = "hexBinary")] public byte[] Unknown07 = new byte[4];
        public List<CharData> CharDatas = new();
    }

    public partial class ZEResFontRawData
    {
        static public ZEResFontRawData Read(string filePath)
        {
            ZEResFontRawData pak = new();
            using var dr = new MemDataReader(filePath);
            dr.Read(out pak.FontType);
            dr.Read(out pak.Unknown01);
            dr.ReadString(out pak.Name1);
            dr.ReadString(out pak.Name2);
            dr.Read(out pak.CharCount);
            for (int i = 0; i < pak.CharCount; i++)
            {
                var charIdx = new CharNo();
                dr.Read(out charIdx.Code);
                dr.Read(out charIdx.No);
                pak.CharNos.Add(charIdx);
            }

            dr.Read(out pak.Unknown02);
            dr.Read(out pak.Unknown03);
            dr.Read(out pak.Unknown04);
            dr.Read(out pak.Unknown05);

            dr.ReadBytes(pak.Unknown06);

            for (int iChar = 0; iChar < pak.CharCount; iChar++)
            {
                CharData charData = new();
                dr.Read(out charData.H);
                dr.Read(out charData.Right);
                dr.Read(out charData.W);
                dr.Read(out charData.DataLen);

                charData.Data = new byte[charData.DataLen];
                dr.ReadBytes(charData.Data);

                dr.ReadBytes(charData.Unknown);

                dr.Read(out charData.Left);
                dr.Read(out charData.Top);

                pak.CharDatas.Add(charData);
            }

            dr.ReadBytes(pak.Unknown07);

            return pak;
        }

        public void WriteBinary(string path)
        {
            using var bw = new BinaryWriter(File.Open(path, FileMode.Create));
            bw.Write(FontType);
            bw.Write(Unknown01);
            bw.Write(Name1.Length);
            bw.Write(Encoding.UTF8.GetBytes(Name1));
            bw.Write(Name2.Length);
            bw.Write(Encoding.UTF8.GetBytes(Name2));
            bw.Write(CharCount);
            foreach (var charNo in CharNos)
            {
                bw.Write(charNo.Code);
                bw.Write(charNo.No);
            }

            bw.Write(Unknown02);
            bw.Write(Unknown03);
            bw.Write(Unknown04);
            bw.Write(Unknown05);

            bw.Write(Unknown06);

            foreach (var charData in CharDatas)
            {
                bw.Write(charData.H);
                bw.Write(charData.Right);
                bw.Write(charData.W);
                bw.Write(charData.DataLen);
                if (charData.DataLen > 0)
                    bw.Write(charData.Data);
                bw.Write(charData.Unknown);

                bw.Write(charData.Left);
                bw.Write(charData.Top);
            }

            bw.Write(Unknown07);
        }
    }
}
