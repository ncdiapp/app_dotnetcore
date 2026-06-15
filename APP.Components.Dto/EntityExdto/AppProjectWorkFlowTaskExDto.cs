using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the  Extend Relation Entity 'AppProjectWorkFlowTask'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectWorkFlowTaskExDto : AppProjectWorkFlowTaskDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppMessageListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppMessageExDto>>(o=>o.AppMessageList);
            public static readonly string AppPorjectWorkFlowTaskTimeSheetListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppPorjectWorkFlowTaskTimeSheetExDto>>(o=>o.AppPorjectWorkFlowTaskTimeSheetList);
            public static readonly string AppProjectOrTaskTranscationListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectOrTaskTranscationExDto>>(o=>o.AppProjectOrTaskTranscationList);
            public static readonly string AppProjectOrWorkFlow_ListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlow_List);
            public static readonly string AppProjectPerspectiveTaskListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectPerspectiveTaskExDto>>(o=>o.AppProjectPerspectiveTaskList);
            public static readonly string AppProjectTaskCheckListListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskCheckListExDto>>(o=>o.AppProjectTaskCheckListList);
            public static readonly string AppProjectTaskExpenseListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskExpenseExDto>>(o=>o.AppProjectTaskExpenseList);
            public static readonly string AppProjectTaskPredecessor_ListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskPredecessorExDto>>(o=>o.AppProjectTaskPredecessor_List);
            public static readonly string AppProjectTaskPredecessorListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskPredecessorExDto>>(o=>o.AppProjectTaskPredecessorList);
            public static readonly string AppProjectTaskResourceListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskResourceExDto>>(o=>o.AppProjectTaskResourceList);
            public static readonly string AppProjectTaskTagListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskTagExDto>>(o=>o.AppProjectTaskTagList);
            public static readonly string AppProjectTaskTimeLogListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectTaskTimeLogExDto>>(o=>o.AppProjectTaskTimeLogList);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppProjectWorkFlowConditionListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectWorkFlowConditionExDto>>(o=>o.AppProjectWorkFlowConditionList);
            public static readonly string AppProjectWorkFlowTask_ListProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  ObservableSet<AppProjectWorkFlowTaskExDto>>(o=>o.AppProjectWorkFlowTask_List); 
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppProjectWorkFlowTaskProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTaskExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppProjectWorkFlowTaskExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppMessageList = new  ObservableSet<AppMessageExDto>();
            AppPorjectWorkFlowTaskTimeSheetList = new  ObservableSet<AppPorjectWorkFlowTaskTimeSheetExDto>();
            AppProjectOrTaskTranscationList = new  ObservableSet<AppProjectOrTaskTranscationExDto>();
            AppProjectOrWorkFlow_List = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppProjectPerspectiveTaskList = new  ObservableSet<AppProjectPerspectiveTaskExDto>();
            AppProjectTaskCheckListList = new  ObservableSet<AppProjectTaskCheckListExDto>();
            AppProjectTaskExpenseList = new  ObservableSet<AppProjectTaskExpenseExDto>();
            AppProjectTaskPredecessor_List = new  ObservableSet<AppProjectTaskPredecessorExDto>();
            AppProjectTaskPredecessorList = new  ObservableSet<AppProjectTaskPredecessorExDto>();
            AppProjectTaskResourceList = new  ObservableSet<AppProjectTaskResourceExDto>();
            AppProjectTaskTagList = new  ObservableSet<AppProjectTaskTagExDto>();
            AppProjectTaskTimeLogList = new  ObservableSet<AppProjectTaskTimeLogExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowConditionList = new  ObservableSet<AppProjectWorkFlowConditionExDto>();
            AppProjectWorkFlowTask_List = new  ObservableSet<AppProjectWorkFlowTaskExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppMessageEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppMessageExDto> AppMessageList
        {
            get
            {
			    return  GetValue<ObservableSet<AppMessageExDto>>(AppMessageListProperty);    
            }
            set
            {
				SetValue(AppMessageListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppPorjectWorkFlowTaskTimeSheetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppPorjectWorkFlowTaskTimeSheetExDto> AppPorjectWorkFlowTaskTimeSheetList
        {
            get
            {
			    return  GetValue<ObservableSet<AppPorjectWorkFlowTaskTimeSheetExDto>>(AppPorjectWorkFlowTaskTimeSheetListProperty);    
            }
            set
            {
				SetValue(AppPorjectWorkFlowTaskTimeSheetListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectOrTaskTranscationEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectOrTaskTranscationExDto> AppProjectOrTaskTranscationList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectOrTaskTranscationExDto>>(AppProjectOrTaskTranscationListProperty);    
            }
            set
            {
				SetValue(AppProjectOrTaskTranscationListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectOrWorkFlowExDto> AppProjectOrWorkFlow_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectOrWorkFlowExDto>>(AppProjectOrWorkFlow_ListProperty);    
            }
            set
            {
				SetValue(AppProjectOrWorkFlow_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectPerspectiveTaskEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectPerspectiveTaskExDto> AppProjectPerspectiveTaskList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectPerspectiveTaskExDto>>(AppProjectPerspectiveTaskListProperty);    
            }
            set
            {
				SetValue(AppProjectPerspectiveTaskListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskCheckListEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskCheckListExDto> AppProjectTaskCheckListList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskCheckListExDto>>(AppProjectTaskCheckListListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskCheckListListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskExpenseEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskExpenseExDto> AppProjectTaskExpenseList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskExpenseExDto>>(AppProjectTaskExpenseListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskExpenseListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskPredecessorEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskPredecessorExDto> AppProjectTaskPredecessor_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskPredecessorExDto>>(AppProjectTaskPredecessor_ListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskPredecessor_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskPredecessorEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskPredecessorExDto> AppProjectTaskPredecessorList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskPredecessorExDto>>(AppProjectTaskPredecessorListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskPredecessorListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskResourceEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskResourceExDto> AppProjectTaskResourceList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskResourceExDto>>(AppProjectTaskResourceListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskResourceListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskTagEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskTagExDto> AppProjectTaskTagList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskTagExDto>>(AppProjectTaskTagListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskTagListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskTimeLogEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskTimeLogExDto> AppProjectTaskTimeLogList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskTimeLogExDto>>(AppProjectTaskTimeLogListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskTimeLogListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowActionExDto> AppProjectWorkFlowActionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowActionExDto>>(AppProjectWorkFlowActionListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowActionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowConditionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowConditionExDto> AppProjectWorkFlowConditionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowConditionExDto>>(AppProjectWorkFlowConditionListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowConditionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowTaskEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowTaskExDto> AppProjectWorkFlowTask_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowTaskExDto>>(AppProjectWorkFlowTask_ListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowTask_ListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlowExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlowProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlowProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowTaskEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowTaskExDto ForeignAppProjectWorkFlowTaskExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowTaskExDto>(ForeignAppProjectWorkFlowTaskProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowTaskProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionExDto ForeignAppTransactionExDto
        {
            get
            {
			    return  GetValue<AppTransactionExDto>(ForeignAppTransactionProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionProperty,value);
            }
        }	



        #endregion
        
    }
}

