using System.Collections.Generic;

namespace AppAI.Web.Models;

/// <summary>
/// Request body for SaveAll* APIs that persist an ObservableSet.
/// ObservableSet implements IEnumerable — Newtonsoft expects a JSON array for that type,
/// so clients post { InternalItems, DeletedItemIds } and the controller builds the ObservableSet.
/// </summary>
public class ObservableSetSaveRequest<T> where T : class
{
    public List<object> DeletedItemIds { get; set; }

    public List<T> InternalItems { get; set; }
}
