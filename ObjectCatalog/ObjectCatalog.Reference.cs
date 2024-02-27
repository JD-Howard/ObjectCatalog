namespace System.Collections.Specialized;

public sealed partial class ObjectCatalog<T>
{
    
    /// <summary>
    /// Internal class responsible for wrapping the ObjectCatalogs T objects. This exists to
    /// enable weak references that do not prevent garbage collection. In most cases the
    /// ObjectCatalog is mostly an assistant to data analysis, it wasn't intended to be the sole source of models
    /// </summary>
    internal class Reference : IDisposable
    {
        private T? _strong;
        private WeakReference<T>? _weak;
        private bool _isDisposed = false;
        
        internal Reference(T value, ObjectCatalogType refType)
        {
            _strong = refType == ObjectCatalogType.StrongReference ? value : null;
            _weak = refType == ObjectCatalogType.WeakReferenced ? new WeakReference<T>(value) : null;
        }

        internal T? Materialize()
        {
            if (_isDisposed)
                return null;
            
            if (_strong is not null)
                return _strong;
            
            if (_weak?.TryGetTarget(out var obj) == true)
                return obj;

            // without a weak reference this whole object is effectively dead
            // increase performance of future requests by disposing our wrapper
            Dispose(); 
            return null;
        }
        
        public void Dispose()
        {
            if (_isDisposed)
                return;
            
            _isDisposed = true;
            
            _strong = null;
            _weak = null;
        }
        
        //public static implicit operator T?(Reference obj) => obj.Materialize();
    }
}