using System;
using System.Collections.Generic;

namespace com.genteure.cqp.whatanime
{
    internal static class Cooldown
    {
        // /api/search 返回的状态
        private static int _quota = 0;
        private static DateTime _expire = DateTime.Now;
        private static bool _notResetYet = true;


        /// <summary>
        /// 剩余的次数
        /// </summary>
        internal static int RemainingQuota
        {
            get
            {
                if (_notResetYet && _expire < DateTime.Now)
                {
                    _quota = 10;
                    _notResetYet = false;
                }
                return _quota;
            }
        }

        /// <summary>
        /// 当前可以执行搜索
        /// </summary>
        internal static bool CanSearch => RemainingQuota > 0;

        /// <summary>
        /// 剩余冷却时间
        /// </summary>
        internal static double RemainingSeconds => CanSearch ? 0d : (_expire - DateTime.Now).TotalSeconds;


        /// <summary>
        /// 设置新的 ratelimit 参数
        /// </summary>
        /// <param name="quota"></param>
        /// <param name="expire"></param>
        internal static void SetCooldown(int quota, int expire)
        {
            _quota = quota;
            _expire = DateTime.Now + TimeSpan.FromSeconds(expire);
            _notResetYet = true;
        }

        /// <summary>
        /// 触发了 HTTP 429
        /// </summary>
        internal static void TriggeredHTTP429()
        {
            if (RemainingSeconds < 10)
                _expire = DateTime.Now + TimeSpan.FromSeconds(120);
            else
                _expire += TimeSpan.FromSeconds(60);
        }

        /// <summary>
        /// 检查单个人的冷却状态
        /// </summary>
        /// <param name="fromQQ"></param>
        /// <returns></returns>
        internal static int Check(long fromQQ)
        {
            if (cdlist.TryGetValue(fromQQ, out DateTime v))
                if (v > DateTime.Now)
                    return (int)Math.Ceiling((v - DateTime.Now).TotalSeconds);
                else
                {
                    cdlist.Remove(fromQQ);
                    return 0;
                }
            else
                return 0;
        }

        /// <summary>
        /// 设置单个人冷却
        /// </summary>
        /// <param name="fromQQ"></param>
        internal static void Set(long fromQQ)
        {
            cdlist[fromQQ] = DateTime.Now + TimeSpan.FromSeconds(Config.CDSeconds);
        }

        private static Dictionary<long, DateTime> cdlist = new Dictionary<long, DateTime>();
    }
}
