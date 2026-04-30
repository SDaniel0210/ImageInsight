using System.Collections.Generic;
using System.Diagnostics;

namespace ImageInsight
{
    public static class AppRuntime
    {
        public static List<string> Logs { get; } = new();

        public static Process? BackendProcess { get; set; }

        public static int BackendPort { get; set; } = 8000;

        public static bool IsBackendRunning =>
            BackendProcess != null && !BackendProcess.HasExited;
    }
}