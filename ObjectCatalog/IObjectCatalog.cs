using System.Linq.Expressions;

namespace System.Collections.Specialized;

public interface IObjectCatalog<T>: IDisposable, 
    ObjectCatalog<T>.IObjCatalogIndices,
    ObjectCatalog<T>.IObjCatalogMaterialize,
    ObjectCatalog<T>.IObjectCatalogSearch
    where T : class
{

    void Remove(T? item);
    void Add(T? item);
    
}