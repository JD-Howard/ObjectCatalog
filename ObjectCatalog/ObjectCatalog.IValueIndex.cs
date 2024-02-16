namespace System.Collections.Specialized;

public partial class ObjectCatalog<T>
{
    internal interface IValueIndex : IDisposable
    {
        internal string AccessKey { get; }
        internal int[]? Find(object? key);
        internal void Add(T obj, int objectId, bool allowNullKeys);
        internal void Remove(int objectId);
    }
}