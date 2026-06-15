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
    /// DTO class for the  Extend Relation Entity 'AppSearchField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchFieldExDto : AppSearchFieldDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppFormLinkTarget__ListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTarget__List);
            public static readonly string AppFormLinkTarget_ListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTarget_List);
            public static readonly string AppFormLinkTargetListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppFormLinkTargetExDto>>(o=>o.AppFormLinkTargetList);
            public static readonly string AppSearchSavedValueListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppSearchSavedValueExDto>>(o=>o.AppSearchSavedValueList);
            public static readonly string AppSearchViewField_ListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppSearchViewFieldExDto>>(o=>o.AppSearchViewField_List);
            public static readonly string AppSearchViewFieldListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppSearchViewFieldExDto>>(o=>o.AppSearchViewFieldList);
            public static readonly string AppTransactionUnitSearchFieldMappingListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>>(o=>o.AppTransactionUnitSearchFieldMappingList);
            public static readonly string AppViewFiledSearchFiledMappingListProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  ObservableSet<AppViewFiledSearchFiledMappingExDto>>(o=>o.AppViewFiledSearchFiledMappingList); 
            public static readonly string ForeignAppEntityInfoProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  AppEntityInfoExDto>(o=>o.ForeignAppEntityInfoExDto);
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppSearchFieldExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto); 

        
        #endregion
	
	
        public AppSearchFieldExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppFormLinkTarget__List = new  ObservableSet<AppFormLinkTargetExDto>();
            AppFormLinkTarget_List = new  ObservableSet<AppFormLinkTargetExDto>();
            AppFormLinkTargetList = new  ObservableSet<AppFormLinkTargetExDto>();
            AppSearchSavedValueList = new  ObservableSet<AppSearchSavedValueExDto>();
            AppSearchViewField_List = new  ObservableSet<AppSearchViewFieldExDto>();
            AppSearchViewFieldList = new  ObservableSet<AppSearchViewFieldExDto>();
            AppTransactionUnitSearchFieldMappingList = new  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>();
            AppViewFiledSearchFiledMappingList = new  ObservableSet<AppViewFiledSearchFiledMappingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchSavedValueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchSavedValueExDto> AppSearchSavedValueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchSavedValueExDto>>(AppSearchSavedValueListProperty);    
            }
            set
            {
				SetValue(AppSearchSavedValueListProperty,value);
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



        #endregion
        
    }
}

