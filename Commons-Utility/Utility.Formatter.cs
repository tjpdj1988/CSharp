using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utility.Formatter
{
    /// <summary>
    /// 人民币大写金额.
    /// </summary>
    public class ChineseYuanUpperFormatter : IFormatProvider, ICustomFormatter
    {
        private const string formatString = "#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A";
        private const string regex = @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatType"></param>
        /// <returns></returns>
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            // Check whether this is an appropriate callback             
            if (!this.Equals(formatProvider))
                return null;

            try
            {
                decimal _arg = Convert.ToDecimal(arg);
                string _value = Regex.Replace(_arg.ToString(formatString), regex, "${b}${z}");
                return Regex.Replace(_value, ".", delegate(Match m)
                {
                    return "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟萬億兆京垓秭穰"[m.Value[0] - '-'].ToString();
                });
            }
            catch (Exception)
            {
                throw new FormatException(string.Format("'{0}' is not a Numeric.", arg.ToString()));
            }

        }

    }
}
