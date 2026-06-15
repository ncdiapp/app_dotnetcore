using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[ApiController]
[Route("webapi/[controller]/[action]")]
public class ExcelController : ControllerBase
{
    ///webapi/Excel/SaveExcel

    [HttpPost]
    public bool SaveExcel(object jsonDtoData)
    {
        //  base.InitializeSecurity();
        // jsonDtoData.AppLanguageKeyList.DeletedItemIds = new System.Collections.Generic.List<object>(jsonDtoData.DeletedItemsIds);
        // AppLanguageBL.SaveAppLanguageExDto(jsonDtoData);

        return true;
    }

    [HttpGet]
    public IActionResult GetSum(int a, int b)
    {
        //  base.InitializeSecurity();
        // jsonDtoData.AppLanguageKeyList.DeletedItemIds = new System.Collections.Generic.List<object>(jsonDtoData.DeletedItemsIds);
        // AppLanguageBL.SaveAppLanguageExDto(jsonDtoData);

        return Ok(a + b);
    }
}
