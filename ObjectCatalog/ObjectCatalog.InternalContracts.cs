using System.Linq.Expressions;

namespace System.Collections.Specialized;

public sealed partial class ObjectCatalog<T>
{
    internal interface IValueIndex : IDisposable
    {
        internal string AccessKey { get; }
        internal int[]? Find(object? key, int[]? filter);
        internal void Add(T obj, int objectId, bool allowNullKeys);
        internal void Remove(int objectId);
    }
    
    
    public interface IObjCatalogIndices // ObjectCatalog only
    {
        IObjectCatalog<T> AddIndex<TResult>(Expression<Func<T, TResult>> accessExp);
        IObjectCatalog<T> AddIndex<TResult>(Enum indexPath, Func<T, TResult> accessor);
        IObjectCatalog<T> AddIndex<TResult>(string indexPath, Func<T, TResult> accessor);
        IObjectCatalog<T> AddIndex<TResult, TNormal>(Expression<Func<T, TResult>> accessExp, Func<TResult?, TNormal?> normalizer);
        IObjectCatalog<T> AddIndex<TResult, TNormal>(Enum indexPath, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer);
        IObjectCatalog<T> AddIndex<TResult, TNormal>(string indexPath, Func<T, TResult> accessor, Func<TResult?, TNormal?> normalizer);
    }

    public interface IObjectCatalogSearch // Result & IObjectCatalog
    {
        IObjCatalogFindResult Find<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey);
        IObjCatalogFindResult Find<TNormal>(Enum indexType, TNormal? valueKey);
        IObjCatalogFindResult Find<TNormal>(string accessKey, TNormal? valueKey);
        T? FirstOrDefault<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey);
        T? FirstOrDefault<TNormal>(Enum indexType, TNormal? valueKey);
        T? FirstOrDefault<TNormal>(string accessKey, TNormal? valueKey);
    }

    public interface IObjCatalogMaterialize // Result & IObjectCatalog
    {
        int Count { get; }
        
        T?[] Get();
        T[] GetNonNull();
    }

    public interface IObjCatalogResultExtend // ObjectCatalogResult only
    {
        IObjectCatalogSearch Then { get; }
    }

    public interface IObjCatalogFindResult : IObjCatalogMaterialize, IObjCatalogResultExtend;
}