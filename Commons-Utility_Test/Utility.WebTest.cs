using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using Utility.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Commons_Utility_Test
{
    [TestClass]
    public class ImagesTest
    {
        string dir = Environment.CurrentDirectory;

        [TestMethod]
        public void CAPTCHATest()
        {
            using (Bitmap image = new Bitmap(70, 30))
            {
                string s = Images.CAPTCHA(image, 5, new Font("隶书", 20), 20);
                Assert.IsTrue(s.Length == 5);
                image.Save(Path.Combine(dir, "CAPTCHA.jpg"), ImageFormat.Jpeg);
            }
        }

        [TestMethod]
        public void ThumbnailTest()
        {
            using (Image image1 = Image.FromFile(Path.Combine(dir, "白夜茶会.jpg")))
            {
                using (Image image2 = new Bitmap(image1.Width / 2, image1.Height / 2))
                {
                    Images.Thumbnail(image1, image2, Images.HW);
                    image2.Save(Path.Combine(dir, "_白夜茶会.jpg"), ImageFormat.Jpeg);
                }
            }
        }
    }
}
