using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZEVLR_LIB
{
    public class CommandLines
    {
        public static readonly Dictionary<string, Func<string, string[], Task<bool>>> Cmds = new()
        {
            { "unpack", Cmd_UnpackBin },
            { "unpack-fnt", Cmd_UnpackFnt },
            { "unpack-dlg", Cmd_UnpackDlg },
            { "krlist", Cmd_ExportKrCharList },
            { "patch", Cmd_Patch },
            { "repack", Cmd_Repack },
        };

        public static async Task<bool> Execute(string rootDirPath, string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("[Err] not enough arguments.");
                return false;
            }

            var cmd = CommandLines.Cmds.Where(p => p.Key == args[0]).FirstOrDefault();
            if (cmd.Value == null)
            {
                Console.WriteLine("[Err] invalid commands.");
                return false;
            }

            if (!await cmd.Value(rootDirPath, args))
            {
                Console.WriteLine($"[Err] command {args[0]} failed.");
                return false;
            }

            return true;
        }

        public static async Task<bool> Cmd_UnpackBin(string rootDirPath, string[] args)
        {
            try
            {
                var srcFilePath = Path.Combine(rootDirPath, "org_bin", Global.Settings.BinFileName);
                if (!File.Exists(srcFilePath))
                {
                    throw new Exception($"[Err] {srcFilePath} file not exist.");
                }

                var unpackDirPath = Path.Combine(rootDirPath, "org_res");

                await Task.Run(() =>
                {
                    {
                        var pak = new ZEPak(srcFilePath);
                        var result = PakPatcher.UnpackResources(pak, unpackDirPath);
                        Console.WriteLine(result);
                    }

                    {
                        var result = FntPatcher.UnpackFonts(
                            Path.Combine(unpackDirPath, "fnt"),
                            Path.Combine(rootDirPath, "org_fnt"));
                        Console.WriteLine(result);
                    }

                    {
                        var result = DlgPatcher.DecompileLuaAll(
                            Path.Combine(unpackDirPath, "luac"),
                            Path.Combine(rootDirPath, "org_dlg"),
                            rootDirPath);
                        Console.WriteLine(result);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }

        public static async Task<bool> Cmd_UnpackFnt(string rootDirPath, string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    throw new Exception($"[Err] incorrect srcFilePath.");
                }
                var srcDirPath = args[1];

                if (args.Length < 3)
                {
                    throw new Exception($"[Err] incorrect dstFilePath.");
                }
                var dstDirPath = args[2];


                await Task.Run(() =>
                {
                    {
                        var result = FntPatcher.UnpackFonts(srcDirPath, dstDirPath);
                        Console.WriteLine(result);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }

        public static async Task<bool> Cmd_UnpackDlg(string rootDirPath, string[] args)
        {
            try
            {
                if (args.Length < 2)
                {
                    throw new Exception($"[Err] incorrect srcFilePath.");
                }
                var srcDirPath = args[1];

                if (args.Length < 3)
                {
                    throw new Exception($"[Err] incorrect dstFilePath.");
                }
                var dstDirPath = args[2];


                await Task.Run(() =>
                {
                    {
                        var result = DlgPatcher.DecompileLuaAll(srcDirPath, dstDirPath, rootDirPath);
                        Console.WriteLine(result);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }

        public static async Task<bool> Cmd_ExportKrCharList(string rootDirPath, string[] args)
        {
            try
            {
                var srcDirPath = Path.Combine(rootDirPath, "mod_dlg");
                var dstFilePath = Path.Combine(rootDirPath, "kr.txt");

                SortedSet<char> outList = new();
                foreach (var filePath in Directory.GetFiles(srcDirPath, "*.lua"))
                {
                    using var reader = new StreamReader(filePath);
                    string line = string.Empty;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        FntPatcher.GenerateKrChars(line, outList);
                    }
                }
                await File.WriteAllTextAsync(dstFilePath, new string(outList.Count == 0 ? "" : outList.ToArray()), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }
        public static async Task<bool> Cmd_Patch(string rootDirPath, string[] args)
        {
            try
            {
                var orgFntPath = Path.Combine(rootDirPath, "org_res", "fnt");
                var orgDlgPath = Path.Combine(rootDirPath, "org_dlg");
                var modFntPath = Path.Combine(rootDirPath, "mod_fnt");
                var modDlgPath = Path.Combine(rootDirPath, "mod_dlg");
                var patchResPath = Path.Combine(rootDirPath, "patch_res");

                await Task.Run(() =>
                {
                    var bmfPkg = BMFontPackage.Load(Path.Combine(modFntPath, "kr.fnt"), Directory.GetFiles(modFntPath, "*.png").ToList());

                    {
                        var result = FntPatcher.PatchFonts(bmfPkg, orgFntPath, patchResPath);
                        Console.WriteLine(result);
                    }
                    {
                        var result = DlgPatcher.RecompileLuaAll(modDlgPath, patchResPath, rootDirPath);
                        Console.WriteLine(result);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }
        public static async Task<bool> Cmd_Repack(string rootDirPath, string[] args)
        {
            try
            {
                var srcFilePath = Path.Combine(rootDirPath, "org_bin", Global.Settings.BinFileName);
                if (!File.Exists(srcFilePath))
                {
                    throw new Exception($"[Err] {srcFilePath} file not exist.");
                }

                var patchFiles = Directory.GetFiles(Path.Combine(rootDirPath, "patch_res")).ToList();
                if (patchFiles.Count == 0)
                {
                    throw new Exception($"[Err] patch files not exist.");
                }

                var unpackDirPath = Path.Combine(rootDirPath, "org_res");

                await Task.Run(() =>
                {
                    {
                        var pak = new ZEPak(srcFilePath);
                        var result = PakPatcher.UpdateFiles(pak, patchFiles, Path.Combine(rootDirPath, "patch_bin", Global.Settings.BinFileName));
                        Console.WriteLine(result);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            return true;
        }
    }
}
