namespace System.Collections.Specialized;

/// <summary>
/// Determines whether ObjectCatalog can prevent an object from being garbage collected.
/// ObjectCatalog defaults to WeakReferenced and is recommended unless it is the only
/// persistent source of object data. There is a small performance penalty by using the
/// default WeakReference, but it is rather negligible even with 1 million objects.
/// </summary>
public enum ObjectCatalogType
{
    StrongReference,
    WeakReferenced
}