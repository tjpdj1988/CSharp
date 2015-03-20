
namespace Utility.Drawing
{
    using System;
    using System.Drawing;
    using System.Text;

    #region Class Images(图片相关)
    /// <summary>
    /// 图片相关操作
    /// </summary>
    public static class Images
    {
        /// <summary>
        /// 拉伸
        /// </summary>
        public const int HW = 0;

        /// <summary>
        /// 按高等比缩放
        /// </summary>
        public const int H = 1;

        /// <summary>
        /// 按宽等比缩放
        /// </summary>
        public const int W = 2;

        /// <summary>
        /// 裁剪
        /// </summary>
        public const int Cut = 3;

        /// <summary>
        /// 生成图片验证码
        /// </summary>
        /// <param name="image">Bitmap 实例，此实例必须指定width和height.可用 new Bitmap(width, height) 构建实例(70,30)</param>
        /// <param name="length">验证码长度</param>
        /// <param name="font">验证码使用的字体</param>
        /// <param name="lines">模糊线数量</param>
        /// <returns>验证码字符串</returns>
        public static string CAPTCHA(Bitmap image, int length, Font font, int lines)
        {
            //创建随机数。
            Random rand = new Random();

            Graphics grph = Graphics.FromImage(image);
            Color color = Color.FromArgb(150 + rand.Next(50), 150 + rand.Next(50), 150 + rand.Next(50));
            grph.Clear(color);


            //绘制干扰线。
            for (int i = 0; i < lines; i++)
            {
                color = Color.FromArgb(130 + rand.Next(50), 130 + rand.Next(50), 130 + rand.Next(50));
                Pen pen = new Pen(color);

                grph.DrawLine(pen, rand.Next(image.Width), rand.Next(image.Height), rand.Next(image.Width), rand.Next(image.Height));

            }

            for (int i = 0; i < 10; i++)
            {
                color = Color.FromArgb(130 + rand.Next(50), 130 + rand.Next(50), 130 + rand.Next(50));
                image.SetPixel(rand.Next(image.Width), rand.Next(image.Height), color);
            }

            string _return = "";

            for (int i = 0; i < length; )
            {
                char ch = (char)rand.Next(128);
                if (char.IsLetterOrDigit(ch))
                {
                    color = Color.FromArgb(80 + rand.Next(50), 80 + rand.Next(50), 80 + rand.Next(50));
                    SolidBrush brush = new SolidBrush(color);
                    grph.DrawString(ch.ToString(), font, brush, i * 10, 0);
                    _return += ch;
                    i++;
                }
            }
            return _return;
        }

        /// <summary>
        /// 缩略图
        /// </summary>
        /// <param name="original">原图片</param>
        /// <param name="thumbnail">缩略图，必须指定高和宽</param>
        /// <param name="mode"></param>
        public static void Thumbnail(Image original, Image thumbnail, int mode)
        {
            int towidth = thumbnail.Width;
            int toheight = thumbnail.Height;
            int x = 0;
            int y = 0;
            int ow = original.Width;
            int oh = original.Height;

            switch (mode)
            {

                case 1://指定高，宽按比例
                    towidth = original.Width * thumbnail.Height / original.Height;
                    break;
                case 2://指定宽，高按比例                    
                    toheight = original.Height * thumbnail.Width / original.Width;
                    break;
                case 3://指定高宽裁减（不变形）                
                    if ((double)original.Width / (double)original.Height > (double)towidth / (double)toheight)
                    {
                        oh = original.Height;
                        ow = original.Height * towidth / toheight;
                        y = 0;
                        x = (original.Width - ow) / 2;
                    }
                    else
                    {
                        ow = original.Width;
                        oh = original.Width * thumbnail.Height / towidth;
                        x = 0;
                        y = (original.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }

            //新建一个画板
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(thumbnail);

            //设置高质量插值法
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            //设置高质量,低速度呈现平滑程度
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //清空画布并以透明背景色填充
            g.Clear(System.Drawing.Color.Transparent);

            //在指定位置并且按指定大小绘制原图片的指定部分
            g.DrawImage(original, new System.Drawing.Rectangle(0, 0, towidth, toheight),
                new System.Drawing.Rectangle(x, y, ow, oh),
                System.Drawing.GraphicsUnit.Pixel);
        }

    }
    #endregion
}
