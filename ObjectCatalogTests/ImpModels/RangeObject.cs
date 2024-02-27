namespace ObjectCatalogTests.ImpModels;

// This data model exists to test normalizing a set of properties to a custom object and then testing if it contains the base type.
// In practice, the end user wrapper object will need to represent the normalized state and the value lookup state so they can be
// directly compared, this will require the end user wrapper object to be aware if it is the key or the target type.
public struct RangeObject
{
    public int MinSize;
    public int MaxSize;
}