using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DGame
{
    public static class ShellHelper
    {
        public static bool Run(string cmd, string workDir, List<string> environmentVars = null, int timeoutMs = 30000)
        {
            Process process = new Process();
            bool success = false;

            try
            {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                string app = "bash";
                string splitChar = ":";
                string arguments = "-c";
#elif UNITY_EDITOR_WIN
                string app = "cmd.exe";
                string splitChar = ";";
                string arguments = "/c";
#endif
                ProcessStartInfo startInfo = new ProcessStartInfo(app)
                {
                    Arguments = $"{arguments} \"{cmd}\"",
                    CreateNoWindow = true,
                    ErrorDialog = true,
                    UseShellExecute = false,
                    WorkingDirectory = workDir,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                };

                if (environmentVars != null && environmentVars.Count > 0)
                {
                    string pathVar = startInfo.EnvironmentVariables["PATH"];

                    if (pathVar == null)
                    {
                        pathVar = "";
                    }

                    foreach (var item in environmentVars)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            if (pathVar.Length > 0)
                            {
                                pathVar += splitChar;
                            }

                            pathVar += item;
                        }
                    }

                    startInfo.EnvironmentVariables["PATH"] = pathVar;
                }

                process.StartInfo = startInfo;

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                        UnityEngine.Debug.Log(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                        UnityEngine.Debug.LogError(args.Data);
                    }
                };

                UnityEngine.Debug.Log($"Executing: {cmd}");
                UnityEngine.Debug.Log($"Working Directory: {workDir}");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 使用 WaitForExit 而不是忙等待
                if (process.WaitForExit(timeoutMs))
                {
                    // 确保所有异步输出都完成
                    Thread.Sleep(100);
                    success = process.ExitCode == 0;

                    if (!success)
                    {
                        UnityEngine.Debug.LogError($"Process exited with code: {process.ExitCode}");
                        UnityEngine.Debug.LogError($"Error output: {error}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"Process timeout after {timeoutMs}ms");
                    process.Kill();
                }

                process.CancelOutputRead();
                process.CancelErrorRead();
                return success;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
            finally
            {
                process.Close();
                process.Dispose();
            }
        }

        public static async Task<bool> RunAsync(string cmd, string workDir, List<string> environmentVars = null,
            int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            Process process = new Process();
            bool success = false;

            try
            {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        string app = "bash";
        string splitChar = ":";
        string arguments = "-c";
#elif UNITY_EDITOR_WIN
                string app = "cmd.exe";
                string splitChar = ";";
                string arguments = "/c";
#endif

                ProcessStartInfo startInfo = new ProcessStartInfo(app)
                {
                    Arguments = $"{arguments} \"{cmd}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = workDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                if (environmentVars != null && environmentVars.Count > 0)
                {
                    string pathVar = startInfo.EnvironmentVariables["PATH"] ?? "";

                    foreach (var item in environmentVars)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            pathVar += splitChar + item;
                        }
                    }

                    startInfo.EnvironmentVariables["PATH"] = pathVar;
                }

                process.StartInfo = startInfo;

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                        UnityEngine.Debug.Log(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                        UnityEngine.Debug.LogError(args.Data);
                    }
                };

                UnityEngine.Debug.Log($"Executing: {cmd}");
                UnityEngine.Debug.Log($"Working Directory: {workDir}");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 使用异步等待
                using var timeoutCts = new CancellationTokenSource();

                using var linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var processTask = Task.Run(() =>
                {
                    process.WaitForExit();
                    return process.ExitCode;
                }, linkedCts.Token);

                var completedTask = await Task.WhenAny(processTask, Task.Delay(timeoutMs, linkedCts.Token));

                if (completedTask == processTask)
                {
                    int exitCode = await processTask;
                    success = exitCode == 0;

                    if (!success)
                    {
                        UnityEngine.Debug.LogError($"Process exited with code: {exitCode}");
                        UnityEngine.Debug.LogError($"Error output: {error}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"Process timeout after {timeoutMs}ms");
                    process.Kill();
                    timeoutCts.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.LogError("Process was cancelled");
                process.Kill();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                success = false;
            }
            finally
            {
                process.Close();
                process.Dispose();
            }

            return success;
        }

        public static bool RunByPath(string path, string arguments = null, string workingDirectory = null,
            int timeoutMs = 30000)
        {
            if (string.IsNullOrEmpty(path))
            {
                UnityEngine.Debug.LogError("Process path cannot be null or empty");
                return false;
            }

            if (!File.Exists(path))
            {
                UnityEngine.Debug.LogError($"Process not found at path: {path}");
                return false;
            }

            Process process = null;
            bool success = false;

            try
            {
                process = new Process();
                var startInfo = new ProcessStartInfo(path)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // 设置可选参数
                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    startInfo.WorkingDirectory = workingDirectory;
                }
                else
                {
                    // 默认使用可执行文件所在目录
                    startInfo.WorkingDirectory = Path.GetDirectoryName(path);
                }

                process.StartInfo = startInfo;

                // 使用 StringBuilder 收集输出以便调试
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                        UnityEngine.Debug.Log($"[Process Output]: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                        UnityEngine.Debug.LogError($"[Process Error]: {args.Data}");
                    }
                };

                // 启动进程
                UnityEngine.Debug.Log($"Starting process: {path}");

                if (!string.IsNullOrEmpty(arguments))
                {
                    UnityEngine.Debug.Log($"Arguments: {arguments}");
                }

                UnityEngine.Debug.Log($"Working Directory: {startInfo.WorkingDirectory}");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                UnityEngine.Debug.Log($"Started process with ID: {process.Id} for path: {path}");

                // 等待进程完成（原代码缺少等待逻辑）
                if (process.WaitForExit(timeoutMs))
                {
                    // 给输出流一些时间完成
                    Thread.Sleep(100);
                    success = process.ExitCode == 0;

                    if (success)
                    {
                        UnityEngine.Debug.Log($"Process completed successfully with exit code: {process.ExitCode}");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Process failed with exit code: {process.ExitCode}");

                        if (error.Length > 0)
                        {
                            UnityEngine.Debug.LogError($"Full error output:\n{error}");
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"Process timeout after {timeoutMs}ms");
                    process.Kill();
                    success = false;
                }

                return success;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error starting process at path {path}: {e.Message}");
                UnityEngine.Debug.LogException(e);
                return false;
            }
            finally
            {
                // 确保资源被正确释放
                if (process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }

                        process.CancelOutputRead();
                        process.CancelErrorRead();
                        process.Close();
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Error cleaning up process: {ex.Message}");
                    }
                }
            }
        }

        public static async Task<bool> RunByPathAsync(string path, string arguments = null, string workingDirectory = null, int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            Process process = null;

            try
            {
                process = new Process();
                var startInfo = new ProcessStartInfo(path)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                if (!string.IsNullOrEmpty(arguments))
                    startInfo.Arguments = arguments;

                startInfo.WorkingDirectory = !string.IsNullOrEmpty(workingDirectory) ? workingDirectory : Path.GetDirectoryName(path);

                process.StartInfo = startInfo;

                var tcs = new TaskCompletionSource<bool>();
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                        UnityEngine.Debug.Log($"[Process Output]: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                        UnityEngine.Debug.LogError($"[Process Error]: {args.Data}");
                    }
                };

                process.Exited += (_, __) => { tcs.TrySetResult(true); };

                process.EnableRaisingEvents = true;

                UnityEngine.Debug.Log($"Starting process: {path}");
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                UnityEngine.Debug.Log($"Started process with ID: {process.Id}");

                // 等待进程退出或超时
                var delayTask = Task.Delay(timeoutMs, cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, delayTask);

                if (completedTask == tcs.Task)
                {
                    // 进程正常退出
                    await tcs.Task;
                    Thread.Sleep(100); // 确保输出完成
                    bool success = process.ExitCode == 0;

                    if (!success)
                    {
                        UnityEngine.Debug.LogError($"Process failed with exit code: {process.ExitCode}");
                    }

                    return success;
                }
                else
                {
                    // 超时
                    UnityEngine.Debug.LogError($"Process timeout after {timeoutMs}ms");
                    process.Kill();
                    return false;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error starting process: {e.Message}");
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }
    }
}