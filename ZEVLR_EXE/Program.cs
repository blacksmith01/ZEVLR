using System;
using System.Linq;
using System.Threading.Tasks;
using ZEVLR_LIB;

namespace ZEVLR_EXE
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Global.Init();

            await CommandLines.Execute(AppDomain.CurrentDomain.BaseDirectory, args);
        }
    }
}
