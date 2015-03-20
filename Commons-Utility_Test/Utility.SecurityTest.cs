using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utility.Security;
using System.Text;

namespace Commons_Utility_Test
{
    [TestClass]
    public class CryptographyTest
    {
        const string text = "123456789";
        const string md5 = "25f9e794323b453885f5181f1b624d0b";

        [TestMethod]
        public void MD5()
        {
            StringBuilder result = Cryptography.MD5(text);
            Assert.IsTrue(md5 == result.ToString());
            Assert.IsTrue("323b453885f5181f" == result.ToString(8, 16));
        }

        [TestMethod]
        public void DES()
        {
            string result = Cryptography.DESEncrypt(text,md5);
            result = Cryptography.DESDecrypt(result, md5);
            Assert.IsTrue(result == text);
        
        }
    }
}
