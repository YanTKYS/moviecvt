using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MovieConverter
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.ThreadException += OnThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            => WriteStartupLog(e.ExceptionObject as Exception);

        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
            => WriteStartupLog(e.Exception);

        internal static void WriteStartupLog(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string appDir = Path.GetDirectoryName(Environment.ProcessPath)
                    ?? AppContext.BaseDirectory;
                string logDir = Path.Combine(appDir, "logs");
                Directory.CreateDirectory(logDir);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string logPath = Path.Combine(logDir, $"startup_error_{timestamp}.log");

                string version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "unknown";

                File.WriteAllText(logPath,
                    $"DateTime   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                    $"AppVersion : {version}\r\n" +
                    $"OS         : {Environment.OSVersion}\r\n" +
                    $"Type       : {ex.GetType().FullName}\r\n" +
                    $"Message    : {ex.Message}\r\n" +
                    $"StackTrace :\r\n{ex.StackTrace}\r\n" +
                    (ex.InnerException != null
                        ? $"\r\nInnerException:\r\n  Type   : {ex.InnerException.GetType().FullName}\r\n  Message: {ex.InnerException.Message}\r\n"
                        : ""),
                    System.Text.Encoding.UTF8);
            }
            catch
            {
                // ログ書き込み失敗は無視（ディスク満杯・アクセス拒否等）
            }
        }
    }
}
