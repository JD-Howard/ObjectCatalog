using System.Linq.Expressions;

namespace System.Collections.Specialized;

public interface IObjectCatalog<T>: IDisposable, 
    ObjectCatalog<T>.IObjectCatalogIndices,
    ObjectCatalog<T>.IObjectCatalogIndexkeys,
    ObjectCatalog<T>.IObjectCatalogMaterialize,
    ObjectCatalog<T>.IObjectCatalogSearch
    where T : class
{

    void Remove(T? item);
    void Add(T? item);
    
}