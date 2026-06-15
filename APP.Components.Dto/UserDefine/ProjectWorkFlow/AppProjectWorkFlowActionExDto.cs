using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppProjectWorkFlowActionDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Guid? NextWorkFlowGuId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string ConditionName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string FormulaExpressionUI
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? LinkToDataLoadId
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public string CommandConditionFieldDbFieldName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppActionAttributeDto ActionAttribute
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? ApiOperationId
        {
            get; 
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string BatchNumber
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsWorkflowAutomationRootCommand
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppMasterDetailDto RootTransactionFormData
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectWorkFlowActionDto> WorkflowChildTreeNodes
        {
            get;
            set;
        }

        


        [DataMember(EmitDefaultValue = false)]
        public int? ExternalTransactionId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ParentTreeNodeCommandId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string DisplayName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string TransactionName { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public string ParentNodeLogicId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ProgressStatus { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string TreeNodeLogicId { get; set; }


        [IgnoreDataMember]
        public string CurrentExecutingTreeNodeLogicId { get; set; }


        [IgnoreDataMember]
        public List<AppProjectWorkFlowActionDto> WorkflowCommandNodeTree
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ErrorMessage { get; set; }


        [IgnoreDataMember]
        public bool IsFtpFilePath { 
            get {
                if (ActionType.HasValue && ActionAttribute != null && !string.IsNullOrWhiteSpace(ActionAttribute.FilePath)
                    && (ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTableFromExcel
                        || ActionType.Value == (int)EmAppTransactionCommandType.ImportToDatabaseTableFromJson)
                    )
                {
                    if (ActionAttribute.FilePath.ToLower().StartsWith("ftp"))
                    {
                        return true;
                    }                             
                }

                return false;
            } 
        }
    }
}

