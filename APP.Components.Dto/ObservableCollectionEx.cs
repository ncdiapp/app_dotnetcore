using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;

namespace APP.Components.Dto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class ObservableCollectionEx<T> : SortableObservableCollection<T> where T : ObservableObject
    {
        public event EventHandler<EventArgs> ExAllChanged;

        public ObservableCollectionEx()
        {
        }

        public ObservableCollectionEx(IList<T> collection)
            : base(collection)
        {
            Unsubscribe(this);
            Subscribe(this);
        }

        private void Subscribe(System.Collections.IList iList)
        {
            if (iList != null)
            {
                foreach (T element in iList)
                {
                    element.PropertyChanged += (sender, eventArgs) => this.OnElementPropertyChanged(sender, eventArgs);
                }
            }

            this.CollectionChanged += (sender, eventArgs) =>
            {
                if (ExAllChanged != null)
                {
                    if (eventArgs.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (var element in eventArgs.NewItems.Cast<T>())
                        {
                            element.PropertyChanged += (senderE, eventArgEs) => this.OnElementPropertyChanged(senderE, eventArgEs);
                        }
                    }
                    if (eventArgs.Action == NotifyCollectionChangedAction.Remove)
                    {
                        foreach (var element in eventArgs.OldItems.Cast<T>())
                        {
                            element.PropertyChanged -= (senderE, eventArgEs) => this.OnElementPropertyChanged(senderE, eventArgEs);
                        }
                    }
                    ExAllChanged(sender, eventArgs);
                }
            };
        }

        private void Unsubscribe(System.Collections.IList iList)
        {
            if (iList != null)
            {
                foreach (T element in iList)
                    element.PropertyChanged -= (sender, eventArgs) => this.OnElementPropertyChanged(sender, eventArgs);

                this.CollectionChanged -= (sender, eventArgs) => { if (ExAllChanged != null) ExAllChanged(sender, eventArgs); };
            }
        }

        protected void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ExAllChanged != null)
            {
                ExAllChanged(sender, e);
            }
        }
    }

    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public SortableObservableCollection()
        {
        }

        public SortableObservableCollection(IList<T> collection)
            : base(collection)
        {
        }

        public void Sort()
        {
            this.Sort(0, Count, null);
        }

        // Pass custom comparer to Sort method
        public void Sort(IComparer<T> comparer)
        {
            this.Sort(0, Count, comparer);
        }

        // Use this method to sort part of a collection
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            (Items as List<T>).Sort(index, count, comparer);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private int _index;

        public new TValue this[TKey key]
        {
            get { return this.GetValue(key); }
            set { this.SetValue(key, value); }
        }

        public new void Add(TKey key, TValue value)
        {
            if (base.ContainsKey(key))
            {
                SetValue(key, value);
            }
            else
            {
                base.Add(key, value);
                //this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this.FindPair(key), _index));
                //OnPropertyChanged("Item[]");
            }
        }

        public new void Clear()
        {
            base.Clear();
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public new bool Remove(TKey key)
        {
            var pair = this.FindPair(key);

            if (base.ContainsKey(key))
            {
                if (base.Remove(key))
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, pair, _index));
                    OnPropertyChanged("Item[]");
                    return true;
                }
            }
            return false;
        }

        private TValue GetValue(TKey key)
        {
            if (ContainsKey(key))
                return base[key];
            else
                return default(TValue);
        }

        private void SetValue(TKey key, TValue value)
        {
            if (ContainsKey(key))
            {
                base[key] = value;
                var pair = this.FindPair(key);
                var index = _index;
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, pair, pair, index));
                OnPropertyChanged("Item[]");
            }
            else
            {
                this.Add(key, value);
            }
        }

        private KeyValuePair<TKey, TValue> FindPair(TKey key)
        {
            _index = 0;
            foreach (var pair in this)
            {
                if (pair.Key.Equals(key))
                    return pair;

                _index++;
            }
            return default(KeyValuePair<TKey, TValue>);
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}