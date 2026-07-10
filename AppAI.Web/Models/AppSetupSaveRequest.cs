using System.Collections.Generic;
using APP.Components.EntityDto;

namespace AppAI.Web.Models;

/// <summary>
/// Request body for SaveAllAppSetupEntityDto.
/// ObservableSet cannot bind from { InternalItems: [...] } under Newtonsoft
/// (IEnumerable types expect a JSON array), so use an explicit wrapper.
/// </summary>
public class AppSetupSaveRequest
{
    public List<object> DeletedItemIds { get; set; }

    public List<AppSetupExDto> InternalItems { get; set; }
}
