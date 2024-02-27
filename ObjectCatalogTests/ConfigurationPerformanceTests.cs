using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ObjectCatalogTests.DataModels;
using ObjectCatalogTests.Factories;
using Xunit.Abstractions;

namespace ObjectCatalogTests;

public class ConfigurationPerformanceTests
{
    private readonly ITestOutputHelper _testContext;
    private List<ParentItem> _sources;
    private int _quantity = 1000000;

    public ConfigurationPerformanceTests(ITestOutputHelper testContext)
    {
        _testContext = testContext;
        _sources = ParentChildFactory.PerformanceTesting(_quantity).ToList();
    }
    
    
    [Fact]
    public void Constructor_Performance()
    {
        var sw = new Stopwatch();
        
        sw.Restart();
        var catalog1 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference);
        sw.Stop();
        var strong = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Construction Time: {_quantity:N0} = {strong}ms");

        sw.Restart();
        var items1 = catalog1.Get();
        sw.Stop();
        var strongGetItems = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced GetItems() Time: {_quantity:N0} = {strongGetItems}ms");
        
        sw.Restart();
        var catalog2 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.WeakReferenced);
        sw.Stop();
        var weak = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Construction Time: {_quantity:N0} = {weak}ms");
        
        sw.Restart();
        var items2 = catalog2.Get();
        sw.Stop();
        var weakGetItems = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced GetItems() Time: {_quantity:N0} = {weakGetItems}ms");
        
        Assert.Equal(items1.Length, items2.Count(x => x is not null));
        Assert.True(strong < weak);
        Assert.True(strongGetItems < weakGetItems);
        
        // Observations: (1,000,000 objects)
        // The constructor building weak references has a 4:1 performance hit vs the strong reference version
        // The GetItems() materialization for weak references has a 3:4 performance hit vs strong references
        // Conclusion:
        // There is some overhead associated with the weak references, but the majority is on the front end.
        // The retrieving performance will be negligible and weak reference should be the default setting.
        
        catalog1.Dispose();
        catalog2.Dispose();
    }
    
    
    
    [Fact]
    public void AddIndexNormalizer_Performance()
    {
        var sw = new Stopwatch();
        var totalMs = 0.0;
        var catalog1 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference);
        var catalog2 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference);
        var weakRefs1 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog3 = ObjectCatalog<ParentItem>.Create(weakRefs1, ObjectCatalogType.WeakReferenced);
        var weakRefs2 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog4 = ObjectCatalog<ParentItem>.Create(weakRefs2, ObjectCatalogType.WeakReferenced);
        
        
        sw.Restart();
        catalog1.AddIndex(x => x.Description);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Simple String Parent: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog2.AddIndex(x => x.Description, y => y?.ToUpper());
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Normalized String Parent: {_quantity:N0} = {totalMs}ms");
        
        
        sw.Restart();
        catalog3.AddIndex(x => x.Description);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Simple String Parent: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog4.AddIndex(x => x.Description, y => y?.ToUpper());
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Normalized String Parent: {_quantity:N0} = {totalMs}ms");
        
        // Observations: (1,000,000 objects)
        // Both strong & weak versions run at a 2:1 timespan
        // Conclusion:
        // There is no meaningful impact by adding a normalizer beyond the amount of work it is doing.
        
        catalog1.Dispose();
        catalog2.Dispose();
        catalog3.Dispose();
        catalog4.Dispose();
    }
    
    
    
    [Fact]
    public void AddIndexNestedNormalizerNoNulls_Performance()
    {
        var sw = new Stopwatch();
        var totalMs = 0.0;
        var catalog1 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference);
        var catalog2 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference);
        var weakRefs1 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog3 = ObjectCatalog<ParentItem>.Create(weakRefs1, ObjectCatalogType.WeakReferenced);
        var weakRefs2 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog4 = ObjectCatalog<ParentItem>.Create(weakRefs2, ObjectCatalogType.WeakReferenced);
        
        
        sw.Restart();
        catalog1.AddIndex(x => x.Child.Description);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Simple String? Child: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog2.AddIndex(x => x.Child.Description, y => y?.ToUpper());
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Normalized String? Child: {_quantity:N0} = {totalMs}ms");
        
        
        sw.Restart();
        catalog3.AddIndex(x => x.Child.Description);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Simple String? Child: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog4.AddIndex(x => x.Child.Description, y => y?.ToUpper());
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Normalized String? Child: {_quantity:N0} = {totalMs}ms");
        
        // Observations: (1,000,000 objects)
        // When comparing the results of this index drill-down into a child objects property against performing
        // a similar operation on a the parent object, the performance takes a rather large hit of almost 5:1
        // Conclusion:
        // While this method is capable of indexing children, and the whole point of this is to create an
        // index once and not have to keep performing the extraction work, nested indices are impactful!
        
        catalog1.Dispose();
        catalog2.Dispose();
        catalog3.Dispose();
        catalog4.Dispose();
    }
    
    
    [Fact]
    public void AddIndexNestedNormalizerAllowNulls_Performance()
    {
        var sw = new Stopwatch();
        var totalMs = 0.0;
        var catalog1 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference, ObjectCatalogBehavior.IndexNulls);
        var catalog2 = ObjectCatalog<ParentItem>.Create(_sources, ObjectCatalogType.StrongReference, ObjectCatalogBehavior.IndexNulls);
        var weakRefs1 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog3 = ObjectCatalog<ParentItem>.Create(weakRefs1, ObjectCatalogType.WeakReferenced, ObjectCatalogBehavior.IndexNulls);
        var weakRefs2 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog4 = ObjectCatalog<ParentItem>.Create(weakRefs2, ObjectCatalogType.WeakReferenced, ObjectCatalogBehavior.IndexNulls);
        
        
        sw.Restart();
        catalog1.AddIndex(x => x.Child.Description);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Simple String? Child: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog2.AddIndex(x => x.Child.Description, y => y?.ToUpper());
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced Normalized String? Child: {_quantity:N0} = {totalMs}ms");
        
        
        sw.Restart();
        catalog3.AddIndex(x => x.Child.Description);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Simple String? Child: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog4.AddIndex(x => x.Child.Description, y => y?.ToUpper());
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced Normalized String? Child: {_quantity:N0} = {totalMs}ms");
        
        // Observations: (1,000,000 objects)
        // When comparing the results to the NoNulls equivalent test, there is only a slight impact.
        // Conclusion:
        // Indexing nested properties and their null values had a negligible performance impacts.
        // This difference is probably only because more objects were allowed to be tracked/referenced.
        
        catalog1.Dispose();
        catalog2.Dispose();
        catalog3.Dispose();
        catalog4.Dispose();
    }
    
    
    [Fact]
    public void AddIndexToNestedObject_Performance()
    {
        var sw = new Stopwatch();
        var totalMs = 0.0;
        var strongRefs1 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog1 = ObjectCatalog<ParentItem>.Create(strongRefs1, ObjectCatalogType.StrongReference, ObjectCatalogBehavior.IndexNulls);
        var weakRefs2 = ParentChildFactory.PerformanceTesting(_quantity);
        var catalog2 = ObjectCatalog<ParentItem>.Create(weakRefs2, ObjectCatalogType.WeakReferenced, ObjectCatalogBehavior.IndexNulls);
        
        
        sw.Restart();
        catalog1.AddIndex(x => x.Child);
        sw.Stop();
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Strong Referenced ChildObject? of Parent: {_quantity:N0} = {totalMs}ms");
        
        sw.Restart();
        catalog2.AddIndex(x => x.Child);
        sw.Stop();
        Assert.True(totalMs < sw.Elapsed.TotalMilliseconds);
        totalMs = sw.Elapsed.TotalMilliseconds;
        _testContext.WriteLine($"Weak Referenced ChildObject? of Parent: {_quantity:N0} = {totalMs}ms");
        
        // Observations: (1,000,000 objects)
        // When comparing the results of indexing an custom object property vs one of the child objects
        // child properties, it has a 2:1 performance impact. This is significant because that is a 10:1
        // impact vs a more simple native object like string.
        // Conclusion:
        // Again, this is front loading all this work to make future operations quicker, but this rather
        // negative impact suggests using this as an reference lookup index might not be the best approach.
        
        catalog1.Dispose();
        catalog2.Dispose();
    }
    
    
    
}