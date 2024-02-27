using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Specialized;

public sealed partial class ObjectCatalog<T>
{
    internal class NormalizedIndex<TResult, TNormal> : IValueIndex
    {
        private Func<T, TResult?> _accessor;
        private Func<TResult?, TNormal?> _normalizer;
        private Dictionary<TNormal, HashSet<int>> _valueIndex = new();
        private HashSet<int> _nullCase = new();
        private bool _isDisposed = false;

        public string AccessKey { get; private set; }


        internal NormalizedIndex(string accessKey, Func<T, TResult?> accessor, Func<TResult?, TNormal?> normalizer)
        {
            AccessKey = accessKey;
            _accessor = accessor;
            _normalizer = normalizer;
        }
        

        public int[]? Find(object? key, int[]? filter)
        {
            if (key is null)
                return _nullCase.Any() ? _nullCase.ToArray() : null;

            if (key is not TNormal typedKey)
                return null;
            
            if (!_valueIndex.TryGetValue(typedKey, out var index))
                return null;
            
            return filter?.Intersect(index).ToArray() ?? index.ToArray();
        }
        

        public void Add(T obj, int objectId, bool allowNullKeys)
        {
            var value = _normalizer(_accessor(obj));

            if (value is null && !allowNullKeys)
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
            _normalizer = null;
            _accessor = null;
        }
        
        public override string ToString() 
            => $"Exp: {AccessKey}   KeyCount: {_valueIndex.Count}";
    }
}