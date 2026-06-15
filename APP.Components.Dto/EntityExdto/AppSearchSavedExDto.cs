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
    /// DTO class for the  Extend Relation Entity 'AppSearchSaved'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchSavedExDto : AppSearchSavedDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSearchSavedValueListProperty = ObjectInfoHelper.GetName<AppSearchSavedExDto,  ObservableSet<AppSearchSavedValueExDto>>(o=>o.AppSearchSavedValueList);
            public static readonly string AppTransactionUnitLinkedSearchListProperty = ObjectInfoHelper.GetName<AppSearchSavedExDto,  ObservableSet<AppTransactionUnitLinkedSearchExDto>>(o=>o.AppTransactionUnitLinkedSearchList); 
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppSearchSavedExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppSearchSavedExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppSecurityGroupProperty = ObjectInfoHelper.GetName<AppSearchSavedExDto,  AppSecurityGroupExDto>(o=>o.ForeignAppSecurityGroupExDto);
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppSearchSavedExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto); 

        
        #endregion
	
	
        public AppSearchSavedExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSearchSavedValueList = new  ObservableSet<AppSearchSavedValueExDto>();
            AppTransactionUnitLinkedSearchList = new  ObservableSet<AppTransactionUnitLinkedSearchExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityGroupEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityGroupExDto ForeignAppSecurityGroupExDto
        {
            get
            {
			    return  GetValue<AppSecurityGroupExDto>(ForeignAppSecurityGroupProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityGroupProperty,value);
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



        #endregion
        
    }
}

