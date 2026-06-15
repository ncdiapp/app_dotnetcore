using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using APP.Framework.Collections;

namespace APP.Framework
{
    /// <summary>
    ///   Represents an editable object with change tracking.
    /// </summary>
    [DataContract(Namespace = FrameworkContractNamespaces.Core)]
    public class EditableObject : ObservableObject, IEditableObject
    {
        private bool _isModified = false;

        /// <summary>
        /// Name of the property Id
        /// </summary>
        public static readonly string IdProperty = ObjectInfoHelper.GetName<EditableObject, object>(o => o.Id);
        /// <summary>
        /// Name of the property IsNew
        /// </summary>
        public static readonly string IsNewProperty = ObjectInfoHelper.GetName<EditableObject, bool>(o => o.IsNew);
        /// <summary>
        /// Name of the property IsModified
        /// </summary>
        public static readonly string IsModifiedProperty = ObjectInfoHelper.GetName<EditableObject, bool>(o => o.IsModified);

        /// <summary>
        ///   Initializes a new instance of the <see cref = "EditableObject" /> class.
        /// </summary>
        public EditableObject()
        {
            IsChangeTrackingEnabled = true;
        }

        /// <summary>
        ///   Gets the id.
        /// </summary>
        /// <value>
        ///   The id.
        /// </value>
        [DataMember(EmitDefaultValue = false)]
        public object Id
        {
            get
            {
                return GetValue<object>(IdProperty);
            }
            set
            {
#if SILVERLIGHT

                if (SetValue(IdProperty, value).PropertyHasChanged)
                {
                    OnPropertyChanged(IsNewProperty);
                }
#else            
                value = ConvertValueToInt(value);
                if (SetValue(IdProperty, value).PropertyHasChanged)
                {
                    OnPropertyChanged(IsNewProperty);
                }
#endif

            }
        }


        public static int? ConvertValueToInt(object sourceValue)
        {
            if (sourceValue == null) return null;

            int outvalue;
            if (int.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }

        /// <summary>
        ///   Determine if the current instance is new or not.
        /// </summary>
        public virtual bool IsNew
        {
            get
            {
                return Id == null;
            }
        }


        public object NewItemTrackGuiId
        {
            get; set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public bool IsModified
        //{
        //    get
        //    {
        //        return _isModified;
        //    }
        //    set
        //    {
        //        if (_isModified != value)
        //        {
        //            _isModified = value;
        //            OnPropertyChanged(IsModifiedProperty);
        //        }
        //    }
        //}


#if SILVERLIGHT
        /// <summary>
        ///   Determine if the current instance is modified or not.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsModified
        {
            get
            {
                return _isModified;
            }
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    OnPropertyChanged(IsModifiedProperty);
                }
            }
        }
#else



        [DataMember]
        public bool IsModified
        {
            get
            {
                return _isModified;
            }
            set
            {
                _isModified = value;
            }
        }


#endif
        /// <summary>
        ///   Determine if the system track the changes done to this instance.
        /// </summary>
        public bool IsChangeTrackingEnabled { get; private set; }

        /// <summary>
        ///   Stop to track the changes done to this instance.
        /// </summary>
        public void StopChangeTracking()
        {
            IsChangeTrackingEnabled = false;
        }

        /// <summary>
        ///   Gets or sets the original property values.
        /// </summary>
        /// <value>
        ///   The original property values.
        /// </value>
        internal Dictionary<string, object> OriginalPropertyValues { get; set; }

        /// <summary>
        ///   Start to track the changes done to this instance.
        /// </summary>
        public void ResumeChangeTracking()
        {
            IsChangeTrackingEnabled = true;
        }

        /// <summary>
        ///   Raises the <see cref = "E:PropertyChanged" /> event.
        /// </summary>
        /// <param name = "args">The <see cref = "System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (IsChangeTrackingEnabled && args.PropertyName != IsModifiedProperty)
            {
                IsModified = true;
            }
        }

        [OnDeserializing]
        internal void StopChangeTracking(StreamingContext context)
        {
            StopChangeTracking();
        }

        [OnDeserialized]
        internal void ResumeChangeTracking(StreamingContext context)
        {
            ResumeChangeTracking();
        }

        /// <summary>
        ///   Called during the initialization of the instance
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            OriginalPropertyValues = new Dictionary<string, object>();
        }

        /// <summary>
        ///   Sets the value of the property represented by the expression and raise the <see cref = "E:PropertyChanged" /> event.
        ///   If the parameter trackOriginalValue is true the object will keep the original value of the property, then it will be
        ///   possible to read this value with the method GetOriginalValue()
        /// </summary>
        /// <typeparam name = "TValue">The type of the value.</typeparam>
        /// <param name = "expression">The expression.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "trackOriginalValue">if set to <c>true</c> he object will keep the original value of the property, then it will be
        ///   possible to read this value with the method GetOriginalValue().</param>
        /// <returns>
        ///   Return true if the value changed; otherwise false
        /// </returns>
        protected SetPropertyValueResult SetValue<TValue>(Expression<Func<TValue>> expression, TValue value, bool trackOriginalValue)
        {
            SetPropertyValueResult result = SetValue(expression, value);

            if (result.PropertyHasChanged && trackOriginalValue && IsChangeTrackingEnabled && !OriginalPropertyValues.ContainsKey(result.PropertyName))
            {
                OriginalPropertyValues.Add(result.PropertyName, result.OldValue);
            }

            return result;
        }

        /// <summary>
        /// Sets the value of the property represented by the expression and raise the <see cref="E:PropertyChanged"/> event.
        /// If the parameter trackOriginalValue is true the object will keep the original value of the property, then it will be
        /// possible to read this value with the method GetOriginalValue()
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        /// <param name="trackOriginalValue">if set to <c>true</c> he object will keep the original value of the property, then it will be
        /// possible to read this value with the method GetOriginalValue().</param>
        /// <returns>
        /// Return true if the value changed; otherwise false
        /// </returns>
        protected SetPropertyValueResult SetValue<TValue>(string propertyName, TValue value, bool trackOriginalValue)
        {
            SetPropertyValueResult result = SetValue(propertyName, value);

            if (result.PropertyHasChanged && trackOriginalValue && IsChangeTrackingEnabled && !OriginalPropertyValues.ContainsKey(result.PropertyName))
            {
                OriginalPropertyValues.Add(result.PropertyName, result.OldValue);
            }

            return result;
        }

        /// <summary>
        ///   Gets the original value of the property represented by the expression.
        /// </summary>
        /// <typeparam name = "TValue">The type of the value.</typeparam>
        /// <param name = "expression">The expression.</param>
        /// <returns>
        ///   Return the original value of the property
        /// </returns>
        protected TValue GetOriginalValue<TValue>(Expression<Func<TValue>> expression)
        {
            string propertyName = ObjectInfoHelper.GetName(expression);
            return GetOriginalValue<TValue>(propertyName);
        }

        /// <summary>
        /// Gets the original value of the property represented by the expression.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// Return the original value of the property
        /// </returns>
        public TValue GetOriginalValue<TObject, TValue>(Expression<Func<TObject, TValue>> expression)
            where TObject : class
        {
            string propertyName = ObjectInfoHelper.GetName(expression);
            return GetOriginalValue<TValue>(propertyName);
        }

        /// <summary>
        /// Gets the original value of the property represented by the expression.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>
        /// Return the original value of the property
        /// </returns>
        public TValue GetOriginalValue<TValue>(string propertyName)
        {
            object value;

            if (OriginalPropertyValues.TryGetValue(propertyName, out value) && value != null)
            {
                return (TValue)value;
            }

            return default(TValue);
        }

        /// <summary>
        ///   Determines whether any related entities are modified (Including the current instance).
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any related entities are modified; otherwise, <c>false</c>.
        /// </returns>
        public bool IsRelatedEntitiesModified()
        {
            if (IsModified)
            {
                return true;
            }

            Type entityType = GetType();

            PropertyInfo[] properties = entityType.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                EditableMemberAttribute attribute = property.GetCustomAttributes(typeof(EditableMemberAttribute), true)
                    .Cast<EditableMemberAttribute>()
                    .SingleOrDefault();

                if (attribute != null)
                {
                    object value = property.GetValue(this, null);

                    if (value != null)
                    {
                        IEditableObject editableObject = value as IEditableObject;
                        IEditableCollection editableCollection = value as IEditableCollection;

                        if (editableObject == null && editableCollection == null)
                        {
                            throw new NotSupportedException("The editable object attribute can only be applied to an IEditableObject or an IEditableCollection");
                        }

                        if ((editableObject != null && editableObject.IsRelatedEntitiesModified()) || (editableCollection != null && editableCollection.IsModified()))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

#if !SILVERLIGHT
        // for json
        [DataMember]
        public Dictionary<string, List<object>> DictDeletedItemsIds
        {
            get;
            set;
        }

        [DataMember]
        public List<object> DeletedItemsIds
        {
            get;
            set;
        }
#endif

    }
}