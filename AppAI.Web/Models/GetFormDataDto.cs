using APP.Components.Dto;
using APP.Components.EntityDto;

namespace AppWeb.Models;

public class GetFormDataDto
{
    public string userName { get; set; }
    public int? transactionId { get; set; }
    public string rootPrimaryKeyValue { get; set; }
    public int? transGroupId { get; set; }
    public int? autoExecuteCommandId { get; set; }
    public StaticSearchResultRowJsonDto selectDataRow { get; set; }
}
