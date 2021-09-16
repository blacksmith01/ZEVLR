using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ZEVLR_LIB.Common
{
    public class ProcessExec
    {
        public static bool Run(string ExeName, string arguments, string patchDirPath, out string result)
        {
            var sbOutput = new StringBuilder(1024);
            var sbError = new StringBuilder(1024);
            ProcessStartInfo processInfo;

            processInfo = new ProcessStartInfo(ExeName, arguments);
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.WorkingDirectory = patchDirPath;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            using var process = Process.Start(processInfo);
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {

                }
                else
                {
                    sbError.AppendLine(e.Data);
                }
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {

                }
                else
                {
                    sbOutput.AppendLine(e.Data);
                }
            };

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();

            var exitCode = process.ExitCode;

            var error = sbError.ToString();
            if (string.IsNullOrEmpty(error))
            {
                result = sbOutput.ToString();
                return true;
            }
            else
            {
                result = sbError.ToString();
                return false;
            }
        }
    }
}
