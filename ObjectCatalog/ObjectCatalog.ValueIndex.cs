using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Specialized;

public sealed partial class ObjectCatalog<T>
{
    internal class ValueIndex<TResult> : IObjectCatalogIndex
    {
        private Func<T, TResult?> _accessor;
        private Dictionary<TResult, HashSet<int>> _valueIndex = new();
        private HashSet<int> _nullCase = new();
        private ObjectCatalogBehavior _types;
        private bool _isDisposed = false;

        public string AccessKey { get; private set; }
        
        internal ValueIndex(string accessKey, Func<T, TResult?> accessor, ObjectCatalogBehavior constraint)
        {
            AccessKey = accessKey;
            _accessor = accessor;
            _types = constraint;
        }

        public int[]? Find(object? key, int[]? filter)
        {
            if (key is null)
                return _nullCase.Any() ? _nullCase.ToArray() : null;

            // This line is preventing a normalizer from being something capable of implicit/equals() matching
            // You could work around this if TResult was actually type Object, but that adds more boxing.
            // Since I've already null checked, the TryGetValue should be able to work with type object.
            // Might even limit the number of IEquality implementations necessary for end user wrappers.
            // TODO: perform performance tests between TResult & object
            if (key is not TResult typedKey)
                return null;
            
            if (!_valueIndex.TryGetValue(typedKey, out var index))
                return null;
            
            return filter?.Intersect(index).ToArray() ?? index.ToArray();
        }

        public void Add(T obj, int objectId)
        {
            var value = _accessor(obj);

            if (value is null && _types != ObjectCatalogBehavior.IndexNulls)
                return;

            if (value is null)
                _nullCase.Add(objectId);
            else if (_valueIndex.TryGetValue(value, out var index))
                index.Add(objectId);
            else
                _valueIndex.Add(value, new HashSet<int>(new [] {objectId}));
        }


        public void Remove(int objectId)
        {
            _nullCase.Remove(objectId);
            
            foreach (var key in _valueIndex.Keys.ToArray())
            {
                var hash = _valueIndex[key];
                var wasRemoved = hash.Remove(objectId);
                
                if (wasRemoved && hash.Count == 0 && key is not null)
                    _valueIndex.Remove(key);
            }
        }

        
        public TValue[] GetKeys<TValue>()
        {
            var result = new TValue[_valueIndex.Count];
            var index = 0;
            foreach (var key in _valueIndex.Keys)
            {
                if (key is TValue value)
                    result[index++] = value;
                else
                    index++;
            }

            return result;
        }
        
        
        public void Dispose()
        {
            if (_isDisposed)
                return;
            
            _isDisposed = true;
            
            foreach (var hash in _valueIndex.Values)
                hash.Clear();
            _valueIndex.Clear();
            _valueIndex = null;
            
            _nullCase.Clear();
            _nullCase = null;
            _accessor = null;
        }

        public override string ToString() 
            => $"Exp: {AccessKey}   KeyCount: {_valueIndex.Count}";
    }
}