namespace System.Collections.Specialized;

public partial class ObjectCatalog<T>
{
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
        
        public static implicit operator T?(Reference obj) => obj.Materialize();
    }
}