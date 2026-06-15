using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using APP.Framework.Collections;
using APP.Framework.Validation;

#if SILVERLIGHT
using System.Collections.ObjectModel;
#endif

namespace APP.Framework.Communication
{
    /// <summary>
    /// Represents the result of a communication operation
    /// </summary>
    /// <typeparam name="T">The type of the result object</typeparam>
    [DataContract(Name = "OperationCallResultOf{0}", Namespace = FrameworkContractNamespaces.Core)]
    public class OperationCallResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationCallResult&lt;T&gt;"/> class.
        /// </summary>
        public OperationCallResult()
        {
            ValidationResult = new ValidationResult();
        }

        /// <summary>
        /// Gets or sets the client error.
        /// </summary>
        /// <value>
        /// The client error.
        /// </value>
        [IgnoreDataMember]
        public Exception ClientError { get; set; }

        /// <summary>
        /// Gets a value that specifies whether the operation has a result.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public bool HasResult
        {
            get
            {
                return Object != null || ObjectList != null;
            }
            internal set
            {
                //use for serialization
            }
        }

        /// <summary>
        /// Gets if the operation is successful.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public bool IsSuccessful
        {
            get
            {
                return ClientError == null && ValidationResult.IsValid;
            }
            internal set
            {
                //use for serialization
            }
        }

        [DataMember(EmitDefaultValue = true)]
        public bool IsForcedToUpdateUI
        {
            get;set;
        }


        


        /// <summary>
        /// Gets a value that specifies whether the operation is successful and has a result.
        /// </summary>
        public bool IsSuccessfulWithResult
        {
            get
            {
                return IsSuccessful && HasResult;
            }
        }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        [DataMember(EmitDefaultValue = false)]
        public T Object { get; set; }

        /// <summary>
        /// Gets or sets the result list.
        /// </summary>
        /// <value>
        /// The result list.
        /// </value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "By Design")]
        [DataMember(EmitDefaultValue = false)]
#if !SILVERLIGHT
        public IEnumerable<T> ObjectList { get; set; }

#else
        public ObservableCollection<T> ObjectList { get; set; }
#endif

        /// <summary>
        /// Gets or sets the validation result.
        /// </summary>
        /// <value>
        /// The validation result.
        /// </value>
        [DataMember(EmitDefaultValue = false)]
        public ValidationResult ValidationResult { get; set; }

        /// <summary>
        /// Gets the object list as set.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <returns>
        /// Return the set
        /// </returns>
        public ObservableSet<TItem> GetObjectListAsSet<TItem>()
            where TItem : class, IEditableObject
        {
            if (ObjectList == null)
            {
                return null;
            }

            ObservableSet<TItem> set = new ObservableSet<TItem>();

            set.UnionWith(ObjectList.Cast<TItem>());

            return set;
        }
    }
}