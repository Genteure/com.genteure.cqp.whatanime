using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace com.genteure.cqp.whatanime
{
    internal static class WhatAnimeAPI
    {
        private const int TIMEOUT = 30 * 1000;

        private static string ApiHost => Config.ApiHost;
        private static string ApiToken => Config.ApiToken;


        /// <summary>
        /// Let you verify that you have a valid user account. And see your search quota limit for your account.
        /// </summary>
        /// <returns></returns>
        internal static async Task<JObject> MeAsync()
            => JObject.Parse(await RequestStringAsync($"/api/me?token={ApiToken}"));


        /// <summary>
        /// Get a list of all indexed anime (for search filtering)
        /// </summary>
        /// <returns></returns>
        internal static async Task<JArray> ListAsync()
            => JArray.Parse(await RequestStringAsync($"/api/list?token={ApiToken}"));
        // 好像坏掉了


        /// <summary>
        /// Search
        /// </summary>
        /// <param name="image">Base64 Encoded Image</param>
        /// <param name="filter">Limit search in specific year / season</param>
        /// <returns></returns>
        internal static async Task<JObject> SearchAsync(string image, string filter = "")
        {
            JObject result = null;
            try
            {
                if (!Cooldown.CanSearch)
                    throw new ApplicationException($"全局冷却中: 剩余 {Cooldown.RemainingSeconds} 秒");
                result = JObject.Parse(await RequestStringAsync($"/api/search?token={ApiToken}", $"image={image}" + (filter == "" ? "" : "&filter=" + filter)));
                Cooldown.SetCooldown(result["quota"].ToObject<int>(), result["expire"].ToObject<int>());
                return result;
            }
            catch (WebException we)
            {
                switch ((int)(we.Response as HttpWebResponse).StatusCode)
                {
                    case 429:
                        Cooldown.TriggeredHTTP429();
                        break;
                }
                CoolQApi.AddLog(CoolQApi.LogLevel.Debug, "ErrorDebug", "Search Result: " + result?.ToString(Newtonsoft.Json.Formatting.None) ?? "[NULL]");
                throw;
            }

        }

        /// <summary>
        /// Get Thumbnail
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        internal static async Task<string> ThumbnailAsync(JObject j)
            => await ThumbnailAsync(j["season"].ToObject<string>(), j["anime"].ToObject<string>(),
                j["filename"].ToObject<string>(), j["at"].ToObject<string>(), j["tokenthumb"].ToObject<string>());

        /// <summary>
        /// Get Thumbnail
        /// </summary>
        /// <param name="season"></param>
        /// <param name="anime"></param>
        /// <param name="filename"></param>
        /// <param name="at"></param>
        /// <param name="tokenthumb"></param>
        /// <returns></returns>
        internal static async Task<string> ThumbnailAsync(string season, string anime, string filename, string at, string tokenthumb)
            => await ImageProcessor.WebImage2CQIMGAsync(
                $"https://whatanime.ga/thumbnail.php?season={season}&anime={encodeURIComponent(anime)}&file={encodeURIComponent(filename)}&t={at}&token={tokenthumb}");

        internal static Stream Preview(string season, string anime, string file, string t, string token)
        {
            throw new NotImplementedException();
        }

        private static string encodeURIComponent(string str) => Uri.EscapeUriString(str);

        private static async Task<string> RequestStringAsync(string path, string data = "")
        {
            var request = WebRequest.CreateHttp(ApiHost + path);

            request.Timeout = TIMEOUT; // 30s
            request.UserAgent = Config.USER_AGENT;
            request.Method = data == "" ? "GET" : "POST";

            if (data != "")
            {
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                var b = Encoding.UTF8.GetBytes(data);
                using (var stream = request.GetRequestStream())
                    stream.Write(b, 0, b.Length);
            }

            return new StreamReader((await request.GetResponseAsync()).GetResponseStream(), Encoding.UTF8).ReadToEnd();
        }
    }
}
