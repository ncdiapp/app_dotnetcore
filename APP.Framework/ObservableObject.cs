using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace APP.Framework
{
    /// <summary>
    ///   Provide binding support in silverlight and WPF
    /// </summary>
    [DataContract(Namespace = FrameworkContractNamespaces.Core)]
    public class ObservableObject : INotifyPropertyChanged
    {
        // Events
        /// <summary>
        ///   Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // Constructors
        /// <summary>
        ///   Initializes a new instance of the <see cref = "ObservableObject" /> class.
        /// </summary>
        public ObservableObject()
        {
            Initialize();
        }

        // Properties
        /// <summary>
        ///   Gets or sets the property values.
        /// </summary>
        /// <value>The property values.</value>
        protected Dictionary<string, object> PropertyValues { get; private set; }

        // Accessors methods
        /// <summary>
        ///   Gets the value of the property represented by the expression.
        /// </summary>
        /// <typeparam name = "TValue">The type of the value.</typeparam>
        /// <param name = "expression">The expression.</param>
        /// <returns>Return the value of the property</returns>
        protected TValue GetValue<TValue>(Expression<Func<TValue>> expression)
        {
            return GetValue<TValue>(ObjectInfoHelper.GetName(expression));
        }

        /// <summary>
        ///   Gets the value of the property represented by the expression.
        /// </summary>
        /// <typeparam name = "TValue">The type of the value.</typeparam>
        /// <param name = "propertyName">Name of the property.</param>
        /// <returns>
        ///   Return the value of the property
        /// </returns>
        public virtual TValue GetValue<TValue>(string propertyName)
        {
            ArgumentValidator.IsNotNullOrEmpty("propertyName", propertyName);

            object value;

			PropertyValues.TryGetValue(propertyName, out value);


			if (PropertyValues.TryGetValue(propertyName, out value) && value != null)
            {
                return (TValue)value;
            }

            return default(TValue);
        }

        /// <summary>
        ///   Sets the value of the property represented by the expression and raise the <see cref = "E:PropertyChanged" /> event.
        /// </summary>
        /// <typeparam name = "TValue">The type of the value.</typeparam>
        /// <param name = "expression">The expression.</param>
        /// <param name = "value">The value.</param>
        /// <returns>
        ///   Return the result of the set operation
        /// </returns>
        public SetPropertyValueResult SetValue<TValue>(Expression<Func<TValue>> expression, TValue value)
        {
            return SetValue(ObjectInfoHelper.GetName(expression), value);
        }

        /// <summary>
        /// Sets the value of the property represented by the expression and raise the <see cref="E:PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// Return the result of the set operation
        /// </returns>
        public virtual SetPropertyValueResult SetValue(string propertyName, object value)
        {
            SetPropertyValueResult result = new SetPropertyValueResult();

            result.PropertyName = propertyName;
            result.NewValue = value;

            object oldValue;

            if (!PropertyValues.TryGetValue(result.PropertyName, out oldValue))
            {
                SetNewPropertyValue(result, value);
            }
            else if (!Equals(oldValue, value))
            {
                SetExitingPropertyValue(result, value, oldValue);
            }

            if (result.PropertyHasChanged)
            {
                OnPropertyChanged(result);
            }

            return result;
        }

        /// <summary>
        ///   Sets the new property value.
        /// </summary>
        /// <param name = "result">The result.</param>
        /// <param name = "value">The value.</param>
        private void SetNewPropertyValue(SetPropertyValueResult result, object value)
        {
            if (value != null)
            {
                PropertyValues.Add(result.PropertyName, value);
                result.PropertyHasChanged = true;
            }
        }

        /// <summary>
        ///   Sets the exiting property value.
        /// </summary>
        /// <param name = "result">The result.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "oldValue">The old value.</param>
        private void SetExitingPropertyValue(SetPropertyValueResult result, object value, object oldValue)
        {
            if (value == null)
            {
                PropertyValues.Remove(result.PropertyName);
            }
            else
            {
                PropertyValues[result.PropertyName] = value;
            }

            result.OldValue = oldValue;
            result.PropertyHasChanged = true;
        }

        /// <summary>
        ///   Clears the properties.
        /// </summary>
        public void ClearProperties()
        {
            var propertyNames = PropertyValues.Keys.ToArray();
            PropertyValues = new Dictionary<string, object>();
            propertyNames.ForAll(o => OnPropertyChanged(o));
        }

        /// <summary>
        ///   Clone this instance.
        ///   If the value of a property is a reference type the clone will target the same instance.
        ///   The value will not be cloned.
        /// </summary>
        /// <typeparam name = "T">Type of the clone</typeparam>
        /// <returns>Return the clone of the current object</returns>
        public T Clone<T>()
            where T : ObservableObject
        {
            T clone = Activator.CreateInstance<T>();

            foreach (var property in PropertyValues)
            {
                clone.PropertyValues.Add(property.Key, property.Value);
            }

            return clone;
        }

        // Initialization methods
        private void Initialize()
        {
            PropertyValues = new Dictionary<string, object>();
            OnInitialize();
        }

        /// <summary>
        ///   Called during the initialization of the instance
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        // Serialization event methods
        [OnDeserializing]
        internal void Initialize(StreamingContext context)
        {
            Initialize();
        }

        // OnPropertyChanged methods
        /// <summary>
        ///   Raises the <see cref = "E:PropertyChanged" /> event.
        /// </summary>
        /// <typeparam name = "TValue">The type of the value.</typeparam>
        /// <param name = "property">The property.</param>
        /// <exception cref = "System.ArgumentNullException">Raised when the property expression is null</exception>
        protected void OnPropertyChanged<TValue>(Expression<Func<TValue>> property)
        {
            ArgumentValidator.IsNotNull("property", property);

            string propertyName = ObjectInfoHelper.GetName(property);

            OnPropertyChanged(propertyName);
        }

        /// <summary>
        ///   Raises the <see cref = "E:PropertyChanged" /> event.
        /// </summary>
        /// <param name = "propertyName">Name of the property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(args);
        }

        /// <summary>
        ///   Raises the <see cref = "E:PropertyChanged" /> event.
        /// </summary>
        /// <param name = "result">The result of the set of the property.</param>
        protected void OnPropertyChanged(SetPropertyValueResult result)
        {
            ObservablePropertyChangedEventArgs args = new ObservablePropertyChangedEventArgs(result);
            OnPropertyChanged(args);
        }

        /// <summary>
        ///   Raises the <see cref = "E:PropertyChanged" /> event.
        /// </summary>
        /// <param name = "args">The <see cref = "System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, args);
            }
        }
    }
}