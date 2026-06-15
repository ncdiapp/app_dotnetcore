using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using APP.Framework.Globalization;

namespace APP.Framework.Validation
{
    /// <summary>
    ///   Represent a validation item
    /// </summary>
    [DataContract(Namespace = FrameworkContractNamespaces.Core)]
    public class ValidationItem
    {
        private string _localizedMessage;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ValidationItem" /> class.
        /// </summary>
        /// <param name = "entityType">Type of the entity.</param>
        /// <param name = "key">The key.</param>
        /// <param name = "type">The type.</param>
        /// <param name = "message">The message.</param>
        public ValidationItem(Type entityType, string key, ValidationItemType type, string message)
            : this()
        {
            Key = key;
            ItemType = type;
            EntityType = entityType;
            Message = message;
            DateCreated = DateTime.UtcNow;
        }

        public ValidationItem(Type entityType, string key, ValidationItemType type, string message, bool isHaveDetails, string details)
           : this(entityType, key, type, message)
        {           
            Details = details;
        }


        /// <summary>
        ///   Initializes a new instance of the <see cref = "ValidationItem" /> class.
        /// </summary>
        /// <param name = "entityType">Type of the entity.</param>
        /// <param name = "key">The key.</param>
        /// <param name = "type">The type.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "propertyName">Name of the property.</param>
        public ValidationItem(Type entityType, string key, ValidationItemType type, string message, string propertyName)
            : this(entityType, key, type, message)
        {
            PropertyName = propertyName;
        }

        // Use by the serialization
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ValidationItem" /> class.
        ///   This constructor is used by the data contract serializer and should not be called in the code
        /// </summary>
        internal ValidationItem()
        {
            MessageParameters = new List<object>();
        }

        /// <summary>
        ///   Gets the type of the entity.
        /// </summary>
        /// <value>The type of the entity.</value>
        public Type EntityType { get; internal set; }

        /// <summary>
        ///   Gets the type of the item.
        /// </summary>
        /// <value>The type of the item.</value>
        [DataMember(EmitDefaultValue = false)]
        public ValidationItemType ItemType { get; internal set; }

        /// <summary>
        ///   Gets the key.
        /// </summary>
        /// <value>The key.</value>
       // [DataMember]
        public string Key { get; internal set; }

        /// <summary>
        ///   Gets the localized and formatted message.
        /// </summary>
        [DataMember]
        public string LocalizedMessage
        {
            get
            {
                if (_localizedMessage == null)
                {
                    _localizedMessage = BuildLocalizedMessage();
                }

                return _localizedMessage;
            }
            internal set
            {
                // use by the serialization
            }
        }

        /// <summary>
        ///   Gets the message.
        /// </summary>
        /// <value>The message.</value>
        [DataMember(EmitDefaultValue = false)]
        public string Message { get; internal set; }


        [DataMember(EmitDefaultValue = false)]
        public DateTime DateCreated { get; internal set; }


        [IgnoreDataMember]
        public int? TransactionId { get; set; }

        [IgnoreDataMember]
        public string TransactionRId { get; set; }

        [IgnoreDataMember]
        public int? CommandId { get; set; }


        [IgnoreDataMember]
        public bool IsChildCommandValidationItem { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Details { get; internal set; }

        /// <summary>
        ///   Gets the message parameters.
        /// </summary>
      //  [DataMember(EmitDefaultValue = false)]
        public IList<object> MessageParameters { get; internal set; }

        /// <summary>
        ///   Gets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        [DataMember(EmitDefaultValue = false)]
        public string PropertyName { get; internal set; }

        /// <summary>
        /// Adds the message parameter.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>Returns the current instance</returns>
        public ValidationItem AddMessageParameter(object parameter)
        {
            MessageParameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Adds the message parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Returns the current instance</returns>
        public ValidationItem AddMessageParameters(params object[] parameters)
        {
            foreach (var parameter in parameters)
            {
                MessageParameters.Add(parameter);
            }
            return this;
        }

        /// <summary>
        ///   Returns a <see cref = "System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///   A <see cref = "System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string itemTypeMessage = StringLocalizer.LocalizeEnumValue("v2k_", ItemType);
            return itemTypeMessage + ": " + LocalizedMessage;
        }

        /// <summary>
        ///   Builds the localized message.
        /// </summary>
        /// <returns>Returns the localized and formatted message</returns>
        private string BuildLocalizedMessage()
        {
            string localizedMessage;

            if (Message.TrimHasValue())
            {
                localizedMessage = StringLocalizer.Localize(Key, Message);
            }
            else
            {
                localizedMessage = StringLocalizer.Localize(Key);
            }

            string toReturn = localizedMessage;

            try 
            {
                toReturn = string.Format(CultureInfo.InvariantCulture, localizedMessage, MessageParameters.ToArray());
            }
            catch (Exception ex)
            { 
            
            }

            return toReturn;
        }
    }
}