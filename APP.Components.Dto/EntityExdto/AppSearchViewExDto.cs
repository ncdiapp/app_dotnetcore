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
    /// DTO class for the  Extend Relation Entity 'AppSearchView'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchViewExDto : AppSearchViewDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppEsiteCatalogue__ListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppEsiteCatalogueExDto>>(o=>o.AppEsiteCatalogue__List);
            public static readonly string AppEsiteCatalogue_ListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppEsiteCatalogueExDto>>(o=>o.AppEsiteCatalogue_List);
            public static readonly string AppEsiteCatalogueListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppEsiteCatalogueExDto>>(o=>o.AppEsiteCatalogueList);
            public static readonly string AppFormListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppFormExDto>>(o=>o.AppFormList);
            public static readonly string AppFormLinkTargetListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTargetList);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList);
            public static readonly string AppSearch_ListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppSearchExDto>>(o=>o.AppSearch_List);
            public static readonly string AppSearchSavedListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppSearchSavedExDto>>(o=>o.AppSearchSavedList);
            public static readonly string AppSearchViewFieldListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppSearchViewFieldExDto>>(o=>o.AppSearchViewFieldList);
            public static readonly string AppSearchViewReportListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppSearchViewReportExDto>>(o=>o.AppSearchViewReportList);
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppTransactionNavigationListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppTransactionNavigationExDto>>(o=>o.AppTransactionNavigationList);
            public static readonly string AppTransactionUnitLinkedSearchListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppTransactionUnitLinkedSearchExDto>>(o=>o.AppTransactionUnitLinkedSearchList);
            public static readonly string AppViewFiledSearchFiledMappingListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppViewFiledSearchFiledMappingExDto>>(o=>o.AppViewFiledSearchFiledMappingList);
            public static readonly string AppViewLinkedSeaechOrUrlListProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  ObservableSet<AppViewLinkedSeaechOrUrlExDto>>(o=>o.AppViewLinkedSeaechOrUrlList); 
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto);
            public static readonly string ForeignAppSearch__Property = ObjectInfoHelper.GetName<AppSearchViewExDto,  AppSearchExDto>(o=>o.ForeignAppSearch__ExDto);
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppSearchViewExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppSearchViewExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppEsiteCatalogue__List = new  ObservableSet<AppEsiteCatalogueExDto>();
            AppEsiteCatalogue_List = new  ObservableSet<AppEsiteCatalogueExDto>();
            AppEsiteCatalogueList = new  ObservableSet<AppEsiteCatalogueExDto>();
            AppFormList = new  ObservableSet<AppFormExDto>();
            AppFormLinkTargetList = new  ObservableSet<AppFormLinkTargetExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>();
            AppSearch_List = new  ObservableSet<AppSearchExDto>();
            AppSearchSavedList = new  ObservableSet<AppSearchSavedExDto>();
            AppSearchViewFieldList = new  ObservableSet<AppSearchViewFieldExDto>();
            AppSearchViewReportList = new  ObservableSet<AppSearchViewReportExDto>();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppTransactionNavigationList = new  ObservableSet<AppTransactionNavigationExDto>();
            AppTransactionUnitLinkedSearchList = new  ObservableSet<AppTransactionUnitLinkedSearchExDto>();
            AppViewFiledSearchFiledMappingList = new  ObservableSet<AppViewFiledSearchFiledMappingExDto>();
            AppViewLinkedSeaechOrUrlList = new  ObservableSet<AppViewLinkedSeaechOrUrlExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsiteCatalogueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsiteCatalogueExDto> AppEsiteCatalogue__List
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsiteCatalogueExDto>>(AppEsiteCatalogue__ListProperty);    
            }
            set
            {
				SetValue(AppEsiteCatalogue__ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsiteCatalogueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsiteCatalogueExDto> AppEsiteCatalogue_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsiteCatalogueExDto>>(AppEsiteCatalogue_ListProperty);    
            }
            set
            {
				SetValue(AppEsiteCatalogue_ListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppEsiteCatalogueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppEsiteCatalogueExDto> AppEsiteCatalogueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppEsiteCatalogueExDto>>(AppEsiteCatalogueListProperty);    
            }
            set
            {
				SetValue(AppEsiteCatalogueListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormExDto> AppFormList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormExDto>>(AppFormListProperty);    
            }
            set
            {
				SetValue(AppFormListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchExDto> AppSearch_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchExDto>>(AppSearch_ListProperty);    
            }
            set
            {
				SetValue(AppSearch_ListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchViewReportEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchViewReportExDto> AppSearchViewReportList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchViewReportExDto>>(AppSearchViewReportListProperty);    
            }
            set
            {
				SetValue(AppSearchViewReportListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppViewLinkedSeaechOrUrlEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppViewLinkedSeaechOrUrlExDto> AppViewLinkedSeaechOrUrlList
        {
            get
            {
			    return  GetValue<ObservableSet<AppViewLinkedSeaechOrUrlExDto>>(AppViewLinkedSeaechOrUrlListProperty);    
            }
            set
            {
				SetValue(AppViewLinkedSeaechOrUrlListProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchExDto ForeignAppSearch__ExDto
        {
            get
            {
			    return  GetValue<AppSearchExDto>(ForeignAppSearch__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearch__Property,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchExDto ForeignAppSearchExDto
        {
            get
            {
			    return  GetValue<AppSearchExDto>(ForeignAppSearchProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchProperty,value);
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

