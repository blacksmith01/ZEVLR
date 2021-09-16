using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZEVLR_LIB
{
    public static class Global
    {
        public static AppSettings Settings { get; private set; }
        public static void Init()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build().Get<AppSettings>();
        }
    }
}
