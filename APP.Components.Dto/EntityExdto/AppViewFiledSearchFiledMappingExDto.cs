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
    /// DTO class for the  Extend Relation Entity 'AppViewFiledSearchFiledMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppViewFiledSearchFiledMappingExDto : AppViewFiledSearchFiledMappingDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto);
            public static readonly string ForeignAppSearchFieldProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchFieldExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto);
            public static readonly string ForeignAppSearchViewFieldProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewFieldExDto); 

        
        #endregion
	
	
        public AppViewFiledSearchFiledMappingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewFieldExDto ForeignAppSearchViewFieldExDto
        {
            get
            {
			    return  GetValue<AppSearchViewFieldExDto>(ForeignAppSearchViewFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchViewFieldProperty,value);
            }
        }	



        #endregion
        
    }
}

