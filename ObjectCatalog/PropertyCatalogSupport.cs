using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Collections.Specialized;

public partial class ObjectCatalog<T>
{
    // private readonly string[] _knownSortable = new [] {"DateOnly", "DateTime", "DateTimeOffset", "Decimal", "Guid", "Half", "HashCode", "Index", "Int128", "Range", "String", "TimeSpan", "UInt128"};
    // public bool IsPrimitiveSource<TValue>(TValue obj) => IsPrimitiveSource(typeof(TValue));
    // public bool IsPrimitiveSource(Type type)
    // {
    //     if (type is null || type.IsInterface)
    //         return false;
    //         
    //     if (type.IsEnum || type.IsPrimitive || _knownSortable.Contains(type.Name))
    //         return true;
    //         
    //     var uType = Nullable.GetUnderlyingType(type);
    //     if (uType is null || uType.IsClass || uType == type)
    //         return false;
    //     
    //     return IsPrimitiveSource(uType);
    // }
}