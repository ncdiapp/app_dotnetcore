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
	//TODO not finish @
	public class AppTransactionPostPorcessDispatchBL
	{
		internal static void PostPorcessFormDataAfterSave(
			AppTransactionStructureDto aAppMasterDetailStructureDto,
			object rootPkValue,
			AppMasterDetailDto orgAppMasterDetailDto,
            AppMasterDetailDto currentAppformDataDto,
            bool isCallingFromWorkFlow,
			OperationCallResult<AppMasterDetailDto> aOperationCallResult
				
			)
        {

            var aAppTransactionExDto = aAppMasterDetailStructureDto.HierarchyTransactionExdto;

        

            aOperationCallResult.Object = currentAppformDataDto;




            // Can use command to replace (TODO, need to check 
            //PorcessPostPorcessAndWorkFlowTask(aAppMasterDetailStructureDto, rootPkValue, orgAppMasterDetailDto, isCallingFromWorkFlow, aOperationCallResult, aAppTransactionExDto, currentAppformDataDto);

           // aOperationCallResult.Object = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(currentAppformDataDto.TransactionId, rootPkValue); ;
        }

        private static void PorcessPostPorcessAndWorkFlowTask(AppTransactionStructureDto aAppMasterDetailStructureDto, object rootPkValue, AppMasterDetailDto orgAppMasterDetailDto, bool isCallingFromWorkFlow, OperationCallResult<AppMasterDetailDto> aOperationCallResult, AppTransactionExDto aAppTransactionExDto, AppMasterDetailDto currentAppformDataDto)
        {
            Dictionary<string, object> rootOneToOneFields = currentAppformDataDto.DictOneToOneFields;

            //	AppTransProcessTransactionAuditTrailBL.ProcessTransactionAuditTrail(aAppMasterDetailStructureDto, currentAppformDataDto, orgAppMasterDetailDto,  rootPkValue);

            var rootMasterUnit = aAppTransactionExDto.RootMasterUnit;

            // Psot exterla method call
            List<AppTransactionPostProcessExDto> postProcessList = AppTransactionPostProcessBL.RetrieveProcessListByTransactionId(currentAppformDataDto.TransactionId).OrderBy(o => o.ProcessFlow).ToList();


            if (!postProcessList.IsEmpty())
            {
                ProcessPostTransaction(postProcessList, rootMasterUnit, rootOneToOneFields);
            }


            //	// need to break up cscading call  AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppMasterDetailDto, false);
            if (!isCallingFromWorkFlow)
            {

                // need to create for the new 
                if (orgAppMasterDetailDto == null)
                {
                    AppProjectWorkFlowProcessBL.CreateFormRunningTimeTaskWorkflows(aAppTransactionExDto, rootPkValue);
                }

                var refreshMasterDetailFromDatabase = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData((int)aAppTransactionExDto.Id, rootPkValue);

                bool neeToRefreshFormData = AppProjectWorkFlowDataFormSynchBL.DoSynFormAndTaskWrokFlow(orgAppMasterDetailDto, refreshMasterDetailFromDatabase, aAppTransactionExDto, aAppMasterDetailStructureDto, aOperationCallResult);

            }
        }


        //@orderId=transcatioFied| @orderIdDetail=transcatioFied etc....
        public static void ProcessPostTransaction(List<AppTransactionPostProcessExDto> postProcessList, AppTransactionUnitExDto rootMasterUnit, Dictionary<string, object> RootOneToOneFields)
		{

			Dictionary<string, string> dictIDbfieldname = rootMasterUnit.AppTransactionFieldList.ToDictionary(o => o.Id.ToString(), o => o.DataBaseFieldName);

			foreach (AppTransactionPostProcessExDto aAppTransactionPostProcessExDto in postProcessList)
			{
				string ProcedureName = aAppTransactionPostProcessExDto.PostStoreProcedureName;
				if (!string.IsNullOrWhiteSpace(ProcedureName))
				{
					List<SqlParameter> paras = new List<SqlParameter>();

					if (!string.IsNullOrWhiteSpace(aAppTransactionPostProcessExDto.ParameterOptions))
					{
						string[] pairNameVlaueList = aAppTransactionPostProcessExDto.ParameterOptions.Split("|".ToArray());

						foreach (string pair in pairNameVlaueList)
						{
							string[] keyValue = pair.Split("=".ToArray());

							if (keyValue.Length == 2)
							{
								string filedId = keyValue[1];
								string fiedDbname = dictIDbfieldname[filedId];

								SqlParameter newSqlpa = new SqlParameter(keyValue[0], RootOneToOneFields[fiedDbname]);
								paras.Add(newSqlpa);






							}



						}


					}
					using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
					{
						adapter.CallStoreProc(ProcedureName, paras);

					}

				}


			}

		}

	}
}
