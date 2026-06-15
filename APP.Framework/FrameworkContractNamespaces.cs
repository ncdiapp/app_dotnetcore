using System.Linq;

namespace APP.Framework
{
    /// <summary>
    ///   Constant class that contains the namespace used for the data contract serialization
    /// </summary>
    public static class FrameworkContractNamespaces
    {
        /// <summary>
        /// Core namespace
        /// </summary>
        public const string Core = "http://app.com/";

        /// <summary>
        /// Search namespace
        /// </summary>
        public const string Search = Core + "search/";

        /// <summary>
        /// Search namespace
        /// </summary>
        public const string Dynamic = Core + "dynamic/";

        /// <summary>
        /// Excel namespace
        /// </summary>
        public const string Excel = Core + "excel/";

        /// <summary>
        /// Form Designer namespace
        /// </summary>
        public const string FormDesigner = Core + "formdesigner/";
    }
}