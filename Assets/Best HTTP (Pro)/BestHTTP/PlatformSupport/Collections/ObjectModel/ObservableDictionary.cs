﻿#if NETFX_CORE
    using specialized = System.Collections.Specialized;
#else
using specialized = PlatformSupport.Collections.Specialized;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PlatformSupport.Collections.ObjectModel
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, specialized.INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private const string CountString = "Count";
        private const string IndexerName = "Item[]";
        private const string KeysName = "Keys";
        private const string ValuesName = "Values";

        protected IDictionary<TKey, TValue> Dictionary { get; private set; }

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Dictionary).GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void AddRange(IDictionary<TKey, TValue> items)
        {
            if (items == null) throw new ArgumentNullException("items");

            if (items.Count > 0)
            {
                if (Dictionary.Count > 0)
                {
                    if (items.Keys.Any(k => Dictionary.ContainsKey(k)))
                        throw new ArgumentException("An item with the same key has already been added.");
                    foreach (var item in items) Dictionary.Add(item);
                }
                else
                {
                    Dictionary = new Dictionary<TKey, TValue>(items);
                }

                OnCollectionChanged(specialized.NotifyCollectionChangedAction.Add, items.ToArray());
            }
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null) throw new ArgumentNullException("key");

            TValue item;
            if (Dictionary.TryGetValue(key, out item))
            {
                if (add) throw new ArgumentException("An item with the same key has already been added.");
                if (Equals(item, value)) return;
                Dictionary[key] = value;

                OnCollectionChanged(specialized.NotifyCollectionChangedAction.Replace,
                    new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, item));
            }
            else
            {
                Dictionary[key] = value;

                OnCollectionChanged(specialized.NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        private void OnPropertyChanged()
        {
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged()
        {
            OnPropertyChanged();
            if (CollectionChanged != null)
                CollectionChanged(this,
                    new specialized.NotifyCollectionChangedEventArgs(specialized.NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(specialized.NotifyCollectionChangedAction action,
            KeyValuePair<TKey, TValue> changedItem)
        {
            OnPropertyChanged();
            if (CollectionChanged != null)
                CollectionChanged(this, new specialized.NotifyCollectionChangedEventArgs(action, changedItem));
        }

        private void OnCollectionChanged(specialized.NotifyCollectionChangedAction action,
            KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            OnPropertyChanged();
            if (CollectionChanged != null)
                CollectionChanged(this, new specialized.NotifyCollectionChangedEventArgs(action, newItem, oldItem));
        }

        private void OnCollectionChanged(specialized.NotifyCollectionChangedAction action, IList newItems)
        {
            OnPropertyChanged();
            if (CollectionChanged != null)
                CollectionChanged(this, new specialized.NotifyCollectionChangedEventArgs(action, newItems));
        }

        #region Constructors

        public ObservableDictionary()
        {
            Dictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public ObservableDictionary(int capacity)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        #endregion

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys => Dictionary.Keys;

        public bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException("key");

            TValue value;
            Dictionary.TryGetValue(key, out value);
            var removed = Dictionary.Remove(key);
            if (removed)

                //OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value));
                OnCollectionChanged();


            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values => Dictionary.Values;

        public TValue this[TKey key]
        {
            get => Dictionary[key];
            set => Insert(key, value, false);
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Insert(item.Key, item.Value, true);
        }

        public void Clear()
        {
            if (Dictionary.Count > 0)
            {
                Dictionary.Clear();
                OnCollectionChanged();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Dictionary.CopyTo(array, arrayIndex);
        }

        public int Count => Dictionary.Count;

        public bool IsReadOnly => Dictionary.IsReadOnly;

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        #endregion
    }
}