using System.Linq;
using System.Runtime.Serialization;


namespace APP.Framework.Validation
{
    /// <summary>
    ///   Determine the type of the validation item
    /// </summary>
    [DataContract(Namespace = FrameworkContractNamespaces.Core)]
    public enum ValidationItemType
    {
        /// <summary>
        ///   The type is not set
        /// </summary>
        [EnumMember]
        NotSet = 0,

        /// <summary>
        ///   The validation item is an error
        /// </summary>
        [EnumMember]
        Error = 1,

        /// <summary>
        ///   The validation item is a warning
        /// </summary>
        [EnumMember]
        Warning = 2,

        /// <summary>
        ///   The validation item is a message
        /// </summary>
        [EnumMember]
        Message = 3,
    }
}