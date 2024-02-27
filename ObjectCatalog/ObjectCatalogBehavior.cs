namespace System.Collections.Specialized;

/// <summary>
/// Determines if Null is valid for tracking purposes. ObjectCatalog defaults to IndexNonNullValues
/// and would be desired in the vast majority of scenarios, but using IncludeNulls will aggregate
/// and serve objects that resolved to a null key. 
/// </summary>
public enum ObjectCatalogBehavior
{
    IndexNonNullValues,
    IndexNulls
}