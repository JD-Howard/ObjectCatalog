using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// Collection Performance Tests
// https://web.archive.org/web/20180302080357/http://blog.bodurov.com/Performance-SortedList-SortedDictionary-Dictionary-Hashtable/
// https://web.archive.org/web/20230818014039/https://www.gamedevblog.com/2019/07/sorted-sets-in-c-and-performance-a-mystery-remains.html

/*
Why is the SortedDictionary so much slower than all the others?
----------------------------------------------------------------------------------------------------------- 
It's a tradeoff between CPU-usage and RAM-usage. Dictionary is faster than SortedDictionary because it is
implemented as a hash-table, an algorithm that is designed to use excess memory in order to use as few
operations as possible. SortedDictionary is a binary-search-tree, an algorithm that is designed to use as
many operations as necessary to use as little RAM as possible.
*/


namespace System.Collections.Specialized;

public partial class ObjectCatalog<T> : IDisposable where T : class
{
    private readonly Dictionary<string, IValueIndex> _indices = new();
    private readonly List<Reference> _sources = new();
    private readonly ObjectCatalogType _refType;
    private readonly bool _allowNullKeys;
    private readonly WeakReference<ObservableCollection<T>>? _observable = null;
    private readonly T?[] _default = Array.Empty<T>();

    /// <summary>
    /// If your using the ObjectCatalogType.WeakReference (default)
    /// behavior, then use your source list instead of GetItems()
    /// </summary>
    public T?[] GetItems() 
        => _sources.Select(x => x.Materialize()).ToArray();

    public ObjectCatalog(
        IEnumerable<T> source, 
        ObjectCatalogType refType = ObjectCatalogType.WeakReferenced, 
        ObjectCatalogBehavior behavior = ObjectCatalogBehavior.IndexNonNullValues)
    {
        if (source is ObservableCollection<T> col)
        {
            _observable = new WeakReference<ObservableCollection<T>>(col);
            col.CollectionChanged += ObservableCollectionChanged;
        }

        _refType = refType;
        _allowNullKeys = behavior == ObjectCatalogBehavior.IncludeNulls;
        foreach (var item in source)
        {
            if (item is null)
                continue;

            _sources.Add(new Reference(item, _refType));
        }
    }

        

    private void ObservableCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var item in e.NewItems)
                    Add(item as T);
                break;
            case NotifyCollectionChangedAction.Remove:
                var items = GetItems();
                foreach (var item in e.OldItems)
                    RemoveInternal(item as T, items);
                break;
            case NotifyCollectionChangedAction.Reset:
                Reset();
                break;
        }
    }

    public void Remove(T? item)
    {
        if (item is not null)
            RemoveInternal(item, GetItems());
    }

    private void RemoveInternal(T? item, T?[] trackedItems)
    {
        if (item is null)
            return;
            
        var index = Array.IndexOf(trackedItems, item);
        if (index < 0)
            return;

        // dispose the object reference being tracked
        _sources[index].Dispose();
            
        // Removing the Reference IDs doesn't actually add value, but we do
        // want the capability of purging keys when a ValueIndex becomes empty.
        foreach (var valueIndex in _indices.Values)
            valueIndex.Remove(index);
    }

    public void Add(T? item)
    {
        if (item is null)
            return;

        var index = _sources.Count;
        _sources.Add(new Reference(item, _refType));
        foreach (var valueIndex in _indices.Values)
            valueIndex.Add(item, index, _allowNullKeys);
    }

        
    public T?[] Find<TKey,TNormal>(Expression<Func<T,TKey?>> accessor, TNormal? valueKey)
    {
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");

        var name = accessor.ToString().Replace(accessor.Parameters.First().Name, "$");
        if (!_indices.ContainsKey(name))
            AddIndexInternal<TKey, TKey>(name, accessor.Compile(), null);

        return Find(name, valueKey);
    }

    public T?[] Find<TNormal>(Enum indexPath, TNormal? valueKey)
        => Find(indexPath.ToString(), valueKey);
        
    public T?[] Find<TNormal>(string accessKey, TNormal? valueKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException("Accessor cannot be null or empty.");

        if (valueKey is null && !_allowNullKeys)
            return _default;

        _indices.TryGetValue(accessKey, out var index);
                
        var targets = index?.Find(valueKey);
        if (targets is null)
            return _default;

        return targets.Select(i => _sources[i].Materialize()).ToArray();
    }

        
        
        
        
    public T? FirstOrDefault<TKey,TNormal>(Expression<Func<T,TKey?>> accessor, TNormal? valueKey)
    {
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");

        var name = accessor.ToString().Replace(accessor.Parameters.First().Name, "$");
        if (!_indices.ContainsKey(name))
            AddIndexInternal<TKey, TKey>(name, accessor.Compile(), null);

        return FirstOrDefault(name, valueKey);
    }

    public T? FirstOrDefault<TNormal>(Enum indexPath, TNormal? valueKey)
        => FirstOrDefault(indexPath.ToString(), valueKey);
        
    public T? FirstOrDefault<TNormal>(string accessKey, TNormal? valueKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException("Accessor cannot be null or empty.");

        if (valueKey is null && !_allowNullKeys)
            return null;

        _indices.TryGetValue(accessKey, out var index);
                
        var targets = index?.Find(valueKey);
        if (targets is null)
            return null;

        foreach (var i in targets)
            if (_sources[i].Materialize() is T obj)
                return obj;

        return null;
    }
        
        
        
        
    public ObjectCatalog<T> AddIndex<TResult>(Expression<Func<T, TResult>> accessExp) 
        => AddIndex(accessExp?.ToString().Replace(accessExp.Parameters.First().Name, "$"), accessExp?.Compile());

    public ObjectCatalog<T> AddIndex<TResult>(Enum indexPath, Func<T, TResult> accessor)
        => AddIndex(indexPath.ToString(), accessor);

    public ObjectCatalog<T> AddIndex<TResult>(string indexPath, Func<T, TResult> accessor)
    {
        if (indexPath is null)
            throw new ArgumentException("IndexPath cannot be null.");
            
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");
            
        if (_indices.ContainsKey(indexPath))
            return this;
            
        AddIndexInternal<TResult,TResult>(indexPath, accessor, null);
        return this;
    }
        
        
        
    public ObjectCatalog<T> AddIndex<TResult, TNormal>(Expression<Func<T, TResult>> accessExp, Func<TResult?, TNormal?> normalizer)
        => AddIndex(accessExp?.ToString().Replace(accessExp.Parameters.First().Name, "$"), accessExp?.Compile(), normalizer);

    public ObjectCatalog<T> AddIndex<TResult, TNormal>(Enum indexPath, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer)
        => AddIndex(indexPath.ToString(), accessor, normalizer);
        
    public ObjectCatalog<T> AddIndex<TResult, TNormal>(string indexPath, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer)
    {
        if (indexPath is null)
            throw new ArgumentException("IndexPath cannot be null.");
            
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");
            
        if (normalizer is null)
            throw new ArgumentException("Normalizer cannot be null and must be a valid lambda expression.");

        if (_indices.ContainsKey(indexPath))
            return this;
            
        AddIndexInternal(indexPath, accessor, normalizer);
        return this;
    }
        
        
        
    private IValueIndex? AddIndexInternal<TResult,TNormal>(string name, Func<T, TResult?> accessor, Func<TResult?, TNormal?>? normalizer)
    {
        IValueIndex index = normalizer is null
            ? new ValueIndex<TResult>(name, accessor)
            : new NormalizedIndex<TResult, TNormal>(name, accessor, normalizer);
                    
            
        for (int i = 0; i < _sources.Count; i++)
        {
            var item = _sources[i].Materialize();
            if (item is null)
                continue;

            index.Add(item, i, _allowNullKeys);
        }

        _indices.Add(name, index);
            
        return index;
    }
        
        
    public void Dispose() 
        => Reset(true);

    private void Reset(bool disposing = false)
    {
        if (disposing && _observable?.TryGetTarget(out var col) == true)
            col.CollectionChanged -= ObservableCollectionChanged;
            
        foreach (var index in _indices.Values)
            index.Dispose();

        foreach (var reference in _sources)
            reference.Dispose();
            
        _indices.Clear();
        _sources.Clear();
    }
}