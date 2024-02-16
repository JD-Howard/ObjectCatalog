using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ObjectCatalogTests.Factories;
using ObjectCatalogTests.Models;
using Xunit.Abstractions;

namespace ObjectCatalogTests;

public class LookupNoNullsPerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _testContext;
    private List<ParentItem> _sources;
    private int _quantity = 1000000;
    private readonly ObjectCatalog<ParentItem> _noNullCatalog;
    private readonly ObjectCatalog<ParentItem> _noNullNormalCatalog;

    private static string? GetChildDesc(ParentItem item) => item?.Child?.Description;
    private static string? GetChildName(ParentItem item) => item?.Child?.TypeName;

    public LookupNoNullsPerformanceTests(ITestOutputHelper testContext)
    {
        _testContext = testContext;
        _sources = ParentChildFactory.PerformanceTesting(_quantity).ToList();
        ParentChildFactory.ApplyChaos(_sources, _quantity / 50, 50, x => x.Description = null);
        ParentChildFactory.ApplyChaos(_sources, _quantity / 50, 2, x => x.Name = null);
        ParentChildFactory.ApplyChaos(_sources, _quantity / 25, 25, x => x.Child.Description = null);
        ParentChildFactory.ApplyChaos(_sources, _quantity / 50, 3, x => x.Child = null);
        
        _noNullCatalog = new ObjectCatalog<ParentItem>(_sources)
            .AddIndex(x => x.UniqueId, y => y < _quantity / 100) // bool operational index
            .AddIndex(x => x.Name) // uniform
            .AddIndex(x => x.Description) // semi-uniform
            .AddIndex(x => x.Child)
            .AddIndex(x => GetChildName(x)) // unique
            .AddIndex(x => x.GetDescription()); // often can be null
        _noNullNormalCatalog = new ObjectCatalog<ParentItem>(_sources, ObjectCatalogType.StrongReference)
            .AddIndex(x => x.Name, y => y?.ToUpper()) // uniform
            .AddIndex(x => x.Description, y => y?.ToUpper()) // semi-uniform
            .AddIndex(x => GetChildName(x), y => y?.ToUpper()) // unique
            .AddIndex(x => x.GetDescription(), y => y?.ToUpper()); // often can be null
    }
    
    
    [Fact]
    public void BoolOperationalParentLookupNoNormalization_Performance()
    {
        const bool isLessThenQtyDiv100 = true;
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullCatalog.Find(x => x.UniqueId, isLessThenQtyDiv100);
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => x.UniqueId < _quantity / 100).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        Assert.True(catalogResult.Length == whereResult.Length);
        // Observations: (1,000,000 objects)
        // I ran this using various quantity divider intervals, it follows the performance of everything else where the smaller
        // the returned number of objects, the better it performed.
        // Conclusion:
        // See observation.
    }
    
    
    
    
    
    [Fact]
    public void SemiUniformParentLookupNoNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullCatalog.Find(x => x.Description, "SemiUniform:Even");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "SemiUniform:Even".Equals(x.Description)).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // This straight search test with ~500k of results performs a bit worse in the catalog 2:3
        // Conclusion:
        // The WHERE wins this but not substantially
    }
    
    
    [Fact]
    public void UniformParentLookupNoNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullCatalog.Find(x => x.Name, "Uniform");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "Uniform".Equals(x.Name)).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // This straight search test with ~1M results performs a bit much worse in the catalog 2:1
        // Conclusion:
        // I don't really understand why LINQ is outperforming the catalog by 2x under the simplest conditions.
    }
    
    
    [Fact]
    public void SemiUniformParentLookupWithNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullNormalCatalog.Find(x => x.Description, "SEMIUNIFORM:EVEN");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "SEMIUNIFORM:EVEN".Equals(x.Description?.ToUpper())).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // Things are getting interesting, the catalog had nearly identical performance with its unormalized counterpart.
        // However, the LINQ version ran significantly worse 9:3
        // Conclusion:
        // Recycling the normalization process is a very clear winner affecting the lookup speed, but this is still based
        // on ~500K results and is expected to diverge more under more typical conditions.
    }
    
    
    [Fact]
    public void UniformParentLookupWithNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullNormalCatalog.Find(x => x.Name, "UNIFORM");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "UNIFORM".Equals(x.Name?.ToUpper())).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // This is slightly odd, the ~1M results on the catalog effectively took 2x as long than the semi-uniform counterpart.
        // The WHERE counterpart roughly stayed about the same, but maybe a little worse.
        // Conclusion:
        // So far this is consistently taking 2x as long to use the Catalog, but the catalog is astronomically faster when
        // the value being searched for doesn't exist at all. It would seem very large object result sets are costing the
        // catalog in performance. It isn't all that substantial, but good to know that verifying a given property even
        // has entropy worth indexing before hand clearly makes some sense...
    }
    
    
    
    
    
    
    
    
    
    
    [Fact]
    public void LookupChildObjects_Performance()
    {
        var sw = new Stopwatch();
        var target = _sources.Last().Child;
        
        sw.Restart();
        var catalogResult = _noNullCatalog.Find(x => x.Child, target);
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => target.Equals(x.Child)).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // Results are consistent with other tests that have a small result set. 16:1 faster than WHERE on single object return.
    }
    
    
    
    
    
    
    
    
    
    
    
    [Fact]
    public void UniqueChildLookupNoNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullCatalog.Find(x => GetChildName(x), "Stuff203");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "Stuff203".Equals(x.Child?.TypeName)).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // This straight search test with ~500k of results performs a bit worse in the catalog 2:3
        // Conclusion:
        // The WHERE wins this but not substantially
    }
    
    
    [Fact]
    public void NullUniqueChildLookupNoNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullCatalog.Find(x => x.GetDescription(), "Things203");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "Things203".Equals(x.Child?.Description)).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // This straight search test with ~1M results performs a bit much worse in the catalog 2:1
        // Conclusion:
        // I don't really understand why LINQ is outperforming the catalog by 2x under the simplest conditions.
    }
    
    
    [Fact]
    public void UniqueChildLookupWithNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullNormalCatalog.Find(x => GetChildName(x), "STUFF203");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "STUFF203".Equals(x.Child?.TypeName?.ToUpper())).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // Things are getting interesting, the catalog had nearly identical performance with its unormalized counterpart.
        // However, the LINQ version ran significantly worse 9:3
        // Conclusion:
        // Recycling the normalization process is a very clear winner affecting the lookup speed, but this is still based
        // on ~500K results and is expected to diverge more under more typical conditions.
    }
    
    
    [Fact]
    public void NullUniqueChildLookupWithNormalization_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalogResult = _noNullNormalCatalog.Find(x => x.GetDescription(), "THINGS203");
        sw.Stop();
        var catalogTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Catalog Search Time Total Objects {_quantity:N0} : Found {catalogResult.Length} in {catalogTime}ms");

        sw.Restart();
        var whereResult = _sources.Where(x => "THINGS203".Equals(x.Child?.Description?.ToUpper())).ToArray();
        sw.Stop();
        var whereTime = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Where Search Time Total Objects {_quantity:N0} : Found {whereResult.Length} in {whereTime}ms");
        
        // Observations: (1,000,000 objects)
        // This is slightly odd, the ~1M results on the catalog effectively took 2x as long than the semi-uniform counterpart.
        // The WHERE counterpart roughly stayed about the same, but maybe a little worse.
        // Conclusion:
        // So far this is consistently taking 2x as long to use the Catalog, but the catalog is astronomically faster when
        // the value being searched for doesn't exist at all. It would seem very large object result sets are costing the
        // catalog in performance. It isn't all that substantial, but good to know that verifying a given property even
        // has entropy worth indexing before hand clearly makes some sense...
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    


    public void Dispose()
    {
        _noNullCatalog.Dispose();
        _noNullNormalCatalog.Dispose();
    }
}