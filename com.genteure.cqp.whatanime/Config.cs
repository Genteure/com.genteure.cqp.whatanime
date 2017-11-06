using Newtonsoft.Json.Linq;
using System.IO;

namespace com.genteure.cqp.whatanime
{
    internal static class Config
    {
        private static JObject _raw = null;


        /// <summary>
        /// Load config file
        /// </summary>
        internal static void Load()
            => _raw = JObject.Parse(File.ReadAllText(Path.Combine(CoolQApi.GetAppDirectory(), "config.json")));


        /// <summary>
        /// Api Host
        /// </summary>
        internal static string ApiHost => _raw?["ApiHost"]?.ToObject<string>() ?? string.Empty;

        /// <summary>
        /// Api Token
        /// </summary>
        internal static string ApiToken => _raw?["ApiToken"]?.ToObject<string>() ?? string.Empty;

        /// <summary>
        /// Master QQ
        /// </summary>
        internal static long MasterQQ => _raw?["MasterQQ"]?.ToObject<long>() ?? 0;

        /// <summary>
        /// Main Command
        /// </summary>
        internal static string MainCommand => _raw?["MainCommand"]?.ToObject<string>() ?? ".番剧识别";

        /// <summary>
        /// Help Message
        /// </summary>
        internal static string HelpMessage => _raw?["HelpMessage"]?.ToObject<string>() ?? string.Empty;

        /// <summary>
        /// Cooldown Seconds
        /// </summary>
        internal static double CDSeconds => _raw?["CDSeconds"]?.ToObject<double>() ?? 2.5d * 60d; // 2.5 分钟
    }
}
