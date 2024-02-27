namespace ObjectCatalogTests.DataModels;

public static class Extensions
{
    public static string? MakeKeyString(this ChildItem item) 
        => item is null ? null : $"Key: {item.UniqueId} | Name: {item.TypeName}";

    public static string? GetName(this ParentItem item)
        => item?.Child?.TypeName;
    
    public static string? GetDescription(this ParentItem item)
        => item?.Child?.Description;
}