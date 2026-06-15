using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace APP.Framework.Validation
{
    /// <summary>
    ///   Represent a validation result
    /// </summary>
    [DataContract(Namespace = FrameworkContractNamespaces.Core)]
    public class ValidationResult
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ValidationResult" /> class.
        /// </summary>
        public ValidationResult()
        {
            Items = new List<ValidationItem>();
        }

        /// <summary>
        ///   Gets the error count.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int ErrorCount
        {
            get
            {
                return Items.Count(o => o.ItemType == ValidationItemType.Error);
            }
            internal set
            {
                // use for the serialization for the property
            }
        }

        /// <summary>
        ///   Gets the has errors.
        /// </summary>
        /// 
         [DataMember(EmitDefaultValue = true)]
        public bool HasErrors
        {
            get
            {
                return ErrorCount > 0;
            }
        }

        /// <summary>
        ///   Gets the has items.
        /// </summary>
        /// 
         [DataMember(EmitDefaultValue = true)]
        public bool HasItems
        {
            get
            {
                return Items.Count > 0;
            }
        }

        /// <summary>
        ///   Gets the has messages.
        /// </summary>
        /// 
         [DataMember(EmitDefaultValue = true)]
        public bool HasMessages
        {
            get
            {
                return MessageCount > 0;
            }
        }

        /// <summary>
        ///   Gets the has warnings.
        /// </summary>
        public bool HasWarnings
        {
            get
            {
                return WarningCount > 0;
            }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        [DataMember(EmitDefaultValue = true)]
        public bool IsValid
        {
            get
            {
                return Items.Where(e => e.ItemType == ValidationItemType.Error).Count() == 0;
            }
            internal set
            {
                // use for the serialization for the property
            }
        }

        /// <summary>
        ///   Gets or sets the validation items.
        /// </summary>
        /// <value>The validation items.</value>
        [DataMember(EmitDefaultValue = false)]
        public IList<ValidationItem> Items { get; internal set; }

        /// <summary>
        /// Gets the localized errors.
        /// </summary>
        /// <value>
        /// The localized errors.
        /// </value>
        /// 
         [DataMember(EmitDefaultValue = true)]
        public string LocalizedErrors
        {
            get
            {
                return string.Join(Environment.NewLine, Items.Where(o => o.ItemType == ValidationItemType.Error));
            }
        }

        /// <summary>
        /// Gets the localized messages.
        /// </summary>
        /// <value>
        /// The localized messages.
        /// </value>
        /// 
         [DataMember(EmitDefaultValue = true)]
        public string LocalizedMessages
        {
            get
            {
                return string.Join(Environment.NewLine, Items.Where(o => o.ItemType == ValidationItemType.Message));
            }
        }

        /// <summary>
        /// Gets the localized result.
        /// </summary>
        /// <value>
        /// The localized result.
        /// </value>
        /// 
         [DataMember(EmitDefaultValue = true)]
        public string LocalizedResult
        {
            get
            {
                return string.Join(Environment.NewLine, Items);
            }
        }

        /// <summary>
        /// Gets the localized warnings.
        /// </summary>
        /// <value>
        /// The localized warnings.
        /// </value>
        public string LocalizedWarnings
        {
            get
            {
                return string.Join(Environment.NewLine, Items.Where(o => o.ItemType == ValidationItemType.Warning));
            }
        }

        /// <summary>
        ///   Gets the message count.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int MessageCount
        {
            get
            {
                return Items.Count(o => o.ItemType == ValidationItemType.Message);
            }
            internal set
            {
                // use for the serialization for the property
            }
        }

        /// <summary>
        ///   Gets the warning count.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int WarningCount
        {
            get
            {
                return Items.Count(o => o.ItemType == ValidationItemType.Warning);
            }
            internal set
            {
                // use for the serialization for the property
            }
        }

        /// <summary>
        /// Adds the error.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="message">The message.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddError(Type entityType, string key, string message)
        {
            return AddItem(entityType, key, ValidationItemType.Error, message);
        }

        /// <summary>
        /// Adds the error.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="message">The message.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddError(Type entityType, string key, string message, string propertyName)
        {
            return AddItem(entityType, key, ValidationItemType.Error, message, propertyName);
        }

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="type">The type.</param>
        /// <param name="message">The message.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddItem(Type entityType, string key, ValidationItemType type, string message)
        {
            ValidationItem item = new ValidationItem(entityType, key, type, message);
            Items.Add(item);
            return item;
        }

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="type">The type.</param>
        /// <param name="message">The message.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddItem(Type entityType, string key, ValidationItemType type, string message, string propertyName)
        {
            ValidationItem item = new ValidationItem(entityType, key, type, message, propertyName);
            Items.Add(item);
            return item;
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="message">The message.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddMessage(Type entityType, string key, string message)
        {
            return AddItem(entityType, key, ValidationItemType.Message, message);
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="message">The message.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public ValidationItem AddMessage(Type entityType, string key, string message, string propertyName)
        {
            return AddItem(entityType, key, ValidationItemType.Message, message, propertyName);
        }

        /// <summary>
        /// Adds the warning.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="message">The message.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddWarning(Type entityType, string key, string message)
        {
            return AddItem(entityType, key, ValidationItemType.Warning, message);
        }

        /// <summary>
        /// Adds the warning.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="key">The key.</param>
        /// <param name="message">The message.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Return the created item.</returns>
        public ValidationItem AddWarning(Type entityType, string key, string message, string propertyName)
        {
            return AddItem(entityType, key, ValidationItemType.Warning, message, propertyName);
        }

        /// <summary>
        ///   Finds all items by property.
        /// </summary>
        /// <param name = "propertyName">Name of the property. Can be null or empty</param>
        /// <returns>Return the list of validation items for the specified property</returns>
        public IEnumerable<ValidationItem> FindItemByProperty(string propertyName)
        {
            if (propertyName.TrimHasValue())
            {
                return Items.Where(o => string.Equals(o.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase));
            }

            return Items.Where(o => !o.PropertyName.TrimHasValue());
        }

        /// <summary>
        ///   Finds all items by type.
        /// </summary>
        /// <param name = "type">The type.</param>
        /// <returns>Return the list of validation items for the specified type</returns>
        public IEnumerable<ValidationItem> FindItemByType(ValidationItemType type)
        {
            return Items.Where(o => o.ItemType == type);
        }

        /// <summary>
        ///   Merges the specified result.
        /// </summary>
        /// <param name = "result">The result.</param>
        public void Merge(ValidationResult result)
        {
            result.Items.ForAll(o => Items.Add(o));
        }
    }
}