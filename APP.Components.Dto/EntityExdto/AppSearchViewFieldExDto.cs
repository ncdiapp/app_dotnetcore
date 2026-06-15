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
    /// DTO class for the  Extend Relation Entity 'AppSearchViewField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchViewFieldExDto : AppSearchViewFieldDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppFormLayoutItemListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppFormLayoutItemExDto>>(o=>o.AppFormLayoutItemList);
            public static readonly string AppFormLinkTarget___ListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTarget___List);
            public static readonly string AppFormLinkTarget____ListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTarget____List);
            public static readonly string AppFormLinkTarget__ListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTarget__List);
            public static readonly string AppFormLinkTargetListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTargetList);
            public static readonly string AppFormLinkTarget_ListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTarget_List);
            public static readonly string AppSearchViewReportParamterMappingListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppSearchViewReportParamterMappingExDto>>(o=>o.AppSearchViewReportParamterMappingList);
            public static readonly string AppTransactionUnitSearchViewFieldMappingListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>>(o=>o.AppTransactionUnitSearchViewFieldMappingList);
            public static readonly string AppViewFiledSearchFiledMappingListProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  ObservableSet<AppViewFiledSearchFiledMappingExDto>>(o=>o.AppViewFiledSearchFiledMappingList); 
            public static readonly string ForeignAppEntityInfoProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  AppEntityInfoExDto>(o=>o.ForeignAppEntityInfoExDto);
            public static readonly string ForeignAppSearchField_Property = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchField_ExDto);
            public static readonly string ForeignAppSearchFieldProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchFieldExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppTransactionField_Property = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField_ExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppSearchViewFieldExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto); 

        
        #endregion
	
	
        public AppSearchViewFieldExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppFormLayoutItemList = new  ObservableSet<AppFormLayoutItemExDto>();
            AppFormLinkTarget___List = new  ObservableSet<AppFormLinkTargetExDto>();
            AppFormLinkTarget____List = new  ObservableSet<AppFormLinkTargetExDto>();
            AppFormLinkTarget__List = new  ObservableSet<AppFormLinkTargetExDto>();
            AppFormLinkTargetList = new  ObservableSet<AppFormLinkTargetExDto>();
            AppFormLinkTarget_List = new  ObservableSet<AppFormLinkTargetExDto>();
            AppSearchViewReportParamterMappingList = new  ObservableSet<AppSearchViewReportParamterMappingExDto>();
            AppTransactionUnitSearchViewFieldMappingList = new  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>();
            AppViewFiledSearchFiledMappingList = new  ObservableSet<AppViewFiledSearchFiledMappingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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
       public  ObservableSet<AppFormLinkTargetExDto> AppFormLinkTarget___List
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLinkTargetExDto>>(AppFormLinkTarget___ListProperty);    
            }
            set
            {
				SetValue(AppFormLinkTarget___ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLinkTargetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLinkTargetExDto> AppFormLinkTarget____List
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLinkTargetExDto>>(AppFormLinkTarget____ListProperty);    
            }
            set
            {
				SetValue(AppFormLinkTarget____ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLinkTargetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLinkTargetExDto> AppFormLinkTarget__List
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLinkTargetExDto>>(AppFormLinkTarget__ListProperty);    
            }
            set
            {
				SetValue(AppFormLinkTarget__ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormLinkTargetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormLinkTargetExDto> AppFormLinkTarget_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormLinkTargetExDto>>(AppFormLinkTarget_ListProperty);    
            }
            set
            {
				SetValue(AppFormLinkTarget_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewReportParamterMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewReportParamterMappingExDto> AppSearchViewReportParamterMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewReportParamterMappingExDto>>(AppSearchViewReportParamterMappingListProperty);    
            }
            set
            {
				SetValue(AppSearchViewReportParamterMappingListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppViewFiledSearchFiledMappingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppViewFiledSearchFiledMappingExDto> AppViewFiledSearchFiledMappingList
        {
            get
            {
			    return  GetValue<ObservableSet<AppViewFiledSearchFiledMappingExDto>>(AppViewFiledSearchFiledMappingListProperty);    
            }
            set
            {
				SetValue(AppViewFiledSearchFiledMappingListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchFieldExDto ForeignAppSearchField_ExDto
        {
            get
            {
			    return  GetValue<AppSearchFieldExDto>(ForeignAppSearchField_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchField_Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchFieldExDto ForeignAppSearchFieldExDto
        {
            get
            {
			    return  GetValue<AppSearchFieldExDto>(ForeignAppSearchFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchFieldProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewExDto ForeignAppSearchViewExDto
        {
            get
            {
			    return  GetValue<AppSearchViewExDto>(ForeignAppSearchViewProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionField_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField_Property,value);
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



        #endregion
        
    }
}

