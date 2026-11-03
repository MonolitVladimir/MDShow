// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace MdShow
{
    internal static class L
    {
#if ENGLISH_ONLY
        internal static readonly bool EnglishOnly = true;
        internal static readonly string[] Languages = { "en-US" };
#else
        internal static readonly bool EnglishOnly = false;
        internal static readonly string[] Languages = { "en-US", "ru-RU", "de-DE", "ar", "zh-CN" };
#endif
        internal static readonly Dictionary<string, Dictionary<string, string>> Catalogs = Load();
        internal static readonly List<string> Diagnostics = new List<string>();
        internal static string Language = Resolve("auto", CultureInfo.CurrentUICulture.Name);

        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("web.locales.json"))
            using (var reader = new StreamReader(stream))
                return new JavaScriptSerializer().Deserialize<Dictionary<string, Dictionary<string, string>>>(reader.ReadToEnd());
        }

        internal static string Resolve(string preference, string system)
        {
#if ENGLISH_ONLY
            return "en-US";
#else
            if (Languages.Contains(preference)) return preference;
            if (preference != "auto") return "en-US";
            system = (system ?? "").ToLowerInvariant();
            foreach (string prefix in new[] { "ru", "de", "ar", "en" })
                if (system == prefix || system.StartsWith(prefix + "-", StringComparison.Ordinal))
                    return prefix == "ru" ? "ru-RU" : prefix == "de" ? "de-DE" : prefix == "ar" ? "ar" : "en-US";
            if (system == "zh-cn" || system == "zh-sg" || system == "zh-hans" || system.StartsWith("zh-hans-", StringComparison.Ordinal)) return "zh-CN";
            return "en-US";
#endif
        }

        internal static string Placeholders(string value)
        {
            return String.Join(",", Regex.Matches(value, @"\{\d+\}").Cast<Match>().Select(m => m.Value).OrderBy(x => x));
        }

        internal static string T(string key, params object[] args)
        {
            string fallback, value;
            if (!Catalogs["en-US"].TryGetValue(key, out fallback)) { Diagnostics.Add("Unknown key: " + key); return key; }
            Dictionary<string, string> catalog;
            if (!Catalogs.TryGetValue(Language, out catalog) || !catalog.TryGetValue(key, out value) || String.IsNullOrWhiteSpace(value) || Placeholders(value) != Placeholders(fallback))
            { Diagnostics.Add("Fallback: " + Language + "/" + key); value = fallback; }
            // Substitute literal numbered placeholders; never interpret translated format strings.
            return Regex.Replace(value, @"\{(\d+)\}", m => {
                int index = Int32.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                return index < args.Length ? Convert.ToString(args[index], CultureInfo.InvariantCulture) : m.Value;
            });
        }
    }
}
