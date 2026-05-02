using System.Collections.Generic;
using System.Diagnostics;

namespace ImageInsight
{
    public static class AppRuntime
    {
        public static Process? BackendProcess { get; set; }

        public static bool IsBackendRunning =>
            BackendProcess != null && !BackendProcess.HasExited;

        public static List<string> Logs { get; } = new();

        public static bool AutoStartAttempted { get; set; } = false;

        public static int? CurrentBackendPort { get; set; }

        public static ValidationRuntimeState ValidationState { get; } = new();
    }

    public class ValidationRuntimeState
    {
        public string SourcePath { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();
        public int CurrentIndex { get; set; } = -1;

        public AnalyzeResponse? LastAnalyzeResult { get; set; }

        public string LastMessage { get; set; } = "Ready.";
        public string StatusText { get; set; } = "Status: Idle";
        public string IsValidatedText { get; set; } = "False";
    }
}