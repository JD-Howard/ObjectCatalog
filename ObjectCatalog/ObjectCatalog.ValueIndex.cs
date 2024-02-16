﻿using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Specialized;

public partial class ObjectCatalog<T>
{
    internal class ValueIndex<TResult> : IValueIndex
    {
        private Func<T, TResult?> _accessor;
        private Dictionary<TResult, HashSet<int>> _valueIndex = new();
        private HashSet<int> _nullCase = new();
        private bool _isDisposed = false;

        public string AccessKey { get; private set; }
        
        internal ValueIndex(string accessKey, Func<T, TResult?> accessor)
        {
            AccessKey = accessKey;
            _accessor = accessor;
        }

        public int[]? Find(object? key)
        {
            if (key is null)
                return _nullCase.Any() ? _nullCase.ToArray() : null;

            if (key is not TResult typedKey)
                return null;
            
            if (!_valueIndex.TryGetValue(typedKey, out var index))
                return null;
            
            return index.ToArray();
        }

        public virtual void Add(T obj, int objectId, bool allowNullKeys)
        {
            var value = _accessor(obj);

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

        public virtual void Dispose()
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