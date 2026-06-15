using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using APP.LBL.RelationClasses;
////using APP.Persistence.Common;
using Newtonsoft.Json;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;




//---Unit 212   Cascading

//DDLParentLevelID: 2134

//RelationTable: SP_testGetGroup

//pararentCscading: @TermCourseDetailId

//ChildCascading: TotalHours:TotalHours|GroupRate:GroupRate|LineItemTotal:TotalCost

//DataRetrieveType 2



//---Unit 213   one to one

//RelationTable: SP_GetRegistrationOrderDetailForIndividualClassHours_callDataSet

//pararentCscading: StartDate:@startDate|EndDate:@endDate;222>@CourseCalendar=WeekDayId:WeekDayId|StartTime:StartTime|EndTime:EndTime;

//ChildCascading: Qty:TotalHours

//DataRetrieveType 3


//---Unit 222   one to many

//RelationTable: SP_GetIndividualClassSessionAvailableTeacher

//pararentCscading: TermCourseDetailId:@TermCourseDetailId|TermId:@TermId|StartDate:@StartDate|EndDate:@EndDate|StartTime:@StartTime|EndTime:@EndTime|WeekDayId:@WeekdayId

//ChildCascading:TeacherName:TeacherName|TeacherAge:TeacherAge

// 

//DataRetrieveType 2



// Caculation result could trigger  cascading triger, need to disable cscading after assignment
using APP.Framework;
namespace App.BL
{
    public static class AppCascadingMutipleColumnBL
    {


        

// RelationTable: SP_GetRegistrationOrderDetailForIndividualClassHours_callDataSet

 //first set: root paramter; second set: child
        //pararentCscading: startDate:@StartDate|endDate:@EndDate; 1234>@CourseCalendar=column1:ChildTransFiled1 | column2: ChildTransFiled2 | | column3: ChildTransFiled3;

 //TranFiled:
//ChildCascading:   1234>@ TotalHours:Qty|CostRate:CostRate|TotalCost:LineItemTotal


        public static AppRetrieveMutipleColumnDataSourceDto AppRetrieveMutipleColumnDataSourceDto(AppRetrieveMutipleColumnDataSourceDto appRetrieveMutipleColumnDataSourceDto)
        {

            AppChildDataDto InputRowData = appRetrieveMutipleColumnDataSourceDto.InputRowData;
            int retrieveDataFiledID = appRetrieveMutipleColumnDataSourceDto.RetrieveDataFiledID;
            AppTransactionFieldExDto aAppTransactionFieldExDto =  AppTransactionBL.RetrieveOneAppTransactionFieldExDto(retrieveDataFiledID);

            string WarningMessage = string.Empty ;
          

            if (aAppTransactionFieldExDto.DataRetrieveType.HasValue
                && (aAppTransactionFieldExDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToManyMapping
                    || aAppTransactionFieldExDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToOneMapping
                
                   )


                )
            {


             
                 Dictionary<string, string> rootTranSqlparamapping = aAppTransactionFieldExDto.InputParentTransFiledNameSqlParaMapping;
                 Dictionary<string, Dictionary<string, string>> dictChildTranSqlparamapping = aAppTransactionFieldExDto.InputChildTransFiledNameSqlParaNameMapping;


                 string queryStorcname = aAppTransactionFieldExDto.CascadingRelationTable ;
                 List<SqlParameter> sqlParamterList = new List<SqlParameter>();

                 foreach (string transFiledName in rootTranSqlparamapping.Keys)
                 {

                     object inputParaValue = InputRowData.DictOneToOneFields[transFiledName];
                     SqlParameter aSqlParameter = new SqlParameter();
                     // aSqlParameter.IsNullable = true;
                     aSqlParameter.ParameterName = rootTranSqlparamapping[transFiledName];
                     // need to convert?
                     aSqlParameter.Value = inputParaValue;
                     sqlParamterList.Add(aSqlParameter);

                     if(inputParaValue == null ||  string.IsNullOrEmpty (inputParaValue.ToString ()))
                     {

                         WarningMessage = WarningMessage + " " + transFiledName + " " + " Is Empty";
                     
                     }
                 
                 }


                 if (! string.IsNullOrEmpty(WarningMessage))
                 {

                     appRetrieveMutipleColumnDataSourceDto.WarningMessage = WarningMessage;

                     return appRetrieveMutipleColumnDataSourceDto;
                 }

                 foreach (string unitISqlName in dictChildTranSqlparamapping.Keys)
                 {

                   

                     string [] unitId_dataTblepar =  unitISqlName.Split(">".ToCharArray());

                     string unitId = unitId_dataTblepar[0];
                     string dataTableparamet = unitId_dataTblepar[1];

      
                     DataTable dataTableValue = new DataTable(dataTableparamet);

                     Dictionary<string, string> transFiledcolumn = dictChildTranSqlparamapping[unitISqlName];

                     foreach (string column in transFiledcolumn.Values)
                     {
                         dataTableValue.Columns.Add(column);
                     }

                    List<AppChildDataDto> childSetData = InputRowData.DictOneToManyFields[unitId];

                    foreach (AppChildDataDto aAppChildDataDto in childSetData)
                    {
                        Dictionary<string, object> dictrow = aAppChildDataDto.DictOneToOneFields;

                        DataRow dtRow = dataTableValue.NewRow();


                         foreach (string tranField in transFiledcolumn.Keys)
                         {
                             string columnName = transFiledcolumn[tranField];
                             dtRow[columnName] = dictrow[tranField];
                         }

                         dataTableValue.Rows.Add(dtRow);
                     
                     }

                     SqlParameter aSqlParameter = new SqlParameter();
                     aSqlParameter.Value = dataTableValue;
                     aSqlParameter.ParameterName = dataTableparamet;
                     aSqlParameter.SqlDbType = SqlDbType.Structured;
                     sqlParamterList.Add(aSqlParameter);

                 }

                 DataTable fillDataTable = new DataTable();


                 if (aAppTransactionFieldExDto.AppExternalSourceFrom == (int)EmAppExternalSourceFrom.StoredProcedure)
                 {
                     using (DataAccessAdapter aDataAccessAdapterWithDataSource = AppTenantAdapterBL.GetTenantAdapter())
                     {
                         aDataAccessAdapterWithDataSource.CallRetrievalStoredProcedure(queryStorcname, sqlParamterList.ToArray(), fillDataTable);

                     }

                 }
                 else
                 {

                     int? exteralMethodId = ControlTypeValueConverter.ConvertValueToInt(queryStorcname);

                  

                     if (exteralMethodId.HasValue)
                     {
                         List<object> valueList = new List<object>();
                         foreach (var paramter in sqlParamterList)
                         {
                             valueList.Add(paramter.Value);
                             

                         }
                         fillDataTable = AppExternalMethodRegisterBL.GetExternalMethodDataTable(exteralMethodId, valueList.ToArray());
                     
                     }

                 
                 }


                 if (aAppTransactionFieldExDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToOneMapping)
                 {

                     if (fillDataTable.Rows.Count > 0)
                     {

                         DataRow dataRow = fillDataTable.Rows[0];

                         var returnRowData = new Dictionary<string, object>();

                         appRetrieveMutipleColumnDataSourceDto.ReturnRowData = returnRowData;


                         var outputTransactionFieldAndSpResultFieldMapping = aAppTransactionFieldExDto.OutputTransactionFieldAndSpResultFieldMapping;

                        
                           foreach (string filed in outputTransactionFieldAndSpResultFieldMapping.Keys)
                           {

                               returnRowData[filed] = dataRow[outputTransactionFieldAndSpResultFieldMapping[filed]];
                           
                           }
                       
                                              
                     }

                 }

                 else if (aAppTransactionFieldExDto.DataRetrieveType.Value == (int)EmAppCascadingSourceType.ExternalOneToManyMapping)
                 {
                     List<LookupItemDto> childAllList = new List<LookupItemDto>();
                     appRetrieveMutipleColumnDataSourceDto.DictReturnFieldDataSet = new Dictionary<int, List<LookupItemDto>>();

                     if (fillDataTable.Rows.Count > 0)
                     {

                        
                    

                         string transFiledDataSetMapping = aAppTransactionFieldExDto.CascadingRelationTableChildKeyField;
                         
                           
                             AppCascadingBL.ConvertDataRowToLookupItemWithDepdendenColumn(transFiledDataSetMapping, childAllList, fillDataTable);
                            
         
                     }
                     appRetrieveMutipleColumnDataSourceDto.DictReturnFieldDataSet.Add(aAppTransactionFieldExDto.MasterEntityFieldlId.Value, childAllList);

                 }
        
            }

   


            appRetrieveMutipleColumnDataSourceDto.InputRowData = null;
            appRetrieveMutipleColumnDataSourceDto.WarningMessage = WarningMessage;


            return appRetrieveMutipleColumnDataSourceDto;

        }

  
        private static APP.Components.EntityDto.AppRetrieveMutipleColumnDataSourceDto test()
        {
            AppRetrieveMutipleColumnDataSourceDto testData = new AppRetrieveMutipleColumnDataSourceDto();
            testData.ReturnRowData = new Dictionary<string, object>();
            testData.ReturnRowData.Add("Qty", 10);
            testData.ReturnRowData.Add("CostRate", 100);
            testData.ReturnRowData.Add("LineItemTotal", 1000);


            testData.DictReturnFieldDataSet = new Dictionary<int, List<LookupItemDto>>();
            List<LookupItemDto> aList = new List<LookupItemDto>();
            aList.Add(new LookupItemDto() { Id = 1, Display = "AAA" });
            aList.Add(new LookupItemDto() { Id = 2, Display = "BBB" });

            testData.DictReturnFieldDataSet.Add(2192, aList);

            return testData;
        }
    }
}