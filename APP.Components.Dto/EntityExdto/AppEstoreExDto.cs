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
    /// DTO class for the  Extend Relation Entity 'AppEstore'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEstoreExDto : AppEstoreDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSearchView__Property = ObjectInfoHelper.GetName<AppEstoreExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchView__ExDto);
            public static readonly string ForeignAppSearchView_Property = ObjectInfoHelper.GetName<AppEstoreExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchView_ExDto);
            public static readonly string ForeignAppSearchViewProperty = ObjectInfoHelper.GetName<AppEstoreExDto,  AppSearchViewExDto>(o=>o.ForeignAppSearchViewExDto); 

        
        #endregion
	
	
        public AppEstoreExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSearchViewEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSearchViewExDto ForeignAppSearchView__ExDto
        {
            get
            {
			    return  GetValue<AppSearchViewExDto>(ForeignAppSearchView__Property ) ;    
            }
            set
            {
				SetValue(ForeignAppSearchView__Property,value);
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



        #endregion
        
    }
}

