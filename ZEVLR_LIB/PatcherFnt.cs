using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZEVLR_LIB.Common;

namespace ZEVLR_LIB
{
    public static class FntPatcher
    {
        static public void GenerateKrChars(string line, SortedSet<char> outList)
        {
            var reg = new Regex("\".*?\"");
            var matches = reg.Matches(line);
            foreach (var item in matches)
            {
                foreach (var ch in item.ToString())
                {
                    if (ch >= CharCodes.Kr.Min && ch <= CharCodes.Kr.Max)
                    {
                        outList.Add(ch);
                    }
                }
            }
        }

        static public string UnpackFonts(string srcDirPath, string dstDirPath)
        {
            int unpacked = 0;
            foreach (var filePath in Directory.GetFiles(srcDirPath, "*.fnt"))
            {
                var raw = ZEResFontRawData.Read(filePath);
                //XmlEx.Write(raw, Path.Combine(dstDirPath, Path.GetFileNameWithoutExtension(filePath) + ".org.xml"));
                var ctx = ZEResFontContext.Create(raw);
                XmlEx.Write(ctx, Path.Combine(dstDirPath, Path.GetFileNameWithoutExtension(filePath) + ".xml"));
                ctx.WritePng(Path.Combine(dstDirPath, Path.GetFileNameWithoutExtension(filePath) + ".png"));
                unpacked++;
            }
            return $"unpacked {unpacked} font files.";
        }
        static public string PatchFonts(BMFontPackage bmfPkg, string srcDirPath, string dstDirPath)
        {
            int patched = 0;
            foreach (var filePath in Directory.GetFiles(srcDirPath, "*.fnt"))
            {
                var raw = ZEResFontRawData.Read(filePath);
                var ctx = ZEResFontContext.Create(raw);
                ctx.Patch(bmfPkg);
                ctx.GenerateRawData().WriteBinary(Path.Combine(dstDirPath, Path.GetFileName(filePath)));
                patched++;
            }
            return $"patched {patched} font files.";
        }
    }
}
