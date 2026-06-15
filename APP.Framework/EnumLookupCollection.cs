using System.Collections.ObjectModel;

namespace APP.Framework
{
    /// <summary>
    /// Collection of EnumLookup
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    public class EnumLookupCollection<TEnum> : Collection<EnumLookup<TEnum>>
    {
    }
}