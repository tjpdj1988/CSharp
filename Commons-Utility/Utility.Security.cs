using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Utility.Security
{
    /// <summary>
    /// 加密解密类
    /// </summary>
    public static class Cryptography
    {

        /// <summary>
        /// 计算32位 MD5值.(16位可通过ToString(8, 16)截取)
        /// </summary>
        /// <param name="text">输入的文本.</param>
        /// <returns>md5字符串</returns>
        public static StringBuilder MD5(string text)
        {
            byte[] input = Encoding.Default.GetBytes(text);
            byte[] result = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(input);
            StringBuilder _return = new StringBuilder(32);
            for (int i = 0; i < result.Length; i++)
            {
                _return.Append(result[i].ToString("x2"));
            }

            return _return;
        }

        /// <summary>
        /// DES 加密
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="key">密钥</param>
        /// <returns>加密文本</returns>
        public static string DESEncrypt(string text, string key)
        {
            StringBuilder KEY_IV = MD5(key);

            using (DES des = (DES)CryptoConfig.CreateFromName("DES"))
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(KEY_IV.ToString(8, 8));
                des.IV = ASCIIEncoding.ASCII.GetBytes(KEY_IV.ToString(17, 8));

                byte[] input = Encoding.Default.GetBytes(text);

                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(input, 0, input.Length);
                    cs.FlushFinalBlock();

                    StringBuilder _return  = new StringBuilder();
                    foreach (byte b in ms.ToArray())
                    {
                        _return.AppendFormat("{0:X2}", b);
                    }
                    return _return.ToString();
                }
            }
        }

        /// <summary>
        /// DES 解密
        /// </summary>
        /// <param name="text">加密文本</param>
        /// <param name="key">密钥</param>
        /// <returns>解密文本</returns>
        public static string DESDecrypt(string text, string key)
        {
            // 转换成字节数组
            byte[] input = new byte[text.Length / 2];
            int x, i;
            for (x = 0; x < input.Length; x++)
            {
                i = Convert.ToInt32(text.Substring(x * 2, 2), 16);
                input[x] = (byte)i;
            }

            // 开始解密过程
            StringBuilder KEY_IV = MD5(key);

            using (DES des = (DES)CryptoConfig.CreateFromName("DES"))
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(KEY_IV.ToString(8, 8));
                des.IV = ASCIIEncoding.ASCII.GetBytes(KEY_IV.ToString(17, 8));               

                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(input, 0, input.Length);
                    cs.FlushFinalBlock();
                    return Encoding.Default.GetString(ms.ToArray());
                }
            }
        }
      
    }
}
