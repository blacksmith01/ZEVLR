using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ZEVLR_LIB
{
    public class DlgPatcher
    {
        public static IEnumerable<string> RetrieveLuaFilePaths(string rootPath)
        {
            var dirList = Directory.GetFiles(rootPath);
            var luaList = new List<string>(dirList.Length);
            var buffer = new byte[ZEResLuaBinFile.Sig.Length];
            foreach (var filepath in dirList)
            {
                using var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Read(buffer) < ZEResLuaBinFile.Sig.Length)
                    continue;

                if (!buffer.SequenceEqual(ZEResLuaBinFile.Sig))
                    continue;

                luaList.Add(filepath);
            }
            return luaList;
        }

        public static string DecompileLua(string srcFilePath, string patchDirPath, string dstFilePath)
        {
            var cmd = $"-jar unluac.jar --rawstring --opmap opmap.txt \"{srcFilePath}\" --output \"{dstFilePath}\"";

            var sb = new StringBuilder(1024);
            ProcessStartInfo processInfo;

            processInfo = new ProcessStartInfo("java", cmd);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.WorkingDirectory = patchDirPath;
            processInfo.RedirectStandardError = true;

            using var process = Process.Start(processInfo);
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {

                }
                else
                {
                    sb.Append(e.Data);
                }
            };

            process.BeginErrorReadLine();

            process.WaitForExit();

            var exitCode = process.ExitCode;

            return sb.ToString();
        }

        static public string DecompileLuaAll(string datSrcPath, string datDstPath, string patchDirPath)
        {
            var sucs = new ConcurrentBag<string>();

            var cts = new CancellationTokenSource();
            var taskArr = new Task[Environment.ProcessorCount];
            var srcFilePaths = Directory.GetFiles(datSrcPath, "*.luac");
            for (int itask = 0; itask < taskArr.Length; itask++)
            {
                var taskIdx = itask;
                taskArr[itask] = Task.Run(() =>
                {
                    for (int ifile = taskIdx; ifile < srcFilePaths.Length; ifile += taskArr.Length)
                    {
                        if (cts.IsCancellationRequested)
                            break;

                        var srcFilePath = srcFilePaths[ifile];
                        var dstFilePath = Path.Combine(datDstPath, Path.GetFileNameWithoutExtension(srcFilePath) + ".lua");
                        if (!RunExec("java", $"-jar unluac.jar --rawstring --opmap opmap.txt \"{srcFilePath}\" --output \"{dstFilePath}\"", patchDirPath, out var result))
                        {
                            sucs.Add(srcFilePath);
                        }
                        else
                        {
                            cts.Cancel();
                            File.Delete(dstFilePath);

                            throw new Exception($"[Err] failed decompile {srcFilePath} file.");
                        }
                    }
                });
            }

            Task.WaitAll(taskArr);

            return $"unpacked {sucs.Count} dlg files.";
        }

        static public string RecompileLuaAll(string datSrcPath, string datDstPath, string patchDirPath)
        {
            var tempDirPath = Path.Combine(patchDirPath, ".temp");
            Directory.Delete(tempDirPath, true);
            Directory.CreateDirectory(tempDirPath);

            var opcodes = File.ReadAllText(Path.Combine(patchDirPath, "opmap.txt"));

            int sucs = 0;

            var cts = new CancellationTokenSource();
            var taskArr = new Task[Environment.ProcessorCount];
            var srcFilePaths = Directory.GetFiles(datSrcPath, "*.lua");
            for (int itask = 0; itask < taskArr.Length; itask++)
            {
                var taskIdx = itask;
                taskArr[itask] = Task.Run(() =>
                {
                    for (int ifile = taskIdx; ifile < srcFilePaths.Length; ifile += taskArr.Length)
                    {
                        if (cts.IsCancellationRequested)
                            break;

                        var srcFilePath = srcFilePaths[ifile];
                        var normalLuacFilePath = Path.Combine(tempDirPath, $"{taskIdx}.normal.luac");
                        if (!RunExec(Path.Combine(patchDirPath, "luac5.1.exe"), $"-o {normalLuacFilePath} {srcFilePath}", patchDirPath, out var result))
                        {
                            cts.Cancel(); throw new Exception($"[Err] failed compile {srcFilePath} file.");
                        }

                        if (!RunExec("java", $"-jar unluac.jar --rawstring --disassemble \"{normalLuacFilePath}\"", patchDirPath, out result))
                        {
                            cts.Cancel(); throw new Exception($"[Err] failed dissasemble {srcFilePath} file.");
                        }

                        var disassembledFilePath = Path.Combine(tempDirPath, $"{taskIdx}.disasm");
                        {
                            var mainFunctionPos = result.IndexOf(".function	main");
                            if (mainFunctionPos < 0)
                            {
                                cts.Cancel(); throw new Exception($"[Err] failed find main function in {srcFilePath} file.");
                            }
                            result = result.Insert(mainFunctionPos, $"{opcodes}\r\n\r\n");
                            File.WriteAllText(disassembledFilePath, result);
                        }

                        var dstFilePath = Path.Combine(datDstPath, $"{Path.GetFileNameWithoutExtension(srcFilePath)}.luac");
                        if (!RunExec("java", $"-jar unluac.jar --rawstring --assemble \"{disassembledFilePath}\" --output \"{dstFilePath}\"", patchDirPath, out result))
                        {
                            cts.Cancel(); throw new Exception($"[Err] failed assemble {srcFilePath} file.");
                        }

                        Interlocked.Increment(ref sucs);
                    }
                });
            }

            Task.WaitAll(taskArr);

            return $"unpacked {sucs} dlg files.";
        }
    }
}
