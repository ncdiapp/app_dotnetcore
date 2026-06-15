using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace APP.Framework
{
    /// <summary>
    ///   Help to validate any type of method argument
    /// </summary>
    public static class ArgumentValidator
    {
        /// <summary>
        /// Raise an exception if the argument is null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        /// <exception cref="System.ArgumentNullException">Raised when the argument is null</exception>
        public static void IsNotNull(string argumentName, object argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Raise an exception if the argument is null.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="message">The message.</param>
        /// <param name="argument">The argument.</param>
        /// <param name="messageParameters">The message parameters.</param>
        /// <exception cref="System.ArgumentNullException">Raised when the argument is null</exception>
        public static void IsNotNull(string argumentName, string message, object argument, params string[] messageParameters)
        {
            if (argument == null)
            {
                string formatedMessage = string.Format(CultureInfo.InvariantCulture, message, messageParameters);
                throw new ArgumentNullException(argumentName, formatedMessage);
            }
        }

        /// <summary>
        /// Raise an exception if the argument is null or empty.
        /// </summary>
        /// <typeparam name="T">The type of objects of the enumeration</typeparam>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="argument">The argument.</param>
        /// <exception cref="System.ArgumentNullException">Raised when the argument is null or empty</exception>
        public static void IsNotNullOrEmpty<T>(string argumentName, IEnumerable<T> argument)
        {
            if (argument == null || argument.IsEmpty())
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        ///   Raise an exception if the argument is null, empty or contains only whitespaces.
        /// </summary>
        /// <param name = "argumentName">Name of the argument.</param>
        /// <param name = "argument">The argument.</param>
        /// <exception cref = "System.ArgumentNullException">Raised when the argument is null or empty</exception>
        public static void IsNotNullOrEmpty(string argumentName, string argument)
        {
            if (!argument.TrimHasValue())
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Raise an exception if the argument is null, empty or contains only whitespaces.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="message">The message.</param>
        /// <param name="argument">The argument.</param>
        /// <param name="messageParameters">The message parameters.</param>
        /// <exception cref="System.ArgumentNullException">Raised when the argument is null or empty</exception>
        public static void IsNotNullOrEmpty(string argumentName, string message, string argument, params string[] messageParameters)
        {
            if (!argument.TrimHasValue())
            {
                string formatedMessage = string.Format(CultureInfo.InvariantCulture, message, messageParameters);
                throw new ArgumentNullException(argumentName, formatedMessage);
            }
        }

        /// <summary>
        /// Raise an exception if the condition is <c>false</c>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="condition">The condition to validate</param>
        /// <param name="messageParameters">The message parameters.</param>
        /// <exception cref="System.ArgumentException">Raised when the condition is <c>false</c></exception>
        public static void IsTrue(string message, bool condition, params string[] messageParameters)
        {
            if (!condition)
            {
                string formatedMessage = string.Format(CultureInfo.InvariantCulture, message, messageParameters);
                throw new ArgumentException(formatedMessage);
            }
        }

        /// <summary>
        /// Raise an exception if the condition is <c>false</c>.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="message">The message.</param>
        /// <param name="condition">The condition to validate</param>
        /// <param name="messageParameters">The message parameters.</param>
        /// <exception cref="System.ArgumentException">Raised when the condition is <c>false</c></exception>
        public static void IsTrue(string argumentName, string message, bool condition, params string[] messageParameters)
        {
            if (!condition)
            {
                string formatedMessage = string.Format(CultureInfo.InvariantCulture, message, messageParameters);
                throw new ArgumentException(formatedMessage, argumentName);
            }
        }

        /// <summary>
        /// Raise an exception if the condition is <c>true</c>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="condition">The condition to validate</param>
        /// <param name="messageParameters">The message parameters.</param>
        /// <exception cref="System.ArgumentException">Raised when the condition is <c>true</c></exception>
        [Obsolete("Useless and confusing, use IsTrue instead")]
        public static void IsFalse(string message, bool condition, params string[] messageParameters)
        {
            IsTrue(message, !condition, messageParameters);
        }

        /// <summary>
        /// Raise an exception if the condition is <c>true</c>.
        /// </summary>
        /// <param name="argumentName">Name of the argument.</param>
        /// <param name="message">The message.</param>
        /// <param name="condition">The condition to validate</param>
        /// <param name="messageParameters">The message parameters.</param>
        /// <exception cref="System.ArgumentException">Raised when the condition is <c>true</c></exception>
        [Obsolete("Useless and confusing, use IsTrue instead")]
        public static void IsFalse(string argumentName, string message, bool condition, params string[] messageParameters)
        {
            IsTrue(message, argumentName, !condition, messageParameters);
        }
    }
}