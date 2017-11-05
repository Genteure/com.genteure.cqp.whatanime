using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.genteure.cqp.whatanime
{
    internal static class Cooldown
    {
        // /api/me 所返回的初始状态

        internal static int InitialQuota = 10;
        internal static int InitialQuotaTTL = 60;


        // /api/search 返回的状态

        internal static int Quota = 10;
        internal static DateTime Expire = DateTime.Now;

        /// <summary>
        /// 当前可以执行搜索
        /// </summary>
        internal static bool CanSearch => Quota > 0;

        /// <summary>
        /// 剩余冷却时间
        /// </summary>
        internal static double RemainingSeconds => CanSearch ? 0d : (Expire - DateTime.Now).TotalSeconds;

        /// <summary>
        /// 设置新的 ratelimit 参数
        /// </summary>
        /// <param name="quota"></param>
        /// <param name="expire"></param>
        internal static void SetCooldown(int quota, int expire)
        {
            Quota = quota;
            Expire = DateTime.Now + TimeSpan.FromSeconds(expire);
        }
    }
}
