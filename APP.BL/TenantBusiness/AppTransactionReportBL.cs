using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.IO;


using APP.Framework;
namespace App.BL
{
    public static class AppTransactionReportBLBL
    {
        //http://localhost/PlmApp/ReportPdf.aspx?ReportId=5&RepParaTransactionRid=48

        const string RepParaTransactionRidParaName = "RepParaTransactionRid";


        public static List<int> GenerateReportPdfFile(object transactionId, object transactionRidValue)
        {



            var transcationDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            int? forderId = transcationDto.TransactionFileStorageRootFolderId;

            List<int> returnFileIds = new List<int>();

            EntityCollection<AppTranscationReportEntity> tranreorList = GetOneTranscationAllReportEntity(transactionId);

            Dictionary<string, object> dictQueryReportParaAndValue = new Dictionary<string, object>();
            dictQueryReportParaAndValue[RepParaTransactionRidParaName] = transactionRidValue;


            foreach (var transcationReportEntity in tranreorList)
            {
                var reportEntity = transcationReportEntity.AppReport;

                //PageReport pageReport = new PageReport();

                //// IIS Hosts System.Environment.CurrentDirectory : c:\windwos\system32\inersrv
                //// string exePath =    System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location);
                //// need to define report reposotory in appsetup
                //FileInfo fi = new FileInfo(string.Format(@"{0}{1}", AppCompanyBL.GetMyCompanyReportPath(), reportEntity.ReportFileName));
                //pageReport.Load(fi);

                //AssingReportParaWithQueryString(pageReport, dictQueryReportParaAndValue);
                //ReAssignConnectionString(pageReport);
                //pageReport.Run();
                //MemoryStream memStream = ConvertPageReportToPdfStream(pageReport);

               // byte[] imageContent = memStream.ToArray();

                // string originalpath =  AppCompanyBL.GetMyCompanyImagePath()+ DocumentInfoDto.ImageOriginalSizeLocation + Guid.NewGuid().ToString();
                //
                //File.WriteAllBytes(originalpath, imageContent);




                //AppFileExDto newFileExDto = new AppFileExDto();
                //newFileExDto.FolderId = forderId;

                //newFileExDto.FileType = (int)EmAppDocumentType.PDF;
                //// newFileExDto.OriginalFilePath = originalpath;
                //newFileExDto.FileCode = string.Format("Reprot{0}_{1}.pdf", transactionId, transactionRidValue);
                //newFileExDto.Extension = ".pdf";
                //newFileExDto.FileContent = imageContent;
                //var aOperationCallResult = AppFileBL.SaveOneAppFileEntityDto(newFileExDto);

                //returnFileIds.Add((int)aOperationCallResult.Object.Id);

                return new List<int>(); // for test only, need to return real file id after save file to DBMS
            }


            // Write the PDF stream to the output stream.


            return returnFileIds;
        }

        //public static void ReAssignConnectionString(PageReport pageRpt)
        //{


        //    DataAccessAdapter aDataAccessAdapter = AppTenantAdapterBL.GetTenantAdapter();



        //    // change conenction string on the fly ...
        //    foreach (var ds in pageRpt.Report.DataSources)
        //    {
        //        ds.DataSourceReference = null;

        //        //ds.ConnectionProperties.ConnectString = @"Data Source=LAB-PLMSBACKUP\SQL2012;Initial Catalog=NewLook_Plms_Print;User ID=sa;Password=sa";
        //        ds.ConnectionProperties.ConnectString = aDataAccessAdapter.ConnectionString;
        //        ds.ConnectionProperties.DataProvider = "SQL";
        //    }


        //    //foreach (DataSet ds in report.DataSets)
        //    //{
        //    //	Query query = ds.Query;
        //    //	SharedDataSet shareDataset = ds.SharedDataSet;

        //    //}
        //}


        //public static MemoryStream ConvertPageReportToPdfStream(PageReport pageReport)
        //{
        //    PdfExport pdf = new PdfExport();

        //    // Create a new memory stream that will hold the pdf output
        //    System.IO.MemoryStream memStream = new System.IO.MemoryStream();
        //    // Export the report to PDF.
        //    pdf.Export(pageReport.Document, memStream);
        //    return memStream;
        //}

        //public static void AssingReportParaWithQueryString(PageReport pageReport, Dictionary<string,object> dictQueryReportParaAndValue) 
        //{
        //    var reportParamterList = pageReport.Document.Parameters;



        //    foreach (string queryPara in dictQueryReportParaAndValue.Keys)
        //    {

        //            var aparametr = reportParamterList[queryPara];
        //            if (aparametr != null)
        //            {
        //                aparametr.CurrentValue = dictQueryReportParaAndValue[queryPara];
        //            }


        //    }
        //}

        public static ObservableSet<AppTranscationReportExDto> RetrieveAllAppTranscationReportListByTransactionId(object transactionId)
        {
            EntityCollection<AppTranscationReportEntity> entityList = GetOneTranscationAllReportEntity(transactionId);

            var aDtoList = new ObservableSet<AppTranscationReportExDto>();
            foreach (var AppTranscationReportEntity in entityList)
            {
                AppTranscationReportExDto aAppTranscationReportExDto = AppTranscationReportConverter.ConvertEntityToExDto(AppTranscationReportEntity);


                aDtoList.Add(aAppTranscationReportExDto);

            }
            return aDtoList;
        }

        private static EntityCollection<AppTranscationReportEntity> GetOneTranscationAllReportEntity(object transactionId)
        {
            EntityCollection<AppTranscationReportEntity> entityList = new EntityCollection<AppTranscationReportEntity>();
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppTranscationReportEntity);
                rootPath.Add(AppTranscationReportEntity.PrefetchPathAppReport);



                IRelationPredicateBucket filter = new RelationPredicateBucket(AppTranscationReportFields.TranscationId == transactionId);


                adapter.FetchEntityCollection(entityList, filter, rootPath);



            }

            return entityList;
        }

        public static AppTranscationReportEntity RetrieveOneAppTranscationReportEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTranscationReportEntity aAppTranscationReportEntity = new AppTranscationReportEntity(int.Parse(Id.ToString()));
                adapter.FetchEntity(aAppTranscationReportEntity);
                return aAppTranscationReportEntity;
            }
        }

        public static OperationCallResult<AppTranscationReportExDto> SaveAllAppTranscationReportExDto(ObservableSet<AppTranscationReportExDto> aSet, int transactionId)
        {
            OperationCallResult<AppTranscationReportExDto> aOperationCallResult = new OperationCallResult<AppTranscationReportExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));
            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteDto(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppTranscationReportListByTransactionId(transactionId);
            }

            return aOperationCallResult;

        }


        private static ValidationResult ProcessNewDto(AppTranscationReportExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTranscationReportEntity aAppTranscationReportEntity = new AppTranscationReportEntity();
            AppTranscationReportConverter.CopyDtoToEntity(aAppTranscationReportEntity, aDto);



            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTranscationReportEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTranscationReportEntity), "plm_AppTranscationReportEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Entity Logical Validation Exception
                catch (ORMEntityValidationException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTranscationReportEntity), "plm_AppTranscationReportEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTranscationReportEntity), "plm_AppTranscationReportEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppTranscationReportExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppTranscationReportEntity aAppTranscationReportEntity = RetrieveOneAppTranscationReportEntity(aDto.Id);

            AppTranscationReportConverter.CopyDtoToEntity(aAppTranscationReportEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppTranscationReportEntity, false, true);
                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTranscationReportEntity), "plm_AppTranscationReportEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTranscationReportEntity), "plm_AppTranscationReportEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDeleteDto(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppTranscationReportEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppTranscationReportEntity), new RelationPredicateBucket(AppTranscationReportFields.TransctionReportId == Id));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTranscationReportEntity), "plm_AppTranscationReportEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }





    }


}