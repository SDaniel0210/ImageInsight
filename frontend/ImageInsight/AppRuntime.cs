using System.Collections.Generic;
using System.Diagnostics;

namespace ImageInsight
{
    public static class AppRuntime
    {
        public static List<string> Logs { get; } = new();

        public static Process? BackendProcess { get; set; }

        public static bool AutoStartServiceAttempted { get; set; } = false;

        public static bool IsBackendRunning =>
            BackendProcess != null && !BackendProcess.HasExited;
    }
}