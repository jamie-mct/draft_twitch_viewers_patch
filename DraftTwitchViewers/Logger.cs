using System.IO;
using UnityEngine;

#region NO_LOCALIZATION
namespace DraftTwitchViewers
{
    class Logger
    {
        public static void DebugLog(string text)
        {
            Log.Info("(Log): " + text);
        }

        public static void LogInfo(string message) => Log.Info($"[INFO] | {message}");
        public static void LogWarn(string message) => Log.Warning($"[WARN] | {message}");
        public static void LogError(string message) => Debug.LogError($"Draft Twitch Viewers : [ERR ] | {message}");

        public static void DebugWarning(string text)
        {
            Log.Warning("(Warning): " + text);
        }

        public static void DebugError(string text)
        {
            Log.Error("(ERROR): " + text);
        }

        public static void LogToFile(string text, bool asLines)
        {
            try
            {
                if (asLines)
                {
                    string[] lines = text.Split("\n".ToCharArray());

                    File.WriteAllLines(@"C:\DTV Log.txt", lines);
                }
                else
                {
                    File.WriteAllText(@"C:\DTV Log.txt", text);
                }
            }
            catch
            {

            }
        }
    }
}
#endregion
