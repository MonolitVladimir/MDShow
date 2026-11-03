// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace MdShow
{
    // Explicit QA-only opt-in. Never include document text, names, paths or credentials.
    internal static class StartupTrace
    {
        private static Stopwatch clock;
        private static double processOffset;
        private static string output;
        private static readonly List<object> events = new List<object>();
        internal static bool Enabled { get { return clock != null; } }
        internal static string Runtime = "";
        internal static int ResourceRequests;

        internal static void Start(string[] args)
        {
            string option = args.FirstOrDefault(a => a.StartsWith("--qa-startup-trace=", StringComparison.Ordinal));
            if (option == null) return;
            try
            {
                output = Path.GetFullPath(option.Substring("--qa-startup-trace=".Length).Trim('"'));
                processOffset = Math.Max(0, (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds);
                clock = Stopwatch.StartNew();
                events.Add(new { stage = "processCreated", milliseconds = 0.0 });
                Mark("managedEntry");
            }
            catch (Exception) { clock = null; }
        }

        internal static void Mark(string stage)
        {
            if (!Enabled || events.Count >= 256) return;
            events.Add(new { stage, milliseconds = Math.Round(processOffset + clock.Elapsed.TotalMilliseconds, 3) });
        }

        internal static void Flush()
        {
            if (!Enabled) return;
            try
            {
                File.WriteAllText(output, new JavaScriptSerializer().Serialize(new {
                    schema = 1, clock = "Stopwatch; process creation offset sampled once at managed entry",
                    edition = Profile.Scope, runtime = Runtime, resourceRequests = ResourceRequests, events
                }));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
