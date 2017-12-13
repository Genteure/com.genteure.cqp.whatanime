using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.genteure.cqp.whatanime
{
    internal static class ImageProcessor
    {
        private static readonly Regex CQIMG_REGEX = new Regex("^url=(http.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int HTTP_TIMEOUT = 10 * 1000;
        private const string HTTP_USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36";

        /// <summary>
        /// 从接收到的 CQIMG 文件转换为压缩后的 data/jpeg Base64 文本
        /// </summary>
        /// <param name="cqimg">接收到的文件名</param>
        /// <returns>data:image/jpeg;base64,{0}</returns>
        internal static async Task<string> CQIMG2Base64Async(string cqimg)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "image", cqimg + ".cqimg");
            var url = CQIMG_REGEX.Match(File.ReadAllText(path)).Groups[1].Value;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = HTTP_TIMEOUT;
            request.UserAgent = HTTP_USERAGENT;

            var response = (HttpWebResponse)(await request.GetResponseAsync());
            MemoryStream result = new MemoryStream();
            var image = Image.FromStream(response.GetResponseStream());

            if (image.Width < 320 || image.Height < 180) throw new ApplicationException("图片太小！");
            if (image.Width * 10 / image.Height < 12) throw new ApplicationException("图片宽高比需要大于 1.2 ！");

            image.Save(result, GetEncoderInfo("image/jpeg"), new EncoderParameters()
            {
                Param = new EncoderParameter[] { new EncoderParameter(Encoder.Quality, 50L) }
            });

            return Convert.ToBase64String(result.ToArray());
        }

        /// <summary>
        /// 下载网络图片到本地并转换成可直接发送的 CQ码
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <returns>CQ码</returns>
        internal static async Task<string> WebImage2CQIMGAsync(string url)
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "image", "whatanime");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = HTTP_TIMEOUT;
            request.UserAgent = HTTP_USERAGENT;

            var response = (HttpWebResponse)(await request.GetResponseAsync());
            string filename = ((int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString("X") + "-" + url.GetHashCode().ToString("X") + ".jpg";
            Image.FromStream(response.GetResponseStream()).Save(Path.Combine(dir, filename), ImageFormat.Jpeg);
            return CoolQApi.CQC_Image("whatanime\\" + filename);
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
            => ImageCodecInfo.GetImageEncoders().ToList().First(x => x.MimeType == mimeType);

    }
}
