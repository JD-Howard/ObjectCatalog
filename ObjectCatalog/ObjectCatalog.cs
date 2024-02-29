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

public sealed partial class ObjectCatalog<T> : IObjectCatalog<T> where T : class
{
    private readonly Dictionary<string, IObjectCatalogIndex> _indices = new();
    private readonly List<Reference> _sources = new();
    private readonly ObjectCatalogType _refType;
    private readonly ObjectCatalogBehavior _typeConstraint;
    private readonly WeakReference<ObservableCollection<T>>? _observable = null;
    private static readonly T?[] DefaultArrNullT = Array.Empty<T>();
    private static readonly T[] DefaultArrT = Array.Empty<T>();
    private static readonly int[] DefaultArrInt = Array.Empty<int>();
    private static readonly ObjectCatalogResult DefaultResult = new ObjectCatalogResult();

    public int Count => _sources.Count;
    
    /// <summary>
    /// If your using the ObjectCatalogType.WeakReference (default)
    /// behavior, then use your source list instead of GetItems()
    /// </summary>
    public T?[] Get() 
        => _sources.Select(x => x.Materialize()).ToArray();

    internal T?[] GetInternal(int[]? targets)
        => targets is null ? DefaultArrNullT : targets.Select(i => _sources[i].Materialize()).ToArray();
    
    
    public T[] GetNonNull() 
        => _sources.Select(x => x.Materialize()).Where(y => y is not null).ToArray()!;
    internal T[] GetInternalNonNull(int[]? targets)
        => targets is null ? DefaultArrT : targets.Select(i => _sources[i].Materialize()).Where(y => y is not null).ToArray();


    
    public static IObjectCatalog<T> Create(IEnumerable<T> source)
        => new ObjectCatalog<T>(source, ObjectCatalogType.WeakReferenced, ObjectCatalogBehavior.IndexNonNullValues);
    public static IObjectCatalog<T> Create(IEnumerable<T> source, ObjectCatalogType refType)
        => new ObjectCatalog<T>(source, refType, ObjectCatalogBehavior.IndexNonNullValues);
    public static IObjectCatalog<T> Create(IEnumerable<T> source, ObjectCatalogType refType, ObjectCatalogBehavior behavior)
        => new ObjectCatalog<T>(source, refType, behavior);
    
    private ObjectCatalog() { }
    private ObjectCatalog(IEnumerable<T> source, ObjectCatalogType refType, ObjectCatalogBehavior behavior)
    {
        if (source is ObservableCollection<T> col)
        {
            _observable = new WeakReference<ObservableCollection<T>>(col);
            col.CollectionChanged += ObservableCollectionChanged;
        }

        _refType = refType;
        _typeConstraint = behavior;
        if (source is null)
            return;
        
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
                var items = Get();
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
            RemoveInternal(item, Get());
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
            valueIndex.Add(item, index);
    }

        
    public IObjectCatalogResult<T> Find<TKey,TNormal>(Expression<Func<T,TKey?>> accessor, TNormal? valueKey) 
        => ObjectCatalogResult.From(FindInternal(accessor, valueKey, null), this);
    public int[] FindInternal<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey, int[]? filter)
    {
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");

        var name = accessor.ToString().Replace(accessor.Parameters.First().Name, "$");
        if (!_indices.ContainsKey(name))
            AddIndexInternal<TKey, TKey>(name, accessor.Compile(), null, _typeConstraint);

        return FindInternal(name, valueKey, null);
    }

    
    public IObjectCatalogResult<T> Find<TNormal>(Enum indexType, TNormal? valueKey)
        => ObjectCatalogResult.From(FindInternal(indexType.ToString(), valueKey, null), this);
    
    
    public IObjectCatalogResult<T> Find<TNormal>(string accessKey, TNormal? valueKey) 
        => ObjectCatalogResult.From(FindInternal(accessKey, valueKey, null), this);
    internal int[] FindInternal<TNormal>(string accessKey, TNormal? valueKey, int[]? filter)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException("Accessor cannot be null or empty.");

        _indices.TryGetValue(accessKey, out var index);
        return index?.Find(valueKey, filter) ?? DefaultArrInt;
    }

    
    
    

    public T? FirstOrDefault<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey)
        => FirstInternal(accessor, valueKey, null);
    internal T? FirstInternal<TKey,TNormal>(Expression<Func<T,TKey?>> accessor, TNormal? valueKey, int[]? filter)
    {
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");

        var name = accessor.ToString().Replace(accessor.Parameters.First().Name, "$");
        if (!_indices.ContainsKey(name))
            AddIndexInternal<TKey, TKey>(name, accessor.Compile(), null, _typeConstraint);

        return FirstOrDefault(name, valueKey);
    }

    
    public T? FirstOrDefault<TNormal>(Enum indexType, TNormal? valueKey)
        => FirstOrDefault(indexType.ToString(), valueKey);

    
    public T? FirstOrDefault<TNormal>(string accessKey, TNormal? valueKey)
        => FirstInternal(accessKey, valueKey, null);
    internal T? FirstInternal<TNormal>(string accessKey, TNormal? valueKey, int[]? filter)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException("Accessor cannot be null or empty.");

        _indices.TryGetValue(accessKey, out var index);
                
        var targets = index?.Find(valueKey, filter);
        if (targets is null)
            return null;

        foreach (var i in targets)
            if (_sources[i].Materialize() is T obj)
                return obj;

        return null;
    }
        
        
        
        
    public IObjectCatalog<T> AddIndex<TResult>(Expression<Func<T, TResult>> accessExp) 
        => AddIndex(accessExp?.ToString().Replace(accessExp.Parameters.First().Name, "$"), accessExp?.Compile(), _typeConstraint);
    public IObjectCatalog<T> AddIndex<TResult>(Expression<Func<T, TResult>> accessExp, ObjectCatalogBehavior constraint) 
        => AddIndex(accessExp?.ToString().Replace(accessExp.Parameters.First().Name, "$"), accessExp?.Compile(), constraint);

    public IObjectCatalog<T> AddIndex<TResult>(Enum indexType, Func<T, TResult> accessor)
        => AddIndex(indexType.ToString(), accessor, _typeConstraint);
    public IObjectCatalog<T> AddIndex<TResult>(Enum indexType, Func<T, TResult> accessor, ObjectCatalogBehavior constraint)
        => AddIndex(indexType.ToString(), accessor, constraint);

    public IObjectCatalog<T> AddIndex<TResult>(string accessKey, Func<T, TResult> accessor)
        => AddIndex(accessKey, accessor, _typeConstraint);
    public IObjectCatalog<T> AddIndex<TResult>(string accessKey, Func<T, TResult> accessor, ObjectCatalogBehavior constraint)
    {
        if (accessKey is null)
            throw new ArgumentException("IndexPath cannot be null.");
            
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");
            
        if (_indices.ContainsKey(accessKey))
            return this;
            
        AddIndexInternal<TResult,TResult>(accessKey, accessor, null, constraint);
        return this;
    }
        
    public IObjectCatalog<T> AddIndex<TResult, TNormal>(Expression<Func<T, TResult>> accessExp, Func<TResult?, TNormal?> normalizer)
        => AddIndex(accessExp?.ToString().Replace(accessExp.Parameters.First().Name, "$"), accessExp?.Compile(), normalizer, _typeConstraint);
    public IObjectCatalog<T> AddIndex<TResult, TNormal>(Expression<Func<T, TResult>> accessExp, Func<TResult?, TNormal?> normalizer, ObjectCatalogBehavior constraint)
        => AddIndex(accessExp?.ToString().Replace(accessExp.Parameters.First().Name, "$"), accessExp?.Compile(), normalizer, constraint);
    
    public IObjectCatalog<T> AddIndex<TResult, TNormal>(Enum indexType, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer)
        => AddIndex(indexType.ToString(), accessor, normalizer, _typeConstraint);
    public IObjectCatalog<T> AddIndex<TResult, TNormal>(Enum indexType, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer, ObjectCatalogBehavior constraint)
        => AddIndex(indexType.ToString(), accessor, normalizer, constraint);
        
    public IObjectCatalog<T> AddIndex<TResult, TNormal>(string accessKey, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer)
        => AddIndex(accessKey, accessor, normalizer, _typeConstraint);
    public IObjectCatalog<T> AddIndex<TResult, TNormal>(string accessKey, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer, ObjectCatalogBehavior constraint)
    {
        if (accessKey is null)
            throw new ArgumentException("IndexPath cannot be null.");
            
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");
            
        if (normalizer is null)
            throw new ArgumentException("Normalizer cannot be null and must be a valid lambda expression.");

        if (_indices.ContainsKey(accessKey))
            return this;
            
        AddIndexInternal(accessKey, accessor, normalizer, constraint);
        return this;
    }
        
        
        
    private IObjectCatalogIndex? AddIndexInternal<TResult,TNormal>(string name, Func<T, TResult?> accessor, Func<TResult?, TNormal?>? normalizer, ObjectCatalogBehavior constraint)
    {
        IObjectCatalogIndex index = normalizer is null
            ? new ValueIndex<TResult>(name, accessor, constraint)
            : new NormalizedIndex<TResult, TNormal>(name, accessor, normalizer, constraint);
                    
            
        for (int i = 0; i < _sources.Count; i++)
        {
            var item = _sources[i].Materialize();
            if (item is null)
                continue;

            index.Add(item, i);
        }

        _indices.Add(name, index);
            
        return index;
    }
     
    
    public TKey[] GetKeys<TKey>(Expression<Func<T,TKey>> accessor)
    {
        if (accessor is null)
            throw new ArgumentException("Accessor cannot be null and must be a valid lambda expression.");

        var name = accessor.ToString().Replace(accessor.Parameters.First().Name, "$");
        if (!_indices.ContainsKey(name))
            return Array.Empty<TKey>();

        return GetKeys<TKey>(name);
    }

    public TKey[] GetKeys<TKey>(Enum indexType)
        => GetKeys<TKey>(indexType.ToString());
        
    public TKey[] GetKeys<TKey>(string accessKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new ArgumentException("accessKey cannot be null or empty.");

        _indices.TryGetValue(accessKey, out var index);

        var targets = index?.GetKeys<TKey>();
        if (targets is null)
            return Array.Empty<TKey>();

        return targets;
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