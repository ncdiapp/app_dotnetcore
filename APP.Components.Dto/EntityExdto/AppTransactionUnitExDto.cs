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
    /// DTO class for the  Extend Relation Entity 'AppTransactionUnit'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitExDto : AppTransactionUnitDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppConditionalAction_ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalAction_List);
            public static readonly string AppConditionalAction__ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalAction__List);
            public static readonly string AppConditionalActionListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalActionList);
            public static readonly string AppFormGridLayoutItemBindField_ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(o=>o.AppFormGridLayoutItemBindField_List);
            public static readonly string AppFormGridLayoutItemBindFieldListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(o=>o.AppFormGridLayoutItemBindFieldList);
            public static readonly string AppFormLayoutItemListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppFormLayoutItemExDto>>(o=>o.AppFormLayoutItemList);
            public static readonly string AppFormLinkTargetListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTargetList);
            public static readonly string AppProjectWorkFlowConditionListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppProjectWorkFlowConditionExDto>>(o=>o.AppProjectWorkFlowConditionList);
            public static readonly string AppSearchViewListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppSearchViewExDto>>(o=>o.AppSearchViewList);
            public static readonly string AppSecuritySysObjGroupUser_ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUser_List);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppTransactionDataLoadListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionDataLoadExDto>>(o=>o.AppTransactionDataLoadList);
            public static readonly string AppTransactionField_ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_List);
            public static readonly string AppTransactionFieldListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionFieldList);
            public static readonly string AppTransactionPostProcessListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionPostProcessExDto>>(o=>o.AppTransactionPostProcessList);
            public static readonly string AppTransactionSaveAsMappingListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionSaveAsMappingExDto>>(o=>o.AppTransactionSaveAsMappingList);
            public static readonly string AppTransactionUnit_ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitExDto>>(o=>o.AppTransactionUnit_List);
            public static readonly string AppTransactionUnitDeleteFlowListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitDeleteFlowExDto>>(o=>o.AppTransactionUnitDeleteFlowList);
            public static readonly string AppTransactionUnitFormulaListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitFormulaExDto>>(o=>o.AppTransactionUnitFormulaList);
            public static readonly string AppTransactionUnitFormula_ListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitFormulaExDto>>(o=>o.AppTransactionUnitFormula_List);
            public static readonly string AppTransactionUnitLinkedSearchListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitLinkedSearchExDto>>(o=>o.AppTransactionUnitLinkedSearchList);
            public static readonly string AppTransactionUnitSearchFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>>(o=>o.AppTransactionUnitSearchFieldMappingList);
            public static readonly string AppTransactionUnitSearchViewFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>>(o=>o.AppTransactionUnitSearchViewFieldMappingList); 
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppTransactionUnitExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppConditionalAction_List = new  ObservableSet<AppConditionalActionExDto>();
            AppConditionalAction__List = new  ObservableSet<AppConditionalActionExDto>();
            AppConditionalActionList = new  ObservableSet<AppConditionalActionExDto>();
            AppFormGridLayoutItemBindField_List = new  ObservableSet<AppFormGridLayoutItemBindFieldExDto>();
            AppFormGridLayoutItemBindFieldList = new  ObservableSet<AppFormGridLayoutItemBindFieldExDto>();
            AppFormLayoutItemList = new  ObservableSet<AppFormLayoutItemExDto>();
            AppFormLinkTargetList = new  ObservableSet<AppFormLinkTargetExDto>();
            AppProjectWorkFlowConditionList = new  ObservableSet<AppProjectWorkFlowConditionExDto>();
            AppSearchViewList = new  ObservableSet<AppSearchViewExDto>();
            AppSecuritySysObjGroupUser_List = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppTransactionDataLoadList = new  ObservableSet<AppTransactionDataLoadExDto>();
            AppTransactionField_List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionFieldList = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionPostProcessList = new  ObservableSet<AppTransactionPostProcessExDto>();
            AppTransactionSaveAsMappingList = new  ObservableSet<AppTransactionSaveAsMappingExDto>();
            AppTransactionUnit_List = new  ObservableSet<AppTransactionUnitExDto>();
            AppTransactionUnitDeleteFlowList = new  ObservableSet<AppTransactionUnitDeleteFlowExDto>();
            AppTransactionUnitFormulaList = new  ObservableSet<AppTransactionUnitFormulaExDto>();
            AppTransactionUnitFormula_List = new  ObservableSet<AppTransactionUnitFormulaExDto>();
            AppTransactionUnitLinkedSearchList = new  ObservableSet<AppTransactionUnitLinkedSearchExDto>();
            AppTransactionUnitSearchFieldMappingList = new  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>();
            AppTransactionUnitSearchViewFieldMappingList = new  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppConditionalActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppConditionalActionExDto> AppConditionalAction_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppConditionalActionExDto>>(AppConditionalAction_ListProperty);    
            }
            set
            {
				SetValue(AppConditionalAction_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppConditionalActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppConditionalActionExDto> AppConditionalAction__List
        {
            get
            {
			    return  GetValue<ObservableSet<AppConditionalActionExDto>>(AppConditionalAction__ListProperty);    
            }
            set
            {
				SetValue(AppConditionalAction__ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormGridLayoutItemBindFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormGridLayoutItemBindFieldExDto> AppFormGridLayoutItemBindField_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(AppFormGridLayoutItemBindField_ListProperty);    
            }
            set
            {
				SetValue(AppFormGridLayoutItemBindField_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormGridLayoutItemBindFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormGridLayoutItemBindFieldExDto> AppFormGridLayoutItemBindFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(AppFormGridLayoutItemBindFieldListProperty);    
            }
            set
            {
				SetValue(AppFormGridLayoutItemBindFieldListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLayoutItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLayoutItemExDto> AppFormLayoutItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLayoutItemExDto>>(AppFormLayoutItemListProperty);    
            }
            set
            {
				SetValue(AppFormLayoutItemListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField_ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField_ListProperty,value);
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
       public  ObservableSet<AppTransactionUnitExDto> AppTransactionUnit_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitExDto>>(AppTransactionUnit_ListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnit_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitDeleteFlowEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitDeleteFlowExDto> AppTransactionUnitDeleteFlowList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitDeleteFlowExDto>>(AppTransactionUnitDeleteFlowListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitDeleteFlowListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitFormulaEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitFormulaExDto> AppTransactionUnitFormulaList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitFormulaExDto>>(AppTransactionUnitFormulaListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitFormulaListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitFormulaEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitFormulaExDto> AppTransactionUnitFormula_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitFormulaExDto>>(AppTransactionUnitFormula_ListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitFormula_ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitSearchFieldMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitSearchFieldMappingExDto> AppTransactionUnitSearchFieldMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitSearchFieldMappingExDto>>(AppTransactionUnitSearchFieldMappingListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitSearchFieldMappingListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitSearchViewFieldMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto> AppTransactionUnitSearchViewFieldMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>>(AppTransactionUnitSearchViewFieldMappingListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitSearchViewFieldMappingListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitExDto ForeignAppTransactionUnitExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnitProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnitProperty,value);
            }
        }	



        #endregion
        
    }
}

