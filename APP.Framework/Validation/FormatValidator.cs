using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace APP.Framework.Validation
{
    /// <summary>
    ///   Validate the format of a value
    /// </summary>
    public static class FormatValidator
    {
        /// <summary>
        ///   Regex used to validate the email
        /// </summary>
        public const string EmailPattern = @"^[A-Z0-9\._\%\+\-]+@[A-Z0-9\.\-]+\.[A-Z]{2,4}$";

        /// <summary>
        ///   Determines whether the specified email has a valid format.
        /// </summary>
        /// <param name = "email">The email.</param>
        /// <returns>
        ///   <c>true</c> if the specified email is valid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidEmail(string email)
        {
            return IsValidFormat(email, EmailPattern, false);
        }

        /// <summary>
        ///   Determines whether the specified value has a valid format.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <param name = "pattern">The pattern.</param>
        /// <param name = "caseSensitive">if set to <c>true</c> the pattern will be case sensitive.</param>
        /// <returns>
        ///   <c>true</c> if the specified value has a valid format; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidFormat(string value, string pattern, bool caseSensitive)
        {
            Regex regex = new Regex(pattern);

            if (caseSensitive)
            {
                return regex.IsMatch(value);
            }

            return regex.IsMatch(value.ToUpper(CultureInfo.InvariantCulture));
        }
    }
}