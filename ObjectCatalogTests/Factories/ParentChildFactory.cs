using ObjectCatalogTests.Models;

namespace ObjectCatalogTests.Factories;

public static class ParentChildFactory
{
    public static void ApplyChaos<T>(List<T> items, int qty, int seed, Action<T> agent)
    {
        var r = new Random(seed);
        for (int i = 0; i < qty; i++)
        {
            var n = r.Next(0, qty);
            agent(items[n]);
        }
    }
    
    
    public static IEnumerable<ParentItem> PerformanceTesting(int quantity)
    {
        var result = new List<ParentItem>();
        for (int i = 0; i < quantity; i++)
        {
            if (i % 2.0 == 0)
            {
                var child = new ChildItem(-i, $"Stuff{i}", null);
                result.Add(new ParentItem(i, "Uniform", $"SemiUniform:Even", child));
            }
            else
            {
                var child = new ChildItem(-i, $"Stuff{i}", $"Things{i}");
                result.Add(new ParentItem(i, "Uniform", $"SemiUniform:Odd", child));
            }
        }

        return result;
    } 
    
    
    public static IEnumerable<ParentItem> ScenarioTesting()
    {
        return new List<ParentItem>
        {
            new ParentItem(1, "ChildIsNull", "Parent1", null),
            new ParentItem(2, "ChildNotNull", "Parent2", new ChildItem(22, null, "NullChildTypeName")),
            new ParentItem(3, "ChildIsNull", "Parent3", null),
            new ParentItem(4, "ChildNotNull", "Parent4", new ChildItem(44, "CommonType", "HasCommonType")),
            new ParentItem(5, "ChildNotNull", "Parent5", new ChildItem(55, null, "NullChildTypeName")),
            new ParentItem(6, "ChildNotNull", "Parent6", new ChildItem(66, "CommonType", "HasCommonType")),
            new ParentItem(7, "ChildNotNull", "Parent7", new ChildItem(77, "UniqueType1", "HasUniqueType")),
            new ParentItem(8, "ChildNotNull", "Parent8", new ChildItem(88, "UniqueType2", "HasUniqueType"))
        };
    } 
    
    
    
}