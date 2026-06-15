using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace APP.Framework
{
    /// <summary>
    ///   Helper class to retrieve the information of an object based on expressions
    /// </summary>
    public static class ObjectInfoHelper
    {
        /// <summary>
        /// Gets the name or path of a property based on his expression.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// Return the name or path of the property
        /// </returns>
        /// <exception cref="System.ArgumentException">Raised when the expression is not a property expression</exception>
        public static string GetName<TSource, TResult>(Expression<Func<TSource, TResult>> expression)
            where TSource : class
        {
            ArgumentValidator.IsTrue("expression", "The argument [expression] must be a property expression",
                                      expression.NodeType == ExpressionType.Lambda && expression.Body.NodeType == ExpressionType.MemberAccess);

            return GetName((MemberExpression)expression.Body);
        }

        /// <summary>
        /// Gets the name of a property based on his expression.
        /// This method can be used only when it's called inside the object that contains the property
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// Return the name or path of the property
        /// </returns>
        /// <exception cref="System.ArgumentException">Raised when the expression is not a property expression</exception>
        public static string GetName<TValue>(Expression<Func<TValue>> expression)
        {
            ArgumentValidator.IsTrue("expression", "The argument [expression] must be a property expression",
                                      expression.NodeType == ExpressionType.Lambda && expression.Body.NodeType == ExpressionType.MemberAccess);

            return GetName((MemberExpression)expression.Body);
        }

        /// <summary>
        /// Gets the name or path of a property/field based on his member expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// Return the name or path of the property
        /// </returns>
        public static string GetName(MemberExpression expression)
        {
            string path = string.Empty;

            MemberExpression currentMember = expression;

            while (currentMember != null)
            {
                MemberInfo property = (MemberInfo)currentMember.Member;
                path = property.Name + path;

                currentMember = currentMember.Expression as MemberExpression;

                if (currentMember != null)
                {
                    path = "." + path;
                }
            }

            return path;
        }
    }
}