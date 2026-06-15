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
    /// DTO class for the  Extend Relation Entity 'AppTransaction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionExDto : AppTransactionDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppConditionalActionListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalActionList);
            public static readonly string AppEsitePagesListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppEsitePagesExDto>>(o=>o.AppEsitePagesList);
            public static readonly string AppFormLinkTargetListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTargetList);
            public static readonly string AppmessageNotificationSettingListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppmessageNotificationSettingExDto>>(o=>o.AppmessageNotificationSettingList);
            public static readonly string AppProjectOrTaskTranscationListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppProjectOrTaskTranscationExDto>>(o=>o.AppProjectOrTaskTranscationList);
            public static readonly string AppProjectOrWorkFlowListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppProjectOrWorkFlowExDto>>(o=>o.AppProjectOrWorkFlowList);
            public static readonly string AppProjectWorkFlowAction_ListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowAction_List);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppProjectWorkFlowTaskListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppProjectWorkFlowTaskExDto>>(o=>o.AppProjectWorkFlowTaskList);
            public static readonly string AppSearchListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppSearchExDto>>(o=>o.AppSearchList);
            public static readonly string AppSearchViewListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppSearchViewExDto>>(o=>o.AppSearchViewList);
            public static readonly string AppSecuritySysObjGroupUser_ListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUser_List);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppSecurityTransactionActionListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppSecurityTransactionActionExDto>>(o=>o.AppSecurityTransactionActionList);
            public static readonly string AppSefolderListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppSefolderExDto>>(o=>o.AppSefolderList);
            public static readonly string AppTransaction___ListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransaction___List);
            public static readonly string AppTransaction_ListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransaction_List);
            public static readonly string AppTransactionDataLoadListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionDataLoadExDto>>(o=>o.AppTransactionDataLoadList);
            public static readonly string AppTransactionDataTransferSettingListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionDataTransferSettingExDto>>(o=>o.AppTransactionDataTransferSettingList);
            public static readonly string AppTransactionFieldListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionFieldList);
            public static readonly string AppTransactionItemListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionItemExDto>>(o=>o.AppTransactionItemList);
            public static readonly string AppTransactionNavigationListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionNavigationExDto>>(o=>o.AppTransactionNavigationList);
            public static readonly string AppTransactionPostProcessListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionPostProcessExDto>>(o=>o.AppTransactionPostProcessList);
            public static readonly string AppTransactionSaveAsMappingListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionSaveAsMappingExDto>>(o=>o.AppTransactionSaveAsMappingList);
            public static readonly string AppTransactionUnitListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionUnitExDto>>(o=>o.AppTransactionUnitList);
            public static readonly string AppTransactionUnitLinkedSearchListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTransactionUnitLinkedSearchExDto>>(o=>o.AppTransactionUnitLinkedSearchList);
            public static readonly string AppTranscationReportListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTranscationReportExDto>>(o=>o.AppTranscationReportList);
            public static readonly string AppTrascationRecycleBinListProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  ObservableSet<AppTrascationRecycleBinExDto>>(o=>o.AppTrascationRecycleBinList); 
            public static readonly string ForeignAppBusinessMgtScopeProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppBusinessMgtScopeExDto>(o=>o.ForeignAppBusinessMgtScopeExDto);
            public static readonly string ForeignAppDataSourceRegisterProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppDataSourceRegisterExDto>(o=>o.ForeignAppDataSourceRegisterExDto);
            public static readonly string ForeignAppEntityInfoProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppEntityInfoExDto>(o=>o.ForeignAppEntityInfoExDto);
            public static readonly string ForeignAppFormProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppFormExDto>(o=>o.ForeignAppFormExDto);
            public static readonly string ForeignAppForm_Property = ObjectInfoHelper.GetName<AppTransactionExDto,  AppFormExDto>(o=>o.ForeignAppForm_ExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppProjectOrWorkFlow_Property = ObjectInfoHelper.GetName<AppTransactionExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlow_ExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransaction__Property = ObjectInfoHelper.GetName<AppTransactionExDto,  AppTransactionExDto>(o=>o.ForeignAppTransaction__ExDto);
            public static readonly string ForeignAppWebApiConfigProperty = ObjectInfoHelper.GetName<AppTransactionExDto,  AppWebApiConfigExDto>(o=>o.ForeignAppWebApiConfigExDto); 

        
        #endregion
	
	
        public AppTransactionExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppConditionalActionList = new  ObservableSet<AppConditionalActionExDto>();
            AppEsitePagesList = new  ObservableSet<AppEsitePagesExDto>();
            AppFormLinkTargetList = new  ObservableSet<AppFormLinkTargetExDto>();
            AppmessageNotificationSettingList = new  ObservableSet<AppmessageNotificationSettingExDto>();
            AppProjectOrTaskTranscationList = new  ObservableSet<AppProjectOrTaskTranscationExDto>();
            AppProjectOrWorkFlowList = new  ObservableSet<AppProjectOrWorkFlowExDto>();
            AppProjectWorkFlowAction_List = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowTaskList = new  ObservableSet<AppProjectWorkFlowTaskExDto>();
            AppSearchList = new  ObservableSet<AppSearchExDto>();
            AppSearchViewList = new  ObservableSet<AppSearchViewExDto>();
            AppSecuritySysObjGroupUser_List = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppSecurityTransactionActionList = new  ObservableSet<AppSecurityTransactionActionExDto>();
            AppSefolderList = new  ObservableSet<AppSefolderExDto>();
            AppTransaction___List = new  ObservableSet<AppTransactionExDto>();
            AppTransaction_List = new  ObservableSet<AppTransactionExDto>();
            AppTransactionDataLoadList = new  ObservableSet<AppTransactionDataLoadExDto>();
            AppTransactionDataTransferSettingList = new  ObservableSet<AppTransactionDataTransferSettingExDto>();
            AppTransactionFieldList = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionItemList = new  ObservableSet<AppTransactionItemExDto>();
            AppTransactionNavigationList = new  ObservableSet<AppTransactionNavigationExDto>();
            AppTransactionPostProcessList = new  ObservableSet<AppTransactionPostProcessExDto>();
            AppTransactionSaveAsMappingList = new  ObservableSet<AppTransactionSaveAsMappingExDto>();
            AppTransactionUnitList = new  ObservableSet<AppTransactionUnitExDto>();
            AppTransactionUnitLinkedSearchList = new  ObservableSet<AppTransactionUnitLinkedSearchExDto>();
            AppTranscationReportList = new  ObservableSet<AppTranscationReportExDto>();
            AppTrascationRecycleBinList = new  ObservableSet<AppTrascationRecycleBinExDto>(); 
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppConditionalActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppConditionalActionExDto> AppConditionalActionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppConditionalActionExDto>>(AppConditionalActionListProperty);    
            }
            set
            {
				SetValue(AppConditionalActionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsitePagesEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsitePagesExDto> AppEsitePagesList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsitePagesExDto>>(AppEsitePagesListProperty);    
            }
            set
            {
				SetValue(AppEsitePagesListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLinkTargetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLinkTargetExDto> AppFormLinkTargetList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLinkTargetExDto>>(AppFormLinkTargetListProperty);    
            }
            set
            {
				SetValue(AppFormLinkTargetListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppmessageNotificationSettingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppmessageNotificationSettingExDto> AppmessageNotificationSettingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppmessageNotificationSettingExDto>>(AppmessageNotificationSettingListProperty);    
            }
            set
            {
				SetValue(AppmessageNotificationSettingListProperty,value);
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
       public  ObservableSet<AppProjectOrWorkFlowExDto> AppProjectOrWorkFlowList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectOrWorkFlowExDto>>(AppProjectOrWorkFlowListProperty);    
            }
            set
            {
				SetValue(AppProjectOrWorkFlowListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowActionExDto> AppProjectWorkFlowAction_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowActionExDto>>(AppProjectWorkFlowAction_ListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowAction_ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchExDto> AppSearchList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchExDto>>(AppSearchListProperty);    
            }
            set
            {
				SetValue(AppSearchListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewExDto> AppSearchViewList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewExDto>>(AppSearchViewListProperty);    
            }
            set
            {
				SetValue(AppSearchViewListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecuritySysObjGroupUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecuritySysObjGroupUserExDto> AppSecuritySysObjGroupUser_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecuritySysObjGroupUserExDto>>(AppSecuritySysObjGroupUser_ListProperty);    
            }
            set
            {
				SetValue(AppSecuritySysObjGroupUser_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecuritySysObjGroupUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecuritySysObjGroupUserExDto> AppSecuritySysObjGroupUserList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecuritySysObjGroupUserExDto>>(AppSecuritySysObjGroupUserListProperty);    
            }
            set
            {
				SetValue(AppSecuritySysObjGroupUserListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityTransactionActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityTransactionActionExDto> AppSecurityTransactionActionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityTransactionActionExDto>>(AppSecurityTransactionActionListProperty);    
            }
            set
            {
				SetValue(AppSecurityTransactionActionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSefolderEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSefolderExDto> AppSefolderList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSefolderExDto>>(AppSefolderListProperty);    
            }
            set
            {
				SetValue(AppSefolderListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionExDto> AppTransaction___List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionExDto>>(AppTransaction___ListProperty);    
            }
            set
            {
				SetValue(AppTransaction___ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionDataLoadEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionDataLoadExDto> AppTransactionDataLoadList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionDataLoadExDto>>(AppTransactionDataLoadListProperty);    
            }
            set
            {
				SetValue(AppTransactionDataLoadListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionDataTransferSettingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionDataTransferSettingExDto> AppTransactionDataTransferSettingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionDataTransferSettingExDto>>(AppTransactionDataTransferSettingListProperty);    
            }
            set
            {
				SetValue(AppTransactionDataTransferSettingListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionFieldListProperty);    
            }
            set
            {
				SetValue(AppTransactionFieldListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionItemExDto> AppTransactionItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionItemExDto>>(AppTransactionItemListProperty);    
            }
            set
            {
				SetValue(AppTransactionItemListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionNavigationEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionNavigationExDto> AppTransactionNavigationList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionNavigationExDto>>(AppTransactionNavigationListProperty);    
            }
            set
            {
				SetValue(AppTransactionNavigationListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionPostProcessEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionPostProcessExDto> AppTransactionPostProcessList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionPostProcessExDto>>(AppTransactionPostProcessListProperty);    
            }
            set
            {
				SetValue(AppTransactionPostProcessListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionSaveAsMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionSaveAsMappingExDto> AppTransactionSaveAsMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionSaveAsMappingExDto>>(AppTransactionSaveAsMappingListProperty);    
            }
            set
            {
				SetValue(AppTransactionSaveAsMappingListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitExDto> AppTransactionUnitList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitExDto>>(AppTransactionUnitListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitLinkedSearchEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitLinkedSearchExDto> AppTransactionUnitLinkedSearchList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitLinkedSearchExDto>>(AppTransactionUnitLinkedSearchListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitLinkedSearchListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTranscationReportEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTranscationReportExDto> AppTranscationReportList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTranscationReportExDto>>(AppTranscationReportListProperty);    
            }
            set
            {
				SetValue(AppTranscationReportListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTrascationRecycleBinEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTrascationRecycleBinExDto> AppTrascationRecycleBinList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTrascationRecycleBinExDto>>(AppTrascationRecycleBinListProperty);    
            }
            set
            {
				SetValue(AppTrascationRecycleBinListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppBusinessMgtScopeEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppBusinessMgtScopeExDto ForeignAppBusinessMgtScopeExDto
        {
            get
            {
			    return  GetValue<AppBusinessMgtScopeExDto>(ForeignAppBusinessMgtScopeProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppBusinessMgtScopeProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDataSourceRegisterEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDataSourceRegisterExDto ForeignAppDataSourceRegisterExDto
        {
            get
            {
			    return  GetValue<AppDataSourceRegisterExDto>(ForeignAppDataSourceRegisterProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDataSourceRegisterProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppEntityInfoEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppEntityInfoExDto ForeignAppEntityInfoExDto
        {
            get
            {
			    return  GetValue<AppEntityInfoExDto>(ForeignAppEntityInfoProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppEntityInfoProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormExDto ForeignAppFormExDto
        {
            get
            {
			    return  GetValue<AppFormExDto>(ForeignAppFormProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppFormProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormExDto ForeignAppForm_ExDto
        {
            get
            {
			    return  GetValue<AppFormExDto>(ForeignAppForm_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppForm_Property,value);
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
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlow_ExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlow_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlow_Property,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionExDto ForeignAppTransaction__ExDto
        {
            get
            {
			    return  GetValue<AppTransactionExDto>(ForeignAppTransaction__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransaction__Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppWebApiConfigEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppWebApiConfigExDto ForeignAppWebApiConfigExDto
        {
            get
            {
			    return  GetValue<AppWebApiConfigExDto>(ForeignAppWebApiConfigProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppWebApiConfigProperty,value);
            }
        }	



        #endregion
        
    }
}

