using System.Linq;
using System.Linq.Expressions;

namespace System.Collections.Specialized;

public sealed partial class ObjectCatalog<T>
{
    public sealed class ObjectCatalogResult : IObjectCatalogSearch, IObjectCatalogResult<T>
    {
        private int[]? _result;
        private WeakReference<ObjectCatalog<T>>? _parent;
        private ObjectCatalog<T>? Parent 
            => _parent?.TryGetTarget(out var catalog) == true ? catalog : null;

        public int Count => _result?.Length ?? 0;

        internal ObjectCatalogResult(){}

        public static IObjectCatalogResult<T> From(int[]? result, ObjectCatalog<T>? parent)
        {
            if (parent is null)
                return DefaultResult;
            
            return new ObjectCatalogResult()
            {
                _parent = new WeakReference<ObjectCatalog<T>>(parent),
                _result = result ?? DefaultArrInt
            };
        }
        
        public IObjectCatalogResult<T> Find<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey)
        {
            _result = Parent?.FindInternal(accessor, valueKey, _result) ?? DefaultArrInt;
            return this;
        }

        public IObjectCatalogResult<T> Find<TNormal>(Enum indexType, TNormal? valueKey)
        {
            _result = Parent?.FindInternal(indexType.ToString(), valueKey, _result) ?? DefaultArrInt;
            return this;
        }

        public IObjectCatalogResult<T> Find<TNormal>(string accessKey, TNormal? valueKey)
        {
            _result = Parent?.FindInternal(accessKey, valueKey, _result) ?? DefaultArrInt;
            return this;
        }

        public T? FirstOrDefault<TKey, TNormal>(Expression<Func<T, TKey?>> accessor, TNormal? valueKey) 
            => Parent?.FirstInternal(accessor, valueKey, _result);

        public T? FirstOrDefault<TNormal>(Enum indexType, TNormal? valueKey)
            => Parent?.FirstInternal(indexType.ToString(), valueKey, _result);

        public T? FirstOrDefault<TNormal>(string accessKey, TNormal? valueKey)
            => Parent?.FirstInternal(accessKey, valueKey, _result);

        private static T? Materialize(WeakReference<T>? weakRef) 
            => weakRef?.TryGetTarget(out var obj) == true ? obj : null;


        public T[] GetNonNull()
            => Parent?.GetInternalNonNull(_result) ?? DefaultArrT;

        

        public T?[] Get() 
            => Parent?.GetInternal(_result) ?? DefaultArrNullT;

        public IObjectCatalogSearch Then => this;
    }
}