using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

using APP.Components.Dto;


namespace APP.Framework
{

    public static class CssMinifier
    {
        public static int AppendReplacement(this Match match, StringBuilder sb, string input, string replacement, int index)
        {
            var preceding = input.Substring(index, match.Index - index);

            sb.Append(preceding);
            sb.Append(replacement);

            return match.Index + match.Length;
        }

        public static void AppendTail(this Match match, StringBuilder sb, string input, int index)
        {
            sb.Append(input.Substring(index));
        }

        public static uint ToUInt32(this ValueType instance)
        {
            return Convert.ToUInt32(instance);
        }

        public static string RegexReplace(this string input, string pattern, string replacement)
        {
            return Regex.Replace(input, pattern, replacement);
        }

        public static string RegexReplace(this string input, string pattern, string replacement, RegexOptions options)
        {
            return Regex.Replace(input, pattern, replacement, options);
        }

        public static string Fill(this string format, params object[] args)
        {
            return String.Format(format, args);
        }

        public static string RemoveRange(this string input, int startIndex, int endIndex)
        {
            return input.Remove(startIndex, endIndex - startIndex);
        }

        public static bool EqualsIgnoreCase(this string left, string right)
        {
            return String.Compare(left, right, true) == 0;
        }

        public static string ToHexString(this int value)
        {
            var sb = new StringBuilder();
            var input = value.ToString();

            foreach (char digit in input)
            {
                sb.Append("{0:x2}".Fill(digit.ToUInt32()));
            }

            return sb.ToString();
        }

        #region YUI Compressor's CssMin originally written by Isaac Schlueter

        /// <summary>
        /// Minifies CSS.
        /// </summary>
        /// <param name="css">The CSS content to minify.</param>
        /// <returns>Minified CSS content.</returns>
        public static string CssMinify(this string css)
        {
            return CssMinify(css, 0);
        }

        /// <summary>
        /// Minifies CSS with a column width maximum.
        /// </summary>
        /// <param name="css">The CSS content to minify.</param>
        /// <param name="columnWidth">The maximum column width.</param>
        /// <returns>Minified CSS content.</returns>
        public static string CssMinify(this string css, int columnWidth)
        {
            css = css.RemoveCommentBlocks();
            css = css.RegexReplace("\\s+", " ");
            css = css.RegexReplace("\"\\\\\"}\\\\\"\"", "___PSEUDOCLASSBMH___");
            css = css.RemovePrecedingSpaces();
            css = css.RegexReplace("([!{}:;>+\\(\\[,])\\s+", "$1");
            css = css.RegexReplace("([^;\\}])}", "$1;}");
            css = css.RegexReplace("([\\s:])(0)(px|em|%|in|cm|mm|pc|pt|ex)", "$1$2");
            css = css.RegexReplace(":0 0 0 0;", ":0;");
            css = css.RegexReplace(":0 0 0;", ":0;");
            css = css.RegexReplace(":0 0;", ":0;");
            css = css.RegexReplace("background-position:0;", "background-position:0 0;");
            css = css.RegexReplace("(:|\\s)0+\\.(\\d+)", "$1.$2");
            css = css.ShortenRgbColors();
            css = css.ShortenHexColors();
            css = css.RegexReplace("[^\\}]+\\{;\\}", "");

            if (columnWidth > 0)
            {
                css = css.BreakLines(columnWidth);
            }

            css = css.RegexReplace("___PSEUDOCLASSBMH___", "\"\\\\\"}\\\\\"\"");
            css = css.Trim();

            return css;
        }

        private static string RemoveCommentBlocks(this string input)
        {
            var startIndex = 0;
            var endIndex = 0;
            var iemac = false;

            startIndex = input.IndexOf(@"/*", startIndex);
            while (startIndex >= 0)
            {
                endIndex = input.IndexOf(@"*/", startIndex + 2);
                if (endIndex >= startIndex + 2)
                {
                    if (input[endIndex - 1] == '\\')
                    {
                        startIndex = endIndex + 2;
                        iemac = true;
                    }
                    else if (iemac)
                    {
                        startIndex = endIndex + 2;
                        iemac = false;
                    }
                    else
                    {
                        input = input.RemoveRange(startIndex, endIndex + 2);
                    }
                }
                startIndex = input.IndexOf(@"/*", startIndex);
            }

            return input;
        }

        private static string ShortenRgbColors(this string css)
        {
            var sb = new StringBuilder();
            Regex p = new Regex("rgb\\s*\\(\\s*([0-9,\\s]+)\\s*\\)");
            Match m = p.Match(css);

            int index = 0;
            while (m.Success)
            {
                string[] colors = m.Groups[1].Value.Split(',');
                StringBuilder hexcolor = new StringBuilder("#");

                foreach (string color in colors)
                {
                    int val = Int32.Parse(color);
                    if (val < 16)
                    {
                        hexcolor.Append("0");
                    }
                    hexcolor.Append(val.ToHexString());
                }

                index = m.AppendReplacement(sb, css, hexcolor.ToString(), index);
                m = m.NextMatch();
            }

            m.AppendTail(sb, css, index);
            return sb.ToString();
        }

        private static string ShortenHexColors(this string css)
        {
            var sb = new StringBuilder();
            Regex p = new Regex("([^\"'=\\s])(\\s*)#([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])");
            Match m = p.Match(css);

            int index = 0;
            while (m.Success)
            {
                if (m.Groups[3].Value.EqualsIgnoreCase(m.Groups[4].Value) &&
                    m.Groups[5].Value.EqualsIgnoreCase(m.Groups[6].Value) &&
                    m.Groups[7].Value.EqualsIgnoreCase(m.Groups[8].Value))
                {
                    var replacement = String.Concat(m.Groups[1].Value, m.Groups[2].Value, "#", m.Groups[3].Value, m.Groups[5].Value, m.Groups[7].Value);
                    index = m.AppendReplacement(sb, css, replacement, index);
                }
                else
                {
                    index = m.AppendReplacement(sb, css, m.Value, index);
                }

                m = m.NextMatch();
            }

            m.AppendTail(sb, css, index);
            return sb.ToString();
        }

        private static string RemovePrecedingSpaces(this string css)
        {
            var sb = new StringBuilder();
            Regex p = new Regex("(^|\\})(([^\\{:])+:)+([^\\{]*\\{)");
            Match m = p.Match(css);

            int index = 0;
            while (m.Success)
            {
                var s = m.Value;
                s = s.RegexReplace(":", "___PSEUDOCLASSCOLON___");

                index = m.AppendReplacement(sb, css, s, index);
                m = m.NextMatch();
            }
            m.AppendTail(sb, css, index);

            var result = sb.ToString();
            result = result.RegexReplace("\\s+([!{};:>+\\(\\)\\],])", "$1");
            result = result.RegexReplace("___PSEUDOCLASSCOLON___", ":");

            return result;
        }

        private static string BreakLines(this string css, int columnWidth)
        {
            int i = 0;
            int start = 0;

            var sb = new StringBuilder(css);
            while (i < sb.Length)
            {
                var c = sb[i++];
                if (c == '}' && i - start > columnWidth)
                {
                    sb.Insert(i, '\n');
                    start = i;
                }
            }
            return sb.ToString();
        }
        #endregion

    }
   
}