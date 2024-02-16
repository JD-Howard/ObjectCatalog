namespace ObjectCatalogTests.Models;

public class ChildItem
{
    public int UniqueId { get; set; }
    public string TypeName { get; set; }
    public string Description { get; set; }

    public ChildItem(int uniqueId, string typeName, string description)
    {
        UniqueId = uniqueId;
        TypeName = typeName;
        Description = description;
    }

    public override string ToString() 
        => $"ID={UniqueId}::Type={TypeName}::Desc={Description}";
}