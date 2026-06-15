using System.Linq;
using System.Text;
using APP.Framework;


namespace System.IO
{
    /// <summary>
    ///   Extensions methods for the class stream
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        ///   Convert a stream to a string
        /// </summary>
        /// <param name = "stream">The stream.</param>
        /// <returns>Return the string that represent the stream</returns>
        public static string ToStringValue(this Stream stream)
        {
            ArgumentValidator.IsNotNull("stream", stream);

            stream.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }
}