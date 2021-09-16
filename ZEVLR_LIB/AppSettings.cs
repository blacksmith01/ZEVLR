using System;
using System.Collections.Generic;
using System.Text;

namespace ZEVLR_LIB
{
    public class AppSettings
    {
        public class Character
        {
            public int AddRatioTop { get; set; }
            public int AddRatioLeft { get; set; }
            public int AddRatioRight { get; set; }
        }
        public string BinFileName { get; set; } = "ze2_data_jp.bin";

        public Character Char { get; set; }
    }
}
