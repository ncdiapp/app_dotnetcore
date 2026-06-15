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

	public enum EmAuditorAction { Initial = 1, Update = 2, Delete = 3, Approve = 4, NewRow = 5, UpdateRow = 6, DeleteRow = 7 }
	public class AppTransProcessTransactionAuditTrailBL
	{
		//internal static void PostPorcessFormDataAfterSave(
		//	AppMasterDetailDto rootClientAppformDataDto,
		//	bool isCallingFromWorkFlow,
		//	AppTransactionExDto aAppTransactionExDto,
		//	OperationCallResult<AppMasterDetailDto> aOperationCallResult,
		//	Dictionary<string, object> rootOneToOneFields,

		//	object orgFromDtoRootValue,
		//	AppMasterDetailDto orgAppMasterDetailDto,

		//	object rootPkValue,
		//	AppTransactionStructureDto aAppMasterDetailStructureDto)
		//{


		//	ProcessTransactionAuditTrail(aAppMasterDetailStructureDto, rootClientAppformDataDto, orgAppMasterDetailDto,  rootPkValue);
		//	var rootMasterUnit = aAppTransactionExDto.RootMasterUnit;

		//	// Psot exterla method call
		//	List<AppTransactionPostProcessExDto> postProcessList = AppTransactionPostProcessBL.RetrieveProcessListByTransactionId(rootClientAppformDataDto.TransactionId).OrderBy(o => o.ProcessFlow).ToList();

		//	AppMasterDetailDto refreshMasterDetailFromDatabase = null;
		//	if (!postProcessList.IsEmpty())
		//	{
		//		ProcessPostTransaction(postProcessList, rootMasterUnit, rootOneToOneFields);
		//		refreshMasterDetailFromDatabase = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(rootClientAppformDataDto.TransactionId, rootPkValue);

		//	}


		//	//	// need to break up cscading call  AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppMasterDetailDto, false);
		//	if (!isCallingFromWorkFlow)
		//	{
		//		if (orgFromDtoRootValue == null)
		//		{
		//			AppProjectWorkFlowProcessBL.CreateFormRunniTimeWorkflows(aAppTransactionExDto.Id, rootPkValue);
		//		}

		//		bool neeToRefreshFormData = AppProjectWorkFlowDataFormSynchBL.DoSynFormAndTaskWrokFlow(orgAppMasterDetailDto, refreshMasterDetailFromDatabase, aAppTransactionExDto, aAppMasterDetailStructureDto);

		//		if (neeToRefreshFormData)
		//		{
		//			refreshMasterDetailFromDatabase = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(rootClientAppformDataDto.TransactionId, rootPkValue);
		//		}

		//	}

		//	if (refreshMasterDetailFromDatabase == null)
		//	{
		//		refreshMasterDetailFromDatabase = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(rootClientAppformDataDto.TransactionId, rootPkValue);

		//	}

		//	aOperationCallResult.Object = refreshMasterDetailFromDatabase;
		//}


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

		public static void ProcessTransactionAuditTrail
			(
				AppTransactionStructureDto aAppMasterDetailStructureDto,
				AppMasterDetailDto currentAppformDataDto,
				AppMasterDetailDto orgAppMasterDetailDto,
			
				object rootPkValue
			)
		{

			List<AppTransAuditTrailLogDto> auditDtoList = new List<AppTransAuditTrailLogDto>();

			// it is new Master detail record..
			//   System.DateTime.Now.Ticks   
			long batchNo = ControlTypeValueConverter.ConvertDateTimeToYMDHMS(System.DateTime.Now);  

			if (orgAppMasterDetailDto == null)
			{

				// need to refresh all data and log
				currentAppformDataDto =AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(currentAppformDataDto.TransactionId, rootPkValue);

				var auditTrailLogDtoList =PorcessRootAndSiblingUnitInitiaLog(aAppMasterDetailStructureDto, currentAppformDataDto, rootPkValue, batchNo);

				auditDtoList.AddRange(auditTrailLogDtoList);

				var rootUnit = aAppMasterDetailStructureDto.HierarchyTransactionExdto.RootMasterUnit;

				foreach (var childUnit in rootUnit.Children)
				{
					// Porcess Chind unit
					List<AppChildDataDto> newChildDateSet = currentAppformDataDto.DictOneToManyFields[childUnit.Id.ToString()];
					var childAuditFiledList = aAppMasterDetailStructureDto.DictUnitIdAuditorFieldNames[childUnit.Id.ToString()];

					foreach (AppChildDataDto newChildDataDto in newChildDateSet)
					{
						if(!newChildDataDto.DictOneToOneFields.IsEmpty ())
						{
							Dictionary<string, object> dictChildOneToOne = newChildDataDto.DictOneToOneFields;

							//object chikdPdValue = dictChildOneToOne[]
							CreateInitlActionPerUnit(aAppMasterDetailStructureDto, rootPkValue, null, null, dictChildOneToOne, childUnit.Id, batchNo);

						}

						// need key fike 

					}

				}








			}
			else // need to check update monitor...
			{
				var updateTrailList =ProcessRootUnitUpdateLog(aAppMasterDetailStructureDto, currentAppformDataDto, orgAppMasterDetailDto, rootPkValue,batchNo);

				auditDtoList.AddRange(updateTrailList);
			}

			EntityCollection<AppTransAuditTrailLogEntity> auditEntityList = new EntityCollection<AppTransAuditTrailLogEntity>();
			foreach (var dto in auditDtoList)
			{
				AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity = new AppTransAuditTrailLogEntity();
				AppTransAuditTrailLogConverter.CopyDtoToEntity(aAppTransAuditTrailLogEntity, dto);

				auditEntityList.Add(aAppTransAuditTrailLogEntity);

			}

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				adapter.SaveEntityCollection(auditEntityList);
			}



		}

		private static List<AppTransAuditTrailLogDto> ProcessRootUnitUpdateLog(AppTransactionStructureDto aAppMasterDetailStructureDto,
			AppMasterDetailDto newAppformDataDto,
			AppMasterDetailDto orgAppMasterDetailDto,
			object rootPkValue, long batchNo)
		{
			List<AppTransAuditTrailLogDto> toReturnauditDtoList = new List<AppTransAuditTrailLogDto>();


			object rootUnnitID = aAppMasterDetailStructureDto.HierarchyTransactionExdto.RootMasterUnit.Id;

			var rootUnitAuditFiledList = aAppMasterDetailStructureDto.DictUnitIdAuditorFieldNames[rootUnnitID.ToString()];

			var dictNewOnToOne = newAppformDataDto.DictOneToOneFields;
			var dictOldOnToOne = orgAppMasterDetailDto.DictOneToOneFields;

			foreach (var rootFied in rootUnitAuditFiledList)
			{
				string dbFiedName = rootFied.Key;
				int transactionFieldId = rootFied.Value;


				AppTransAuditTrailLogDto aAppTransAuditTrailLogDto = new AppTransAuditTrailLogDto();

				aAppTransAuditTrailLogDto.TransactionId = aAppMasterDetailStructureDto.TransactionId;
				aAppTransAuditTrailLogDto.RootValueId = rootPkValue as int?;
				aAppTransAuditTrailLogDto.TransactionFieldId = rootFied.Value;
				aAppTransAuditTrailLogDto.BatchNoId = batchNo;


				object newfieldValue = dictNewOnToOne[dbFiedName];
				string newvalueLog = newfieldValue == null ? string.Empty : newfieldValue.ToString();


				object oldfieldValue = dictOldOnToOne[dbFiedName];
				string oldvalueLog = oldfieldValue == null ? string.Empty : oldfieldValue.ToString();

				if (oldvalueLog != newvalueLog)
				{

					aAppTransAuditTrailLogDto.TraiLogAction = EmAuditorAction.Update.ToString();



					aAppTransAuditTrailLogDto.ModifiedValueBefor = oldvalueLog;

					aAppTransAuditTrailLogDto.ModifiedValueAfter = newvalueLog;

					toReturnauditDtoList.Add(aAppTransAuditTrailLogDto);

				}

				
			}

			return toReturnauditDtoList;
		}

		private static List<AppTransAuditTrailLogDto> PorcessRootAndSiblingUnitInitiaLog(AppTransactionStructureDto aAppMasterDetailStructureDto, AppMasterDetailDto newAppformDataDto, object pkValue,long batchNo)
		{


			var dictOnToOne = newAppformDataDto.DictOneToOneFields;
			object rootUnnitID = aAppMasterDetailStructureDto.HierarchyTransactionExdto.RootMasterUnit.Id;

			List<AppTransAuditTrailLogDto> auditDtoList = CreateInitlActionPerUnit(aAppMasterDetailStructureDto, pkValue, null,null,dictOnToOne, rootUnnitID,  batchNo);

		
			if(!newAppformDataDto.DictSiblingOneToOneFields.IsEmpty ())
			{
				foreach (string siblingUnitid in newAppformDataDto.DictSiblingOneToOneFields.Keys)
				{
					
					Dictionary<string, object> siblingUnitOneToOneFields = newAppformDataDto.DictSiblingOneToOneFields[siblingUnitid];
					List<AppTransAuditTrailLogDto> siblingauditDtoList = CreateInitlActionPerUnit(aAppMasterDetailStructureDto, pkValue, null, null, siblingUnitOneToOneFields, siblingUnitid, batchNo);
					auditDtoList.AddRange(siblingauditDtoList);

				}
			}


			return auditDtoList;
		}

		private static List<AppTransAuditTrailLogDto> CreateInitlActionPerUnit(AppTransactionStructureDto aAppMasterDetailStructureDto, 
			object rootpkValue, object childpkValue, object grandChildpkValue , Dictionary<string, object> dictOnToOne, object unitId, long batchNo)
		{
			var rootUnitAuditFiledList = aAppMasterDetailStructureDto.DictUnitIdAuditorFieldNames[unitId.ToString()];


			List<AppTransAuditTrailLogDto> auditDtoList = new List<AppTransAuditTrailLogDto>();
			foreach (var rootFied in rootUnitAuditFiledList)
			{
				string dbFiedName = rootFied.Key;
				int transactionFieldId = rootFied.Value;


				AppTransAuditTrailLogDto aAppTransAuditTrailLogDto = new AppTransAuditTrailLogDto();
				aAppTransAuditTrailLogDto.BatchNoId = batchNo;
				aAppTransAuditTrailLogDto.TransactionId = aAppMasterDetailStructureDto.TransactionId;
				aAppTransAuditTrailLogDto.RootValueId = rootpkValue as int?;
				aAppTransAuditTrailLogDto.TransactionFieldId = rootFied.Value;
				aAppTransAuditTrailLogDto.TraiLogAction = EmAuditorAction.Initial.ToString();
				aAppTransAuditTrailLogDto.UnitId = ControlTypeValueConverter.ConvertValueToInt(unitId);
				//aAppTransAuditTrailLogDto.un
				aAppTransAuditTrailLogDto.RootValueId  = ControlTypeValueConverter.ConvertValueToInt(rootpkValue);

				aAppTransAuditTrailLogDto.ChildUnitRowValueId = childpkValue == null ? null : childpkValue.ToString();
				aAppTransAuditTrailLogDto.GrandChildUnitRowValueId = grandChildpkValue == null ? null : grandChildpkValue.ToString();

				object fieldValue = dictOnToOne[dbFiedName];
				string valueLog = fieldValue == null ? string.Empty : fieldValue.ToString();
				aAppTransAuditTrailLogDto.ModifiedValueBefor = valueLog;

				auditDtoList.Add(aAppTransAuditTrailLogDto);


			}

			return auditDtoList;
		}
	}
}
