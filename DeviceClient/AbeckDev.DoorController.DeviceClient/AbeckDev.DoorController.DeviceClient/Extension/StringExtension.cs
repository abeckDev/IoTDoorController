using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AbeckDev.DoorController.DeviceClient.Extension
{
    public static class StringExtension
    {
        public static async Task<string> RunProcessAsync(this string commandPath, string argument, CancellationToken cancellationToken = default)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = commandPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.StartInfo.ArgumentList.Add(argument);
            process.Start();
            string standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string standardError = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Command '{commandPath}' failed with exit code {process.ExitCode}: {standardError}");
            }

            return standardOutput;
        }
    }
}
