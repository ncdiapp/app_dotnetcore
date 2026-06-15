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
    /// DTO class for the  Extend Relation Entity 'AppTransactionUnitLinkedSearch'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitLinkedSearchExDto : AppTransactionUnitLinkedSearchDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSecuritySysObjGroupUserListProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  ObservableSet<AppSecuritySysObjGroupUserExDto>>(o=>o.AppSecuritySysObjGroupUserList);
            public static readonly string AppTransactionUnitSearchFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>>(o=>o.AppTransactionUnitSearchFieldMappingList);
            public static readonly string AppTransactionUnitSearchViewFieldMappingListProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>>(o=>o.AppTransactionUnitSearchViewFieldMappingList); 
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppSearchSavedProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  AppSearchSavedExDto>(o=>o.ForeignAppSearchSavedExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitLinkedSearchExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppTransactionUnitLinkedSearchExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSecuritySysObjGroupUserList = new  ObservableSet<AppSecuritySysObjGroupUserExDto>();
            AppTransactionUnitSearchFieldMappingList = new  ObservableSet<AppTransactionUnitSearchFieldMappingExDto>();
            AppTransactionUnitSearchViewFieldMappingList = new  ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchSavedEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchSavedExDto ForeignAppSearchSavedExDto
        {
            get
            {
			    return  GetValue<AppSearchSavedExDto>(ForeignAppSearchSavedProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchSavedProperty,value);
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

