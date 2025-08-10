using FileSystem.HostFileAPI;
using System.IO;

namespace Debugging
{
    static class EventLogger {

        private static short MaxEventFiles = 255;     // Keep Low to not over-consume ROM memory
        private static List<string>? EventLogs { get; set; } = null;    // reset at the end of the frame
        internal static ManageFile EventsFile { get; set; } = null;
        internal static bool DebugMode = true;
        private static int LogCount = 0;

        static public void Report(string log = "", string tag = "ENTRY") {
            string compositeErrorLog = $"{tag} ({DateTime.Now:HH:mm:ss}) - {log}\n";
            EventLogs.Add(compositeErrorLog);
        }

        static internal ManageFile? StartSession(bool debugMode) {
            EventLogs = new List<string> ();

            if (debugMode) {
                EventsFile = new ManageFile(Path.Combine(ManageFile.LocalAppPath, "Schefflera", "Logs"), "DLog.txt");
                EventsFile.LogTo($"NEW SESSION ({DateTime.Now:HH:mm:ss}). Logging to: {EventsFile.GlobalFilePath}\n", true);
                return EventsFile;
            }
            return null;
        }

        static bool PushLogsToFile(List<string> pushList) {
            for (byte i = 0; i < pushList.Count; i++) {
                bool result = PushLogsToFile(pushList[i]);
                if (!result) return result;
            }
            return true;
        }

        static bool PushLogsToFile(string pushEntry) {
            if (EventsFile == null) {
                EventsFile = StartSession(true);
                if (!File.Exists(EventsFile.GlobalFilePath)) {
                    return false;
                }
            }

            EventsFile.LogTo(pushEntry);
            return true;
        }

        static internal void FlushLogs() {
            if (DebugMode) {
                PushLogsToFile($"FRAME {++LogCount}\n");
                PushLogsToFile(EventLogs);
            }
            EventLogs = new List<string> ();
        }

        internal static void CursorReturnLog(string content, int printHeight = 1, int printWidth = 1, bool awaitKeyPress = true) {

            (int, int) returnTo = Console.GetCursorPosition();

            Console.SetCursorPosition(printWidth, printHeight);
            Console.Write(content);

            Console.SetCursorPosition(returnTo.Item1, returnTo.Item2);
            if (awaitKeyPress) Console.ReadKey(true);
        }
    }
}