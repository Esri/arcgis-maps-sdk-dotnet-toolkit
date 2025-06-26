// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Esri.ArcGISRuntime.Toolkit;

/// <summary>
/// A publicly read-only collection that can still be modified internally and raise collection changed events.
/// </summary>
/// <typeparam name="T">Collection type</typeparam>
internal sealed class ReadonlyObservableCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private static PropertyChangedEventArgs CountChanged = new PropertyChangedEventArgs(nameof(Count)); // Re-used to avoid allocating event args on each event
    private static PropertyChangedEventArgs ItemsChanged = new PropertyChangedEventArgs("Items[]"); // Re-used to avoid allocating event args on each event
    private readonly List<T> _items;

    public ReadonlyObservableCollection()
    {
        _items = new List<T>();
    }

    public T this[int index] => _items[index];

    public int Count => _items.Count;

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    internal void AddItem(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        _items.Add(item);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
        PropertyChanged?.Invoke(this, CountChanged);
        PropertyChanged?.Invoke(this, ItemsChanged);
    }

    internal void AddRange(IEnumerable<T> items)
    {
        var startIndex = _items.Count;
        _items.AddRange(items);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), startIndex));
        PropertyChanged?.Invoke(this, CountChanged);
        PropertyChanged?.Invoke(this, ItemsChanged);
    }

    internal void RemoveItem(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        var idx = _items.IndexOf(item);
        if (idx >= 0 && _items.Remove(item))
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, idx));
            PropertyChanged?.Invoke(this, CountChanged);
            PropertyChanged?.Invoke(this, ItemsChanged);
        }
    }

    internal void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Items[]"));
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
}
