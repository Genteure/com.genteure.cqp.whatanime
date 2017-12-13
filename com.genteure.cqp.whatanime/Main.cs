using GraphQL;
using Newtonsoft.Json.Linq;
using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.genteure.cqp.whatanime
{
    internal static class Main
    {
        internal const string APP_ID = "com.genteure.cqp.whatanime";

        /// <summary>
        /// 插件启用
        /// </summary>
        /// <returns></returns>
        [DllExport("_eventEnable", CallingConvention.StdCall)]
        internal static CoolQApi.Event Startup()
        {
            Config.Load();
            return CoolQApi.Event.Ignore;
        }

        [DllExport("_eventGroupMsg", CallingConvention.StdCall)]
        internal static CoolQApi.Event ProcessGroupMessageAsync(int subType, int sendTime, long fromGroup, long fromQQ, string fromAnonymous, string msg, int font)
        {
            try
            {
                var result = _ProcessGroupMessageAsync(subType, sendTime, fromGroup, fromQQ, fromAnonymous, msg, font);
                result.Wait();
                return result.Result;
            }
            catch (Exception ex)
            {
                CoolQApi.SendPrivateMsg(Config.MasterQQ, ex.ToString());
                return CoolQApi.Event.Ignore;
            }
        }
        private static async Task<CoolQApi.Event> _ProcessGroupMessageAsync(int subType, int sendTime, long fromGroup, long fromQQ, string fromAnonymous, string msg, int font)
        {
            // 如果没有以主命令开头，则忽略
            if (!msg.StartsWith(Config.MainCommand))
                return CoolQApi.Event.Ignore;

            // 要回复给发送人的消息
            string reply = string.Empty;

            // 获取消息中的图片
            var regex_result = REGEX_GETIMAGE.Match(msg);

            if (regex_result.Success)
            { // 如果消息中带有图片

                int cooldown = Cooldown.Check(fromQQ);
                if (cooldown > 0 && fromQQ != Config.MasterQQ) // 检查发送账号是否处于冷却状态
                    reply = $"单人冷却：请等候 {cooldown} 秒后再试";
                else
                    try
                    {
                        CoolQApi.SendGroupMsg(fromGroup, CoolQApi.CQC_At(fromQQ) + " 正在搜索中...");

                        ReplyContent replyContent = ReplyContent.Normal;

                        string image = "data:image/jpeg;base64," + await ImageProcessor.CQIMG2Base64Async(regex_result.Groups[1].Value);
                        var result = await WhatAnimeAPI.SearchAsync(image);
                        JObject info = result?["docs"]?[0] as JObject;

                        Cooldown.Set(fromQQ);

                        if (info == null)
                        {
                            reply = "未搜索到任何信息";
                        }
                        else
                        {
                            int anilist_id = info["anilist_id"].ToObject<int>();
                            double similarity = info["similarity"].ToObject<double>() * 100d;

                            var anilist_result = anilist.Query(ANILIST_QUERY, new { id = anilist_id }, Config.USER_AGENT).Get("Media");

                            // isAdult: boolean
                            if (anilist_result.isAdult.ToObject<bool>())
                                replyContent |= ReplyContent.R18;

                            // genres: string[]
                            var genres = new List<string>((string[])anilist_result.genres.ToObject<string[]>());
                            if (genres.Any(x => x == "Hentai" || x == "Ecchi"))
                                replyContent |= ReplyContent.NoPreview;

                            // 🌚
                            if (anilist_id == 10380 || anilist_id == 11879 || anilist_id == 20852)
                                replyContent |= ReplyContent.FuckOff;

                            if (similarity < 88)
                                replyContent |= ReplyContent.SearchTip;

                            // startDate.year: number

                            // season:
                            // WINTER
                            // SPRING
                            // SUMMER
                            // FALL
                            string anilist_season = anilist_result.startDate.year + "年";
                            switch (anilist_result.season.ToString())
                            {
                                case "WINTER":
                                    anilist_season += "冬季";
                                    break;
                                case "SPRING":
                                    anilist_season += "春季";
                                    break;
                                case "SUMMER":
                                    anilist_season += "夏季";
                                    break;
                                case "FALL":
                                    anilist_season += "秋季";
                                    break;
                            }

                            // status:
                            // FINISHED 已完结
                            // RELEASING 更新中
                            // NOT_YET_RALEASED 未发布
                            // CANCELLED 取消发布
                            string anilist_status;
                            switch (anilist_result.status.ToString())
                            {
                                case "FINISHED":
                                    anilist_status = "已完结";
                                    break;
                                case "RELEASING":
                                    anilist_status = "更新中";
                                    break;
                                case "NOT_YET_RALEASED":
                                    anilist_status = "未发布";
                                    break;
                                case "CANCELLED":
                                    anilist_status = "取消发布";
                                    break;
                                default:
                                    anilist_status = string.Empty; // Error?
                                    break;
                            }

                            // source:
                            // ORIGINAL 原创
                            // MANGA 漫改
                            // LIGHT_NOVEL 轻改
                            string anilist_source;
                            switch (anilist_result.source.ToString())
                            {
                                case "ORIGINAL":
                                    anilist_source = "原创";
                                    break;
                                case "MANGA":
                                    anilist_source = "漫改";
                                    break;
                                case "LIGHT_NOVEL":
                                    anilist_source = "轻改";
                                    break;
                                default:
                                    anilist_source = "其他";
                                    break;
                            }


                            if (replyContent.HasFlag(ReplyContent.FuckOff))
                            {
                                Cooldown.Set(fromQQ, 60 * 60 * 24); // 1 day
                                CoolQApi.SetGroupBan(fromGroup, fromQQ, Config.FuckOffDuration);
                                reply = Config.FuckOffMessage;
                            }
                            else if (replyContent.HasFlag(ReplyContent.R18))
                            {
                                Cooldown.Set(fromQQ, 60 * 10); // 10 minutes
                                reply = Config.R18Message;
                            }
                            else
                            {
                                Task<string> getimage = null;
                                if (!replyContent.HasFlag(ReplyContent.NoPreview))
                                    getimage = WhatAnimeAPI.ThumbnailAsync(info);

                                TimeSpan time = TimeSpan.FromSeconds(info["at"].ToObject<double>());
                                string episode = info["episode"].Type == JTokenType.Integer
                                    ? $"第 {info["episode"].ToObject<int>()} 话"
                                    : info["episode"].ToObject<string>();

                                reply = "搜索结果：" + Environment.NewLine;
                                reply += info["title"].ToObject<string>() + Environment.NewLine;
                                reply += info["title_chinese"].ToObject<string>() + Environment.NewLine;
                                reply += episode + " " + $"{time.Hours} 小时 {time.Minutes} 分 {time.Seconds} 秒" + Environment.NewLine;
                                reply += "相似度：" + similarity.ToString() + "%" + Environment.NewLine;
                                reply += $"{anilist_season} {anilist_status} {anilist_source}" + Environment.NewLine;

                                reply += Environment.NewLine;
                                if (replyContent.HasFlag(ReplyContent.NoPreview))
                                { reply += "擦边球，没有预览"; }
                                else
                                {
                                    reply += "对比截图预览 (Preview unavailable 是截图失败)" + Environment.NewLine;
                                    reply += await getimage;
                                }

                                if (replyContent.HasFlag(ReplyContent.SearchTip))
                                    reply += Environment.NewLine + Config.SearchTipMessage;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        reply = "发生了错误：" + ex.Message;
                        CoolQApi.AddLog(CoolQApi.LogLevel.Debug, "番剧识别错误", ex.ToString());
                    }
            }
            else
            { // 如果消息中不带图片
                reply = Config.HelpMessage;
            }

            if (reply != string.Empty)
                CoolQApi.SendGroupMsg(fromGroup, CoolQApi.CQC_At(fromQQ) + "\n" + reply);
            return CoolQApi.Event.Block;
        }

        private static readonly Regex REGEX_GETIMAGE = new Regex(@"\[CQ:image,file=(.{32}\.(?:png|jpg|gif))\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly GraphQLClient anilist = new GraphQLClient("https://graphql.anilist.co/");
        private const string ANILIST_QUERY = @"query($id:Int!){Media(id:$id){genres isAdult startDate{year} season source status}}";

        [Flags]
        internal enum ReplyContent
        {
            Normal = 1 << 0,
            SearchTip = 1 << 1,
            NoPreview = 1 << 2,
            R18 = 1 << 3,
            FuckOff = 1 << 4,
        }

    }
}
