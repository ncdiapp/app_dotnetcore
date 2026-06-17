using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;

using APP.Framework;
namespace App.BL
{
    public static class AppExternalMethodRegisterBL
    {
        private static readonly string ExternalDllRepository = AppDomain.CurrentDomain.BaseDirectory + @"ExternalDllRepository\";


        public static EntityCollection<AppExternalMethodRegisterEntity> RetrieveAllAppExternalMethodRegisterEntity()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppExternalMethodRegisterEntity> list = new EntityCollection<AppExternalMethodRegisterEntity>();
                SortClause aSortClause = AppExternalMethodRegisterFields.MethodDisplayName | SortOperator.Ascending;

                adapter.FetchEntityCollection(list, null, 0, new SortExpression(aSortClause), null);

                return list;
            }
        }

 
        public static AppExternalMethodRegisterEntity RetrieveOneAppExternalMethodRegisterEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity = new AppExternalMethodRegisterEntity(int.Parse(Id.ToString()));

           
                adapter.FetchEntity(aAppExternalMethodRegisterEntity);
                return aAppExternalMethodRegisterEntity;
            }
        }

        public static ObservableSet<AppExternalMethodRegisterExDto> RetrieveAllAppExternalMethodRegisterEntityDto()
        {
            ObservableSet<AppExternalMethodRegisterExDto> aSet = new ObservableSet<AppExternalMethodRegisterExDto>();
            EntityCollection<AppExternalMethodRegisterEntity> list = RetrieveAllAppExternalMethodRegisterEntity();
            foreach (var o in list)
            {
                AppExternalMethodRegisterExDto aDto = AppExternalMethodRegisterConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppExternalMethodRegisterExDto RetrieveOneAppExternalMethodRegisterExDto(object Id)
        {
            AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity = RetrieveOneAppExternalMethodRegisterEntity(Id);
            AppExternalMethodRegisterExDto aAppExternalMethodRegisterExDto = AppExternalMethodRegisterConverter.ConvertEntityToExDto(aAppExternalMethodRegisterEntity);
           


            return aAppExternalMethodRegisterExDto;
        }

        public static OperationCallResult<AppExternalMethodRegisterExDto> SaveAllAppExternalMethodRegisterEntityDto(ObservableSet<AppExternalMethodRegisterExDto> aSet)
        {
            OperationCallResult<AppExternalMethodRegisterExDto> aOperationCallResult = new OperationCallResult<AppExternalMethodRegisterExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (var newItemDto in aSet.FindNewItems())
            {
                var result = ProcessNewDto(newItemDto);
                validationResult.Merge(result);
            }

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppExternalMethodRegisterEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppExternalMethodRegisterEntityDto();
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppExternalMethodRegisterExDto> SaveOneAppExternalMethodRegisterEntityDto(AppExternalMethodRegisterExDto aAppExternalMethodRegisterExDto)
        {
            OperationCallResult<AppExternalMethodRegisterExDto> aOperationCallResult = new OperationCallResult<AppExternalMethodRegisterExDto>();

            var aValidationResult = aAppExternalMethodRegisterExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }


            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



        


            if (aAppExternalMethodRegisterExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppExternalMethodRegisterExDto));
            }
            else if (aAppExternalMethodRegisterExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppExternalMethodRegisterExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
               // var entity = AppExternalMethodRegisterBL.RetrieveOneAppExternalMethodRegisterEntity(aAppExternalMethodRegisterExDto.Id);
                aOperationCallResult.Object = RetrieveOneAppExternalMethodRegisterExDto(aAppExternalMethodRegisterExDto.Id); 
            }

            return aOperationCallResult;
        }

        public static DataTable GetExternalMethodDataTable(object Id, object[] paramter)
        {
            List<string> toReturn = new List<string>();
            var aAppExternalMethodRegisterExDto = RetrieveOneAppExternalMethodRegisterEntity(Id);

            string typeName = aAppExternalMethodRegisterExDto.TypeName;
            string methodName = aAppExternalMethodRegisterExDto.MethodName;





            string pathToDomain = ExternalDllRepository + aAppExternalMethodRegisterExDto.AssemblyName + ".dll";
            Assembly domainAssembly = Assembly.LoadFrom(pathToDomain);
            Type type = domainAssembly.GetType(typeName);


            return  type.GetMethod(methodName).Invoke(null, paramter) as DataTable;

            


        }


        public static OperationCallResult<AppMasterDetailDto> CallExternalMethodMasterDetail(object methodRegisterId, object[] paramter)
        {
            var dto = RetrieveOneAppExternalMethodRegisterEntity(methodRegisterId);
            return AppPluginEngine.Invoke<OperationCallResult<AppMasterDetailDto>>(
                dto.AssemblyName, dto.TypeName, dto.MethodName,
                paramter.Length > 0 ? paramter[0] : null);
        }

        public static OperationCallResult<AppMasterDetailDto> GetFieldTargetExternalMethod(object Id, object[] paramter)
        {
            var dto = RetrieveOneAppExternalMethodRegisterEntity(Id);
            return AppPluginEngine.Invoke<OperationCallResult<AppMasterDetailDto>>(
                dto.AssemblyName, dto.TypeName, dto.MethodName,
                paramter.Length > 0 ? paramter[0] : null);
        }


        public static List<string> GetExternalMethodOutputFieldList(object Id)
        {
            List<string> toReturn = new List<string> ();
           var aAppExternalMethodRegisterExDto = RetrieveOneAppExternalMethodRegisterEntity(Id);

           string typeName = aAppExternalMethodRegisterExDto.TypeName;
           string methodName = aAppExternalMethodRegisterExDto.MethodName;


         //  object[] paramter = { null,null,null};



//Optional parameter values in C# are compiled by injection those values at the callsite
            //When finding the method you need to treat the optional parameters as regular parameters.
           string pathToDomain = ExternalDllRepository + aAppExternalMethodRegisterExDto.AssemblyName + ".dll";
           Assembly domainAssembly = Assembly.LoadFrom(pathToDomain);
           Type type = domainAssembly.GetType(typeName);

           string [] inputPatamters = aAppExternalMethodRegisterExDto.InputParameterList.Split("|".ToCharArray());

           List<object> valueList = new List<object>();

           foreach (string parater in inputPatamters)
           {
               valueList.Add(null);
           }





           DataTable fillDataTable = type.GetMethod(methodName).Invoke(null, valueList.ToArray ()) as DataTable;

           if (fillDataTable != null)
           {
               toReturn.AddRange(fillDataTable.Columns.Cast<DataColumn>().Select(o => o.ColumnName));
           }

           return toReturn;
          
           
        }

        //  RetrieveAllAppExternalMethodRegisterEntityDto(aAppExternalMethodRegisterExDto.Id )

        public static OperationCallResult<object> DeleteOneAppExternalMethodRegisterEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppExternalMethodRegisterEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppExternalMethodRegisterEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppExternalMethodRegisterEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppExternalMethodRegisterEntity), new RelationPredicateBucket(AppExternalMethodRegisterFields.MethodRegisterId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppExternalMethodRegisterEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppExternalMethodRegisterExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppExternalMethodRegisterEntity aParentAppExternalMethodRegisterEntity = new AppExternalMethodRegisterEntity();
            AppExternalMethodRegisterConverter.CopyDtoToEntity(aParentAppExternalMethodRegisterEntity, aDto);

           
            

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppExternalMethodRegisterEntity);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppExternalMethodRegisterEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppExternalMethodRegisterEntity.MethodRegisterId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppExternalMethodRegisterEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppExternalMethodRegisterExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity = RetrieveOneAppExternalMethodRegisterEntity(aDto.Id);

            AppExternalMethodRegisterConverter.CopyDtoToEntity(aAppExternalMethodRegisterEntity, aDto);

          
          

       


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppExternalMethodRegisterEntity, false, true);

                

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppExternalMethodRegisterEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppExternalMethodRegisterExDto), "App_AppExternalMethodRegisterEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

      
    }
}