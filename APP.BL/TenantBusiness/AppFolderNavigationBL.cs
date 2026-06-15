using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.LBL.EntityClasses;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace App.BL
{
	public static class AppFolderNavigationBL
	{
		public static FolderNavigationDto GetFormDefaultTransctionFolderNivigation(int? transactionId)
		{
			FolderNavigationDto toReturn = new FolderNavigationDto();

			var searchViewList = AppTransactionNavigationBL.RetrieveFolderSearchViewList(transactionId);

			toReturn.HairarchyFolderRootList = AppSeFolderBL.RetrieveCurrentUserTranscationFolderHairarchyDto(transactionId.Value);

			AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
			int rootFoderId = appTransactionExDto.MgtRootFolderId.Value;
			var defaultView = searchViewList.Where(o => o.IsDefaultView).First();

			toReturn.ViewList = searchViewList;

			toReturn.TransBusinessType = appTransactionExDto.EmAppTransBusinessType;
			toReturn.TransMgtRootFolderId = rootFoderId;

			toReturn.SearchResultList = AppStaticDataSetSearchBL.GetTransctionFolderViewList(rootFoderId, defaultView.Id as int?, transactionId);

			return toReturn;

			//throw new NotImplementedException();
		}


		public static List<int?> GetMyRecycelBinFileList(int? transactionId)
		{

			var viewEnity = AppFileBL.GetTransNavigationSearchViewEntity(transactionId);

			var fileIdViwColumn = viewEnity.AppSearchViewField.Where(o => o.IsTransRootId.HasValue).First();





			var result = AppFolderNavigationBL.GetCurrentUserFileFolderCategoryViewList(EmAppFileFolderCategory.MyRecycleBin, transactionId);

			var list = result.SearchResultList.Select(o =>  ControlTypeValueConverter.ConvertValueToInt (  o[fileIdViwColumn.SearchViewFieldId])).ToList();

			return list;


		}

		public static FolderNavigationDto GetCurrentUserFileFolderCategoryViewList(EmAppFileFolderCategory? folderCategory, int? transactionId)
		{
			if (folderCategory.HasValue)
			{
				FolderNavigationDto toReturn = new FolderNavigationDto();

				var searchViewList = AppTransactionNavigationBL.RetrieveFolderSearchViewList(transactionId);
				toReturn.ViewList = searchViewList;

				AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
				toReturn.TransBusinessType = appTransactionExDto.EmAppTransBusinessType;

				var defaultView = searchViewList.Where(o => o.IsDefaultView).First();


				if (folderCategory.Value == EmAppFileFolderCategory.Company)

				{
					return GetFormDefaultTransctionFolderNivigation(transactionId);

				}
				else if (folderCategory.Value == EmAppFileFolderCategory.Public)

				{
					int? publicFoderId = AppTenantSettingBL.GetIntValue(EmTenantSettings.PublicFileFolderId);
					if (publicFoderId.HasValue)
					{
						return GeFolderTransctionNivigation(publicFoderId.Value, transactionId);
					}



				}
				else if (folderCategory.Value == EmAppFileFolderCategory.Private)
				{

					int? userFoderId = AppSecurityUserBL.CurrentUserEntity.DefaultVendorRequestFolderId;

                    // 
                    if (userFoderId.HasValue)
                    {
                        return GeFolderTransctionNivigation(userFoderId.Value, transactionId);
                    }
                    else
                    {
                        var userDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(AppSecurityUserBL.CurrentUserId);
                        OperationCallResult<AppSefolderExDto> createFolderResult = AppSecurityUserBL.CreateUserFileFolder(userDto);

                        AppSefolderExDto folderDto = createFolderResult.Object;

                        if (folderDto != null && folderDto.Id != null)
                        {
                            return GeFolderTransctionNivigation((int)folderDto.Id, transactionId);
                        }
                    }
				}


				else if (
							folderCategory.Value == EmAppFileFolderCategory.Favorites
							|| folderCategory.Value == EmAppFileFolderCategory.MyRecentlyFiles
							|| folderCategory.Value == EmAppFileFolderCategory.SharedToMe
							|| folderCategory.Value == EmAppFileFolderCategory.ShareToOthers
							|| folderCategory.Value == EmAppFileFolderCategory.MyRecycleBin
					  )
				{

					toReturn.SearchResultList = AppStaticDataSetSearchBL.GetFileFolderCategoryFileViewList(defaultView.Id as int?, folderCategory.Value, transactionId);

					return toReturn;
				}

			
			}

			return new FolderNavigationDto();
		}



		public static IEnumerable<StaticSearchResultRowJsonDto> GetFileLogicCategoryFullTextSearchResult(EmAppFileFolderCategory? folderCategory, int? searchViewId,int? transactionId ,string searchText)
		{
			if (folderCategory.HasValue)
			{


			 if (
							folderCategory.Value == EmAppFileFolderCategory.Favorites
							|| folderCategory.Value == EmAppFileFolderCategory.MyRecentlyFiles
							|| folderCategory.Value == EmAppFileFolderCategory.SharedToMe
							|| folderCategory.Value == EmAppFileFolderCategory.ShareToOthers
							|| folderCategory.Value == EmAppFileFolderCategory.MyRecycleBin
					  )
				{

					return   AppStaticDataSetSearchBL.GetFileFolderCategoryFileViewList(searchViewId, folderCategory.Value, transactionId, searchText);

					
				}
			}

			return null;
		}

		// neeed add security 
		public static IEnumerable<StaticSearchResultRowJsonDto> GetFileFolderFullTextSearchResult(int? searchViewId, int? folderId, EmAppFolderSearchOption emAppFolderSearchOption, int? transactionId, string fullTextSearch)
		{
			int[] fileIds = null;

			var toReturn = new List<StaticSearchResultRowJsonDto>();

			AppSearchViewEntity viewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntity(searchViewId);

		

			if (viewEntity.AppDataSet == null || (string.IsNullOrEmpty(viewEntity.AppDataSet.QueryText)))
			{
				return toReturn;
			}

			else if(emAppFolderSearchOption==EmAppFolderSearchOption.CurrentFolder )
			{
				// need to return InitalIds

				//AppFileBL.GetInitialFileEntityWithoutContent () 

				fileIds = AppFileBL.RetrieveAllAppFileEntityForFolders(new int[] { folderId.Value }).Select(o => o.FileId).ToArray () ;

				if (!string.IsNullOrWhiteSpace(fullTextSearch))
				{
					var fullTextFiledIdList = AppSearchBL.FullTextLatestVersionFileSearch(fullTextSearch);

					//var fullTextFiledIdList = fullTextList.Select(o => (int)o.Id).ToArray();

					fileIds = fileIds.Intersect(fullTextFiledIdList).ToArray();

				}

			}

			else if (emAppFolderSearchOption == EmAppFolderSearchOption.SubFolders)
			{
				List<int>  allSubFolderIds = AppSeFolderBL.RetrieveSubFolderIds(folderId, transactionId.Value );
				allSubFolderIds.Add(folderId.Value);


				fileIds = AppFileBL.RetrieveAllAppFileEntityForFolders(allSubFolderIds.ToArray ()).Select(o => o.FileId).ToArray();

				if (!string.IsNullOrWhiteSpace(fullTextSearch))
				{
					var fullTextFiledIdList = AppSearchBL.FullTextLatestVersionFileSearch(fullTextSearch);

					fileIds = fileIds.Intersect(fullTextFiledIdList).ToArray();

				}

			}

		

			else if (emAppFolderSearchOption == EmAppFolderSearchOption.MyOwnFiles)
			{
				fileIds = AppFileBL.RetrieveMyOwnAllAppFileEntity().Select(o => o.FileId).ToArray(); ;

				if (!string.IsNullOrWhiteSpace(fullTextSearch))
				{
					var fullTextFiledIdList = AppSearchBL.FullTextLatestVersionFileSearch(fullTextSearch);

					fileIds = fileIds.Intersect(fullTextFiledIdList).ToArray();

				}
			}


			else if (emAppFolderSearchOption == EmAppFolderSearchOption.AllFolders)
			{

				List<int> allSubFolderIds = AppSeFolderSecurityBL.RetrieveCurrentUserFolderEntityByTransaction(transactionId).Select(o => o.FolderId).ToList (); ;
			
				fileIds = AppFileBL.RetrieveAllAppFileEntityForFolders(allSubFolderIds.ToArray()).Select(o => o.FileId).ToArray();


				if (!string.IsNullOrWhiteSpace(fullTextSearch))
				{
					var fullTextFiledIdList = AppSearchBL.FullTextLatestVersionFileSearch(fullTextSearch);

					fileIds = fileIds.Intersect(fullTextFiledIdList).ToArray();

				}



			}



			if (!fileIds.IsEmpty ())
			{
				var dataTableResult = AppStaticDataSetSearchBL.RetrieveFileViewWithFileIds(viewEntity, fileIds);
				toReturn = AppStaticDataSetSearchBL.ConvertSearchResultToJsonRow(viewEntity, dataTableResult,null);
			}

		
			// moust click is lag 
			return toReturn;
					
			//return null;
			//s

		}


		private static FolderNavigationDto GeFolderTransctionNivigation(int folderId, int? transactionId)
		{
			FolderNavigationDto toReturn = new FolderNavigationDto();

			var searchViewList = AppTransactionNavigationBL.RetrieveFolderSearchViewList(transactionId);

			toReturn.HairarchyFolderRootList = AppSeFolderBL.RetrieveCurrentUserAllSubFolderHairarchyDto(folderId, transactionId.Value);

			AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

			int rootFoderId = folderId;
			var defaultView = searchViewList.Where(o => o.IsDefaultView).First();

			toReturn.ViewList = searchViewList;

			toReturn.TransBusinessType = appTransactionExDto.EmAppTransBusinessType;
			toReturn.TransMgtRootFolderId = rootFoderId;

			toReturn.SearchResultList = AppStaticDataSetSearchBL.GetTransctionFolderViewList(rootFoderId, defaultView.Id as int?, transactionId);

			return toReturn;

			//throw new NotImplementedException();
		}

	}
}