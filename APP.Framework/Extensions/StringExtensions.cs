using System.Text;

#if SILVERLIGHT
using System.Linq;
#endif

namespace System
{
    /// <summary>
    ///   Extensions methods for the class string
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///   Determines whether the specified value has value.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the specified value has value; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasValue(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        ///   Trim and determines whether the specified value has value.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the specified value has value; otherwise, <c>false</c>.
        /// </returns>
        public static bool TrimHasValue(this string value)
        {
            if (value.HasValue())
            {
                return value.Trim().HasValue();
            }

            return false;
        }

        /// <summary>
        ///   Convert the string to a byte array
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns>Return the byte array of the string</returns>
        public static byte[] ToByteArray(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        ///   Determines whether the specified string contains the value.
        /// </summary>
        /// <param name = "source">The string.</param>
        /// <param name = "value">The value to check.</param>
        /// <param name = "comparisonType">Type of the comparison.</param>
        /// <returns>
        ///   <c>true</c> if the value parameter occurs within this string, or if value is the empty string (""); otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsExt(this string source, string value, StringComparison comparisonType)
        {
            if (!source.TrimHasValue() || !value.HasValue())
            {
                return false;
            }

            if (value.Length == 0)
            {
                return true;
            }

            return source.IndexOf(value, comparisonType) >= 0;
        }
        public static string GetFirstStringBetweenStrings(this string s, string from, string to)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return string.Empty;

            int idxFrom = s.IndexOf(from);
            int idxStart = idxFrom + from.Length; //we filter "not found" -1, never race condtn

            if (idxFrom == -1 || idxStart >= s.Length - 1)
                return string.Empty;

            int idxEnd = s.IndexOf(to, idxStart); //Exact definition, but intuitively next line meets likely expectations -> YOU CHOOSE
                                                  //int idxEnd = s.IndexOf(to, idxStart + 1); //Start next position after, leaving a space for 1 character to be returned


            if (idxEnd == -1 || idxEnd <= idxStart)
                return string.Empty;

            return s.Substring(idxStart, idxEnd - idxStart);

        }
    }
}