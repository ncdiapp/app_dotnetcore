using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using ExchangeBL.Extension;
using ExchangeBL.Models;
//using GrapeCity.Enterprise.Data.DataEngine.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFRAMEWORK
using System.Windows.Interop;
#endif

using APP.Framework;
namespace ExchangeBL
{
    public class DataExchangeSettingBL
    {
        public static AppIntergrationSettingParameterExDto GetSetting(int actionId)
        {
            return GetSetting(actionId, null);
        }

        public static AppIntergrationSettingParameterExDto GetSetting(string actionCode)
        {
            return GetSetting(null, actionCode);
        }

        public static int SaveSetting(AppIntergrationSettingParameterExDto dataExchangeSettingDTO)
        {
            var result = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dataExchangeSettingDTO);

            if (result.IsSuccessfulWithResult)
            {
                return (int)result.Object.Id;
            }
            else
            {
                return 0;
            }
        }



        public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateSampleJsonDataFromApiConfig(AppIntergrationSettingParameterExDto dto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            AppFileDto payloadFile = dto.PayloadFile;

            var saveresult = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
            if (saveresult.IsSuccessfulWithResult)
            {
                dto = saveresult.Object;
                List<KeyValuePair<string, string>> responseHeaderKeyAndValueList = new List<KeyValuePair<string, string>>();

                string requestMessage = "";

                try
                {

                    if (dto.HttpMethd == "Post" || dto.HttpMethd == "Put")
                    {
                        List<string> responses = null;

                        EmAppApiPayloadDataType payloadDataType = EmAppApiPayloadDataType.JSON;

                        bool isMultipartFormDataContent = dto.OtherSettingsDto != null && dto.OtherSettingsDto.IsMultipartFormDataContent;

                        if (dto.OtherSettingsDto != null && dto.OtherSettingsDto.PayloadDataType.HasValue)
                        {
                            payloadDataType = dto.OtherSettingsDto.PayloadDataType.Value;
                        }



                        if (payloadDataType == EmAppApiPayloadDataType.JSON)
                        {
                            responses = Helper.CallAPIAsync(dto.APIConfigParameters, dto.JsonSampleData, dto.IntergrationSettingId, responseHeaderKeyAndValueList).Result;

                        }
                        else if (payloadDataType == EmAppApiPayloadDataType.FileByteArray)
                        {

                            if (payloadFile != null)
                            {
                                responses = Helper.CallAPIAsync(dto.APIConfigParameters, payloadFile, dto.IntergrationSettingId, responseHeaderKeyAndValueList, EmAppApiPayloadDataType.FileByteArray, isMultipartFormDataContent).Result;
                            }
                            else
                            {
                                int? fileId = ControlTypeValueConverter.ConvertValueToInt(dto.JsonSampleData);

                                if (fileId.HasValue)
                                {
                                    //byte[] fileByteArray = TestGetFileByteArrayFromFileId(fileId.Value);

                                    byte[] fileByteArray = null;
                                    string fileName = "";

                                    EmailHelper.GetAppFileContentFromId(fileId.Value, ref fileByteArray, ref fileName, false);

                                    payloadFile = new AppFileDto() { FileCode = fileName, FileContent = fileByteArray };

                                    responses = Helper.CallAPIAsync(dto.APIConfigParameters, payloadFile, dto.IntergrationSettingId, responseHeaderKeyAndValueList, EmAppApiPayloadDataType.FileByteArray, isMultipartFormDataContent).Result;

                                }
                                else {
                                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "FileId is empty."));
                                }
                            }

                        }
                        else if (payloadDataType == EmAppApiPayloadDataType.ServerFilePath)
                        {
                            AppFilePathDto appFilePathDto = null;
                            
                            try
                            {
                                appFilePathDto = Newtonsoft.Json.JsonConvert.DeserializeObject<AppFilePathDto>(dto.JsonSampleData);
                            }
                            catch (Exception ex)
                            {
                               
                            }

                            if (appFilePathDto == null)
                            {
                                appFilePathDto = new AppFilePathDto();
                                appFilePathDto.FilePath = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dto.JsonSampleData);
                            }
                            

                            responses = Helper.CallAPIAsync(dto.APIConfigParameters, appFilePathDto, dto.IntergrationSettingId, responseHeaderKeyAndValueList, EmAppApiPayloadDataType.ServerFilePath, isMultipartFormDataContent).Result;
                        }

                        if (responses != null && responses.Count > 0)
                        {
                            if (dto.PostResponseDto == null)
                            {
                                dto.PostResponseDto = new APIPostResponseDto();
                            }

                            dto.PostResponseDto.ResponseJsonData = responses.FirstOrDefault();
                            JsonSchema jsonSchema = JsonSchemaBL.GenerateSchemFromSample(dto.PostResponseDto.ResponseJsonData);
                            dto.PostResponseDto.ResponseJsonSchema = jsonSchema.ToJson();

                            validationResult.Merge(AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto).ValidationResult);


                        }
                        else
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed."));
                        }
                    }
                    else
                    {
                        List<string> responses = Helper.CallAPIAsync(dto.APIConfigParameters, null, dto.IntergrationSettingId, responseHeaderKeyAndValueList).Result;

                        if (responses != null && responses.Count > 0)
                        {
                            validationResult.Merge(DataExchangeSettingBL.UpdateJsonSample((int)dto.Id, responses.FirstOrDefault()));
                        }
                        else
                        {
                            validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed."));
                        }
                    }

                }
                catch (Exception ex)
                {
                    string msg = ex.GetInnerMessage();
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + msg));

                    if (dto.APIConfigParameters.RequestInfo != null)
                    {
                        toReturn.Object = new AppIntergrationSettingParameterExDto();
                        toReturn.Object.APIConfigParameters = new APIConfigParameterDTO();
                        toReturn.Object.APIConfigParameters.RequestInfo = dto.APIConfigParameters.RequestInfo;
                    }

                }

                if (!validationResult.HasErrors)
                {
                    toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);

                    toReturn.Object.ResponseHeaderKeyAndValueList = responseHeaderKeyAndValueList;

                    toReturn.Object.APIConfigParameters.RequestInfo = dto.APIConfigParameters.RequestInfo;
                }
            }
            else
            {
                return saveresult;
            }

            return toReturn;
        }




        //public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateAppInternalApiDataSchema(AppIntergrationSettingParameterExDto dto)
        //{
        //    OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
        //    ValidationResult validationResult = new ValidationResult();
        //    toReturn.ValidationResult = validationResult;

        //    AppFileDto payloadFile = dto.PayloadFile;

        //    var saveresult = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
        //    if (saveresult.IsSuccessfulWithResult)
        //    {
        //        dto = saveresult.Object;  

        //        try
        //        {
        //            if (dto.HttpMethd == "Post" || dto.HttpMethd == "Put")
        //            {
                        
        //            }
        //            else
        //            {
                        
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            string msg = ex.GetInnerMessage();
        //            validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + msg));
                                      
        //        }

        //        if (!validationResult.HasErrors)
        //        {
        //            toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);                   
        //        }
        //    }
        //    else
        //    {
        //        return saveresult;
        //    }

        //    return toReturn;
        //}



        //private static byte[] TestGetFileByteArrayFromFileId(int fileId)
        //{
        //    byte[] buffer = new byte[4096];

        //    var appFileExDto = AppFileBL.RetrieveOneLatestAppFileExDto(fileId);

        //    if (appFileExDto != null)
        //    {
        //        string fileCode = appFileExDto.FileCode;
        //        string fileFullPathName = "";

        //        if (!string.IsNullOrEmpty(appFileExDto.OriginalFilePath))
        //        {
        //            fileFullPathName = AppCompanyBL.GetMyCompanyImagePath() + appFileExDto.OriginalFilePath;
        //        }

        //        string extension;

        //        string iniFileIdString = appFileExDto.Id.ToString();

        //        if (appFileExDto.InitialFileId.HasValue)
        //        {
        //            iniFileIdString = appFileExDto.InitialFileId.Value.ToString();
        //        }



        //        string fileName = AppFileBL.fileIdPrefix + iniFileIdString + "_" + appFileExDto.FileCode;


        //        EmAppDocumentType docType = (EmAppDocumentType)appFileExDto.FileType;


        //        if (docType != EmAppDocumentType.Video)
        //        {


        //            if (docType == EmAppDocumentType.PDF
        //                    || docType == EmAppDocumentType.EXCEL
        //                    || docType == EmAppDocumentType.WORD
        //                    || docType == EmAppDocumentType.TXT
        //                    || docType == EmAppDocumentType.PPT
        //                )
        //            {
        //                buffer = appFileExDto.FileContent;

        //            }// // iamge keep to file system


        //            else  //  // iamge keep to file system
        //            {
        //                buffer = StreamHelper.FileToByteArray(fileFullPathName);
        //            }

        //            if (!buffer.IsEmpty())
        //            {
        //                extension = appFileExDto.Extension;
        //                string contentType = DocumentHelper.GetMimeContentType(extension);


        //            }



        //        }
        //        else // it is video, need to stream ...
        //        {

        //            using (var fileStreambuffer = new FileStream(fileFullPathName, FileMode.Open, FileAccess.Read))
        //            {
        //                buffer = new byte[4096];

        //                while (true)
        //                {
        //                    int bytesRead = fileStreambuffer.Read(buffer, 0, buffer.Length);
        //                    if (bytesRead == 0) break;

        //                }

        //            }




        //        }

        //    }

        //    return buffer;
        //}

        public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateDefaultSchemaAndDataSetMappingFromSampleJson(AppIntergrationSettingParameterExDto dto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            var saveresult1 = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
            if (saveresult1.IsSuccessfulWithResult)
            {
                dto = saveresult1.Object;

                try
                {
                    string defaultDataSetMapping = JsonSchemaBL.GenerateSchemaFromSampleAsync((int)dto.Id);
                    AppIntergrationSchemaDataSetMappingDto mappingDto = AppIntergrationSettingBL.ConvertOriginalSchemaDataSetMappingJsonStringToMappingDto(defaultDataSetMapping);

                    string mappindgDtoJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(mappingDto);

                    validationResult.Merge(DataExchangeSettingBL.UpdateSchemaMapping((int)dto.Id, mappindgDtoJsonString));
                }
                catch (Exception ex)
                {
                    string msg = ex.GetInnerMessage();
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + msg));
                }



                if (!validationResult.HasErrors)
                {
                    toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);
                }
            }
            else
            {
                return saveresult1;
            }

            return toReturn;
        }


        public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateRuntimeSchemaFromDataSetMapping(AppIntergrationSettingParameterExDto dto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;


            var saveresult1 = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
            if (saveresult1.IsSuccessfulWithResult)
            {
                dto = saveresult1.Object;

                try
                {
                    string mappingSchema = AppIntergrationSettingBL.GenerateSchemaDataSetMappingJsonStringFromMappingDto(dto.SchemaDataSetMappingDto);

                    List<string> needToRemoveDefinitionNames = dto.SchemaDataSetMappingDto.NodeSettingDtoList.Where(o => o.IsRemoved).Select(o => o.NodeName).ToList();

                    dto.SchemaFromDataSetMapping = JsonSchemaBL.GenerateRuntimeSchemaFromDataSetMapping(mappingSchema, needToRemoveDefinitionNames).GetAwaiter().GetResult();
                    //validationResult.Merge(DataExchangeSettingBL.UpdateSchemaFromMapping((int)dto.Id, dto.SchemaFromDataSetMapping));
                    validationResult.Merge(AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto).ValidationResult);
                }
                catch (Exception ex)
                {
                    string msg = ex.GetInnerMessage();
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + msg));
                }



                if (!validationResult.HasErrors)
                {
                    toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);
                }
            }
            else
            {
                return saveresult1;
            }

            return toReturn;
        }

        public static OperationCallResult<AppIntergrationSettingParameterExDto> CreateOrAlterDatabaseTablesFromRuntimeSchema(AppIntergrationSettingParameterExDto dto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;
                       

            var saveresult1 = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);

            if (saveresult1.IsSuccessfulWithResult)
            {
                dto = saveresult1.Object;               

                try
                {
                    string errorMsg  = JsonSchemaBL.CreateOrAlterDatabaseTableFromSchema((int)dto.Id);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + errorMsg));
                    }
                    else
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_Save_OK", ValidationItemType.Message, "Create Tables Successful."));
                    }                   

                    
                }
                catch (Exception ex)
                {
                    string msg = ex.GetInnerMessage();
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + msg));
                }

                if (!validationResult.HasErrors)
                {
                    toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);
                }
            }
            else
            {
                return saveresult1;
            }

            return toReturn;
        }

        public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateScriptsFromRuntimeSchema(AppIntergrationSettingParameterExDto dto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

            var saveresult1 = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
            if (saveresult1.IsSuccessfulWithResult)
            {
                dto = saveresult1.Object;

                try
                {
                    if (dto.HttpMethd == EnumHttpMethod.Get.ToString())
                    {
                        JsonSchemaBL.GenerateScriptFromSchema((int)dto.Id, EnumSqlScriptCRUDType.Insert);
                    }
                    else if (dto.HttpMethd == EnumHttpMethod.Post.ToString())
                    {
                        JsonSchemaBL.GenerateScriptFromSchema((int)dto.Id, EnumSqlScriptCRUDType.Select);
                        JsonSchemaBL.GenerateScriptFromSchema((int)dto.Id, EnumSqlScriptCRUDType.UpdateKey);
                    }
                }
                catch (Exception ex)
                {
                    string msg = ex.GetInnerMessage();
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, "Update failed. " + msg));
                }



                if (!validationResult.HasErrors)
                {
                    toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);
                }
            }
            else
            {
                return saveresult1;
            }

            return toReturn;
        }


        public static OperationCallResult<AppIntergrationSettingParameterExDto> GenerateTableAndScriptsFromSchemaDataSetMappingDto(AppIntergrationSettingParameterExDto dto)
        {
            OperationCallResult<AppIntergrationSettingParameterExDto> toReturn = new OperationCallResult<AppIntergrationSettingParameterExDto>();
            ValidationResult validationResult = new ValidationResult();
            toReturn.ValidationResult = validationResult;

          

            var generateRuntimeSchemaResult = GenerateRuntimeSchemaFromDataSetMapping(dto);

            TSqlGeneratorSettings tSqlSetting = new TSqlGeneratorSettings();

            if (generateRuntimeSchemaResult.IsSuccessfulWithResult)
            {
                dto = generateRuntimeSchemaResult.Object;
               
            }
            else
            {
                validationResult.Merge(generateRuntimeSchemaResult.ValidationResult);
            }

            if (!validationResult.HasErrors)
            {               
                var createTableResult = CreateOrAlterDatabaseTablesFromRuntimeSchema(dto);

                if (createTableResult.IsSuccessfulWithResult)
                {
                    dto = createTableResult.Object;
                }
                else
                {
                    validationResult.Merge(createTableResult.ValidationResult);
                }
            }

            if (!validationResult.HasErrors)
            {
                var generateScriptResult = GenerateScriptsFromRuntimeSchema(dto);

                if (generateScriptResult.IsSuccessfulWithResult)
                {
                    dto = generateScriptResult.Object;

                    dto.SchemaDataSetMappingDto.GeneratedTableNameList = new List<string>();

                    if (!string.IsNullOrWhiteSpace(dto.JsonQuery))
                    {

                        Dictionary<string, string> dictTypeNameAndQuery = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dto.JsonQuery);

                        Dictionary<string, string> dictTypeNameAndTableName = dto.SchemaDataSetMappingDto.NodeSettingDtoList.Where(o => !string.IsNullOrWhiteSpace(o.NodeName) && !string.IsNullOrWhiteSpace(o.MappingToTableName))
                            .ToDictionary(o => o.NodeName, o => o.MappingToTableName);


                        if (dictTypeNameAndQuery != null)
                        {
                            foreach (string typeName in dictTypeNameAndQuery.Keys)
                            {
                                if (typeName != "single_field" && !string.IsNullOrWhiteSpace(dictTypeNameAndQuery[typeName]))
                                {
                                    if (dictTypeNameAndTableName.ContainsKey(typeName))
                                    {
                                        string tableNameWithPrefix = dto.TablePrefix + tSqlSetting.MappingSplit + dictTypeNameAndTableName[typeName];

                                        dto.SchemaDataSetMappingDto.GeneratedTableNameList.Add(tableNameWithPrefix);
                                    }


                                }
                            }
                        }

                        dto.SchemaDataSetMappingDto.GeneratedTableNameList = dto.SchemaDataSetMappingDto.GeneratedTableNameList.Distinct().OrderBy(o => o).ToList();
                    }


                    var updateTableNameListresult = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);

                    if (updateTableNameListresult.IsSuccessfulWithResult)
                    {
                        dto = updateTableNameListresult.Object;

                        if (dto.DataSourceId.HasValue)
                        {
                            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dto.DataSourceId.Value);
                        }
                        else
                        {
                            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(ServerContext.Instance.CurrnetClientIdentity.DataSourceId);
                        }



                    }

                }
                else
                {
                    validationResult.Merge(generateScriptResult.ValidationResult);
                }
            }

            if (!validationResult.HasErrors)
            {
                string message = "";

                List<string> tableNames = dto.SchemaDataSetMappingDto.GeneratedTableNameList;

                if (tableNames.Count > 0)
                {
                    message = "Generated Table List: \n\n"
                        + string.Join(", \n  ", tableNames);
                }
                else
                {
                    message = "There is no table created. \n"
                        + "To include a schema node in the table creation list, choose a schema node on the left and set the Process Node Value Mode to Create New Table.";
                }


                validationResult.Items.Add(new ValidationItem(null, "App_GenerateScriptsFromRuntimeSchema_Success", ValidationItemType.Message, message));

                toReturn.Object = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(dto.Id);
            }

            return toReturn;
        }


        public static ValidationResult UpdateJsonQuery(int actionId, string jsonQuery)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();
            entity.JsonQuery = jsonQuery;
            return UpdateActionEntity(entity, actionId);
        }

        public static ValidationResult UpdateJsonSample(int actionId, string jsonSample)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();
            entity.JsonSampleData = jsonSample;
            return UpdateActionEntity(entity, actionId);

        }



        public static ValidationResult UpdateJsonSchema(int actionId, string jsonSchema)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();
            entity.JsonSchema = jsonSchema;
            return UpdateActionEntity(entity, actionId);
        }


        public static ValidationResult UpdateSchemaMapping(int actionId, string mappingSchema)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();
            entity.SchemaDataSetMapping = mappingSchema;
            return UpdateActionEntity(entity, actionId);
        }

        public static ValidationResult UpdateSchemaFromMapping(int actionId, string fromMappingSchema)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();
            entity.SchemaFromDataSetMapping = fromMappingSchema;
            return UpdateActionEntity(entity, actionId);
        }

        public static ValidationResult UpdatePostProcessScript(int actionId, string postProcessScript)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();
            entity.PostProcessScript = postProcessScript;
            return UpdateActionEntity(entity, actionId);
        }

        public static ValidationResult UpdateAPIConfigParameters(int actionId, APIConfigParameterDTO apiConfigParameterDTO)
        {
            AppIntergrationSettingParameterEntity entity = new AppIntergrationSettingParameterEntity();

            string scriptString = "";

            if (apiConfigParameterDTO != null)
            {
                scriptString = JsonConvert.SerializeObject(apiConfigParameterDTO);
            }

            entity.ApiconfigParameters = scriptString;
            return UpdateActionEntity(entity, actionId);
        }


      
        private static AppIntergrationSettingParameterExDto GetSetting(int? actionId, string actionCode)
        {
            AppIntergrationSettingParameterExDto dataExchangeSetting = null;

            if (actionId.HasValue)
            {
                dataExchangeSetting = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(actionId);
            }
            else if (!string.IsNullOrWhiteSpace(actionCode))
            {
                dataExchangeSetting = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDtoByActionCode(actionCode);
            }

            if (dataExchangeSetting == null)
            {
                throw new Exception($"There is no such action:{actionId} {actionCode}");
            }

            return dataExchangeSetting;
        }

        //private static void UpdateSetting(string sqlQuery, List<SqlParameter> sqlParameters)
        //{
        //    using (DataAccessAdapter adapter = new DataAccessAdapter(ServerContext.Instance.CurrentUserDbConnectionString))
        //    {
        //       // adapter.StartTransaction(IsolationLevel.ReadCommitted, "SchemaFromDataSetMapping");

        //        adapter.ExecuteExecuteNonQuery(sqlQuery, sqlParameters);

        //        //adapter.Commit();
        //    }
        //}

        private static ValidationResult UpdateActionEntity(AppIntergrationSettingParameterEntity entity, int actionId)
        {
            ValidationResult validationResult = new ValidationResult();

            using (DataAccessAdapter adapter = new DataAccessAdapter(ServerContext.Instance.CurrentUserDbConnectionString))
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppDataSetEntity");
                    adapter.UpdateEntitiesDirectly(entity, new RelationPredicateBucket(AppIntergrationSettingParameterFields.SettingParameterId == actionId));
                    adapter.Commit();
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_Save_OK", ValidationItemType.Message, "Save Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    //Console.WriteLine(ex.Message);
                    validationResult.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity), "App_AppIntergrationSettingParameterEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return validationResult;
        }


    }
}