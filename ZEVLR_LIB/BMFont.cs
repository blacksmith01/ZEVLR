using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    [XmlRoot(ElementName = "font")]
    public class BMFontDescryption
    {
        public class Info
        {
            [XmlAttribute] public string face { get; set; }
            [XmlAttribute] public int size { get; set; }
            [XmlAttribute] public bool bold { get; set; }
            [XmlAttribute] public bool italic { get; set; }
            [XmlAttribute] public string charset { get; set; }
            [XmlAttribute] public bool unicode { get; set; }
            [XmlAttribute] public int stretchH { get; set; }
            [XmlAttribute] public bool smooth { get; set; }
            [XmlAttribute] public int aa { get; set; }
            [XmlAttribute] public string padding { get; set; }
            [XmlAttribute] public string spacing { get; set; }
            [XmlAttribute] public int outline { get; set; }
        }
        public class Common
        {
            [XmlAttribute] public int lineHeight { get; set; }
            [XmlAttribute(AttributeName ="base")] public int Base { get; set; }
            [XmlAttribute] public int scaleW { get; set; }
            [XmlAttribute] public int scaleH { get; set; }
            [XmlAttribute] public int pages { get; set; }
            [XmlAttribute] public int packed { get; set; }
            [XmlAttribute] public int alphaChnl { get; set; }
            [XmlAttribute] public int redChnl { get; set; }
            [XmlAttribute] public int greenChnl { get; set; }
            [XmlAttribute] public int blueChnl { get; set; }
        }

        [XmlType("page")]
        public class Page
        {
            [XmlAttribute] public int id { get; set; }
            [XmlAttribute] public string file { get; set; }
        }

        [XmlType("char")]
        public class Character
        {
            [XmlAttribute] public int id { get; set; }
            [XmlAttribute] public int x { get; set; }
            [XmlAttribute] public int y { get; set; }
            [XmlAttribute] public int width { get; set; }
            [XmlAttribute] public int height { get; set; }
            [XmlAttribute] public int xoffset { get; set; }
            [XmlAttribute] public int yoffset { get; set; }
            [XmlAttribute] public int xadvance { get; set; }
            [XmlAttribute] public int page { get; set; }
            [XmlAttribute] public int chnl { get; set; }
        }

        public Info info { get; set; }

        public Common common { get; set; }
        public List<Page> pages { get; set; }
        public List<Character> chars { get; set; }
    }

    public class BMFontPackage
    {
        public BMFontDescryption Descryption { get; set; } = new();
        public List<byte[]> PixelPages { get; set; } = new();

        static public BMFontPackage Load(string dscFilePath, List<string> imgFilePaths)
        {
            BMFontPackage package = new();

            package.Descryption = XmlEx.Deserialize<BMFontDescryption>(dscFilePath);

            foreach (var imgFilePath in imgFilePaths)
            {
                package.PixelPages.Add(BitmapEx.ReadGreyPixelData(imgFilePath));
            }

            return package;
        }

        public int GetCharMinYoffset()
	    {
		    if (Descryption.chars.Count == 0)
			    return 0;

		    int min_yoffset = Descryption.chars.First().yoffset;
		    foreach (var ch in Descryption.chars) {
			    min_yoffset = Math.Min(min_yoffset, ch.yoffset);
		    }
		    return min_yoffset;
	    }

        public List<int> GetPaddingList()
        {
            return Descryption.info.padding.Split(',').ToList().ConvertAll(x => int.Parse(x));
        }
        public bool IsForcedZeroOffset()
        {
            var paddings = GetPaddingList();
            foreach (var ch in Descryption.chars)
            {
                if (ch.xoffset + paddings[0] != 0 || ch.yoffset + paddings[3] != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
