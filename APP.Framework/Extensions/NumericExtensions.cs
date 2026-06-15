
namespace System
{
    /// <summary>
    /// Extensions methods for the numeric classes
    /// </summary>
    public static class NumericExtensions
    {
        /// <summary>
        /// Ensures the current value is in the specified range.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="current">The current.</param>
        /// <param name="max">The max.</param>
        /// <returns>Return the value in the valid range</returns>
        public static int EnsureRange(this int current, int min, int max)
        {
            return Math.Max(min, Math.Min(max, current));
        }

        /// <summary>
        /// Ensures the current value is in the specified range.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="current">The current.</param>
        /// <param name="max">The max.</param>
        /// <returns>Return the value in the valid range</returns>
        public static double EnsureRange(this double current, double min, double max)
        {
            return Math.Max(min, Math.Min(max, current));
        }

        /// <summary>
        /// Ensures the current value is in the specified range.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="current">The current.</param>
        /// <param name="max">The max.</param>
        /// <returns>Return the value in the valid range</returns>
        public static decimal EnsureRange(this decimal current, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(max, current));
        }

        /// <summary>
        /// Ensures the current value is in the specified range.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="current">The current.</param>
        /// <param name="max">The max.</param>
        /// <returns>Return the value in the valid range</returns>
        public static float EnsureRange(this float current, float min, float max)
        {
            return Math.Max(min, Math.Min(max, current));
        }

        /// <summary>
        /// Ensures the current value is in the specified range.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="current">The current.</param>
        /// <param name="max">The max.</param>
        /// <returns>Return the value in the valid range</returns>
        public static long EnsureRange(this long current, long min, long max)
        {
            return Math.Max(min, Math.Min(max, current));
        }
    }
}
