
namespace Utility.Drawing
{
    using System;
    using System.ComponentModel;
    using System.Drawing.Printing;
    using System.Runtime.InteropServices;
    using System.Text;

    #region Class Printers(打印机相关信息设置)
    /// <summary>
    /// 打印机相关信息设置
    /// </summary>
    public static class Printers
    {
        /// <summary>
        /// 获取所有打印机
        /// </summary>
        /// <returns></returns>
        public static PrinterSettings.StringCollection GetPrinters()
        {
            return PrinterSettings.InstalledPrinters;//获取所有打印机名称            
        }

        /// <summary>
        /// 获取默认打印机
        /// </summary>
        /// <returns></returns>
        public static string GetDefaultPrinter()
        {
            const int ERROR_FILE_NOT_FOUND = 2;
            const int ERROR_INSUFFICIENT_BUFFER = 122;

            int pcchBuffer = 0;
            if (GetDefaultPrinter(null, ref pcchBuffer))
            {
                return null;
            }

            int lastWin32Error = Marshal.GetLastWin32Error();

            if (lastWin32Error == ERROR_INSUFFICIENT_BUFFER)
            {
                StringBuilder pszBuffer = new StringBuilder(pcchBuffer);
                if (GetDefaultPrinter(pszBuffer, ref pcchBuffer))
                {
                    return pszBuffer.ToString();
                }

                lastWin32Error = Marshal.GetLastWin32Error();
            }
            if (lastWin32Error == ERROR_FILE_NOT_FOUND)
            {
                return null;
            }

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        /// <param name="printerName"></param>
        /// <returns></returns>
        [DllImport("Winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDefaultPrinter(string printerName);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);
    }
    #endregion
}
