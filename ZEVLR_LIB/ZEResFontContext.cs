using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    public partial class ZEResFontContext
    {
        public class Character
        {
            [XmlAttribute(DataType = "hexBinary")] public byte[] Code { get; set; } = new byte[2];
            [XmlAttribute] public string Ch { get; set; }
            [XmlAttribute(DataType = "hexBinary")] public byte[] Unknown { get; set; } = new byte[11];
            [XmlAttribute] public int DefNo { get; set; }
            [XmlAttribute] public int DataNo { get; set; }
            [XmlAttribute] public sbyte Left { get; set; }
            [XmlAttribute] public sbyte Top { get; set; }
            [XmlAttribute] public sbyte H { get; set; }
            [XmlAttribute] public sbyte Right { get; set; }
            [XmlAttribute] public sbyte W { get; set; }
            [XmlAttribute] public int PixLen { get; set; }
            [XmlAttribute(DataType = "hexBinary")] public byte[] Pixels { get; set; }

            public void SetCode(uint code)
            {
                var bytes = BitConverter.GetBytes(code);
                Code[0] = bytes[1];
                Code[1] = bytes[0];

                Ch = Encoding.Unicode.GetString(bytes);
                Ch = Ch.TrimEnd('\0');
            }
        }

        public uint FontType { get; set; }
        public int Unknown01 { get; set; }
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public int CharCount { get; set; }
        public int Unknown02 { get; set; }
        public int Unknown03 { get; set; }
        public int Unknown04 { get; set; }
        public int Unknown05 { get; set; }
        [XmlElement(DataType = "hexBinary")] public byte[] Unknown06 { get; set; } = new byte[19];
        [XmlElement(DataType = "hexBinary")] public byte[] Unknown07 { get; set; } = new byte[4];

        public MinMaxVlaue<sbyte> MinMaxBottom;
        public MinMaxVlaue<sbyte> MinMaxTop;
        public MinMaxVlaue<sbyte> MinMaxH;
        public MinMaxVlaue<sbyte> MinMaxAxisW;
        public MinMaxVlaue<sbyte> MinMaxW;
        public MinMaxVlaue<int> MinMaxData;

        public List<Character> Characters = new();

        [XmlIgnore] public SortedDictionary<uint, Character> ChCodeMap = new();
    }

    public partial class ZEResFontContext
    {
        public static ZEResFontContext Create(ZEResFontRawData raw)
        {
            var ctx = new ZEResFontContext();
            ctx.FontType = raw.FontType;
            ctx.Name1 = raw.Name1;
            ctx.Name2 = raw.Name2;
            ctx.CharCount = raw.CharCount;
            ctx.Unknown01 = raw.Unknown01;
            ctx.Unknown02 = raw.Unknown02;
            ctx.Unknown03 = raw.Unknown03;
            ctx.Unknown04 = raw.Unknown04;
            ctx.Unknown05 = raw.Unknown05;
            raw.Unknown06.CopyTo(ctx.Unknown06, 0);
            raw.Unknown07.CopyTo(ctx.Unknown07, 0);

            foreach (var chRaw in raw.CharDatas)
            {
                var ch = new Character();
                ch.Left = chRaw.Left;
                ch.Top = chRaw.Top;
                ch.H = chRaw.H;
                ch.Right = chRaw.Right;
                ch.W = chRaw.W;
                chRaw.Unknown.CopyTo(ch.Unknown, 0);

                if (chRaw.DataLen > 13)
                {
                    var decoder = new SevenZip.Compression.LZMA.Decoder();
                    var lzmaProp = new byte[5];
                    Buffer.BlockCopy(chRaw.Data, 0, lzmaProp, 0, 5);
                    decoder.SetDecoderProperties(lzmaProp);

                    long compressedSize = chRaw.DataLen - 5;

                    using var outStream = new MemoryStream();
                    using var inStream = new MemoryStream(chRaw.Data, 5, (int)compressedSize);
                    decoder.Code(inStream, outStream, compressedSize, -1, null);
                    ch.Pixels = outStream.ToArray();
                    ch.PixLen = ch.Pixels.Length;
                }
                else
                {
                    ch.PixLen = chRaw.DataLen;
                    ch.Pixels = new byte[ch.PixLen];
                    chRaw.Data.CopyTo(ch.Pixels, 0);
                }

                ctx.Characters.Add(ch);
                ctx.MinMaxBottom.Update(ch.Left);
                ctx.MinMaxTop.Update(ch.Top);
                ctx.MinMaxH.Update(ch.H);
                ctx.MinMaxAxisW.Update(ch.Right);
                ctx.MinMaxW.Update(ch.W);
                ctx.MinMaxData.Update(ch.PixLen);
            }

            int no = 1;
            foreach (var chRaw in raw.CharNos)
            {
                if ((int)chRaw.No <= ctx.Characters.Count)
                {
                    var ch = ctx.Characters[(int)chRaw.No - 1];
                    ch.DefNo = no;
                    ch.DataNo = (int)chRaw.No;
                    ch.SetCode(chRaw.Code);
                    ctx.ChCodeMap.Add(chRaw.Code, ch);
                }
                else
                {
                    throw new Exception($"Invalid Charactor No. code={chRaw.Code}, no={chRaw.No})");
                }
                no++;
            }
            ctx.Characters = ctx.ChCodeMap.Values.ToList();

            return ctx;
        }

        public void Patch(BMFontPackage bmfPkg)
        {
            if (!bmfPkg.Descryption.info.unicode)
            {
                throw new Exception("bmfont must use unicode!");
            }

            var isForcedZeroOffset = bmfPkg.IsForcedZeroOffset();
            var minYOffset = bmfPkg.GetCharMinYoffset();
            var paddings = bmfPkg.GetPaddingList();

            foreach (var chBmf in bmfPkg.Descryption.chars)
            {
                var chCode = (uint)chBmf.id;
                if (ChCodeMap.ContainsKey(chCode))
                    continue;

                int xoffset_mod = 0;
                int yoffset_mod = 0;
                int chFixedW = 0;
                int chFixedH = 0;
                if (isForcedZeroOffset)
                {
                    chFixedW = chBmf.width;
                    chFixedH = chBmf.height;
                }
                else
                {
                    xoffset_mod = Math.Max(chBmf.xoffset + paddings[3], 0);
                    yoffset_mod = Math.Max(chBmf.yoffset - minYOffset, 0);
                    chFixedW = Math.Max(xoffset_mod + chBmf.width, chBmf.xadvance + paddings[1] + paddings[3]);
                    chFixedH = yoffset_mod + chBmf.height;
                }

                var modW = 0;
                var modH = 0;

                modW = MinMaxW.Max;// Math.Max(MinMaxW.Max, chFixedW);
                modH = modW == chFixedW ? chFixedH : (int)(modW * chFixedH * 1.0f / chFixedW);

                var ch = new Character();
                ch.DefNo = CharCount + 1;
                ch.DataNo = CharCount + 1;
                ch.SetCode(chCode);
                ch.W = (sbyte)modW;
                ch.H = (sbyte)modH;
                ch.Left = (sbyte)(xoffset_mod + (ch.W * Global.Settings.Char.AddRatioLeft * 0.01f));
                ch.Right = (sbyte)(ch.W - xoffset_mod + (ch.W * Global.Settings.Char.AddRatioRight * 0.01f));
                ch.Top = (sbyte)(ch.H - yoffset_mod + (ch.W * Global.Settings.Char.AddRatioTop * 0.01f));
                ch.PixLen = ch.H * ch.W;
                ch.Pixels = new byte[ch.PixLen];
                Characters.Last().Unknown.CopyTo(ch.Unknown, 0);
                //ch.Unknown = new byte[] { 0x00, 0x01, 0x02, 0x80, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06 };

                var pixels = bmfPkg.PixelPages[chBmf.page];

                if (modW == chFixedW)
                {
                    for (int h = 0; h < ch.H; h++)
                    {
                        for (int w = 0; w < ch.W; w++)
                        {
                            ch.Pixels[h * ch.W + w] = pixels[chBmf.x + w + ((chBmf.y + h) * (bmfPkg.Descryption.common.scaleW))];
                        }
                    }
                }
                else
                {
                    for (int h = 0; h < ch.H; h++)
                    {
                        for (int w = 0; w < ch.W; w++)
                        {
                            var realw = (int)(w * 1.0f * chFixedW / modW);
                            var realh = (int)(h * 1.0f * chFixedH / modH);
                            ch.Pixels[h * ch.W + w] = pixels[chBmf.x + realw + ((chBmf.y + realh) * (bmfPkg.Descryption.common.scaleW))];
                        }
                    }
                }

                Characters.Add(ch);
                CharCount = Characters.Count;
                Unknown01 = CharCount + 1;
                Unknown05 = CharCount + 1;
            }
        }

        public ZEResFontRawData GenerateRawData()
        {
            var pak = new ZEResFontRawData();
            pak.FontType = FontType;
            pak.Name1 = Name1;
            pak.Name2 = Name2;
            pak.Unknown01 = Unknown01;
            pak.Unknown02 = Unknown02;
            pak.Unknown03 = Unknown03;
            pak.Unknown04 = Unknown04;
            pak.Unknown05 = Unknown05;
            Unknown06.CopyTo(pak.Unknown06, 0);
            Unknown07.CopyTo(pak.Unknown07, 0);
            pak.CharCount = CharCount;

            SevenZip.CoderPropID[] propIDs =
                            {
                    SevenZip.CoderPropID.DictionarySize,
                    SevenZip.CoderPropID.PosStateBits,
                    SevenZip.CoderPropID.LitContextBits,
                    SevenZip.CoderPropID.LitPosBits,
                    SevenZip.CoderPropID.EndMarker,
                };

            // these are the default properties, keeping it simple for now:
            object[] properties =
                    {
                    (Int32)(0x10000),
                    (Int32)(2),
                    (Int32)(3),
                    (Int32)(0),
                    true,
                };

            var encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            var chDicDef = new SortedDictionary<int, ZEResFontRawData.CharNo>();
            var chDicData = new SortedDictionary<int, ZEResFontRawData.CharData>();
            foreach (var ch in Characters)
            {
                byte[] bytes = new byte[4];
                bytes[0] = ch.Code[1];
                bytes[1] = ch.Code[0];
                chDicDef.Add(ch.DefNo, new ZEResFontRawData.CharNo { No = (uint)ch.DataNo, Code = BitConverter.ToUInt32(bytes) });

                var chdata = new ZEResFontRawData.CharData
                {
                    Left = ch.Left,
                    Top = ch.Top,
                    Right = ch.Right,
                    H = ch.H,
                    W = ch.W
                };
                ch.Unknown.CopyTo(chdata.Unknown, 0);
                if (ch.PixLen > 13)
                {
                    using MemoryStream strmInStream = new MemoryStream(ch.Pixels);
                    using MemoryStream strmOutStream = new MemoryStream();
                    encoder.WriteCoderProperties(strmOutStream);
                    encoder.Code(strmInStream, strmOutStream, ch.Pixels.Length, -1, null);
                    chdata.Data = strmOutStream.ToArray();
                }
                else
                {
                    chdata.Data = new byte[ch.PixLen];
                    ch.Pixels.CopyTo(chdata.Data, 0);
                }
                chdata.DataLen = (ushort)chdata.Data.Length;

                chDicData.Add(ch.DataNo, chdata);
            }

            pak.CharNos = chDicDef.Values.ToList();
            pak.CharDatas = chDicData.Values.ToList();

            return pak;
        }

        public void WritePng(string path)
        {
            var wcount = (int)Math.Sqrt(CharCount);
            wcount = wcount % 2 == 1 ? wcount + 1 : wcount;
            var hcount = ((CharCount / wcount) + (CharCount % wcount > 0 ? 1 : 0));
            var png_width = MinMaxW.Max * wcount;
            var png_height = MinMaxH.Max * hcount;

            using var bitmap = new Bitmap(png_width, png_height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Black);

            for (int i = 0; i < CharCount; i++)
            {
                var ch = Characters[i];

                if (ch.W == 0 || ch.H == 0)
                    continue;

                var iw = (i % wcount);
                var ih = (i / wcount);
                for (int j = 0; j < ch.PixLen; j++)
                {
                    var jw = (j % ch.W);
                    var jh = (j / ch.W);

                    var wtotal = (iw * MinMaxW.Max + jw);
                    var htotal = (ih * MinMaxH.Max + jh);

                    bitmap.SetPixel(wtotal, htotal, Color.FromArgb(ch.Pixels[j], ch.Pixels[j], ch.Pixels[j]));
                }
            }

            bitmap.Save(path, ImageFormat.Png);
        }
    }
}
