using System.Collections.Generic;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Communication;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AppFolderNavigationController : SecureBaseController
{
    [HttpGet]
    public FolderNavigationDto GetFormDefaultTransctionFolderNivigation(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppFolderNavigationBL.GetFormDefaultTransctionFolderNivigation(transactionId);
        }

        return null;
    }

    [HttpGet]
    public FolderNavigationDto GetCurrentUserFileFolderCategoryViewList(EmAppFileFolderCategory? folderCategory, int? transactionId)
    {
        if (folderCategory.HasValue)
        {
            return AppFolderNavigationBL.GetCurrentUserFileFolderCategoryViewList(folderCategory, transactionId);
        }

        return null;
    }

    [HttpGet]
    public IEnumerable<StaticSearchResultRowJsonDto> GetTransctionFolderViewList(int? folderId, int? searchViewId, int? transactionId)
    {
        return AppStaticDataSetSearchBL.GetTransctionFolderViewList(folderId, searchViewId, transactionId);
    }

    [HttpGet]
    public List<AppFileDto> GetOneFolderLatestFileList(int? folderId, int? transactionId)
    {
        return AppFileBL.GetOneFolderLatestFileList(folderId, transactionId);
    }

    //	var resourcePath = "/webapi/AppFolderNavigation/GetCurrentUserFileFolderCategoryViewList?folderCategory=" + ((int)EmAppFileFolderCategory.MyRecycleBin)   + "&transactionId=" + FileTransactionId;

    [HttpGet]
    public Dictionary<int, int> GetMyRecycelBinFileList(int? transactionId)
    {
        var result = AppFolderNavigationBL.GetMyRecycelBinFileList(transactionId);

        return result.Where(o => o.HasValue).ToDictionary(o => o.Value, o => o.Value);
    }

    // Usage Example: Change view on Favorites or My Recently Files
    [HttpGet]
    public IEnumerable<StaticSearchResultRowJsonDto> GetCurrentUserFileFolderCategoryViewContent(EmAppFileFolderCategory? folderCategory, int? searchViewId, int? transactionId)
    {
        return AppStaticDataSetSearchBL.GetFileFolderCategoryFileViewList(searchViewId, folderCategory.Value, transactionId);
    }

    [HttpPost]
    public bool AddFilesToMyFavourite(List<int> fileIds)
    {
        if (fileIds != null)
        {
            return AppFileCollaborationBL.AddFilesToMyFavourite(fileIds);
        }
        return false;
    }

    [HttpPost]
    public bool RemoveFilesFromMyFavourite(List<int> fileIds)
    {
        return AppFileCollaborationBL.RemoveFilesFromMyFavourite(fileIds);
    }

    [HttpPost]
    public bool AddFilesToShareOther(AppMessageDto fileMessageTemplate)
    {
        if (fileMessageTemplate != null)
        {
            return AppFileCollaborationBL.AddFilesToShareOther(fileMessageTemplate);
        }
        return false;
    }

    [HttpPost]
    // needed parameters: FileshareOtherList, Subject, Message, TransactionId
    public bool SendFileNotificationFromFileSharingMessageTemplate(AppMessageDto fileMessageTemplate)
    {
        if (fileMessageTemplate != null)
        {
            return AppFileCollaborationBL.SendFileNotificationFromFileSharingMessageTemplate(fileMessageTemplate);
        }

        return false;
    }

    [HttpGet]
    public List<AppFileOrFolderShareToOtherDto> GetCurrentUserFilesToShareOtherDtoList(int fileId)
    {
        return AppFileCollaborationBL.GetCurrentUserFilesToShareOtherDtoList(fileId);
    }

    [HttpGet]
    public AppRolesAndUsersDto GetCurrentUserAvailaleShareFileToRolesAndUsers()
    {
        return AppFileCollaborationBL.GetCurrentUserAvailaleShareFileToRolesAndUsers();
    }

    [HttpPost]
    public bool RemoveFilesToShareOther(List<int> fileIds)
    {
        if (fileIds != null)
        {
            return AppFileCollaborationBL.RemoveFilesToShareOther(fileIds);
        }
        return false;
    }

    [HttpPost]
    public bool AddFolderIdsToMyFavourite(List<int> folderIds)
    {
        if (folderIds != null)
        {
            return AppFileCollaborationBL.AddFolderIdsToMyFavourite(folderIds);
        }
        return false;
    }

    //Key TrnascationId,
    [HttpPost]
    public bool MoveFileToRecycleBin(KeyValuePair<int, List<int>>? trannactionIdFileIds)
    {
        if (trannactionIdFileIds.HasValue)
        {
            return AppTransactionRecycleBL.MoveFileToRecycleBin(trannactionIdFileIds.Value);
        }
        return false;
    }

    [HttpPost]
    public bool RestoreFileFromRecycleBin(KeyValuePair<int, List<int>>? trannactionIdFileIds)
    {
        if (trannactionIdFileIds.HasValue)
        {
            return AppTransactionRecycleBL.RestoreFileFromRecycleBin(trannactionIdFileIds.Value);
        }
        return false;
    }

    [HttpPost]
    public bool DeleteFileFromRecycleBin(KeyValuePair<int, List<int>>? trannactionIdFileIds)
    {
        if (trannactionIdFileIds.HasValue)
        {
            return AppTransactionRecycleBL.DeleteFileFromRecycleBin(trannactionIdFileIds.Value);
        }
        return false;
    }

    [HttpPost]
    public bool RemoveFolderIdsFromMyFavourite(List<int> folderIds)
    {
        return false;
    }

    [HttpGet]
    public IEnumerable<StaticSearchResultRowJsonDto> GetFileLogicCategoryFullTextSearchResult(EmAppFileFolderCategory? folderCategory, int? searchViewId, int? transactionId, string searchText)
    {
        return AppFolderNavigationBL.GetFileLogicCategoryFullTextSearchResult(folderCategory, searchViewId, transactionId, searchText);
    }

    [HttpGet]
    public IEnumerable<StaticSearchResultRowJsonDto> GetFileFolderFullTextSearchResult(int? searchViewId, int? folderId, EmAppFolderSearchOption emAppFolderSearchOption, int? transactionId, string searchText)
    {
        return AppFolderNavigationBL.GetFileFolderFullTextSearchResult(searchViewId, folderId, emAppFolderSearchOption, transactionId, searchText);
    }

    [HttpGet]
    public AppSefolderDto[] RetrieveFolderHairarchyDto(int? transactionId, int? entryFolderId)
    {
        if (transactionId.HasValue)
        {
            AppSefolderDto[] toReturn = AppSeFolderBL.RetrieveFolderHairarchyDto(transactionId.Value, entryFolderId);
            return toReturn;
        }
        else
        {
            return null;
        }

        // return AppProductTreeViewBL.RetrieveFolderHairarchyDto();
    }

    [HttpGet]
    public AppSefolderExDto RetrieveOneAppSefolderExDto(int? folderId)
    {
        if (folderId.HasValue)
        {
            return AppSeFolderBL.RetrieveOneAppSefolderExDto(folderId);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppSefolderDto> SaveAppSefolder(AppSefolderDto aAppSefolderExDto)
    {
        if (aAppSefolderExDto != null)
        {
            return AppSeFolderBL.SaveAppSefolder(aAppSefolderExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteAppSefolder(int? folderId)
    {
        if (folderId.HasValue)
        {
            return AppSeFolderBL.DeleteAppSefolder(folderId.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> PasteAppSefolder(int? folderId, int? parentFolderId)
    {
        if (folderId.HasValue && parentFolderId.HasValue)
        {
            return AppSeFolderBL.PasteAppSefolder(folderId.Value, parentFolderId.Value);
        }

        return null;
    }

    [HttpGet]
    public List<AppSefolderResourceExDto> RetrieveOneAppSefolderResourceList(int? folderId)
    {
        if (folderId.HasValue)
        {
            return AppSeFolderBL.RetrieveOneAppSefolderResourceList(folderId.Value);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppSefolderResourceExDto> SaveAppSefolderResource(FolderResourceObjectSetDto folderResourceObjectSetDto)
    {
        if (folderResourceObjectSetDto != null && folderResourceObjectSetDto.FolderId.HasValue
            && folderResourceObjectSetDto.AppSefolderResourceExDtoSet != null)
        {
            folderResourceObjectSetDto.AppSefolderResourceExDtoSet.DeletedItemIds = folderResourceObjectSetDto.DeletedItemIds;

            OperationCallResult<AppSefolderResourceExDto> aOperationCallResult = AppSeFolderBL.SaveAppSefolderResource(folderResourceObjectSetDto.FolderId.Value, folderResourceObjectSetDto.AppSefolderResourceExDtoSet);

            return aOperationCallResult;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> ApplySecurityToSubFolders(FolderResourceObjectSetDto folderResourceObjectSetDto)
    {
        if (folderResourceObjectSetDto != null && folderResourceObjectSetDto.FolderId.HasValue
            && folderResourceObjectSetDto.AppSefolderResourceExDtoSet != null)
        {
            OperationCallResult<bool> aOperationCallResult = AppSeFolderBL.ApplySecurityToSubFolders(folderResourceObjectSetDto.FolderId.Value, folderResourceObjectSetDto.AppSefolderResourceExDtoSet, folderResourceObjectSetDto.TransactionId.Value);

            return aOperationCallResult;
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> RemoveSecurityFromSubFolders(int? folderId, int? transactionId)
    {
        if (folderId.HasValue)
        {
            return AppSeFolderBL.RemoveSecurityFromSubFolders(folderId.Value, transactionId);
        }

        return null;
    }

    [HttpGet]
    public AppSefolderDto[] RetrieveCurrentUserTranscationFolderHairarchyDto(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppSeFolderBL.RetrieveCurrentUserTranscationFolderHairarchyDto(transactionId.Value);
        }

        return null;
    }
}
