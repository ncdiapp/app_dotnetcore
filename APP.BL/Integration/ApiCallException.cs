using System;

namespace ExchangeBL
{
    /// <summary>
    /// Thrown when a third-party API answers with a non-2xx HTTP status.
    /// Keeps the original status code and raw body. Message is the raw body so existing
    /// error messages shown to users stay the same.
    /// </summary>
    public class ApiCallException : Exception
    {
        public int StatusCode { get; }

        public string Body { get; }

        public ApiCallException(int statusCode, string body)
            : base(string.IsNullOrWhiteSpace(body) ? "HTTP " + statusCode : body)
        {
            StatusCode = statusCode;
            Body = body ?? string.Empty;
        }
    }
}
