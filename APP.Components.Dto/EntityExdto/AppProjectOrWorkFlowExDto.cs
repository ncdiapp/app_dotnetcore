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
    /// DTO class for the  Extend Relation Entity 'AppProjectOrWorkFlow'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectOrWorkFlowExDto : AppProjectOrWorkFlowDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppMessageListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppMessageExDto>>(o=>o.AppMessageList);
            public static readonly string AppProjectOrTaskTranscationListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectOrTaskTranscationExDto>>(o=>o.AppProjectOrTaskTranscationList);
            public static readonly string AppProjectOrWorkFlow___ListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlow___List);
            public static readonly string AppProjectOrWorkFlow_ListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlow_List);
            public static readonly string AppProjectPortfolioBoardListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectPortfolioBoardExDto>>(o=>o.AppProjectPortfolioBoardList);
            public static readonly string AppProjectPrivacyListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectPrivacyExDto>>(o=>o.AppProjectPrivacyList);
            public static readonly string AppProjectSnapshot_ListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectSnapshotExDto>>(o=>o.AppProjectSnapshot_List);
            public static readonly string AppProjectSnapshotListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectSnapshotExDto>>(o=>o.AppProjectSnapshotList);
            public static readonly string AppProjectTeamMemberListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectTeamMemberExDto>>(o=>o.AppProjectTeamMemberList);
            public static readonly string AppProjectTemplateResourceListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectTemplateResourceExDto>>(o=>o.AppProjectTemplateResourceList);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppProjectWorkFlowConditionListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectWorkFlowConditionExDto>>(o=>o.AppProjectWorkFlowConditionList);
            public static readonly string AppProjectWorkFlowTaskListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppProjectWorkFlowTaskExDto>>(o=>o.AppProjectWorkFlowTaskList);
            public static readonly string AppTransaction_ListProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransaction_List); 
            public static readonly string ForeignAppCurrencyProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppCurrencyExDto>(o=>o.ForeignAppCurrencyExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppProjectOrWorkFlow__Property = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlow__ExDto);
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppProjectTeamProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppProjectTeamExDto>(o=>o.ForeignAppProjectTeamExDto);
            public static readonly string ForeignAppProjectWorkFlowTask_Property = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTask_ExDto);
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppProjectOrWorkFlowExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppMessageList = new  ObservableSet<AppMessageExDto>();
            AppProjectOrTaskTranscationList = new  ObservableSet<AppProjectOrTaskTranscationExDto>();
            AppProjectOrWorkFlow___List = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppProjectOrWorkFlow_List = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppProjectPortfolioBoardList = new  ObservableSet<AppProjectPortfolioBoardExDto>();
            AppProjectPrivacyList = new  ObservableSet<AppProjectPrivacyExDto>();
            AppProjectSnapshot_List = new  ObservableSet<AppProjectSnapshotExDto>();
            AppProjectSnapshotList = new  ObservableSet<AppProjectSnapshotExDto>();
            AppProjectTeamMemberList = new  ObservableSet<AppProjectTeamMemberExDto>();
            AppProjectTemplateResourceList = new  ObservableSet<AppProjectTemplateResourceExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowConditionList = new  ObservableSet<AppProjectWorkFlowConditionExDto>();
            AppProjectWorkFlowTaskList = new  ObservableSet<AppProjectWorkFlowTaskExDto>();
            AppTransaction_List = new  ObservableSet<AppTransactionExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppApplicationAssetsItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppApplicationAssetsItemExDto> AppApplicationAssetsItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppApplicationAssetsItemExDto>>(AppApplicationAssetsItemListProperty);    
            }
            set
            {
				SetValue(AppApplicationAssetsItemListProperty,value);
            }
        }

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
       public  ObservableSet<AppProjectOrWorkFlowExDto> AppProjectOrWorkFlow___List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectOrWorkFlowExDto>>(AppProjectOrWorkFlow___ListProperty);    
            }
            set
            {
				SetValue(AppProjectOrWorkFlow___ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectPortfolioBoardEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectPortfolioBoardExDto> AppProjectPortfolioBoardList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectPortfolioBoardExDto>>(AppProjectPortfolioBoardListProperty);    
            }
            set
            {
				SetValue(AppProjectPortfolioBoardListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectPrivacyEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectPrivacyExDto> AppProjectPrivacyList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectPrivacyExDto>>(AppProjectPrivacyListProperty);    
            }
            set
            {
				SetValue(AppProjectPrivacyListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectSnapshotEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectSnapshotExDto> AppProjectSnapshot_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectSnapshotExDto>>(AppProjectSnapshot_ListProperty);    
            }
            set
            {
				SetValue(AppProjectSnapshot_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectSnapshotEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectSnapshotExDto> AppProjectSnapshotList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectSnapshotExDto>>(AppProjectSnapshotListProperty);    
            }
            set
            {
				SetValue(AppProjectSnapshotListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTeamMemberEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTeamMemberExDto> AppProjectTeamMemberList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTeamMemberExDto>>(AppProjectTeamMemberListProperty);    
            }
            set
            {
				SetValue(AppProjectTeamMemberListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTemplateResourceEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTemplateResourceExDto> AppProjectTemplateResourceList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTemplateResourceExDto>>(AppProjectTemplateResourceListProperty);    
            }
            set
            {
				SetValue(AppProjectTemplateResourceListProperty,value);
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
       public  ObservableSet<AppProjectWorkFlowTaskExDto> AppProjectWorkFlowTaskList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowTaskExDto>>(AppProjectWorkFlowTaskListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowTaskListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionExDto> AppTransaction_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionExDto>>(AppTransaction_ListProperty);    
            }
            set
            {
				SetValue(AppTransaction_ListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppCurrencyEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppCurrencyExDto ForeignAppCurrencyExDto
        {
            get
            {
			    return  GetValue<AppCurrencyExDto>(ForeignAppCurrencyProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppCurrencyProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppListMenuEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppListMenuExDto ForeignAppListMenuExDto
        {
            get
            {
			    return  GetValue<AppListMenuExDto>(ForeignAppListMenuProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppListMenuProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlow__ExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlow__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlow__Property,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectTeamEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectTeamExDto ForeignAppProjectTeamExDto
        {
            get
            {
			    return  GetValue<AppProjectTeamExDto>(ForeignAppProjectTeamProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectTeamProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowTaskEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowTaskExDto ForeignAppProjectWorkFlowTask_ExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowTaskExDto>(ForeignAppProjectWorkFlowTask_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowTask_Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityUserExDto ForeignAppSecurityUserExDto
        {
            get
            {
			    return  GetValue<AppSecurityUserExDto>(ForeignAppSecurityUserProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityUserProperty,value);
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

