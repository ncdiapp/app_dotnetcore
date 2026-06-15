using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace APP.Framework.Collections
{
	/// <summary>
	///   Represents a set of objects.
	/// </summary>
	/// <typeparam name = "T">Type of the items of the collection</typeparam>
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "It's more specific than a collection")]
	[DataContract(Name = "ObservableSet_{0}", Namespace = FrameworkContractNamespaces.Core)]
	[DebuggerDisplay("Count={Count}")]
	public class ObservableSet<T> : ISet<T>, IEditableCollection, IList<T>, IList
		where T : class, IEditableObject
	{
		// Constructor
		/// <summary>
		///   Initializes a new instance of the <see cref = "ObservableSet&lt;T&gt;" /> class.
		/// </summary>
		public ObservableSet()
		{
			Initialize(new StreamingContext());
		}

		// Events
		/// <summary>
		///   Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///   Occurs when a property value of an item changes.
		/// </summary>
		public event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;

		/// <summary>
		///   Occurs when the collection changes.
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		// Properties
		/// <summary>
		/// Gets or sets the items.
		/// </summary>
		/// <value>The items.</value>
		protected SetItemCollection Items { get; private set; }

		[DataMember]
		[DebuggerNonUserCode]
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal ICollection<T> InternalItems
		{
			get
			{
				return Items;
			}
			set
			{
				if (value == null)
				{
					Items.Clear();
				}
				else
				{
					value.ForAll(o => AddItemInternal(o));
				}
			}
		}




#if SILVERLIGHT

               /// <summary>
        /// Gets or sets the deleted item ids.
        /// </summary>
        /// <value>The deleted item ids.</value>
        [DataMember]
        public List<object> DeletedItemIds { get; set; }
#else
		/// <summary>
		/// Gets or sets the deleted item ids.
		/// </summary>
		/// <value>The deleted item ids.</value>
		/// 

		private List<object> _DeletedItemIds = new List<object>();

		[DataMember]
		public List<object> DeletedItemIds
		{
			get
			{

				return _DeletedItemIds;
			}

			set
			{
				if (value != null && value.Count > 0)
				{
					List<object> toReturn = new List<object>();
					foreach (object o in value)
					{
						toReturn.Add(EditableObject.ConvertValueToInt(o));
					}

					_DeletedItemIds = toReturn;

				}
				else
				{
					_DeletedItemIds = value;

				}



			}

		}
#endif


		/// <summary>
		///   Gets the count.
		/// </summary>
		/// <returns>The number of elements contained in the <see cref = "T:System.Collections.Generic.ICollection`1"></see>.</returns>
		public int Count
		{
			get
			{
				return Items.Count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the collection is read only
		/// </summary>
		/// <returns>true if the <see cref = "T:System.Collections.Generic.ICollection`1"></see> is read-only; otherwise, false.</returns>
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// The element at the specified index.
		///   </returns>
		///
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
		///   </exception>
		///
		/// <exception cref="T:System.NotSupportedException">
		/// The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
		///   </exception>
		T IList<T>.this[int index]
		{
			get
			{
				return Items.ElementAt(index);
			}
			set
			{
				Items.SetElementAt(index, value);
			}
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		/// <returns>
		/// The element at the specified index.
		///   </returns>
		///
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
		///   </exception>
		///
		/// <exception cref="T:System.NotSupportedException">
		/// The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
		///   </exception>
		object IList.this[int index]
		{
			get
			{
				return Items.ElementAt(index);
			}
			set
			{
				Items.SetElementAt(index, (T)value);
			}
		}

		/// <summary>
		/// Gets the is fixed size.
		/// </summary>
		/// <returns>true if the <see cref="T:System.Collections.IList"/> has a fixed size; otherwise, false.</returns>
		bool IList.IsFixedSize
		{
			get { return ((IList)Items).IsFixedSize; }
		}

		/// <summary>
		/// Gets the is synchronized.
		/// </summary>
		/// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
		bool ICollection.IsSynchronized
		{
			get { return ((IList)Items).IsSynchronized; }
		}

		/// <summary>
		/// Gets the sync root.
		/// </summary>
		/// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.</returns>
		object ICollection.SyncRoot
		{
			get { return ((IList)Items).SyncRoot; }
		}

		// Methods
		/// <summary>
		///   Adds an element to the current set and returns a value to indicate if the
		///   element was successfully added.
		/// </summary>
		/// <param name = "item">The element to add to the set.</param>
		/// <returns>true if the element is added to the set; false if the element is already in the set.</returns>
		public bool Add(T item)
		{
			ArgumentValidator.IsNotNull("item", item);
			object key = Items.GetKeyForItemInternal(item);

			if (Items.Contains(key))
			{
				return false;
			}

			if (!item.IsNew)
			{
				DeletedItemIds.Remove(key);
			}

			AddItemInternal(item);

			return true;
		}

		/// <summary>
		/// Adds an element to the current set and returns a value to indicate if the
		/// element was successfully added.
		/// </summary>
		/// <param name="item">The element to add to the set.</param>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.IList"/>.
		/// </summary>
		/// <param name="value">The object to add to the <see cref="T:System.Collections.IList"/>.</param>
		/// <returns>
		/// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
		int IList.Add(object value)
		{
			T item = (T)value;
			Add(item);
			return Items.IndexOf(item);
		}

		/// <summary>
		/// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
		///   </exception>
		///
		/// <exception cref="T:System.NotSupportedException">
		/// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
		///   </exception>
		void IList<T>.Insert(int index, T item)
		{
			ArgumentValidator.IsNotNull("item", item);
			object key = Items.GetKeyForItemInternal(item);

			if (!Items.Contains(key))
			{
				if (!item.IsNew)
				{
					DeletedItemIds.Remove(key);
				}

				AddItemInternal(item, index);
			}
		}

		/// <summary>
		/// Inserts an item to the <see cref="T:System.Collections.IList"/> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted.</param>
		/// <param name="value">The object to insert into the <see cref="T:System.Collections.IList"/>.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		///   <paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.IList"/>. </exception>
		///
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
		///
		/// <exception cref="T:System.NullReferenceException">
		///   <paramref name="value"/> is null reference in the <see cref="T:System.Collections.IList"/>.</exception>
		void IList.Insert(int index, object value)
		{
			ArgumentValidator.IsNotNull("value", value);

			T item = (T)value;
			object key = Items.GetKeyForItemInternal(item);

			if (!Items.Contains(key))
			{
				if (!item.IsNew)
				{
					DeletedItemIds.Remove(key);
				}

				AddItemInternal(item, index);
			}
		}

		/// <summary>
		///   Removes all elements in the specified collection from the current set.
		/// </summary>
		/// <param name = "other">The collection of items to remove from the set.</param>
		public void ExceptWith(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (other.Count() <= 0)
			{
				return;
			}

			foreach (var otherItem in other)
			{
				object key = Items.GetKeyForItemInternal(otherItem);

				RemoveItemInternal(key, otherItem);
			}
		}

		/// <summary>
		///   Modifies the current set so that it contains only elements that are also
		///   in a specified collection.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		public void IntersectWith(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			var otherDictionary = other.ToDictionary(Items.GetKeyForItemInternal);

			var originalDictionary = Items.ToDictionary();

			foreach (var item in originalDictionary)
			{
				if (!otherDictionary.ContainsKey(item.Key))
				{
					RemoveItemInternal(item.Key, item.Value);
				}
			}
		}

		/// <summary>
		///   Determines whether the current set is a property (strict) subset of a specified collection
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		/// <returns>
		///   true if the current set is a correct subset of other; otherwise, false.
		/// </returns>
		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (Items.IsEmpty())
			{
				return !other.IsEmpty();
			}

			if (Items.Count != other.Count())
			{
				return IsSubsetOf(other);
			}

			return false;
		}

		/// <summary>
		///   Determines whether the current set is a correct superset of a specified collection.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		/// <returns>
		///   true if the System.Collections.Generic.ISet&lt;T&gt; object is a correct superset of other; otherwise, false.
		/// </returns>
		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (other.IsEmpty())
			{
				return !Items.IsEmpty();
			}

			if (other.Count() != Items.Count)
			{
				return IsSupersetOf(other);
			}

			return false;
		}

		/// <summary>
		///   Determines whether a set is a subset of a specified collection.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		/// <returns>
		///   true if the current set is a subset of other; otherwise, false.
		/// </returns>
		public bool IsSubsetOf(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (Items.Count <= other.Count())
			{
				var otherDictionary = other.ToDictionary(Items.GetKeyForItemInternal);

				return !Items.Any(o => !otherDictionary.ContainsKey(Items.GetKeyForItemInternal(o)));
			}

			return false;
		}

		/// <summary>
		///   Determines whether the current set is a superset of a specified collection.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		/// <returns>
		///   true if the current set is a superset of other; otherwise, false.
		/// </returns>
		public bool IsSupersetOf(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (other.Count() <= Items.Count)
			{
				return !other.Any(o => !Contains(o));
			}

			return false;
		}

		/// <summary>
		///   Determines whether the current set overlaps with the specified collection.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		/// <returns>true if the current set and other share at least one common element; otherwise, false.</returns>
		public bool Overlaps(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			return !other.IsEmpty() && !Items.IsEmpty() && other.Any(o => Contains(o));
		}

		/// <summary>
		///   Determines whether the current set and the specified collection contain the same elements.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		/// <returns>true if the current set is equal to other; otherwise, false.</returns>
		public bool SetEquals(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			return other.Count() == Items.Count && other.All(o => Contains(o));
		}

		/// <summary>
		///   Modifies the current set so that it contains only elements that are present
		///   either in the current set or in the specified collection, but not both.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (Items.Count == 0)
			{
				UnionWith(other);
			}
			else if (other == this)
			{
				Clear();
			}
			else
			{
				foreach (var item in other)
				{
					if (!Add(item))
					{
						object key = Items.GetKeyForItemInternal(item);

						RemoveItemInternal(key, item);
					}
				}
			}
		}

		/// <summary>
		///   Modifies the current set so that it contains all elements that are present
		///   in both the current set and in the specified collection.
		/// </summary>
		/// <param name = "other">The collection to compare to the current set.</param>
		public void UnionWith(IEnumerable<T> other)
		{
			ArgumentValidator.IsNotNull("other", other);

			if (other != this)
			{
				other.ForAll(o => Add(o));
			}
		}

		/// <summary>
		///   Determines whether the <see cref = "T:System.Collections.Generic.ICollection`1"></see> contains a specific value.
		/// </summary>
		/// <param name = "item">The object to locate in the <see cref = "T:System.Collections.Generic.ICollection`1"></see>.</param>
		/// <returns>
		///   true if item is found in the <see cref = "T:System.Collections.Generic.ICollection`1"></see>; otherwise, false.
		/// </returns>
		public bool Contains(T item)
		{
			object key = Items.GetKeyForItemInternal(item);

			return Items.Contains(key);
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.IList"/> contains a specific value.
		/// </summary>
		/// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"/>.</param>
		/// <returns>
		/// true if the <see cref="T:System.Object"/> is found in the <see cref="T:System.Collections.IList"/>; otherwise, false.
		/// </returns>
		bool IList.Contains(object value)
		{
			return Contains((T)value);
		}

		/// <summary>
		///   Determines whether the <see cref = "T:System.Collections.Generic.ICollection`1"></see> contains a value with the specified key.
		/// </summary>
		/// <param name = "key">The key of the object to locate in the <see cref = "T:System.Collections.Generic.ICollection`1"></see>.</param>
		/// <returns>
		///   true if item is found in the <see cref = "T:System.Collections.Generic.ICollection`1"></see>; otherwise, false.
		/// </returns>
		public bool ContainsByKey(object key)
		{
			return Items.Contains(key);
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		/// <returns>
		/// The index of <paramref name="item"/> if found in the list; otherwise, -1.
		/// </returns>
		int IList<T>.IndexOf(T item)
		{
			return Items.IndexOf(item);
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.IList"/>.
		/// </summary>
		/// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"/>.</param>
		/// <returns>
		/// The index of <paramref name="value"/> if found in the list; otherwise, -1.
		/// </returns>
		int IList.IndexOf(object value)
		{
			return Items.IndexOf((T)value);
		}

		/// <summary>
		///   Copies the elements of the set to an
		///   System.Array, starting at a particular System.Array index.
		/// </summary>
		/// <param name = "array">The one-dimensional System.Array that is the destination of the elements
		/// copied from the set. The System.Array must
		/// have zero-based indexing.</param>
		public void CopyTo(T[] array)
		{
			CopyTo(array, 0);
		}

		/// <summary>
		/// Copies the elements of the set to an
		/// System.Array, starting at a particular System.Array index.
		/// </summary>
		/// <param name = "array">The one-dimensional System.Array that is the destination of the elements
		///   copied from the set. The System.Array must
		///   have zero-based indexing.</param>
		/// <param name = "arrayIndex">The zero-based index in array at which copying begins.</param>
		public void CopyTo(T[] array, int arrayIndex)
		{
			CopyTo(array, arrayIndex, Items.Count);
		}

		/// <summary>
		///   Copies the elements of the set to an
		///   System.Array, starting at a particular System.Array index.
		/// </summary>
		/// <param name = "array">The one-dimensional System.Array that is the destination of the elements
		///   copied from the set. The System.Array must
		///   have zero-based indexing.</param>
		/// <param name = "arrayIndex">The zero-based index in array at which copying begins.</param>
		/// <param name = "count">The count of item to copy.</param>
		public void CopyTo(T[] array, int arrayIndex, int count)
		{
			ArgumentValidator.IsNotNull("array", array);

			if (arrayIndex < 0 || arrayIndex > array.Length || count > (array.Length - arrayIndex))
			{
				throw new ArgumentOutOfRangeException("arrayIndex");
			}

			for (int i = 0; i < count; i++)
			{
				array[arrayIndex + i] = Items.ElementAt(i);
			}
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
		/// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="array"/> is null. </exception>
		///
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		///   <paramref name="index"/> is less than zero. </exception>
		///
		/// <exception cref="T:System.ArgumentException">
		///   <paramref name="array"/> is multidimensional.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>. </exception>
		///
		/// <exception cref="T:System.ArgumentException">The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>. </exception>
		public void CopyTo(Array array, int index)
		{
			ArgumentValidator.IsNotNull("array", array);

			if (index < 0 || index > array.Length || Items.Count > (array.Length - index))
			{
				throw new ArgumentOutOfRangeException("index");
			}

			for (int i = 0; i < Items.Count; i++)
			{
				array.SetValue(Items.ElementAt(i), index + i);
			}
		}

		/// <summary>
		///   Removes the first occurrence of a specific object from the <see cref = "T:System.Collections.Generic.ICollection`1"></see>.
		/// </summary>
		/// <param name = "item">The object to remove from the <see cref = "T:System.Collections.Generic.ICollection`1"></see>.</param>
		/// <returns>
		///   true if item was successfully removed from the <see cref = "T:System.Collections.Generic.ICollection`1"></see>; otherwise, false. This method also returns false if item is not found in the original <see
		///    cref = "T:System.Collections.Generic.ICollection`1"></see>.
		/// </returns>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
		public bool Remove(T item)
		{
			ArgumentValidator.IsNotNull("item", item);

			object key = Items.GetKeyForItemInternal(item);

			return RemoveItemInternal(key, item);
		}

		/// <summary>
		///   Removes the first occurrence of the object with the specified key from the <see
		///    cref = "T:System.Collections.Generic.ICollection`1"></see>.
		/// </summary>
		/// <param name = "key">The key of the object object to remove from the <see
		///    cref = "T:System.Collections.Generic.ICollection`1"></see>.</param>
		/// <returns>
		///   true if item was successfully removed from the <see cref = "T:System.Collections.Generic.ICollection`1"></see>; otherwise, false. This method also returns false if item is not found in the original <see
		///    cref = "T:System.Collections.Generic.ICollection`1"></see>.
		/// </returns>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.ICollection`1"></see> is read-only.</exception>
		public bool RemoveByKey(object key)
		{
			T item;

			if (Items.TryGetItem(key, out item))
			{
				return RemoveItemInternal(key, item);
			}

			return false;
		}

		/// <summary>
		/// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
		///   </exception>
		///
		/// <exception cref="T:System.NotSupportedException">
		/// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
		///   </exception>
		void IList<T>.RemoveAt(int index)
		{
			T item = Items.ElementAt(index);
			Remove(item);
		}

		/// <summary>
		/// Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.
		///   </exception>
		///
		/// <exception cref="T:System.NotSupportedException">
		/// The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.
		///   </exception>
		void IList.RemoveAt(int index)
		{
			T item = Items.ElementAt(index);
			Remove(item);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"/>.
		/// </summary>
		/// <param name="value">The object to remove from the <see cref="T:System.Collections.IList"/>.</param>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
		void IList.Remove(object value)
		{
			Remove((T)value);
		}

		/// <summary>
		///   Removes all items from the <see cref = "T:System.Collections.Generic.ICollection`1"></see>.
		/// </summary>
		/// <exception cref = "T:System.NotSupportedException">The <see cref = "T:System.Collections.Generic.ICollection`1"></see> is read-only. </exception>
		public void Clear()
		{
			Items.ForAll(o => MarkItemAsDeleted(Items.GetKeyForItemInternal(o), o));
			Items.Clear();
			OnCollectionChanged(NotifyCollectionChangedAction.Reset);
		}

		/// <summary>
		///   Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		///   A <see cref = "T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<T> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		/// <summary>
		///   Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		///   An <see cref = "T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		// Tracking methods
		/// <summary>
		///   Determines whether this instance is modified.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance is modified; otherwise, <c>false</c>.
		/// </returns>
		public bool IsModified()
		{
			return DeletedItemIds.Count > 0 || Items.Count(o => o.IsNew || o.IsRelatedEntitiesModified()) > 0;
		}

		/// <summary>
		///   Counts the deleted items.
		/// </summary>
		/// <returns>Return the count of deleted items in the set</returns>
		public int CountDeletedItems()
		{
			return DeletedItemIds.Count;
		}

		/// <summary>
		///   Counts the new items.
		/// </summary>
		/// <returns>Return the count of new items in the set</returns>
		public int CountNewItems()
		{
			return Items.Count(o => o.IsNew);
		}

		/// <summary>
		///   Counts the modified items.
		/// </summary>
		/// <returns>Return the count of modified items in the set</returns>
		public int CountModifiedItems()
		{
			return Items.Count(o => o.IsRelatedEntitiesModified() && !o.IsNew);
		}

		/// <summary>
		///   Finds the deleted item ids in the set.
		/// </summary>
		/// <returns>Return the list of deleted item ids present in the set</returns>
		public IEnumerable<object> FindDeletedItemIds()
		{
			return DeletedItemIds;
		}

		/// <summary>
		///   Finds the modified items in the set.
		/// </summary>
		/// <returns>Return the list of modified items present in the set</returns>
		public IEnumerable<T> FindModifiedItems()
		{
			return Items.Where(o => o.IsRelatedEntitiesModified() && !o.IsNew);
		}

		/// <summary>
		///   Finds the modified or new items in the set.
		/// </summary>
		/// <returns>Return the list of modified or new items present in the set</returns>
		public IEnumerable<T> FindModifiedOrNewItems()
		{
			return Items.Where(o => o.IsRelatedEntitiesModified() || o.IsNew);
		}

		/// <summary>
		///   Finds the new items in the set.
		/// </summary>
		/// <returns>Return the list of new items present in the set</returns>
		public IEnumerable<T> FindNewItems()
		{
			return Items.Where(o => o.IsNew);
		}

		/// <summary>
		///   Find a item by his key
		/// </summary>
		/// <param name = "key">The key.</param>
		/// <returns>Return the item found of the default value of the type T</returns>
		public T SingleOrDefaultByKey(object key)
		{
			T item;

			if (Items.TryGetItem(key, out item))
			{
				return item;
			}

			return default(T);
		}

		// Misc
		/// <summary>
		///   Gets the key for item.
		/// </summary>
		/// <param name = "item">The item.</param>
		/// <returns>Return the key of the item</returns>
		public object GetKeyForItem(T item)
		{
			return Items.GetKeyForItemInternal(item);
		}

		/// <summary>
		/// Initializes the current instance with the specified context.
		/// </summary>
		/// <param name = "context">The context.</param>
		[OnDeserializing]
		internal void Initialize(StreamingContext context)
		{
			DeletedItemIds = new List<object>();
			Items = new SetItemCollection();
		}

		/// <summary>
		/// Adds the item internal.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="index">The index.</param>
		protected virtual void AddItemInternal(T item, int index)
		{
			Items.Insert(index, item);
			item.PropertyChanged += Item_PropertyChanged;
			OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		}

		/// <summary>
		/// Adds the item internal.
		/// </summary>
		/// <param name="item">The item.</param>
		private void AddItemInternal(T item)
		{
			AddItemInternal(item, Count);
		}

		/// <summary>
		/// Removes the item internal.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="item">The item.</param>
		protected virtual bool RemoveItemInternal(object key, T item)
		{
			int index = Items.IndexOf(item);

			if (Items.Remove(key))
			{
				MarkItemAsDeleted(key, item);
				OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Marks the item as deleted.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="item">The item.</param>
		protected virtual void MarkItemAsDeleted(object key, T item)
		{
			if (!item.IsNew)
			{
				DeletedItemIds.Add(key);
			}

			item.PropertyChanged -= Item_PropertyChanged;
		}

		/// <summary>
		/// Sorts the elements of the entire collection using the specified comparison.
		/// </summary>
		/// <param name="comparison">The comparison.</param>
		public void Sort(Comparison<T> comparison)
		{
			Items.Sort(comparison);
			OnCollectionChanged(NotifyCollectionChangedAction.Reset);
		}

		/// <summary>
		/// Sorts the elements of the entire collection using the specified comparer.
		/// </summary>
		/// <param name="comparer">The comparer.</param>
		public void Sort(IComparer<T> comparer)
		{
			Items.Sort(comparer);
			OnCollectionChanged(NotifyCollectionChangedAction.Reset);
		}

		// Events
		/// <summary>
		///   Handles the PropertyChanged event of the item control.
		/// </summary>
		/// <param name = "sender">The source of the event.</param>
		/// <param name = "e">The <see cref = "System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
		private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnItemPropertyChanged(new ItemPropertyChangedEventArgs(sender, e.PropertyName));
		}

		// Event raiser
		/// <summary>
		/// Raises the <see cref = "E:ItemPropertyChanged" /> event.
		/// </summary>
		/// <param name = "e">The <see cref = "System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
		protected virtual void OnItemPropertyChanged(ItemPropertyChangedEventArgs e)
		{
			if (ItemPropertyChanged != null)
			{
				ItemPropertyChanged(this, e);
			}
		}

		/// <summary>
		///   Called when the collection changed.
		/// </summary>
		/// <param name = "action">The action.</param>
		private void OnCollectionChanged(NotifyCollectionChangedAction action)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
		}

		/// <summary>
		///   Called when the collection changed.
		/// </summary>
		/// <param name = "action">The action.</param>
		/// <param name = "item">The item.</param>
		/// <param name = "itemIndex">Index of the item.</param>
		private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int itemIndex)
		{
			if (item != null)
			{
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, itemIndex));
			}
		}

		/// <summary>
		///   Called when the collection changed.
		/// </summary>
		/// <param name = "action">The action.</param>
		/// <param name = "item">The item.</param>
		/// <param name = "oldItem">The old item.</param>
		/// <param name = "itemIndex">Index of the item.</param>
		private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, T oldItem, int itemIndex)
		{
			if (item != null)
			{
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, oldItem, itemIndex));
			}
		}

		/// <summary>
		///   Raises the <see cref = "E:CollectionChanged" /> event.
		/// </summary>
		/// <param name = "e">The <see cref = "System.Collections.Specialized.NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
			{
				CollectionChanged(this, e);
			}

			OnPropertyChanged(new PropertyChangedEventArgs("Count"));
		}

		/// <summary>
		/// Raises the <see cref="E:PropertyChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, e);
			}
		}

		// Nested Type
		/// <summary>
		/// Inner collection of the set
		/// </summary>
		protected class SetItemCollection : KeyedCollection<object, T>
		{
			/// <summary>
			///   When implemented in a derived class, extracts the key from the specified element.
			/// </summary>
			/// <param name = "item">The element from which to extract the key.</param>
			/// <returns>
			///   The key for the specified element.
			/// </returns>
			protected override object GetKeyForItem(T item)
			{
				ArgumentValidator.IsNotNull("item", item);

				if(item.IsNew )
				{
					if(item.NewItemTrackGuiId == null)
					{
						item.NewItemTrackGuiId = System.Guid.NewGuid().ToString();
					}
				}

				return item.IsNew ? item.NewItemTrackGuiId : item.Id;
			}

			/// <summary>
			///   Gets the key for item internal use only.
			/// </summary>
			/// <param name = "item">The item.</param>
			/// <returns>Return the key of the item</returns>
			public object GetKeyForItemInternal(T item)
			{
				return GetKeyForItem(item);
			}

			/// <summary>
			/// Gets the key for new item internal.
			/// </summary>
			/// <param name="item">The item.</param>
			/// <returns></returns>
			public object GetKeyForNewItemInternal(T item)
			{
				return item.GetHashCode();
			}

			/// <summary>
			/// Changes the item key internal.
			/// </summary>
			/// <param name="item">The item.</param>
			/// <param name="newKey">The new key.</param>
			public void ChangeItemKeyInternal(T item, object newKey)
			{
				ChangeItemKey(item, newKey);
			}

			/// <summary>
			/// Convert the collection to a dictionary.
			/// </summary>
			/// <returns>Return the dictionary that represents the collection</returns>
			public IDictionary<object, T> ToDictionary()
			{
				if (Dictionary == null)
				{

					//Items.ToDictionary(

					return Items.ToDictionary(GetKeyForItem);
				}

				return Dictionary;
			}

			/// <summary>
			/// Tries to get the item.
			/// </summary>
			/// <param name="key">The key.</param>
			/// <param name="item">The item.</param>
			/// <returns>
			/// Return the item of the default value
			/// </returns>
			public bool TryGetItem(object key, out T item)
			{
				if (Dictionary != null)
				{
					if (!Dictionary.TryGetValue(key, out item))
					{
						return false;
					}
				}
				else if (Contains(key))
				{
					item = this[key];
				}
				else
				{
					item = null;
					return false;
				}

				return true;
			}

			/// <summary>
			///  Gets the element at the specified index.
			/// </summary>
			/// <param name="index">The zero-based index of the element to get.</param>
			/// <returns>The element at the specified index.</returns>
			public T ElementAt(int index)
			{
				return Items[index];
			}

			/// <summary>
			/// Sets the element at.
			/// </summary>
			/// <param name="index">The index.</param>
			/// <param name="value">The value.</param>
			public void SetElementAt(int index, T value)
			{
				Items[index] = value;
			}

			/// <summary>
			/// Sorts the elements of the entire collection using the specified comparison.
			/// </summary>
			/// <param name="comparison">The comparison.</param>
			public void Sort(Comparison<T> comparison)
			{
				((List<T>)base.Items).Sort(comparison);
			}

			/// <summary>
			/// Sorts the elements of the entire collection using the specified comparer.
			/// </summary>
			/// <param name="comparer">The comparer.</param>
			public void Sort(IComparer<T> comparer)
			{
				((List<T>)base.Items).Sort(comparer);
			}
		}
	}
}