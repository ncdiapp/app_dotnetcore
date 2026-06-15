using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Communication;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AppFileController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<bool> DeleteAppFileDtoByIds(List<int> fileIdList)
    {
        return AppFileBL.DeleteAppFileDtoByIds(fileIdList);
    }

    [HttpGet]
    public HttpResponseMessage GetCsvFile(string fileName)
    {
        var path = @"C:\Temp\test.xlsx";
        HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        result.Content = new StreamContent(stream);
        result.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.ms-excel");
        return result;
    }

    [HttpGet]
    public List<AppFileDto> GetFileUpdateHistoryDtoListByFileId(int? fileId)
    {
        if (fileId.HasValue)
        {
            return AppFileBL.GetFileUpdateHistoryDtoListByFileId(fileId.Value);
        }

        return null;
    }

    [HttpGet]
    public List<AppFileDto> RollBackFileVersion(int? fileId)
    {
        if (fileId.HasValue)
        {
            int? newFileId = AppFileBL.RollBackFileVersion(fileId.Value);

            if (newFileId.HasValue)
            {
                return AppFileBL.GetFileUpdateHistoryDtoListByFileId(newFileId.Value);
            }
        }

        return null;
    }

    // above Api copy from AdministrationController controller, need to remove  file webapi from   AdministrationController

    [HttpPost]
    public string WriteFileComments(AppFileDto appFileDto)
    {
        return AppFileCollaborationBL.WriteFileComments(appFileDto);
    }

    [HttpPost]
    public AppFileDto RenameFileName(AppFileDto appFileDto)
    {
        AppFileCollaborationBL.RenameFileName(appFileDto);

        return appFileDto;
    }

    [HttpPost]
    public AppFileDto DeleteFile(AppFileDto appFileDto)
    {
        AppFileCollaborationBL.DeleteFile(appFileDto);

        return appFileDto;
    }

    [HttpPost]
    public List<int> DeleteFolder(AppSefolderDto appSefolderDto)
    {
        List<int> toReturn = new List<int>();

        toReturn = AppSeFolderBL.DeleteFolder(appSefolderDto);
        return toReturn;
    }

    [HttpGet]
    public string ReadFileComments(int? fileId)
    {
        return AppFileCollaborationBL.ReadFileComments(fileId);
    }

    [HttpGet]
    public List<LookupItemDto> GetExcelFileColumnNameList(string excelFilePath)
    {
        return ExcelImportExportBL.GetExcelFileColumnNameList(excelFilePath);
    }

    [HttpGet]
    public AppFileExDto RetrieveOneOrgAppFileExDto(int? fileId)
    {
        if (fileId.HasValue)
        {
            return AppFileBL.RetrieveOneOrgAppFileExDto(fileId.Value);
        }

        return null;
    }
}
