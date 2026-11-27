using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExchangeBL
{
    /// <summary>
    /// Single place that makes a third-party API response safe for mapping to
    /// Transaction / Search View / other APIs / AI Agent: always a JSON object or JSON array.
    /// </summary>
    public static class ApiResponseNormalizer
    {
        /// <summary>
        /// Property name used when the third-party body is not a JSON object/array.
        /// "App" prefix keeps it from clashing with real third-party properties.
        /// Consumers can detect a wrapped response: the object has exactly this one property.
        /// </summary>
        public const string TextPropertyName = "AppResponseText";

        public const string ErrorPropertyName = "error";

        /// <summary>
        /// Normalizes the body of a successful (2xx) response.
        /// JSON object/array: returned unchanged. Empty body: returned as empty string (callers decide).
        /// Anything else (plain text, JSON scalar, XML, HTML): wrapped as {"AppResponseText": "..."}.
        /// </summary>
        public static string NormalizeSuccessBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            if (TryParseJson(body, out JToken token) && (token.Type == JTokenType.Object || token.Type == JTokenType.Array))
                return body;

            // JSON string literal ("Save success") -> use the unquoted value; everything else keeps the raw text.
            string text = (token != null && token.Type == JTokenType.String) ? token.Value<string>() : body.Trim();

            return new JObject { [TextPropertyName] = text }.ToString(Formatting.None);
        }

        /// <summary>
        /// Builds the JSON body for a failed call: {"error": ...}.
        /// If the third-party error body is itself a JSON object/array it is embedded as is, otherwise as a string.
        /// </summary>
        public static string BuildErrorJson(string errorBody)
        {
            JToken errorValue;

            if (!string.IsNullOrWhiteSpace(errorBody)
                && TryParseJson(errorBody, out JToken token)
                && (token.Type == JTokenType.Object || token.Type == JTokenType.Array))
            {
                errorValue = token;
            }
            else
            {
                errorValue = new JValue(errorBody ?? string.Empty);
            }

            return new JObject { [ErrorPropertyName] = errorValue }.ToString(Formatting.None);
        }

        /// <summary>
        /// JSON to return when the third-party body is empty (e.g. 204).
        /// Uses the saved sample to keep the same root shape: sample is an array -> "[]", otherwise "{}".
        /// </summary>
        public static string EmptyJsonFor(string sampleJson)
        {
            if (!string.IsNullOrWhiteSpace(sampleJson) && sampleJson.TrimStart().StartsWith("[", StringComparison.Ordinal))
                return "[]";

            return "{}";
        }

        private static bool TryParseJson(string text, out JToken token)
        {
            try
            {
                token = JToken.Parse(text);
                return true;
            }
            catch (JsonReaderException)
            {
                token = null;
                return false;
            }
        }
    }
}
