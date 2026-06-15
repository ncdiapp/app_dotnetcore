using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppActionAttributeDto
    {
        //public List<int> ChildActionIdList { get; set; }

        public List<ChildTransactionCommandDto> ChildActionList { get; set; }

        public bool? IsAutoExecuteOnFormOpen { get; set; }

        public bool? LinkToUI { get; set; }

        public bool? IsNeedToOpenNewForm { get; set; }

        public bool? IsShowOnTopMenu { get; set; }

        public bool? IsShowOnSearchViewEventOptionMenu { get; set; }

        public int? ParamId1 { get; set; }

        public int? ParamId2 { get; set; }

        public int? ParamId3 { get; set; }

        public string WebApiMethodName { get; set; }

        //public int? IntegrationOperationId { get; set; }

        public List<KeyValuePair<string, int?>> PathParamMappingList { get; set; }

        public List<KeyValuePair<string, int?>> QueryParamMappingList { get; set; }

        public int? ExternalMethodRegisterId { get; set; }

        public bool IsOpenPopup { get; set; }

        public int? PopupWidth { get; set; }

        public int? PopupHeight { get; set; }

        public int? CallBackCommandID { get; set; }

        public int? TargetTransactionCommandId { get; set; }

        public string OtherConditionExpression { get; set; }

        public int? LinkedSearchId { get; set; }

        public int? NotificationDestinationEmailAddressTransactionFiledId { get; set; }

        public int? SmsMessageToPhoneNumberFiledId { get; set; }

        public int? NotificationDestinationPartnerIdTransactionFiledId { get; set; }

        public int? AssignSqlResultToFiledId { get; set; }

        public int? LinkTargetId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsUseRichTextMessageTemplate { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAttachAllFormFilesToMessage { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? ChildCommandsSwitchConditionFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ToEmailAddress { get; set; }

        //????
        //[DataMember(EmitDefaultValue = false)]
        //public int? NeedToDetectChangeTransactionFieldId { get; set; }
               

        //[DataMember(EmitDefaultValue = false)]
        //public int? DetectedChangeSaveToTransactionFieldId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? CommandsChangedTrigerFieldId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ForeachLoopSourceUnitId { get; set; }
        


        [DataMember(EmitDefaultValue = false)]
        public string ExecutionFormula { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ExecutionFormulaUI { get; set; }


        // Key: DataFiledId, value DataTableComun
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictDataFiledIdColumnName
        {
            get;set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableCommandExecuteResultMessage
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsBatchCommand
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BatchCommandSourceFromType
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BatchCommandDataSetId
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BatchCommandSearchId
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BatchCommandSourceViewFieldId
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string BatchCommandSourceDataSetFieldName
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CommandSuccessMessage { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string CommandFailedMessage { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public int? UserTypeTransFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? UserNameTransFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? UserPasswordTransFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? UserEmailTransFieldId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? UserPartnerIdTransFieldId { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public EmAppCommandErrorLogType? CommandErrorLogType { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public int? ErrorLogTransactionFieldId { get; set; }

        //public List<ChildTransactionCommandDto> ChildActionList { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public EmAppValidationResultPreference? EmAppValidationResultPreference { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public EmAppCommandLoggingPreference? EmAppCommandLoggingPreference { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsLogCommandStartEnd { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsLogErrorDetails { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsLogApiRequest { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsLogApiPayload { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsLogApiResponse { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public int? JsonImportSettingId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ExcelImportSettingId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DbToDbImportSettingId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? RestApiDbImportSettingId { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public bool IsImportMultiFilesFromOneFolder { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FilePath { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string DistinationFilePath { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FileExtensionFilter { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FtpUserName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string FtpPassword { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Arguments { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsWorkflowRootCommand { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, int?> DictBatchSearchCrietraIdAndTransFieldId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<string> DebugKeyList { get; set; }

        /// <summary>SQL statement for ActionType = 42 (Execute SQL Statement).</summary>
        [DataMember(EmitDefaultValue = false)]
        public string SqlStatement { get; set; }

        /// <summary>JSON configuration for external integration action types (83=Shopify, 84=GoogleSheets, 85=Netsuite, 86=REST).</summary>
        [DataMember(EmitDefaultValue = false)]
        public string IntegrationConfigJson { get; set; }

    }


    public class ChildTransactionCommandDto
    {
        public int? Sort { get; set; }

        public int? ExternalTransactionId { get; set; }

        public string ExternalTransactionRId { get; set; }

        public int? CommandId { get; set; }

        public string CommandDisplay { get; set; }

        public bool IsBatchCommand { get; set; }

        public int? PredictValue { get; set; }

        //public int? TriggerFieldChangedPredictValue { get; set; }

        public int? ChangeTriggerRootLevelFieldId { get; set; }
        
        public int? ChangeTriggerChildGridUnitId { get; set; }

        public bool? IsGoToNextCommandWithError { get; set; }

        public bool IsSkip { get; set; }
    }


}