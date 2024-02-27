namespace ObjectCatalogTests.DataModels;

public class ParentItem
{
    public int UniqueId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int ChildId { get; set; }
    public ChildItem Child { get; set; }

    public ParentItem(int uniqueId, string name, string description, ChildItem child)
    {
        UniqueId = uniqueId;
        Name = name;
        Description = description;
        Child = child;
        ChildId = child?.UniqueId ?? -1;
    }

    public override string ToString() 
        => $"ID={UniqueId}::Name={Name}::Desc={Description}:ChildIsNull={Child is null}";
}