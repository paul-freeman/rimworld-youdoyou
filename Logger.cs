// adapted from:
// https://raw.githubusercontent.com/fluffy-mods/WorkTab/02f3548240cd9df26d3933dfca64a8a98e45f05f/Source/Utilities/Logger.cs

// Karel Kroeze
// Logger.cs
// 2017-05-22

using System.Diagnostics;
using Verse;

namespace YouDoYou
{
    public static class Logger
    {
        public static string Identifier => "You Do You";

        public static string FormatMessage(string msg) { return Identifier + " :: " + msg; }

        [Conditional("DEBUG")]
        public static void Assert(object obj, string name)
        {
            Debug($"{name} :: {obj ?? "NULL"}");
        }

        [Conditional("DEBUG")]
        public static void Debug(string msg) { Log.Message(FormatMessage(msg)); }

        [Conditional("TRACE")]
        public static void Trace(string msg) { Log.Error(FormatMessage(msg)); }

        public static void Error(string msg)
        {
            Log.Error(FormatMessage(msg));
        }

        public static void Message(string msg) { Log.Message(FormatMessage(msg)); }
    }
}