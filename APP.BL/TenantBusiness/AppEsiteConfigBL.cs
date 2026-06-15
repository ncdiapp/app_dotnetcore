using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System;
using Newtonsoft.Json;
using System.Data.SqlClient;
//using APP.Persistence.Common;
using System.IO;
using System.ComponentModel.Design;


using System.Text.RegularExpressions;
using APP.Persistence.Common;

using APP.Framework;
namespace App.BL
{

    public static class AppEsiteConfigBL
    {
        public static readonly string App_AppEsiteEntity_Save_OK = "App_AppEsiteEntity_Save_OK";
        public static readonly string App_AppEsiteEntity_Save_Failed = "App_AppEsiteEntity_Save_Failed";
        public static readonly string App_AppEsiteEntityUILayout_Save_OK = "App_AppEsiteEntityUILayout_Save_OK";
        public static readonly string App_AppEsiteEntityUILayout_Save_Failed = "App_AppEsiteEntityUILayout_Save_Failed";
        public static readonly string App_AppEsiteEntity_Delete_Ok = "App_AppEsiteEntity_Delete_Ok";



        public static readonly string App_AppEsiteEntity_Delete_Failed = "App_AppEsiteEntity_Delete_Failed";

        public static readonly string Controller_formMasterDetailCtrl = "formMasterDetailCtrl";
        public static readonly string Controller_searchCtrl = "searchCtrl";

        public static readonly int WebSiteTemplateSiteIdStartFrom = 1000000;
        // public static readonly int MgtMobileDefaultSiteId = 1147483647;
        //public static readonly int ECommDefaultSiteId = 2147483645;




        //public static EsitePageSetExDto RetrieveOneEsitePageSet(object htmlPageId)
        //{
        //    AppEsitePagesExDto htmlPage = RetrieveOneAppEsitePagesExDto(htmlPageId);
        //    if (htmlPage != null)
        //    {
        //        if (htmlPage.EmresourceContentType.HasValue && htmlPage.EmresourceContentType.Value == (int)EmAppWebsitePageType.HTMLPage)
        //        {
        //            EsitePageSetExDto pageSetExDto = new EsitePageSetExDto();
        //            pageSetExDto.HtmlContentPage = htmlPage;

        // pageSetExDto.CssStylePage = RetrieveOneCssStylePageByMetaDesciption(htmlPage.MetaDesciption);

        // if (!string.IsNullOrWhiteSpace(htmlPage.ControllerName)) { if (htmlPage.ControllerName !=
        // Controller_formMasterDetailCtrl && htmlPage.ControllerName != Controller_searchCtrl) {
        // pageSetExDto.JsControllerPage =
        // RetrieveOneJsControllerPageByControllerName(htmlPage.ControllerName); } }

        // return pageSetExDto; } }

        //    return null;
        //}

        //public static OperationCallResult<EsitePageSetExDto> SaveOneEsitePageSet(EsitePageSetExDto esitePageSetExDto)
        //{
        //    OperationCallResult<EsitePageSetExDto> operationCallResult = new OperationCallResult<EsitePageSetExDto>();
        //    ValidationResult validationResult = new ValidationResult();
        //    operationCallResult.ValidationResult = validationResult;

        // if (esitePageSetExDto != null) { if (esitePageSetExDto.HtmlContentPage != null) { var
        // saveHtmlPageResult = SaveAppEsitePagesExDto(esitePageSetExDto.HtmlContentPage); if
        // (saveHtmlPageResult.IsSuccessfulWithResult) { esitePageSetExDto.HtmlContentPage =
        // saveHtmlPageResult.Object; esitePageSetExDto.HtmlContentPageId =
        // saveHtmlPageResult.Object.Id; } else {
        // validationResult.Merge(saveHtmlPageResult.ValidationResult); return operationCallResult; }

        // if (esitePageSetExDto.CssStylePage != null) { if (esitePageSetExDto.CssStylePage.IsNew) {
        // esitePageSetExDto.CssStylePage.MetaDesciption =
        // esitePageSetExDto.HtmlContentPage.MetaDesciption; }

        // var saveCssStylePageResult = SaveAppEsitePagesExDto(esitePageSetExDto.CssStylePage);

        // if (saveCssStylePageResult.ValidationResult.HasErrors) {
        // validationResult.Merge(saveCssStylePageResult.ValidationResult); } }

        // if (esitePageSetExDto.JsControllerPage != null) { if
        // (esitePageSetExDto.JsControllerPage.IsNew) {
        // esitePageSetExDto.JsControllerPage.MetaDesciption =
        // esitePageSetExDto.HtmlContentPage.ControllerName; }

        // var saveJsControllerPageResult = SaveAppEsitePagesExDto(esitePageSetExDto.JsControllerPage);

        // if (saveJsControllerPageResult.ValidationResult.HasErrors) {
        // validationResult.Merge(saveJsControllerPageResult.ValidationResult); } }

        // if (!validationResult.HasErrors) { if (esitePageSetExDto.HtmlContentPageId != null) {
        // operationCallResult.Object = RetrieveOneEsitePageSet(esitePageSetExDto.HtmlContentPageId);
        // } } } }

        //    return operationCallResult;
        //}

        public static OperationCallResult<bool> CreateEsiteWebApplication(int siteId)
        {
            AppEsiteExDto esiteExDto = RetrieveOneAppEsiteExDto(siteId);

            string appSitePath = AppEsiteFileBL.GetWebSiteBasePath(siteId);
            //string staticPageFolderPath = string.Format("{0}pages\\Static\\NeedToPublishStaticPages\\", appSitePath);

            string websitename = "";
            string applicationPath = esiteExDto.Name.Replace(" ", "");
            string physicalPath = AppEsiteFileBL.GetWebSiteBasePath(siteId);
            string applicationPoolName = applicationPath;

            return (OperationCallResult<bool>)AppIISServerBL.CreateWebApplication(websitename, applicationPath, physicalPath, applicationPoolName);
        }

        //public static ObservableSet<AppEsiteDto> RetrieveAllAppEComsiteDto()
        //{
        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        EntityCollection<AppEsiteEntity> list = new EntityCollection<AppEsiteEntity>();
        //        IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsiteFields.EmApplicationType == (int)EmAppESiteApplicationType.ECommerce); ;



        //        //SortExpression expression = new SortExpression(AppEsiteFields.Name | SortOperator.Ascending);
        //        SortExpression expression = null;
        //        adapter.FetchEntityCollection(list, filter, 0, expression);

        //        var aDtoList = new ObservableSet<AppEsiteDto>();

        //        foreach (var o in list)
        //        {
        //            aDtoList.Add(AppEsiteConverter.ConvertEntityToDto(o));
        //        }

        //        return aDtoList;
        //    }
        //}
        //
        public static List<AppEsiteDto> RetrieveApplicationWebsiteDtoList()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppEsiteEntity> list = new EntityCollection<AppEsiteEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsiteFields.EmApplicationType == (int)EmAppESiteApplicationType.NonECommerce | AppEsiteFields.EmApplicationType == (int)EmAppESiteApplicationType.ECommerce);


                //IRelationPredicateBucket filter = new RelationPredicateBucket();

                //if (applicationId.HasValue)
                //{
                //    filter.PredicateExpression.AddWithAnd(AppEsiteFields.SaasApplicationId == applicationId.Value);
                //}


                filter.PredicateExpression.AddWithAnd(AppEsiteFields.EsiteId < WebSiteTemplateSiteIdStartFrom);
                // IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsiteFields.EsiteId < WebSiteTemplateSiteIdStartFrom);


                //SortExpression expression = new SortExpression(AppEsiteFields.Name | SortOperator.Ascending);
                SortExpression expression = null;
                adapter.FetchEntityCollection(list, filter, 0, expression);

                var aDtoList = new List<AppEsiteDto>();

                foreach (var o in list)
                {
                    var dto = AppEsiteConverter.ConvertEntityToDto(o);

                    aDtoList.Add(dto);

                }

                return aDtoList;
            }
        }


        public static List<AppEsiteDto> RetrieveNextJsApplicationList()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppEsiteEntity> list = new EntityCollection<AppEsiteEntity>();

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsiteFields.EmApplicationType == (int)EmAppESiteApplicationType.NextJsApplication);


                filter.PredicateExpression.AddWithAnd(AppEsiteFields.EsiteId < WebSiteTemplateSiteIdStartFrom);

                SortExpression expression = null;
                adapter.FetchEntityCollection(list, filter, 0, expression);

                var aDtoList = new List<AppEsiteDto>();

                foreach (var o in list)
                {
                    var dto = AppEsiteConverter.ConvertEntityToDto(o);

                    aDtoList.Add(dto);

                }

                return aDtoList;
            }
        }

        const string GetSitePageTable = @"SELECT
                                [EsiteID],
                                [PageID]
                                  ,[FileFullPath]
                                      ,[Title]
                                ,[UrlAndHandle]
                                      ,[EMResourceContentType]
    
                                      ,[LoadOrder]
                                      ,[IsActive]
                                      ,[MetaDesciption]
      
      
                                      ,[TransactionID]
     
                                      ,[IsDefault]
                                      ,[ControllerName]
                                      ,[SearchID]
                                      ,[SearchViewID]
                                      ,[IsMasterLayoutPage]
     
                                      ,[NavigationCtrlJavascript]
      
                                  FROM [dbo].[AppESitePages] where EsiteID =@eSiteId";

        public static DataTable RetrieveOneSitesPagesDataTable(int siteId)
        {
            List<SqlParameter> listParamters = new List<SqlParameter>();
            listParamters.Add(new SqlParameter("@eSiteId", siteId));
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                return adapter.ExecuteDataTableRetrievalQuery(GetSitePageTable, listParamters);
            }
        }

        public static void UpdateOneSitesPagesDataTable(int siteId, DataTable sourceDtable)
        {

            sourceDtable.Columns.Add("TransactionID_", typeof(System.Int32));
            sourceDtable.Columns.Add("SearchID_", typeof(System.Int32));

            foreach (DataRow row in sourceDtable.Rows)
            {
                int? traId = ControlTypeValueConverter.ConvertValueToInt(row["TransactionID"]);
                if (traId.HasValue)
                {
                    row["TransactionID_"] = traId.Value;
                }
                else
                {
                    // row["TransactionID_"] = DBNull.Value;
                }
                int? seartraId = ControlTypeValueConverter.ConvertValueToInt(row["SearchID"]);
                if (seartraId.HasValue)
                {
                    row["SearchID_"] = seartraId.Value;
                }
                // row["SearchID_"] = ControlTypeValueConverter.ConvertValueToInt(row["SearchID"]);

            }

            sourceDtable.Columns.Remove("TransactionID");
            sourceDtable.Columns.Remove("SearchID");

            sourceDtable.Columns["TransactionID_"].ColumnName = "TransactionID";
            sourceDtable.Columns["SearchID_"].ColumnName = "SearchID";

            List<SqlParameter> listParamters = new List<SqlParameter>();
            listParamters.Add(new SqlParameter("@eSiteId", siteId));
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                // ,[EsiteID],FileFullPath

                foreach (DataRow dtTow in sourceDtable.Rows)
                {
                    dtTow["EsiteID"] = siteId;
                }

                // need to remove PagesId

                sourceDtable.Columns.Remove("PageID");


                List<string> externaKeyColumn = new List<string>();
                externaKeyColumn.Add("EsiteID");
                externaKeyColumn.Add("FileFullPath");


                DataSoureHelp.ProcessMutileKeyTable(sourceDtable, "", externaKeyColumn, "AppESitePages", new List<string>(), adapter.ConnectionString);
            }
        }


        public static List<AppEsiteDto> RetrieveWebsiteTemplateDtoList()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppEsiteEntity> list = new EntityCollection<AppEsiteEntity>();
                //IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsiteFields.EmApplicationType == (int)EmAppESiteApplicationType.NonECommerce);
                //1147483647
                IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsiteFields.EsiteId >= WebSiteTemplateSiteIdStartFrom);



                //SortExpression expression = new SortExpression(AppEsiteFields.Name | SortOperator.Ascending);
                SortExpression expression = null;
                adapter.FetchEntityCollection(list, filter, 0, expression);

                var aDtoList = new List<AppEsiteDto>();

                foreach (var o in list)
                {
                    var dto = AppEsiteConverter.ConvertEntityToDto(o);
                    dto.SitePublishedBaseUrl = dto.Description;
                    aDtoList.Add(dto);

                }

                return aDtoList;
            }
        }



        public static AppEsiteExDto RetrieveOneAppEsiteExDto(object AppEsiteId)
        {
            AppEsiteEntity aAppEsiteEntity = RetrieveOneAppEsiteEntity(AppEsiteId);
            AppEsiteExDto aAppEsiteDto = AppEsiteConverter.ConvertEntityToExDto(aAppEsiteEntity);

            aAppEsiteDto.RootFolderPath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath((int)AppEsiteId);


            if (aAppEsiteDto.EmApplicationType.HasValue &&
                aAppEsiteDto.EmApplicationType.Value == (int)EmAppESiteApplicationType.NextJsApplication)
            {
                PrepareNextJsEsiteComponentNameList(aAppEsiteDto);
            }
            else
            {
                foreach (var o in aAppEsiteEntity.AppEsiteCatalogue)
                {
                    AppEsiteCatalogueExDto aAppEsiteKeyExDto = AppEsiteCatalogueConverter.ConvertEntityToExDto(o);
                    aAppEsiteDto.AppEsiteCatalogueList.Add(aAppEsiteKeyExDto);
                }

                foreach (var o in aAppEsiteEntity.AppEsitePages.OrderBy(o => o.LoadOrder))
                {
                    AppEsitePagesExDto appEsitePagesExDto = AppEsitePagesConverter.ConvertEntityToExDto(o);
                    PrepreSitePageTypeDisplay(appEsitePagesExDto);

                    aAppEsiteDto.AppEsitePagesList.Add(appEsitePagesExDto);
                }

                //AppEsiteFileBL.LoadBreakWidthDataFromMediaQueryScssFiles(aAppEsiteDto);

                PrepareGlobalSiteThemeParameterList(aAppEsiteDto);

                PrepareEsiteThirdPartControlThemeNameList(aAppEsiteDto);

                PrepareUserDefinedJsFunctionDtoList(aAppEsiteDto);

                PrepareEsiteComponentConfig(aAppEsiteDto);

                PrepareEsitePartnerMapping(aAppEsiteDto);

                if (aAppEsiteDto.EsiteAttribute != null && !string.IsNullOrWhiteSpace(aAppEsiteDto.EsiteAttribute.TemplateCode))
                {
                    aAppEsiteDto.SiteTemplateRegisterDto = GetOneAppEsiteTemplateRegisterDtoByCode(aAppEsiteDto.EsiteAttribute.TemplateCode);
                }
            }


            return aAppEsiteDto;
        }


        public static AppEsiteEntity RetrieveOneStandAloneAppEsiteEntity(object AppEsiteId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteEntity AppEsiteEntity = new AppEsiteEntity(int.Parse(AppEsiteId.ToString()));


                adpater.FetchEntity(AppEsiteEntity);
                return AppEsiteEntity;
            }
        }

        public static bool DeletePageWithPathList(int? esiteId, string[] fileFullPath)
        {
            if (esiteId.HasValue)
            {
                try
                {
                    EntityCollection<AppEsitePagesEntity> entityCollectionList = new EntityCollection<AppEsitePagesEntity>();
                    using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {

                        IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsitePagesFields.FileFullPath == fileFullPath & AppEsitePagesFields.EsiteId == esiteId.Value);


                        adpater.DeleteEntitiesDirectly(typeof(AppEsitePagesEntity), filter);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return false;


        }

        // // need to remove !!! Junk methid !11
        internal static AppEsitePagesExDto RetrieveOneFileExtentPageDto(string fileFullPathOrRelativepath, int siteId)
        {

            string siteBasePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath(siteId);
            string relativepath = fileFullPathOrRelativepath.Replace(siteBasePath, "");

            EntityCollection<AppEsitePagesEntity> entityCollectionList = new EntityCollection<AppEsitePagesEntity>();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsitePagesFields.FileFullPath == relativepath & AppEsitePagesFields.EsiteId == siteId);


                adpater.FetchEntityCollection(entityCollectionList, filter);

            }

            if (entityCollectionList.Count > 0)
            {
                var appEsitePagesExDto = AppEsitePagesConverter.ConvertEntityToExDto(entityCollectionList[0]);
                PrepreSitePageTypeDisplay(appEsitePagesExDto);

                if (appEsitePagesExDto.PageAttribute == null)
                {
                    appEsitePagesExDto.PageAttribute = new AppEsitePageAttributeDto();
                }


                if (appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive == null)
                {
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive = new Dictionary<string, bool>();
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("base", true);
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("sm", true);
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("md", false);
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("lg", true);
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("xl", false);
                    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("2xl", false);
                }

                return appEsitePagesExDto;
            }
            else
            {
                return null;
            }



        }

        public static List<AppEsitePagesDto> RetrieveFileExtentPageDtoList(int? appEsiteId)
        {
            List<AppEsitePagesDto> toReturn = new List<AppEsitePagesDto>();
            EntityCollection<AppEsitePagesEntity> entityCollectionList = new EntityCollection<AppEsitePagesEntity>();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsitePagesFields.EsiteId == appEsiteId

                   & AppEsitePagesFields.FileFullPath != string.Empty);


                adpater.FetchEntityCollection(entityCollectionList, filter);

            }

            foreach (var entity in entityCollectionList)
            {
                toReturn.Add(AppEsitePagesConverter.ConvertEntityToDto(entity));
            }

            return toReturn;
        }
        public static AppEsiteEntity RetrieveOneSimpleAppEsiteEntity(object AppEsiteId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteEntity AppEsiteEntity = new AppEsiteEntity(int.Parse(AppEsiteId.ToString()));

                adpater.FetchEntity(AppEsiteEntity);
                return AppEsiteEntity;
            }
        }

        public static AppEsiteEntity RetrieveOneAppEsiteEntity(object AppEsiteId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteEntity AppEsiteEntity = new AppEsiteEntity(int.Parse(AppEsiteId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppEsiteEntity);

                rootPath.Add(AppEsiteEntity.PrefetchPathAppEsiteCatalogue);
                rootPath.Add(AppEsiteEntity.PrefetchPathAppEsitePages);

                adapter.FetchEntity(AppEsiteEntity, rootPath);
                return AppEsiteEntity;
            }
        }

        private static int? GetCompnayIdBySiteId(object AppEsiteId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteEntity AppEsiteEntity = new AppEsiteEntity(int.Parse(AppEsiteId.ToString()));
                IncludeFieldsList incdeField = new IncludeFieldsList();
                incdeField.Add(AppEsiteFields.AppCreatedByCompanyId);


                adpater.FetchEntity(AppEsiteEntity, null, null, incdeField);
                return AppEsiteEntity.AppCreatedByCompanyId;
            }
        }

        public static string GetSiteIdAnoymousToken(object eSiteId)
        {
            int? compnayId = GetCompnayIdBySiteId(eSiteId);

            if (compnayId.HasValue)
            {
                return AppCacheManagerBL.GetCurrentCompanyAnoymousToken(compnayId.Value);
            }

            return "";
        }
        public static AppEsiteEntity RetrieveRunningTimeOneAppEsiteEntity(object AppEsiteId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteEntity AppEsiteEntity = new AppEsiteEntity(int.Parse(AppEsiteId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppEsiteEntity);

                //IPredicateExpression masterPagepredicate = new PredicateExpression();
                //masterPagepredicate.Add(AppEsitePagesFields.IsMasterLayoutPage == true);

                //rootPath.Add(AppEsiteEntity.PrefetchPathAppEsitePages, 1, masterPagepredicate);

                adpater.FetchEntity(AppEsiteEntity, rootPath);
                return AppEsiteEntity;
            }
        }

        public static EntityCollection<AppEsitePagesEntity> RetrieveOneSiteAppPagesEntityList(object esiteId)
        {
            EntityCollection<AppEsitePagesEntity> list = new EntityCollection<AppEsitePagesEntity>();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                IRelationPredicateBucket filter = new RelationPredicateBucket(AppEsitePagesFields.EsiteId == esiteId);

                SortExpression expression = new SortExpression(AppEsitePagesFields.LoadOrder | SortOperator.Ascending);
                adpater.FetchEntityCollection(list, filter, 0, expression);

                return list;
            }
        }


        public static OperationCallResult<AppEsiteExDto> SaveAsAppWebsite(object appEsiteId)
        {
            AppEsiteExDto saveAsDto = RetrieveOneAppEsiteExDto(appEsiteId);

            saveAsDto.Name = "Copy From :" + saveAsDto.Name;
            saveAsDto.Id = null;

            return SaveAppEsiteExDto(saveAsDto);


        }



        public static OperationCallResult<AppEsiteExDto> SaveAppEsiteExDto(AppEsiteExDto aAppEsiteExDto)
        {
            OperationCallResult<AppEsiteExDto> aOperationCallResult = new OperationCallResult<AppEsiteExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppEsiteEntity aAppEsiteEntity;



            bool isNewEsite = aAppEsiteExDto.IsNew;
            if (aAppEsiteExDto.IsNew)
            {
                aAppEsiteEntity = new AppEsiteEntity();
                AppEsiteConverter.CopyDtoToEntity(aAppEsiteEntity, aAppEsiteExDto);



                foreach (var templatefieldDto in aAppEsiteExDto.AppEsiteCatalogueList)
                {
                    AppEsiteCatalogueEntity aAppEsiteCatalogueEntity = new AppEsiteCatalogueEntity();
                    AppEsiteCatalogueConverter.CopyDtoToEntity(aAppEsiteCatalogueEntity, templatefieldDto);
                    aAppEsiteEntity.AppEsiteCatalogue.Add(aAppEsiteCatalogueEntity);
                }

                //if (aAppEsiteExDto.EmApplicationType.HasValue)
                //{
                //    aAppEsiteExDto.AppEsitePagesList = new ObservableSet<AppEsitePagesExDto>();
                //    var masterPage = new AppEsitePagesExDto();
                //    masterPage.EmresourceContentType = (int)EmAppWebsitePageType.HTMLPage;
                //    masterPage.Title = "MasterLayoutPage";
                //    masterPage.MetaDesciption = "MasterPageNavigationCtrl";
                //    masterPage.IsActive = true;
                //    masterPage.IsMasterLayoutPage = true;
                //    masterPage.HtmlContent = string.Empty;
                //    masterPage.PageJsMethod = string.Empty;
                //    masterPage.PageCssStyle = string.Empty;
                //    masterPage.LoadOrder = 0;

                //    aAppEsiteExDto.AppEsitePagesList.Add(masterPage);

                //    foreach (var pageDto in aAppEsiteExDto.AppEsitePagesList)
                //    {
                //        AppEsitePagesEntity aAppEsitePagesEntity = new AppEsitePagesEntity();
                //        AppEsitePagesConverter.CopyDtoToEntity(aAppEsitePagesEntity, pageDto);
                //        aAppEsiteEntity.AppEsitePages.Add(aAppEsitePagesEntity);
                //    }
                //}


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppEsiteEntity);
                        adapter.Commit();

                        aAppEsiteExDto.Id = aAppEsiteEntity.EsiteId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppEsiteExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppEsiteExDto(aAppEsiteExDto));
            }

            if (!aValidationResult.HasErrors)
            {
                aValidationResult.Merge(AppEsiteFileBL.GenerateMediaQueryFiles(aAppEsiteExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppEsiteExDto(aAppEsiteExDto.Id);

                // need to create site Home repostory
                if (isNewEsite)
                {
                    string sitePath = AppEsiteFileBL.GetCompanyWebSiteBaseFolderPath((int)aAppEsiteExDto.Id);
                    if (!Directory.Exists(sitePath))
                    {
                        Directory.CreateDirectory(sitePath);
                    }


                }



                string mgtBaseUrl = "";

                if (aAppEsiteExDto.EsiteAttribute != null && !string.IsNullOrWhiteSpace(aAppEsiteExDto.EsiteAttribute.MgtSiteBaseUrl))
                {
                    mgtBaseUrl = aAppEsiteExDto.EsiteAttribute.MgtSiteBaseUrl;
                }

                AppEsiteEntity appEsiteEntity = AppEsiteConfigBL.RetrieveOneAppEsiteEntity(aAppEsiteExDto.Id);
                AppEsiteFileBL.UpdateHostSiteMainVariablesJsPage(appEsiteEntity, null, appEsiteEntity.SitePublishedBaseUrl, mgtBaseUrl);
            }

            return aOperationCallResult;
        }



        public static OperationCallResult<AppEsiteExDto> GenerateWebSiteFromWizardSetting(AppEsiteExDto wizardObj)
        {
            OperationCallResult<AppEsiteExDto> aOperationCallResult = new OperationCallResult<AppEsiteExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppEsiteExDto appEsiteExDto = null;
            appEsiteExDto = GenerateWebSiteFromWizardSetting_InitialSave(wizardObj, aValidationResult, appEsiteExDto);

            if (appEsiteExDto != null)
            {
                if (wizardObj.SiteTemplateId.HasValue)
                {
                    AppEsiteFileBL.ImportWebSiteTemplateToApplicationSite(wizardObj.SiteTemplateId.Value, (int)appEsiteExDto.Id, wizardObj.RequestHostServerPath);
                    appEsiteExDto = RetrieveOneAppEsiteExDto(appEsiteExDto.Id);
                    aOperationCallResult.Object = appEsiteExDto;
                }

                //if (wizardObj.PublictSiteLandingPageLayoutId.HasValue)
                //{
                //    //string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;


                //    //string copyFromFolderPath = string.Format(@"{0}Server\Views\WebSiteMgt\AppWebsiteEditor\LandingPageLayout\PublicSite\Layout1\", copyFromFolderPath,);

                //    //return rootFolderPath;
                //}
            }

            return aOperationCallResult;
        }

        public static bool IsDefaultEsiteExist()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppEsiteEntity> list = new EntityCollection<AppEsiteEntity>();
                adapter.FetchEntityCollection(list, new RelationPredicateBucket(AppEsiteFields.EsiteId == 1));

                if (list.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        // For SAAS User Company Default Site Creation
        public static OperationCallResult<AppEsiteExDto> GenerateClientApplicatoinDefaultWebSiteFiles(string companyId, string applicationCode)
        {
            OperationCallResult<AppEsiteExDto> aOperationCallResult = new OperationCallResult<AppEsiteExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string appSitePath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_1\", baseDirectory, companyId);
            string templatePath = string.Format(@"{0}FileRepDevWebsiteTemplate\Site_{1}\", baseDirectory, int.MaxValue);

            if (!Directory.Exists(appSitePath))
            {
                Directory.CreateDirectory(appSitePath);
            }





            AppEsiteFileBL.CopyFolderToNewLocation(templatePath, appSitePath);

            AppEsiteFileBL.UpdateUserCompanyDefaultSiteMainVariablesJsPage(companyId, applicationCode);

            //AppEsiteFileBL.ImportWebSiteTemplateToApplicationSite(wizardObj.SiteTemplateId.Value, (int)appEsiteExDto.Id, wizardObj.RequestHostServerPath);



            return aOperationCallResult;
        }

        public static OperationCallResult<bool> ResetAppWebsite(int esiteId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            int companyId = (int)ServerContext.Instance.CurrentCompanyId;
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string appSitePath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_{2}\", baseDirectory, companyId, esiteId);
            string templatePath = string.Format(@"{0}FileRepDevWebsiteTemplate\Site_{1}\", baseDirectory, int.MaxValue);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    string query = string.Format(@"
                        delete from AppSecurityUserListMenu where MenuID in (
	                        select MenuID from AppListMenu where EsiteID = {0} and not (Name = 'Home' and AppCreatedByID is null) 
                        );

                        delete from AppListMenu where EsiteID = {1} and not (Name = 'Home' and AppCreatedByID is null) 
                    ", esiteId, esiteId);

                    adapter.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppEsiteCatalogueEntity), new RelationPredicateBucket(AppEsiteCatalogueFields.EsiteId == esiteId));
                    adapter.DeleteEntitiesDirectly(typeof(AppEsitePagesEntity), new RelationPredicateBucket(AppEsitePagesFields.EsiteId == esiteId));

                    adapter.Commit();



                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_AppEsiteEntity_Delete_Failed, "AppEsite Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_AppEsiteEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                try
                {
                    bool isSuccess = AppEsiteFileBL.ClearOneFileFolderByPath(appSitePath);

                    if (!isSuccess)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Warning, "Cannot remove all site files. Some files are currently in use.\n\n"));
                    }
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot remove all site files. Some files are currently in use.\n\n"));
                }
            }

            if (!aValidationResult.HasErrors)
            {
                try
                {


                    AppEsiteFileBL.ImportWebSiteTemplateToApplicationSite(int.MaxValue, esiteId, "/MGT/");

                    string applicationCode = AppCompanyBL.GetCompnayDomainIdentityToken(companyId);

                    AppEsiteFileBL.UpdateUserCompanyDefaultSiteMainVariablesJsPage(companyId.ToString(), applicationCode);



                    aValidationResult.Items.Add(new ValidationItem(null, App_AppEsiteEntity_Delete_Ok, ValidationItemType.Message, "Reset Website Successfull."));
                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "plm_UninstallSaasAccount_error", ValidationItemType.Error, "Cannot remove all site files. Some files are currently in use.\n\n"));
                }
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<object> DeleteOneAppEsite(object AppEsiteId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppEsiteCatalogueEntity), new RelationPredicateBucket(AppEsiteCatalogueFields.EsiteId == AppEsiteId));
                    adapter.DeleteEntitiesDirectly(typeof(AppEsitePagesEntity), new RelationPredicateBucket(AppEsitePagesFields.EsiteId == AppEsiteId));
                    adapter.DeleteEntitiesDirectly(typeof(AppEsiteEntity), new RelationPredicateBucket(AppEsiteFields.EsiteId == AppEsiteId));
                    string message = StringLocalizer.Localize(App_AppEsiteEntity_Delete_Ok, "AppEsite Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, App_AppEsiteEntity_Delete_Ok, ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize(App_AppEsiteEntity_Delete_Failed, "AppEsite Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, App_AppEsiteEntity_Delete_Failed, ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = AppEsiteId;
            }

            return aOperationCallResult;
        }

        private static ValidationResult ProcessDirtyAppEsiteExDto(AppEsiteExDto aAppEsiteExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            int[] dirtyCatalogueIds = aAppEsiteExDto.AppEsiteCatalogueList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();
            int[] dirtyPageIds = aAppEsiteExDto.AppEsitePagesList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

            AppEsiteEntity aAppEsiteEntity = RetrieveOneAppEsiteEntity(aAppEsiteExDto.Id);

            Dictionary<int, AppEsiteCatalogueEntity> dictAppEsiteCatalogueFromDbms = aAppEsiteEntity.AppEsiteCatalogue.ToDictionary(o => o.EsiteCatalogueId, o => o);
            Dictionary<int, AppEsitePagesEntity> dictAppEsitePagesFromDbms = aAppEsiteEntity.AppEsitePages.ToDictionary(o => o.PageId, o => o);


            AppEsiteConverter.CopyDtoToEntity(aAppEsiteEntity, aAppEsiteExDto);
            // aAppEsiteEntity.ModifiedDate = System.DateTime.UtcNow; aAppEsiteEntity.ModifiedBy = (int)ServerContext.Instance.CurrentUid;

            //------- check  AppEsiteCatalogue

            // new Items
            foreach (var aChildDto in aAppEsiteExDto.AppEsiteCatalogueList.FindNewItems())
            {
                AppEsiteCatalogueEntity aNewChildEntity = new AppEsiteCatalogueEntity();
                AppEsiteCatalogueConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppEsiteEntity.AppEsiteCatalogue.Add(aNewChildEntity);
            }

            foreach (var aChildDto in aAppEsiteExDto.AppEsitePagesList.FindNewItems())
            {
                AppEsitePagesEntity aNewChildEntity = new AppEsitePagesEntity();
                AppEsitePagesConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aAppEsiteEntity.AppEsitePages.Add(aNewChildEntity);
            }

            // Dirty items
            foreach (var modifyitem in aAppEsiteExDto.AppEsiteCatalogueList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppEsiteCatalogueFromDbms.ContainsKey(dtoKey))
                {
                    AppEsiteCatalogueConverter.CopyDtoToEntity(dictAppEsiteCatalogueFromDbms[dtoKey], modifyitem);
                }
            }

            foreach (var modifyitem in aAppEsiteExDto.AppEsitePagesList.FindModifiedItems())
            {
                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictAppEsitePagesFromDbms.ContainsKey(dtoKey))
                {
                    AppEsitePagesConverter.CopyDtoToEntity(dictAppEsitePagesFromDbms[dtoKey], modifyitem);
                }
            }

            // deletedIDs
            int[] deleteFieldIDs = aAppEsiteExDto.AppEsiteCatalogueList.FindDeletedItemIds().Cast<int>().ToArray();
            int[] deletePageIDs = aAppEsiteExDto.AppEsitePagesList.FindDeletedItemIds().Cast<int>().ToArray();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppEsiteEntity);

                    // Need to delete AppEsiteCatalogueFields
                    if (deleteFieldIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppEsiteCatalogueEntity), new RelationPredicateBucket(AppEsiteCatalogueFields.EsiteId == deleteFieldIDs));
                    }

                    if (deletePageIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppEsitePagesEntity), new RelationPredicateBucket(AppEsitePagesFields.EsiteId == deleteFieldIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Concurrency validation ("Concurrency violation, data not updated")
                catch (ORMConcurrencyException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_Concurrency_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteEntity), "App_AppEsiteEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }



        public static AppEsiteCatalogueExDto RetrieveOneAppEsiteCatalogueExDto(object EstoreId)
        {
            AppEsiteCatalogueEntity aAppEsiteCatalogueEntity = RetrieveOneAppEsiteCatalogueEntity(EstoreId);
            AppEsiteCatalogueExDto aEstoreDto = AppEsiteCatalogueConverter.ConvertEntityToExDto(aAppEsiteCatalogueEntity);


            return aEstoreDto;
        }

        internal static AppEsiteCatalogueExDto SetupEstoreExDto(object EstoreId)
        {
            AppEsiteCatalogueEntity aAppEsiteCatalogueEntity = RetrieveOneAppEsiteCatalogueEntity(EstoreId);
            AppEsiteCatalogueExDto aEstoreDto = AppEsiteCatalogueConverter.ConvertEntityToExDto(aAppEsiteCatalogueEntity);
            SetupEstoeExdto(aEstoreDto);

            return aEstoreDto;
        }
        private static void SetupEstoeExdto(AppEsiteCatalogueExDto aAppEsiteCatalogueExDto)
        {

            if (aAppEsiteCatalogueExDto.TreeNavigationViewId.HasValue)
            {
                aAppEsiteCatalogueExDto.NavigationTreeSearchViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aAppEsiteCatalogueExDto.TreeNavigationViewId);
            }

            // aAppEsiteCatalogueExDto.TreeViewDataSetExDto = AppDataSetBL.RetrieveOneAppDataSetExDto(aAppEsiteCatalogueExDto.NavigationTreeSearchViewExDto.DataSetId);
            if (aAppEsiteCatalogueExDto.CatalogCardViewId.HasValue)
            {
                aAppEsiteCatalogueExDto.CatalogCardViewExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aAppEsiteCatalogueExDto.CatalogCardViewId);


            }

            if (aAppEsiteCatalogueExDto.CatalogCardDetailId.HasValue)
            {
                aAppEsiteCatalogueExDto.CatalogCardDetailExDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(aAppEsiteCatalogueExDto.CatalogCardDetailId);


            }


        }


        public static AppEsiteCatalogueEntity RetrieveOneAppEsiteCatalogueEntity(object EstoreId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsiteCatalogueEntity EstoreEntity = new AppEsiteCatalogueEntity(int.Parse(EstoreId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppEsiteCatalogueEntity);


                adpater.FetchEntity(EstoreEntity, rootPath);
                return EstoreEntity;
            }
        }


        public static OperationCallResult<AppEsiteCatalogueExDto> SaveAppEsiteCatalogueExDto(AppEsiteCatalogueExDto aAppEsiteCatalogueExDto)
        {
            OperationCallResult<AppEsiteCatalogueExDto> aOperationCallResult = new OperationCallResult<AppEsiteCatalogueExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppEsiteCatalogueEntity aAppEsiteCatalogueEntity;

            // prepare Data
            if (aAppEsiteCatalogueExDto.IsNew)
            {
                aAppEsiteCatalogueEntity = new AppEsiteCatalogueEntity();
                AppEsiteCatalogueConverter.CopyDtoToEntity(aAppEsiteCatalogueEntity, aAppEsiteCatalogueExDto);



                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppEsiteCatalogueEntity);
                        adapter.Commit();

                        aAppEsiteCatalogueExDto.Id = aAppEsiteCatalogueEntity.EsiteCatalogueId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_EstoreEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_EstoreEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_EstoreEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            else if (aAppEsiteCatalogueExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppEsiteCatalogueExDto(aAppEsiteCatalogueExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppEsiteCatalogueExDto(aAppEsiteCatalogueExDto.Id);
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> DeleteOneAppEsiteCatalogue(object EstoreId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppEsiteCatalogueEntity), new RelationPredicateBucket(AppEsiteCatalogueFields.EsiteCatalogueId == EstoreId));
                    string message = StringLocalizer.Localize("App_EstoreEntity_Delete_Ok", "Estore Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EstoreEntity_Delete_Ok", ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize("App_EstoreEntity_Delete_Failed", "Estore Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EstoreEntity_Delete_Failed", ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = EstoreId;
            }

            return aOperationCallResult;
        }


        public static AppEsitePagesExDto RetrieveOneAppEsitePagesExDto(object pageId)
        {
            AppEsitePagesEntity aAppEsitePagesEntity = RetrieveOneAppEsitePagesEntity(pageId);
            AppEsitePagesExDto dto = AppEsitePagesConverter.ConvertEntityToExDto(aAppEsitePagesEntity);

            PrepreSitePageTypeDisplay(dto);

            return dto;
        }





        public static OperationCallResult<AppEsitePagesExDto> SaveAppEsitePagesExDto(AppEsitePagesExDto aAppEsitePagesExDto)
        {
            OperationCallResult<AppEsitePagesExDto> aOperationCallResult = new OperationCallResult<AppEsitePagesExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppEsitePagesEntity aAppEsitePagesEntity;

            bool isNewEnttiy = aAppEsitePagesExDto.IsNew;

            if (aAppEsitePagesExDto.EmresourceContentType.HasValue && aAppEsitePagesExDto.EmresourceContentType.Value == (int)EmAppWebsitePageType.HTMLPage
                && string.IsNullOrWhiteSpace(aAppEsitePagesExDto.UrlAndHandle))
            {
                aAppEsitePagesExDto.UrlAndHandle = aAppEsitePagesExDto.MetaDesciption;
            }

            // prepare Data
            if (aAppEsitePagesExDto.IsNew)
            {
                if (aAppEsitePagesExDto.FileFullPath.HasValue())
                {
                    AppEsiteConfigBL.DeletePageWithPathList(aAppEsitePagesExDto.EsiteId, new string[] { aAppEsitePagesExDto.FileFullPath });
                }


                aAppEsitePagesEntity = new AppEsitePagesEntity();
                AppEsitePagesConverter.CopyDtoToEntity(aAppEsitePagesEntity, aAppEsitePagesExDto);



                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppEsitePagesEntity);



                        AppEsitePagesEntity updateEntity = new AppEsitePagesEntity();
                        updateEntity.MetaDesciption = "Page" + aAppEsitePagesEntity.PageId;


                        if (!string.IsNullOrWhiteSpace(aAppEsitePagesExDto.Title))
                        {
                            string title = aAppEsitePagesExDto.Title.Split('.').First();
                            title = title.Replace(' ', '_').Trim();
                            //updateEntity.MetaDesciption = title + "_Page" + aAppEsitePagesEntity.PageId;
                            updateEntity.MetaDesciption = title;
                        }

                        adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppEsitePagesFields.PageId == aAppEsitePagesEntity.PageId));

                        adapter.Commit();

                        aAppEsitePagesExDto.Id = aAppEsitePagesEntity.PageId;

                        var dtoFromDb = RetrieveOneAppEsitePagesExDto(aAppEsitePagesExDto.Id); ;

                        AutoAddPageToSiteMenu(dtoFromDb);

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_EstoreEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    // Entity Logical Validation Exception
                    catch (ORMEntityValidationException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_EstoreEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                    }

                    // Database FK Exeption ........
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_EstoreEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }

                }
            }

            else if (aAppEsitePagesExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppEsitePagesExDto(aAppEsitePagesExDto));

                if (aAppEsitePagesExDto.SearchId.HasValue)
                {
                    AppListMenuBL.SynchronizeOneEsitePageMenus(aAppEsitePagesExDto);
                }


            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                var dtoFromDb = RetrieveOneAppEsitePagesExDto(aAppEsitePagesExDto.Id); ;
                aOperationCallResult.Object = dtoFromDb;
                // need to update RouteState !!


                AppEsiteFileBL.UpdaePageRouteStateJsAndHomePageLink(dtoFromDb);





                if (aAppEsitePagesExDto.StyleSheetUpdateDto != null)
                {
                    AppEsiteFileBL.SaveAppEsiteMediaQueryChangesToStyleSheet(aAppEsitePagesExDto.StyleSheetUpdateDto);
                }
            }

            return aOperationCallResult;
        }

        private static void AutoAddPageToSiteMenu(AppEsitePagesExDto aAppEsitePagesExDto)
        {
            if (aAppEsitePagesExDto.TransactionId.HasValue)
            {

            }
            else
            {
                int menuItemCategory = (int)EmAppMenuItemCategory.PublicPage;

                if (aAppEsitePagesExDto.FileFullPath.Contains("\\Customer\\Dev\\"))
                {
                    menuItemCategory = (int)EmAppMenuItemCategory.ClientPage;
                }
                else if (aAppEsitePagesExDto.FileFullPath.Contains("\\Customer\\Dev\\"))
                {
                    menuItemCategory = (int)EmAppMenuItemCategory.SupplierPage;
                }
                else
                {
                    menuItemCategory = (int)EmAppMenuItemCategory.PublicPage;
                }

                ObservableSet<AppListMenuExDto> menuList = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(aAppEsitePagesExDto.EsiteId, menuItemCategory);

                var newMenuDto = new AppListMenuExDto();
                newMenuDto.Name = Regex.Replace(aAppEsitePagesExDto.MetaDesciption.Replace("_", " "), "([a-z])([A-Z])", "$1 $2").Trim().ToLower();
                newMenuDto.MenuPath = aAppEsitePagesExDto.MetaDesciption;
                newMenuDto.LinkType = 1;
                newMenuDto.RouteCode = aAppEsitePagesExDto.MetaDesciption;
                newMenuDto.EmAppMenuItemCategory = menuItemCategory;
                newMenuDto.EmDeviceMenuShowMode = 3;
                newMenuDto.EsiteId = aAppEsitePagesExDto.EsiteId;

                if (aAppEsitePagesExDto.SearchId.HasValue)
                {
                    newMenuDto.Link = aAppEsitePagesExDto.SearchId.Value.ToString();
                }

                if (menuList.Where(o => o.Sort.HasValue).Count() == 0)
                {
                    newMenuDto.Sort = 1;
                }
                else
                {
                    newMenuDto.Sort = menuList.Where(o => o.Sort.HasValue).Max(p => p.Sort.Value) + 1;
                }

                newMenuDto.AppListMenu_List = new ObservableSet<AppListMenuExDto>();

                AppTreeListMenuBL.SaveOneAppListMenuTreeNode(newMenuDto);

            }
        }

        public static OperationCallResult<object> DeleteOneAppEsitePages(object pageId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.DeleteEntitiesDirectly(typeof(AppEsitePagesEntity), new RelationPredicateBucket(AppEsitePagesFields.PageId == pageId));
                    string message = StringLocalizer.Localize("App_EstoreEntity_Delete_Ok", "Estore Delete Successful");
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EstoreEntity_Delete_Ok", ValidationItemType.Message, message));

                    adapter.Commit();
                }

                // FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize("App_EstoreEntity_Delete_Failed", "Estore Delete Failed" + ex.ToString());
                    aValidationResult.Items.Add(new ValidationItem(null, "App_EstoreEntity_Delete_Failed", ValidationItemType.Error, message));
                }
            }

            // if no any errors
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = pageId;
            }

            return aOperationCallResult;
        }

        public static AppEsiteTemplateRegisterDto GetOneAppEsiteTemplateRegisterDtoByCode(string templateCode)
        {
            Dictionary<string, AppEsiteTemplateRegisterDto> dictTemplateCodeAndDto = RetrieveAllEsiteLayoutTemplates();

            if (dictTemplateCodeAndDto != null && dictTemplateCodeAndDto.ContainsKey(templateCode))
            {
                return dictTemplateCodeAndDto[templateCode];
            }

            return null;
        }

        public static Dictionary<string, AppEsiteTemplateRegisterDto> _dictTemplateCodeAndDto;

        public static Dictionary<string, AppEsiteTemplateRegisterDto> RetrieveAllEsiteLayoutTemplates()
        {
            if (_dictTemplateCodeAndDto != null)
            {
                return _dictTemplateCodeAndDto;
            }
            else
            {
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string templateRegFilePath = string.Format(@"{0}FileRepDevWebsiteTemplate\SiteLayoutTemplate\TemplateRegister.json", baseDirectory);


                    string templateRegisterText = File.ReadAllText(templateRegFilePath);

                    if (!string.IsNullOrWhiteSpace(templateRegisterText))
                    {
                        Dictionary<string, AppEsiteTemplateRegisterDto> dictTemplateCodeAndDto = JsonConvert.DeserializeObject<Dictionary<string, AppEsiteTemplateRegisterDto>>(templateRegisterText);
                        _dictTemplateCodeAndDto = dictTemplateCodeAndDto;
                        return dictTemplateCodeAndDto;
                    }
                }
                catch (Exception ex)
                {

                }
                return null;
            }

        }

        public static OperationCallResult<bool> SetEsiteLayoutTemplate(int esiteId, string templateCode)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (!string.IsNullOrWhiteSpace(templateCode))
            {
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    string templateFolderPath = string.Format(@"{0}FileRepDevWebsiteTemplate\SiteLayoutTemplate\Templates\{1}", baseDirectory, templateCode);
                    string templateConfigFilePath = templateFolderPath + "\\config.json";

                    string defaultTemplateFolderPath = string.Format(@"{0}FileRepDevWebsiteTemplate\SiteLayoutTemplate\Templates\Default", baseDirectory);
                    string defaultTemplateConfigFilePath = defaultTemplateFolderPath + "\\config.json";

                    List<string> needToBackupPathList = new List<string>() {
                                "Public\\Dev\\",
                                "Customer\\Dev\\",
                                "Supplier\\Dev\\",
                                "SharedResource\\Component\\",
                                "SharedResource\\js\\",
                                "SharedResource\\pages\\",
                                "SharedResource\\style\\",
                            };

                    if (Directory.Exists(templateFolderPath) && Directory.Exists(defaultTemplateFolderPath))
                    {
                        var esiteExDto = RetrieveOneAppEsiteExDto(esiteId);

                        if (esiteExDto.EsiteAttribute.TemplateCode != templateCode)
                        {
                            string appSitePath = string.Format(@"{0}FileRepository\Company_{1}\WebSite\Site_1\", baseDirectory, esiteExDto.AppCreatedByCompanyId.Value);
                            string siteBackupPath = appSitePath + "Backup\\";


                            // Backup Org Site Files
                            SetEsiteTemplate_BackupFiles(needToBackupPathList, appSitePath, siteBackupPath);

                            bool isNeedToRollBack = false;

                            try
                            {
                                // install target template files
                                SetEsiteTemplate_InstallTargetTemplateFiles(needToBackupPathList, appSitePath, templateFolderPath, defaultTemplateFolderPath);

                                OperationCallResult<bool> savesiteResult = SetEsiteTemplate_saveEsite(templateCode, templateConfigFilePath, defaultTemplateConfigFilePath, esiteExDto);

                                if (savesiteResult.IsSuccessful)
                                {
                                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_SetEsiteLayoutTemplate_success", ValidationItemType.Message, "Change template successful."));
                                    operationCallResult.Object = true;

                                    //SetEsiteTemplate_DeleteBackupFiles(needToBackupPathList, appSitePath, siteBackupPath);
                                }
                                else
                                {
                                    validationResult.Merge(savesiteResult.ValidationResult);
                                    isNeedToRollBack = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                validationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_SetEsiteLayoutTemplate_Error", ValidationItemType.Error, "Change template failed. \n" + ex.ToString()));

                                isNeedToRollBack = true;

                            }


                            if (isNeedToRollBack)
                            {
                                // Restore From Backup Files
                                SetEsiteTemplate_RestoreFilesFromBackup(needToBackupPathList, appSitePath, siteBackupPath);
                            }

                        }
                        else
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_SetEsiteLayoutTemplate_Error", ValidationItemType.Warning, "The template you're trying to switch to is already in use on your website."));
                        }
                    }
                    else
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_SetEsiteLayoutTemplate_Error", ValidationItemType.Error, "Cannot find template files."));
                    }
                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_SetEsiteLayoutTemplate_Error", ValidationItemType.Error, "Change template failed. \n" + ex.ToString()));
                }
            }



            return operationCallResult;
        }

        private static void SetEsiteTemplate_BackupFiles(List<string> needToBackupPathList, string appSitePath, string siteBackupPath)
        {
            if (Directory.Exists(siteBackupPath))
            {
                SetEsiteTemplate_DeleteBackupFiles(needToBackupPathList, appSitePath, siteBackupPath);
            }

            Directory.CreateDirectory(siteBackupPath);

            foreach (string subPath in needToBackupPathList)
            {
                Directory.CreateDirectory(siteBackupPath + subPath);
                AppEsiteFileBL.CopyFolderToNewLocation(appSitePath + subPath, siteBackupPath + subPath);
            }
        }

        private static void SetEsiteTemplate_InstallTargetTemplateFiles(List<string> needToBackupPathList, string appSitePath, string templateFolderPath, string defaultTemplateFolderPath)
        {
            AppEsiteFileBL.CopyFolderToNewLocation(defaultTemplateFolderPath + "\\TemplateFiles\\", appSitePath);
            AppEsiteFileBL.CopyFolderToNewLocation(templateFolderPath + "\\TemplateFiles\\", appSitePath);
        }

        private static void SetEsiteTemplate_RestoreFilesFromBackup(List<string> needToBackupPathList, string appSitePath, string siteBackupPath)
        {
            foreach (string subPath in needToBackupPathList)
            {
                AppEsiteFileBL.CopyFolderToNewLocation(siteBackupPath + subPath, appSitePath + subPath);
            }
        }

        private static void SetEsiteTemplate_DeleteBackupFiles(List<string> needToBackupPathList, string appSitePath, string siteBackupPath)
        {
            if (Directory.Exists(siteBackupPath))
            {
                Directory.Move(siteBackupPath, appSitePath + "ToDelete\\");
                AppEsiteFileBL.DeleteOneFileFolderByPath(appSitePath + "ToDelete\\");
            }
        }

        private static OperationCallResult<bool> SetEsiteTemplate_saveEsite(string templateCode, string templateConfigFilePath, string defaultTemplateConfigFilePath, AppEsiteExDto esiteExDto)
        {
            esiteExDto.EsiteAttribute.TemplateCode = templateCode;

            string templateConfigText = File.ReadAllText(templateConfigFilePath);

            if (!string.IsNullOrWhiteSpace(templateConfigText))
            {
                esiteExDto.EsiteAttribute.GlobalSiteThemeParameterList = JsonConvert.DeserializeObject<AppEsiteTemplateConfigDto>(templateConfigText).GlobalSiteThemeParameterList;
            }
            else
            {
                esiteExDto.EsiteAttribute.GlobalSiteThemeParameterList = JsonConvert.DeserializeObject<AppEsiteTemplateConfigDto>(defaultTemplateConfigFilePath).GlobalSiteThemeParameterList;
            }

            var savesiteResult = AppEsiteFileBL.SaveGlobalWebsiteThemParameters(esiteExDto);
            return savesiteResult;
        }

        private static ValidationResult ProcessDirtyAppEsiteCatalogueExDto(AppEsiteCatalogueExDto aAppEsiteCatalogueExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppEsiteCatalogueEntity aAppEsiteCatalogueEntity = RetrieveOneAppEsiteCatalogueEntity(aAppEsiteCatalogueExDto.Id);
            AppEsiteCatalogueConverter.CopyDtoToEntity(aAppEsiteCatalogueEntity, aAppEsiteCatalogueExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppEsiteCatalogueEntity);




                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_EstoreEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsiteCatalogueEntity), "App_EstoreEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }







        public static AppEsitePagesEntity RetrieveOneAppEsitePagesEntity(object pageId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsitePagesEntity estoreEntity = new AppEsitePagesEntity(int.Parse(pageId.ToString()));

                //IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppEsitePagesEntity);


                adpater.FetchEntity(estoreEntity);
                return estoreEntity;
            }
        }


        private static ValidationResult ProcessDirtyAppEsitePagesExDto(AppEsitePagesExDto aAppEsitePagesExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();


            AppEsitePagesEntity aAppEsitePagesEntity = RetrieveOneAppEsitePagesEntity(aAppEsitePagesExDto.Id);
            AppEsitePagesConverter.CopyDtoToEntity(aAppEsitePagesEntity, aAppEsitePagesExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppEsitePagesEntity);




                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_EstoreEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_EstoreEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static void PrepreSitePageTypeDisplay(AppEsitePagesExDto appEsitePagesExDto)
        {
            if (appEsitePagesExDto.EmresourceContentType.HasValue)
            {
                int pageType = appEsitePagesExDto.EmresourceContentType.Value;

                if (pageType == (int)EmAppWebsitePageType.HTMLPage)
                {
                    if (appEsitePagesExDto.FileFullPath.Contains("StaticPages"))
                    {
                        appEsitePagesExDto.IsStaticSitePage = true;
                    }

                    if (appEsitePagesExDto.ControllerName == Controller_formMasterDetailCtrl)
                    {
                        appEsitePagesExDto.IsDataModelPage = true;
                        appEsitePagesExDto.PageTypeDisplay = "Data Model Page";


                    }
                    else if (appEsitePagesExDto.ControllerName == Controller_searchCtrl)
                    {
                        appEsitePagesExDto.IsSearchViewPage = true;
                        appEsitePagesExDto.PageTypeDisplay = "Search View Page";


                    }
                    else
                    {
                        //if (appEsitePagesExDto.IsMasterLayoutPage.HasValue && appEsitePagesExDto.IsMasterLayoutPage.Value)
                        //{
                        //    appEsitePagesExDto.PageTypeDisplay = "Site Master Layout Page";
                        //}
                        //else
                        //{
                        //    appEsitePagesExDto.PageTypeDisplay = "Regular Page";
                        //}
                        appEsitePagesExDto.PageTypeDisplay = "Regular Page";
                    }

                    //if (appEsitePagesExDto.PageAttribute == null)
                    //{
                    //    appEsitePagesExDto.PageAttribute = new AppEsitePageAttributeDto();
                    //}


                    //if (appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive == null)
                    //{
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive = new Dictionary<string, bool>();
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("base", true);
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("sm", true);
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("md", false);
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("lg", true);
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("xl", false);
                    //    appEsitePagesExDto.PageAttribute.DictScreenSizeCodeAndIsActive.Add("2xl", false);
                    //}

                }
                //else if (pageType == (int)EmAppWebsitePageType.NavigationCtrlJavascript)
                //{
                //    appEsitePagesExDto.PageTypeDisplay = "Site Navigation Control Javascript";
                //}
                else if (pageType == (int)EmAppWebsitePageType.SiteJavascript)
                {
                    appEsitePagesExDto.PageTypeDisplay = "Site Javascript";
                }
                else if (pageType == (int)EmAppWebsitePageType.SiteCSS)
                {
                    appEsitePagesExDto.PageTypeDisplay = "Site CSS";
                }
                else if (pageType == (int)EmAppWebsitePageType.Other)
                {
                    appEsitePagesExDto.PageTypeDisplay = "Other";
                }
            }
        }


        private static AppEsiteExDto GenerateWebSiteFromWizardSetting_InitialSave(AppEsiteExDto wizardObj, ValidationResult aValidationResult, AppEsiteExDto appEsiteExDto)
        {
            if (wizardObj.Id != null)
            {
                var orgEsiteExDto = RetrieveOneAppEsiteExDto(wizardObj.Id);
                orgEsiteExDto.Name = wizardObj.Name;
                orgEsiteExDto.Description = wizardObj.Description;
                orgEsiteExDto.AppEsitePagesList.DeletedItemIds = appEsiteExDto.AppEsitePagesList.Select(o => o.Id).ToList();
                orgEsiteExDto.AppEsiteCatalogueList.DeletedItemIds = appEsiteExDto.AppEsiteCatalogueList.Select(o => o.Id).ToList();
                orgEsiteExDto.IsModified = true;
                var saveResult = SaveAppEsiteExDto(orgEsiteExDto);

                if (saveResult.IsSuccessfulWithResult)
                {
                    appEsiteExDto = saveResult.Object;
                }
                else
                {
                    aValidationResult.Merge(saveResult.ValidationResult);
                }
            }
            else
            {
                var saveResult = SaveAppEsiteExDto(wizardObj);

                if (saveResult.IsSuccessfulWithResult)
                {
                    appEsiteExDto = saveResult.Object;
                }
                else
                {
                    aValidationResult.Merge(saveResult.ValidationResult);
                }
            }

            return appEsiteExDto;
        }

        private static void PrepareUserDefinedJsFunctionDtoList(AppEsiteExDto aAppEsiteDto)
        {
            if (aAppEsiteDto.EsiteAttribute == null)
            {
                aAppEsiteDto.EsiteAttribute = new EsiteAttributeDto();
            }

            if (aAppEsiteDto.EsiteAttribute.UserDefinedJsFunctionList == null)
            {
                aAppEsiteDto.EsiteAttribute.UserDefinedJsFunctionList = new List<AppEsiteUserDefinedJsFunctionDto>();
            }
        }

        private static void PrepareGlobalSiteThemeParameterList(AppEsiteExDto aAppEsiteDto)
        {
            if (aAppEsiteDto.EsiteAttribute == null)
            {
                aAppEsiteDto.EsiteAttribute = new EsiteAttributeDto();
            }

            if (aAppEsiteDto.EsiteAttribute.GlobalSiteThemeParameterList == null)
            {
                aAppEsiteDto.EsiteAttribute.GlobalSiteThemeParameterList = new List<AppEsiteThemParameterDto>();
            }


            List<AppEsiteThemParameterDto> builtInParamList = new List<AppEsiteThemParameterDto>();
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_DefaultBgColor", ParameterType = "BgColor", ParameterCategory = "Site Default", ParameterValue = "", Description = "Background Color", Sort = 1 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_DefaultFontFamily", ParameterType = "FontFamily", ParameterCategory = "Site Default", ParameterValue = "", Description = "Font Family", Sort = 2 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_DefaultFontColor", ParameterType = "FontColor", ParameterCategory = "Site Default", ParameterValue = "", Description = "Font Color", Sort = 3 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_DefaultFontSize", ParameterType = "FontSize", ParameterCategory = "Site Default", ParameterValue = "", Description = "Font Size", Sort = 4 });

            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteHeaderBgColor", ParameterType = "BgColor", ParameterCategory = "Site Header", ParameterValue = "", Description = "Background Color", Sort = 5 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteHeaderFontFamily", ParameterType = "FontFamily", ParameterCategory = "Site Header", ParameterValue = "", Description = "Font Family", Sort = 6 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteHeaderFontColor", ParameterType = "FontColor", ParameterCategory = "Site Header", ParameterValue = "", Description = "Font Color", Sort = 7 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteHeaderFontSize", ParameterType = "FontSize", ParameterCategory = "Site Header", ParameterValue = "", Description = "Font Size", Sort = 8 });

            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteFooterBgColor", ParameterType = "BgColor", ParameterCategory = "Site Footer", ParameterValue = "", Description = "Background Color", Sort = 9 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteFooterFontFamily", ParameterType = "FontFamily", ParameterCategory = "Site Footer", ParameterValue = "", Description = "Font Family", Sort = 10 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteFooterFontColor", ParameterType = "FontColor", ParameterCategory = "Site Footer", ParameterValue = "", Description = "Font Color", Sort = 11 });
            builtInParamList.Add(new AppEsiteThemParameterDto() { ParameterName = "GlobalSite_SiteFooterFontSize", ParameterType = "FontSize", ParameterCategory = "Site Footer", ParameterValue = "", Description = "Font Size", Sort = 12 });

            var paramList = aAppEsiteDto.EsiteAttribute.GlobalSiteThemeParameterList;
            Dictionary<string, AppEsiteThemParameterDto> dictParamNameAndDto = paramList.ToDictionary(o => o.ParameterName, o => o);

            List<AppEsiteThemParameterDto> newParamList = new List<AppEsiteThemParameterDto>();

            foreach (var builtInParam in builtInParamList)
            {
                if (dictParamNameAndDto.ContainsKey(builtInParam.ParameterName))
                {
                    newParamList.Add(dictParamNameAndDto[builtInParam.ParameterName]);
                }
                else
                {
                    newParamList.Add(builtInParam);
                }
            }

            aAppEsiteDto.EsiteAttribute.GlobalSiteThemeParameterList = newParamList;


        }


        private static void PrepareEsiteThirdPartControlThemeNameList(AppEsiteExDto aAppEsiteDto)
        {
            aAppEsiteDto.ThirdPartControlThemeNameList = new List<string>();

            string siteBasePath = aAppEsiteDto.RootFolderPath;
            string wjFolderPath = siteBasePath + "\\SharedResource\\style\\PartialScss\\ThirdPart\\Wijmo";
            string dpFolderPath = siteBasePath + "\\SharedResource\\style\\PartialScss\\ThirdPart\\DayPilot";

            if (Directory.Exists(wjFolderPath) && Directory.Exists(dpFolderPath))
            {
                List<string> wjfiles = System.IO.Directory.GetFiles(wjFolderPath).ToList();
                List<string> dpfiles = System.IO.Directory.GetFiles(dpFolderPath).ToList();

                List<string> allFileList = wjfiles.Concat(dpfiles).ToList();

                foreach (string s in allFileList)
                {
                    string extension = Path.GetExtension(s);
                    string fileName = Path.GetFileNameWithoutExtension(s);

                    if (extension.ToLower() == ".scss" && fileName.StartsWith("_") && !fileName.EndsWith("_Theme_Default"))
                    {
                        string themeName = fileName.Substring(1);
                        aAppEsiteDto.ThirdPartControlThemeNameList.Add(themeName);
                    }

                }
            }
        }


        private static void PrepareEsiteComponentConfig(AppEsiteExDto aAppEsiteDto)
        {

            string siteBasePath = aAppEsiteDto.RootFolderPath;
            string configFilePath = siteBasePath + "\\SharedResource\\Component\\ComponentConfig.txt";

            try
            {
                string fileContent = File.ReadAllText(configFilePath);

                aAppEsiteDto.ComponentConfigText = fileContent;
            }
            catch (Exception ex)
            {

            }

        }

        private static void PrepareEsitePartnerMapping(AppEsiteExDto aAppEsiteDto)
        {
            aAppEsiteDto.CustomerInfoDataModelId = null;
            aAppEsiteDto.CustomerInfoDbtableName = null;
            aAppEsiteDto.CustomerInfoCustomerIdDbfieldName = null;
            aAppEsiteDto.CustomerInfoEmailDbfieldName = null;
            aAppEsiteDto.CustomerInfoDataTransferId = null;

            aAppEsiteDto.SupplierInfoDataModelId = null;
            aAppEsiteDto.SupplierInfoDbtableName = null;
            aAppEsiteDto.SupplierInfoIdDbfieldName = null;
            aAppEsiteDto.SupplierInfoEmailDbfieldName = null;
            aAppEsiteDto.SupplierInfoDataTransferId = null;

            aAppEsiteDto.IsAllowCustomerRegister = AppTenantSettingBL.GetBoolValue(EmTenantSettings.IsCustomerUserToPartnerOneToOneMapping);
            aAppEsiteDto.IsAllowSupplierRegister = AppTenantSettingBL.GetBoolValue(EmTenantSettings.IsSupplierUserToPartnerOneToOneMapping);

            if (aAppEsiteDto.IsAllowCustomerRegister)
            {
                aAppEsiteDto.CustomerInfoDataModelId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerPartnerTransaction);
                aAppEsiteDto.CustomerInfoDbtableName = AppTenantSettingBL.GetStringValue(EmTenantSettings.CustomerPartnerDbtableName);
                aAppEsiteDto.CustomerInfoCustomerIdDbfieldName = AppTenantSettingBL.GetStringValue(EmTenantSettings.CustomerPartnerIdDbfieldName);
                aAppEsiteDto.CustomerInfoEmailDbfieldName = AppTenantSettingBL.GetStringValue(EmTenantSettings.CustomerPartnerEmailDbfieldName);
                aAppEsiteDto.CustomerInfoDataTransferId = AppTenantSettingBL.GetIntValue(EmTenantSettings.CustomerPartnerDataTransferId);
            }

            if (aAppEsiteDto.IsAllowSupplierRegister)
            {
                aAppEsiteDto.SupplierInfoDataModelId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SupplierPartnerTransaction);
                aAppEsiteDto.SupplierInfoDbtableName = AppTenantSettingBL.GetStringValue(EmTenantSettings.SupplierPartnerDbtableName);
                aAppEsiteDto.SupplierInfoIdDbfieldName = AppTenantSettingBL.GetStringValue(EmTenantSettings.SupplierPartnerIdDbfieldName);
                aAppEsiteDto.SupplierInfoEmailDbfieldName = AppTenantSettingBL.GetStringValue(EmTenantSettings.SupplierPartnerEmailDbfieldName);
                aAppEsiteDto.SupplierInfoDataTransferId = AppTenantSettingBL.GetStringValue(EmTenantSettings.SupplierPartnerDataTransferId);
            }
        }

        private static void PrepareNextJsEsiteComponentNameList(AppEsiteExDto aAppEsiteDto)
        {
            aAppEsiteDto.ComponentTagList = new List<string>();

            string siteBasePath = aAppEsiteDto.RootFolderPath;
            string componentFolderPath = Path.Combine(siteBasePath, "src", "components");

            if (!Directory.Exists(componentFolderPath))
                return;  // nothing to do

            // grab all .tsx files (recursively).  Change to "*.jsx" or add more patterns if needed.
            var componentFiles = Directory
                .EnumerateFiles(componentFolderPath, "*.tsx", SearchOption.AllDirectories);

            // extract the file name without extension, de-duplicate, sort
            var componentNames = componentFiles
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            // assign to your DTO
            aAppEsiteDto.ComponentTagList.AddRange(componentNames);
        }




        #region--------- web site running time content
        public static string RetrieveRunningTimePageViewHTmlContent(object pageId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppEsitePagesEntity pageEntity = new AppEsitePagesEntity(int.Parse(pageId.ToString()));

                IncludeFieldsList filedINclude = new IncludeFieldsList();
                filedINclude.Add(AppEsitePagesFields.HtmlContent);
                filedINclude.Add(AppEsitePagesFields.PageCssStyle);
                adpater.FetchEntity(pageEntity, null, null, filedINclude);
                SetPageCssStyle(pageEntity);

                return pageEntity.HtmlContent;
            }
        }

        private static void SetPageCssStyle(AppEsitePagesEntity pageEntity)
        {
            if (!string.IsNullOrWhiteSpace(pageEntity.PageCssStyle))
            {
                string styleStartTag = "<style>";
                string styleEndTag = "</style>";
                pageEntity.HtmlContent =
                    pageEntity.HtmlContent + System.Environment.NewLine
                    + styleStartTag
                    + System.Environment.NewLine
                    + pageEntity.PageCssStyle
                    + System.Environment.NewLine
                    + styleEndTag;
            }
        }

        public static AppEsiteExDto RetrieveRunningTimeOneAppEsiteExDto(object AppEsiteId)
        {
            AppEsiteEntity aAppEsiteEntity = RetrieveRunningTimeOneAppEsiteEntity(AppEsiteId);
            AppEsiteExDto aAppEsiteDto = AppEsiteConverter.ConvertEntityToExDto(aAppEsiteEntity);
            var masterPage = aAppEsiteEntity.AppEsitePages.FirstOrDefault();
            if (masterPage != null)
            {

                SetPageCssStyle(masterPage);

                aAppEsiteDto.MasteSiteHostLayoutHtmlContent = masterPage.HtmlContent;
            }



            return aAppEsiteDto;
        }

        public static string RetrieveRunningTimeSiteJavascript(object eSiteId)
        {

            string outputContent = "";
            // AppEsiteExDto aAppEsiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(eSiteId);

            EntityCollection<AppEsitePagesEntity> siteJavascriptList = new EntityCollection<AppEsitePagesEntity>();

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filterBucket = new RelationPredicateBucket();
                IPredicateExpression predicate = new PredicateExpression();
                predicate.Add(AppEsitePagesFields.EsiteId == eSiteId);
                predicate.AddWithAnd(AppEsitePagesFields.EmresourceContentType == (int)EmAppWebsitePageType.SiteJavascript);

                filterBucket.PredicateExpression.Add(predicate);


                IncludeFieldsList filedINclude = new IncludeFieldsList();
                filedINclude.Add(AppEsitePagesFields.PageJsMethod);
                adpater.FetchEntityCollection(siteJavascriptList, filedINclude, filterBucket);

            }
            if (siteJavascriptList.Count > 0)
            {
                outputContent = siteJavascriptList
              .OrderBy(o => o.LoadOrder)
              .Select(o => o.PageJsMethod).Aggregate((i, j) => i + System.Environment.NewLine + j);
            }




            return outputContent;
        }
        //

        public static string RetrieveRunningTimeSiteCSS(object eSiteId)
        {

            string outputContent = "";
            // AppEsiteExDto aAppEsiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(eSiteId);

            EntityCollection<AppEsitePagesEntity> siteCssList = new EntityCollection<AppEsitePagesEntity>();

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filterBucket = new RelationPredicateBucket();
                IPredicateExpression predicate = new PredicateExpression();
                predicate.Add(AppEsitePagesFields.EsiteId == eSiteId);
                predicate.AddWithAnd(AppEsitePagesFields.EmresourceContentType == (int)EmAppWebsitePageType.SiteCSS);
                filterBucket.PredicateExpression.Add(predicate);


                IncludeFieldsList filedINclude = new IncludeFieldsList();
                filedINclude.Add(AppEsitePagesFields.PageCssStyle);

                adpater.FetchEntityCollection(siteCssList, filedINclude, filterBucket);

            }
            if (siteCssList.Count > 0)
            {

                outputContent = siteCssList
              .OrderBy(o => o.LoadOrder)
              .Select(o => o.PageCssStyle).Aggregate((i, j) => i + System.Environment.NewLine + j);
            }




            return outputContent;
        }



        public static string RetrieveRunningHtmlPageNavigationCtrlMethodJavascript(object eSiteId)
        {

            string outputContent = "";
            // AppEsiteExDto aAppEsiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(eSiteId);

            EntityCollection<AppEsitePagesEntity> pageNavigationControlMethodList = new EntityCollection<AppEsitePagesEntity>();

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filterBucket = new RelationPredicateBucket();
                IPredicateExpression predicate = new PredicateExpression();
                predicate.Add(AppEsitePagesFields.EsiteId == eSiteId);
                predicate.AddWithAnd(AppEsitePagesFields.EmresourceContentType == (int)EmAppWebsitePageType.HTMLPage);
                filterBucket.PredicateExpression.Add(predicate);


                IncludeFieldsList filedINclude = new IncludeFieldsList();
                filedINclude.Add(AppEsitePagesFields.PageJsMethod);

                adpater.FetchEntityCollection(pageNavigationControlMethodList, filedINclude, filterBucket);

            }
            if (pageNavigationControlMethodList.Count > 0)
            {
                outputContent = pageNavigationControlMethodList
              .OrderBy(o => o.LoadOrder)
              .Select(o => o.PageJsMethod).Aggregate((i, j) => i + System.Environment.NewLine + j);
            }




            return outputContent;
        }


        public static List<AppEsitePagesEntity> RetrieveRunningRouteStateJavascript(object eSiteId)
        {



            EntityCollection<AppEsitePagesEntity> pageNavigationControlMethodList = new EntityCollection<AppEsitePagesEntity>();

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                RelationPredicateBucket filterBucket = new RelationPredicateBucket();
                IPredicateExpression predicate = new PredicateExpression();
                predicate.Add(AppEsitePagesFields.EsiteId == eSiteId);
                predicate.AddWithAnd(AppEsitePagesFields.EmresourceContentType == (int)EmAppWebsitePageType.HTMLPage);
                filterBucket.PredicateExpression.Add(predicate);


                //IncludeFieldsList filedINclude = new IncludeFieldsList();

                //filedINclude.Add(AppEsitePagesFields.MetaDesciption);
                //filedINclude.Add(AppEsitePagesFields.UrlAndHandle);
                //filedINclude.Add(AppEsitePagesFields.ControllerName);


                adpater.FetchEntityCollection(pageNavigationControlMethodList, null, filterBucket);

            }

            List<AppEsitePagesEntity> toReturn = pageNavigationControlMethodList.Where(o => !o.Title.StartsWith("_")).ToList();



            return toReturn;
        }




        #endregion

    }
}