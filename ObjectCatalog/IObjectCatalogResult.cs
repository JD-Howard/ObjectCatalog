namespace System.Collections.Specialized;

public interface IObjectCatalogResult<T> : 
    ObjectCatalog<T>.IObjectCatalogMaterialize, 
    ObjectCatalog<T>.IObjectCatalogResultExtend 
    where T : class { }