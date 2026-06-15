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
    /// DTO class for the  Extend Relation Entity 'AppTransactionField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionFieldExDto : AppTransactionFieldDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppConditionalAction_ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalAction_List);
            public static readonly string AppConditionalAction__ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalAction__List);
            public static readonly string AppConditionalActionListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalActionList);
            public static readonly string AppConditionalAction___ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalAction___List);
            public static readonly string AppFormGridLayoutItemBindFieldListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppFormGridLayoutItemBindFieldExDto>>(o=>o.AppFormGridLayoutItemBindFieldList);
            public static readonly string AppFormLayoutItemListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppFormLayoutItemExDto>>(o=>o.AppFormLayoutItemList);
            public static readonly string AppProjectWorkFlowAction__ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowAction__List);
            public static readonly string AppProjectWorkFlowAction_ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowAction_List);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppProjectWorkFlowConditionListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppProjectWorkFlowConditionExDto>>(o=>o.AppProjectWorkFlowConditionList);
            public static readonly string AppSearchViewField_ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppSearchViewFieldExDto>>(o=>o.AppSearchViewField_List);
            public static readonly string AppSearchViewFieldListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppSearchViewFieldExDto>>(o=>o.AppSearchViewFieldList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppTransactionField_________ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_________List);
            public static readonly string AppTransactionField_____ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_____List);
            public static readonly string AppTransactionField___ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField___List);
            public static readonly string AppTransactionField_______ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_______List);
            public static readonly string AppTransactionField_ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_List);
            public static readonly string AppTransactionField___________ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField___________List);
            public static readonly string AppTransactionField_____________ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionField_____________List);
            public static readonly string AppTransactionFieldAggFunction_ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionFieldAggFunctionExDto>>(o=>o.AppTransactionFieldAggFunction_List);
            public static readonly string AppTransactionSaveAsMapping_ListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionSaveAsMappingExDto>>(o=>o.AppTransactionSaveAsMapping_List);
            public static readonly string AppTransactionSaveAsMappingListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionSaveAsMappingExDto>>(o=>o.AppTransactionSaveAsMappingList);
            public static readonly string AppTransactionUnitExtendFieldValueListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionUnitExtendFieldValueExDto>>(o=>o.AppTransactionUnitExtendFieldValueList);
            public static readonly string AppTransactionUnitSearchFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>>(o=>o.AppTransactionUnitSearchFieldMappingList);
            public static readonly string AppTransactionUnitSearchViewFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>>(o=>o.AppTransactionUnitSearchViewFieldMappingList); 
            public static readonly string ForeignAppEntityInfoProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppEntityInfoExDto>(o=>o.ForeignAppEntityInfoExDto);
            public static readonly string ForeignAppFormLayoutItem_Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppFormLayoutItemExDto>(o=>o.ForeignAppFormLayoutItem_ExDto);
            public static readonly string ForeignAppProjectWorkFlowAction___Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppProjectWorkFlowActionExDto>(o=>o.ForeignAppProjectWorkFlowAction___ExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionField________Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField________ExDto);
            public static readonly string ForeignAppTransactionField____Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField____ExDto);
            public static readonly string ForeignAppTransactionField__Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField__ExDto);
            public static readonly string ForeignAppTransactionField______Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField______ExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionField__________Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField__________ExDto);
            public static readonly string ForeignAppTransactionField____________Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField____________ExDto);
            public static readonly string ForeignAppTransactionFieldAggFunctionProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionFieldAggFunctionExDto>(o=>o.ForeignAppTransactionFieldAggFunctionExDto);
            public static readonly string ForeignAppTransactionUnit_Property = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnit_ExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionFieldExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppTransactionFieldExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppConditionalAction_List = new  ObservableSet<AppConditionalActionExDto>();
            AppConditionalAction__List = new  ObservableSet<AppConditionalActionExDto>();
            AppConditionalActionList = new  ObservableSet<AppConditionalActionExDto>();
            AppConditionalAction___List = new  ObservableSet<AppConditionalActionExDto>();
            AppFormGridLayoutItemBindFieldList = new  ObservableSet<AppFormGridLayoutItemBindFieldExDto>();
            AppFormLayoutItemList = new  ObservableSet<AppFormLayoutItemExDto>();
            AppProjectWorkFlowAction__List = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowAction_List = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppProjectWorkFlowConditionList = new  ObservableSet<AppProjectWorkFlowConditionExDto>();
            AppSearchViewField_List = new  ObservableSet<AppSearchViewFieldExDto>();
            AppSearchViewFieldList = new  ObservableSet<AppSearchViewFieldExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppTransactionField_________List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionField_____List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionField___List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionField_______List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionField_List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionField___________List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionField_____________List = new  ObservableSet<AppTransactionFieldExDto>();
            AppTransactionFieldAggFunction_List = new  ObservableSet<AppTransactionFieldAggFunctionExDto>();
            AppTransactionSaveAsMapping_List = new  ObservableSet<AppTransactionSaveAsMappingExDto>();
            AppTransactionSaveAsMappingList = new  ObservableSet<AppTransactionSaveAsMappingExDto>();
            AppTransactionUnitExtendFieldValueList = new  ObservableSet<AppTransactionUnitExtendFieldValueExDto>();
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppConditionalActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppConditionalActionExDto> AppConditionalAction___List
        {
            get
            {
			    return  GetValue<ObservableSet<AppConditionalActionExDto>>(AppConditionalAction___ListProperty);    
            }
            set
            {
				SetValue(AppConditionalAction___ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowActionExDto> AppProjectWorkFlowAction__List
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowActionExDto>>(AppProjectWorkFlowAction__ListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowAction__ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewFieldExDto> AppSearchViewField_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewFieldExDto>>(AppSearchViewField_ListProperty);    
            }
            set
            {
				SetValue(AppSearchViewField_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewFieldExDto> AppSearchViewFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewFieldExDto>>(AppSearchViewFieldListProperty);    
            }
            set
            {
				SetValue(AppSearchViewFieldListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField_________List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField_________ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField_________ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField_____List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField_____ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField_____ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField___List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField___ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField___ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField_______List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField_______ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField_______ListProperty,value);
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
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField___________List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField___________ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField___________ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionField_____________List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionField_____________ListProperty);    
            }
            set
            {
				SetValue(AppTransactionField_____________ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldAggFunctionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldAggFunctionExDto> AppTransactionFieldAggFunction_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldAggFunctionExDto>>(AppTransactionFieldAggFunction_ListProperty);    
            }
            set
            {
				SetValue(AppTransactionFieldAggFunction_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionSaveAsMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionSaveAsMappingExDto> AppTransactionSaveAsMapping_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionSaveAsMappingExDto>>(AppTransactionSaveAsMapping_ListProperty);    
            }
            set
            {
				SetValue(AppTransactionSaveAsMapping_ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitExtendFieldValueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitExtendFieldValueExDto> AppTransactionUnitExtendFieldValueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitExtendFieldValueExDto>>(AppTransactionUnitExtendFieldValueListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitExtendFieldValueListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormLayoutItemEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormLayoutItemExDto ForeignAppFormLayoutItem_ExDto
        {
            get
            {
			    return  GetValue<AppFormLayoutItemExDto>(ForeignAppFormLayoutItem_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppFormLayoutItem_Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowActionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowActionExDto ForeignAppProjectWorkFlowAction___ExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowActionExDto>(ForeignAppProjectWorkFlowAction___Property ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowAction___Property,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField________ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField________Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField________Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField____ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField____Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField____Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField__ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField__Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField______ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField______Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField______Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionFieldExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionFieldProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField__________ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField__________Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField__________Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField____________ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField____________Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField____________Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldAggFunctionEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldAggFunctionExDto ForeignAppTransactionFieldAggFunctionExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldAggFunctionExDto>(ForeignAppTransactionFieldAggFunctionProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionFieldAggFunctionProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitExDto ForeignAppTransactionUnit_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnit_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnit_Property,value);
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

