namespace Utility.Chinese
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 为区域性质的功能提供支持
    /// </summary>
    public static class ChineseUtils
    {
        private const string formatString = "#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A";
        private const string regex = @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))";


        /// <summary>
        /// 人民币金额转大写(注意：转换时,负值会转成正值.eg: -12.56 -> 壹拾贰元伍角陆分)
        /// </summary>
        /// <param name="value">金额</param>
        /// <returns>大写金额字符串</returns>
        public static string ChineseYuanUpper(decimal value)
        {
            string _return = Regex.Replace(value.ToString(formatString), regex, "${b}${z}");
            return Regex.Replace(_return, ".", delegate(Match m)
            {
                return "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟萬億兆京垓秭穰"[m.Value[0] - '-'].ToString();
            });
        }
    }
}
