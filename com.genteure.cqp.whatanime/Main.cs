using Newtonsoft.Json.Linq;
using RGiesecke.DllExport;
using System;
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
                            // 先开始加载图片
                            var getimage = WhatAnimeAPI.ThumbnailAsync(info);

                            TimeSpan time = TimeSpan.FromSeconds(info["at"].ToObject<double>());
                            string episode = info["episode"].Type == JTokenType.Integer
                                ? $"第 {info["episode"].ToObject<int>()} 话"
                                : info["episode"].ToObject<string>();
                            double similarity = info["similarity"].ToObject<double>() * 100d;
                            string imageCQC = await getimage;

                            reply = "搜索结果：" + Environment.NewLine;
                            reply += info["title"].ToObject<string>() + Environment.NewLine;
                            reply += info["title_chinese"].ToObject<string>() + Environment.NewLine;
                            reply += episode + " " + $"{time.Hours} 小时 {time.Minutes} 分 {time.Seconds} 秒" + Environment.NewLine;
                            reply += "相似度：" + similarity.ToString() + "%" + Environment.NewLine;
                            reply += "相似度低于85%时基本只是相似，不是正确的结果" + Environment.NewLine + Environment.NewLine;
                            reply += "对比截图：" + Environment.NewLine;
                            reply += imageCQC;
                        }
                    }
                    catch (Exception ex)
                    {
                        reply = "发生了错误：" + ex.Message;
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

    }
}
