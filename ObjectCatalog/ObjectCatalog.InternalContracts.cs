using System.Linq.Expressions;

namespace System.Collections.Specialized;

public sealed partial class ObjectCatalog<T>
{
    internal interface IObjectCatalogIndex : IDisposable
    {
        internal string AccessKey { get; }
        internal int[]? Find(object? key, int[]? filter);
        internal TValue[] GetKeys<TValue>();
        internal void Add(T obj, int objectId, bool allowNullKeys);
        internal void Remove(int objectId);
    }
    
    
    public interface IObjectCatalogIndices // ObjectCatalog only
    {
        IObjectCatalog<T> AddIndex<TResult>(Expression<Func<T, TResult>> accessExp);
        IObjectCatalog<T> AddIndex<TResult>(Enum indexType, Func<T, TResult> accessor);
        IObjectCatalog<T> AddIndex<TResult>(string accessKey, Func<T, TResult> accessor);
        IObjectCatalog<T> AddIndex<TResult, TNormal>(Expression<Func<T, TResult>> accessExp, Func<TResult?, TNormal?> normalizer);
        IObjectCatalog<T> AddIndex<TResult, TNormal>(Enum indexType, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer);
        IObjectCatalog<T> AddIndex<TResult, TNormal>(string accessKey, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer);
    }

    public interface IObjectCatalogIndexkeys // ObjectCatalog only 
    {
        TKey[] GetKeys<TKey>(Expression<Func<T, TKey>> accessor);
        TKey[] GetKeys<TKey>(Enum indexType);
        TKey[] GetKeys<TKey>(string accessKey);
    }

    public interface IObjectCatalogSearch // Result & IObjectCatalog
    {
        IObjectCatalogFindResult Find<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey);
        IObjectCatalogFindResult Find<TNormal>(Enum indexType, TNormal? valueKey);
        IObjectCatalogFindResult Find<TNormal>(string accessKey, TNormal? valueKey);
        T? FirstOrDefault<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey);
        T? FirstOrDefault<TNormal>(Enum indexType, TNormal? valueKey);
        T? FirstOrDefault<TNormal>(string accessKey, TNormal? valueKey);
    }

    public interface IObjectCatalogMaterialize // Result & IObjectCatalog
    {
        int Count { get; }
        
        T?[] Get();
        T[] GetNonNull();
    }

    public interface IObjCatalogResultExtend // ObjectCatalogResult only
    {
        IObjectCatalogSearch Then { get; }
    }

    public interface IObjectCatalogFindResult : IObjectCatalogMaterialize, IObjCatalogResultExtend {}
}