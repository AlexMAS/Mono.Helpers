using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Mono.Unix.Native;

namespace System
{
    public static class MonoHelper
    {
        public const int DefaultTimeout = 60 * 1000;


        public static bool RunningAsRoot
        {
            get
            {
                return Syscall.getuid() == 0;
            }
        }

        public static bool RunninOnUnix
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return ((p == 4) || (p == 6) || (p == 128));
            }
        }

        public static bool RunninOnLinux
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return ((p == 4) || (p == 128));
            }
        }

        public static bool RunningOnMono
        {
            get
            {
                return (Type.GetType("Mono.Runtime") != null);
            }
        }


        public static Task ExecuteProcessAsync(string command, string arguments = "", int timeout = DefaultTimeout)
        {
            return Task.Run(() => ExecuteProcessSync(command, arguments, timeout));
        }


        public static Task<ProcessResult> TryExecuteProcessAsync(string command, string arguments = "", int timeout = DefaultTimeout)
        {
            return Task.Run(() => TryExecuteProcessSync(command, arguments, timeout));
        }


        public static void ExecuteProcessSync(string command, string arguments = "", int timeout = DefaultTimeout)
        {
            var result = TryExecuteProcessSync(command, arguments, timeout);

            if (!result.Completed || result.ExitCode != 0)
            {
                var processName = command + (string.IsNullOrEmpty(arguments) ? string.Empty : (" " + arguments));

                if (result.Completed)
                {
                    throw new InvalidOperationException(string.Format(Properties.Resources.ProcessCompletedWithAnError, processName, result.ExitCode, result.Output));
                }

                throw new InvalidOperationException(string.Format(Properties.Resources.ProcessCompletedWithAnErrorByTimeout, processName, timeout));
            }
        }

        public static ProcessResult TryExecuteProcessSync(string command, string arguments = "", int timeout = DefaultTimeout)
        {
            var result = new ProcessResult();

            using (var process = new Process())
            {
                // При запуске на Linux bash-скриптов, возможен код ошибки 255.
                // Решением является добавление заголовка #!/bin/bash в начало скрипта.

                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                using (var outputCloseEvent = new AutoResetEvent(false))
                using (var errorCloseEvent = new AutoResetEvent(false))
                {
                    // Подписка на события записи в выходные потоки процесса

                    var copyOutputCloseEvent = outputCloseEvent;

                    process.OutputDataReceived += (s, e) =>
                                                  {
                                                      // Поток output закрылся (процесс завершил работу)
                                                      if (string.IsNullOrEmpty(e.Data))
                                                      {
                                                          copyOutputCloseEvent.Set();
                                                      }
                                                      else
                                                      {
                                                          outputBuilder.AppendLine(e.Data);
                                                      }
                                                  };

                    var copyErrorCloseEvent = errorCloseEvent;

                    process.ErrorDataReceived += (s, e) =>
                                                 {
                                                     // Поток error закрылся (процесс завершил работу)
                                                     if (string.IsNullOrEmpty(e.Data))
                                                     {
                                                         copyErrorCloseEvent.Set();
                                                     }
                                                     else
                                                     {
                                                         errorBuilder.AppendLine(e.Data);
                                                     }
                                                 };

                    bool isStarted;

                    try
                    {
                        isStarted = process.Start();
                    }
                    catch (Exception error)
                    {
                        // Не удалось запустить процесс, скорей всего, файл не существует или не является исполняемым

                        result.Completed = true;
                        result.ExitCode = -1;
                        result.Output = string.Format(Properties.Resources.CannotExecuteCommand, command, arguments, error.Message);

                        isStarted = false;
                    }

                    if (isStarted)
                    {
                        // Начало чтения выходных потоков процесса в асинхронном режиме, чтобы не создать блокировку
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        // Ожидание завершения процесса и закрытия выходных потоков
                        if (process.WaitForExit(timeout)
                            && outputCloseEvent.WaitOne(timeout)
                            && errorCloseEvent.WaitOne(timeout))
                        {
                            result.Completed = true;
                            result.ExitCode = process.ExitCode;

                            // Вывод актуален только при наличии ошибки
                            if (process.ExitCode != 0)
                            {
                                result.Output = $"{outputBuilder}{errorBuilder}";
                            }
                        }
                        else
                        {
                            try
                            {
                                // Зависшие процессы завершаются принудительно
                                process.Kill();
                            }
                            catch
                            {
                                // Любые ошибки в данном случае игнорируются
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}