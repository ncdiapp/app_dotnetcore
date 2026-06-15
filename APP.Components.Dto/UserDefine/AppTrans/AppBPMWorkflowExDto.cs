//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Runtime.Serialization;
//using System.Text;
//using App.Framework;
//using App.Framework.Collections;
//using APP.Components.Dto;

//namespace APP.Components.EntityDto
//{
//    public partial class BPMWorkflowExDto
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public object Id { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object TransactionId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Name { get; set; }

//        [DataMember(EmitDefaultValue = false)]
//        public string Description { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int WorkflowType { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int WorkflowCategory { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public bool IsAutoStart { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMStepExDto> BPMStepExDtoList { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMStepPathExDto> BPMStepPathExDtoList { get; set; }

//    }


//    public partial class BPMStepExDto
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public object Id { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object UiId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object WorkflowId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Name { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Description { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public bool IsActive { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string IsActiveExpression { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int StateType { get; set; }  // Start State, Condition Checking State, Regular State.


//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMStepConditionActionExDto> StateActionList { get; set; }





//        [DataMember(EmitDefaultValue = false)]
//        public int Top { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int Left { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int Width { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int Height { get; set; }





//    }


//    public partial class BPMStepPathExDto
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public object Id { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object UiId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object WorkflowId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object LinkFromStepId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object LinkToStepId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object LinkFromStepUiId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object LinkToStepUiId { get; set; }


//        //[DataMember(EmitDefaultValue = false)]
//        //public string Name { get; set; }


//        //[DataMember(EmitDefaultValue = false)]
//        //public string Description { get; set; }
//    }


//    public partial class BPMStepConditionExDto
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public object Id { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object StepId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Name { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Description { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int ConditionType { get; set; }    //StartStep, CompleteStep, FieldUpdated, FieldValueEqualTo, BooleanExpression


//        [DataMember(EmitDefaultValue = false)]
//        public int? TransactionFiledID { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object FieldValue { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string BooleanExpression { get; set; }



//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMStepConditionActionExDto> BPMStepConditionActionExDtoList { get; set; }

//    }



//    public partial class BPMStepConditionActionExDto
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public object Id { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object StepId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object ConditionId { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Name { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string Description { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public int ActionType { get; set; }     // Open Transaction Form, commuication (send email, sms message....) , other build in actions (open Search, report, ....), execute external command

//    }


//    public partial class BPMWorkflowChangeHistoryExDto
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public object Id { get; set; }

//        [DataMember(EmitDefaultValue = false)]
//        public object WorkflowId { get; set; }

//        [DataMember(EmitDefaultValue = false)]
//        public object FormPKValue { get; set; }

//        [DataMember(EmitDefaultValue = false)]
//        public object FromStepId { get; set; }

//        [DataMember(EmitDefaultValue = false)]
//        public object ToStepId { get; set; }

//        [DataMember(EmitDefaultValue = false)]
//        public int SortOrder { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public DateTime ChangeDate { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public string ActiveStepIdsString { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<Object> ActiveStepIdList { get; set; }
//    }

//    public partial class FormBPMWorkflowExDto  // Loaded when loading form data, don't need to save to database
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public BPMWorkflowExDto WorkflowStructure { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object FormPKValue { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<Object> ActiveStepIdList { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMWorkflowChangeHistoryExDto> WorkflowChangeHistoryList { get; set; }
//    }


//    public partial class WorkflowCondition  // Loaded when loading form data, don't need to save to database
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public BPMWorkflowExDto WorkflowStructure { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object FormPKValue { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<Object> ActiveStepIdList { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMWorkflowChangeHistoryExDto> WorkflowChangeHistoryList { get; set; }
//    }

//    public partial class WorkflowAction  // Loaded when loading form data, don't need to save to database
//    {
//        [DataMember(EmitDefaultValue = false)]
//        public BPMWorkflowExDto WorkflowStructure { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public object FormPKValue { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<Object> ActiveStepIdList { get; set; }


//        [DataMember(EmitDefaultValue = false)]
//        public List<BPMWorkflowChangeHistoryExDto> WorkflowChangeHistoryList { get; set; }
//    }


//}