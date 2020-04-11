//Microsoft Public License(MS-PL)

//This license governs use of the accompanying software.If you use the software, you
//accept this license.If you do not accept the license, do not use the software.

//1. Definitions
//The terms "reproduce," "reproduction," "derivative works," and "distribution" have the
//same meaning here as under U.S.copyright law.
//A "contribution" is the original software, or any additions or changes to the software.
//A "contributor" is any person that distributes its contribution under this license.
//"Licensed patents" are a contributor's patent claims that read directly on its contribution.

//2. Grant of Rights
//(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
//(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

//3. Conditions and Limitations
//(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
//(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
//(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
//(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
//(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.You may have additional consumer rights under your local laws which this license cannot change.To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
//© 2020 GitHub, Inc.


// This file based on http://blogs.microsoft.co.il/blogs/shimmy/archive/2010/12/26/observabledictionary-lt-tkey-tvalue-gt-c.aspx
// Used under Ms-PL from http://blogs.microsoft.co.il/blogs/shimmy/about.aspx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Documents;

namespace DNS_Server.Classes
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private const string IndexerName = "Item[]";

        protected IDictionary<TKey, TValue> Dictionary { get; private set; }

        #region Constructors

        public ObservableDictionary()
        {
            this.Dictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            this.Dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public ObservableDictionary(int capacity)
        {
            this.Dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            this.Dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        #endregion Constructors

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            this.Insert(key, value, true);
        }

        public bool ContainsKey(TKey key)
        {
            return this.Dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return this.Dictionary.Keys; }
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            TValue value;
            this.Dictionary.TryGetValue(key, out value);
            var removed = this.Dictionary.Remove(key);
            if (removed)
            {

                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value));
            }

            return removed;
        }

        public bool Remove(Predicate<TKey> key)
        {
            if(key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var removedItems = new List<KeyValuePair<TKey, TValue>>();

            foreach(var localKey in this)
            {
                if(key.Invoke(localKey.Key))
                {
                    removedItems.Add(localKey);
                    this.Dictionary.Remove(localKey);
                }
            }

            if(removedItems.Count > 0)
            {
                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems);
            }

            return removedItems.Count > 0;
        }

        public bool RemoveValues(Predicate<TValue> key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var removedItems = new List<KeyValuePair<TKey, TValue>>();

            foreach (var localKey in this)
            {
                if (key.Invoke(localKey.Value))
                {
                    removedItems.Add(localKey);
                    this.Dictionary.Remove(localKey);
                }
            }

            if (removedItems.Count > 0)
            {
                this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems);
            }

            return removedItems.Count > 0;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.Dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return this.Dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.Dictionary[key];
            }
            set
            {
                this.Insert(key, value, false);
            }
        }

        #endregion IDictionary<TKey,TValue> Members

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Insert(item.Key, item.Value, true);
        }

        public void Clear()
        {
            if (this.Dictionary.Count > 0)
            {
                this.Dictionary.Clear();
                this.OnCollectionChanged();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.Dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.Dictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.Dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.Dictionary.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }

        #endregion ICollection<KeyValuePair<TKey,TValue>> Members

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.Dictionary.GetEnumerator();
        }

        #endregion IEnumerable<KeyValuePair<TKey,TValue>> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.Dictionary).GetEnumerator();
        }

        #endregion IEnumerable Members

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion INotifyCollectionChanged Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Members

        public void AddRange(IDictionary<TKey, TValue> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count > 0)
            {
                if (this.Dictionary.Count > 0)
                {
                    if (items.Keys.Any((k) => this.Dictionary.ContainsKey(k)))
                    {
                        throw new ArgumentException("An item with the same key has already been added.");
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            this.Dictionary.Add(item);
                        }
                    }
                }
                else
                {
                    this.Dictionary = new Dictionary<TKey, TValue>(items);
                }

                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, items.ToArray());
            }
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (this.Dictionary.TryGetValue(key, out TValue item))
            {
                if (add)
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }

                if (Equals(item, value))
                {
                    return;
                }

                this.Dictionary[key] = value;

                this.OnCollectionChanged(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, item));
            }
            else
            {
                this.Dictionary[key] = value;

                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        private void OnPropertyChanged()
        {
            this.OnPropertyChanged(nameof(this.Count));
            this.OnPropertyChanged(nameof(this.Keys));
            this.OnPropertyChanged(nameof(this.Values));
            this.OnPropertyChanged(nameof(IndexerName));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged()
        {
            this.OnPropertyChanged();
            if (!(Application.Current?.Dispatcher.CheckAccess() ?? true))
            {
                Application.Current.Dispatcher.Invoke(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
            }
            else
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
        {
            this.OnPropertyChanged();
            if (!(Application.Current?.Dispatcher.CheckAccess() ?? true))
            {
                Application.Current.Dispatcher.Invoke(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, 0)));
            }
            else
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem, 0));
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            this.OnPropertyChanged();
            if (!(Application.Current?.Dispatcher.CheckAccess() ?? true))
            {
                Application.Current.Dispatcher.Invoke(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, 0)));
            }
            else
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, 0));
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
        {
            this.OnPropertyChanged();
            if (!(Application.Current?.Dispatcher.CheckAccess() ?? true))
            {
                Application.Current.Dispatcher.Invoke(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems, 0)));
            }
            else
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems, 0));
            }
        }
    }
}
