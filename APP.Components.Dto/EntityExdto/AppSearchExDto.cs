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
    /// DTO class for the  Extend Relation Entity 'AppSearch'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchExDto : AppSearchDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppApplicationAssetsItemListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppApplicationAssetsItemExDto>>(o=>o.AppApplicationAssetsItemList);
            public static readonly string AppFormLinkTargetListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTargetList);
            public static readonly string AppSearchFieldListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppSearchFieldExDto>>(o=>o.AppSearchFieldList);
            public static readonly string AppSearchParameterListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppSearchParameterExDto>>(o=>o.AppSearchParameterList);
            public static readonly string AppSearchSavedListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppSearchSavedExDto>>(o=>o.AppSearchSavedList);
            public static readonly string AppSearchView__ListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppSearchViewExDto>>(o=>o.AppSearchView__List);
            public static readonly string AppSearchViewListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppSearchViewExDto>>(o=>o.AppSearchViewList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppTransactionNavigationListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppTransactionNavigationExDto>>(o=>o.AppTransactionNavigationList);
            public static readonly string AppTransactionUnitLinkedSearchListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppTransactionUnitLinkedSearchExDto>>(o=>o.AppTransactionUnitLinkedSearchList);
            public static readonly string AppViewFiledSearchFiledMappingListProperty = ObjectInfoHelper.GetName<AppSearchExDto,  ObservableSet<AppViewFiledSearchFiledMappingExDto>>(o=>o.AppViewFiledSearchFiledMappingList); 
            public static readonly string ForeignAppBusinessMgtScopeProperty = ObjectInfoHelper.GetName<AppSearchExDto,  AppBusinessMgtScopeExDto>(o=>o.ForeignAppBusinessMgtScopeExDto);
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppSearchExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto);
            public static readonly string ForeignAppListMenuProperty = ObjectInfoHelper.GetName<AppSearchExDto,  AppListMenuExDto>(o=>o.ForeignAppListMenuExDto);
            public static readonly string ForeignAppSearchView_Property = ObjectInfoHelper.GetName<AppSearchExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchView_ExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppSearchExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppSearchExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppApplicationAssetsItemList = new  ObservableSet<AppApplicationAssetsItemExDto>();
            AppFormLinkTargetList = new  ObservableSet<AppFormLinkTargetExDto>();
            AppSearchFieldList = new  ObservableSet<AppSearchFieldExDto>();
            AppSearchParameterList = new  ObservableSet<AppSearchParameterExDto>();
            AppSearchSavedList = new  ObservableSet<AppSearchSavedExDto>();
            AppSearchView__List = new  ObservableSet<AppSearchViewExDto>();
            AppSearchViewList = new  ObservableSet<AppSearchViewExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppTransactionNavigationList = new  ObservableSet<AppTransactionNavigationExDto>();
            AppTransactionUnitLinkedSearchList = new  ObservableSet<AppTransactionUnitLinkedSearchExDto>();
            AppViewFiledSearchFiledMappingList = new  ObservableSet<AppViewFiledSearchFiledMappingExDto>(); 
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchFieldExDto> AppSearchFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchFieldExDto>>(AppSearchFieldListProperty);    
            }
            set
            {
				SetValue(AppSearchFieldListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchParameterEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchParameterExDto> AppSearchParameterList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchParameterExDto>>(AppSearchParameterListProperty);    
            }
            set
            {
				SetValue(AppSearchParameterListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchSavedEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchSavedExDto> AppSearchSavedList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchSavedExDto>>(AppSearchSavedListProperty);    
            }
            set
            {
				SetValue(AppSearchSavedListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewExDto> AppSearchView__List
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewExDto>>(AppSearchView__ListProperty);    
            }
            set
            {
				SetValue(AppSearchView__ListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDataSetExDto ForeignAppDataSetExDto
        {
            get
            {
			    return  GetValue<AppDataSetExDto>(ForeignAppDataSetProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDataSetProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewExDto ForeignAppSearchView_ExDto
        {
            get
            {
			    return  GetValue<AppSearchViewExDto>(ForeignAppSearchView_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchView_Property,value);
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

