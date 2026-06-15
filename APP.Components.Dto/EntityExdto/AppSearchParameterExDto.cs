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
    /// DTO class for the  Extend Relation Entity 'AppSearchParameter'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchParameterExDto : AppSearchParameterDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppDataSetParameterProperty = ObjectInfoHelper.GetName<AppSearchParameterExDto,  AppDataSetParameterExDto>(o=>o.ForeignAppDataSetParameterExDto);
            public static readonly string ForeignAppSearchProperty = ObjectInfoHelper.GetName<AppSearchParameterExDto,  AppSearchExDto>(o=>o.ForeignAppSearchExDto); 

        
        #endregion
	
	
        public AppSearchParameterExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDataSetParameterEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDataSetParameterExDto ForeignAppDataSetParameterExDto
        {
            get
            {
			    return  GetValue<AppDataSetParameterExDto>(ForeignAppDataSetParameterProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDataSetParameterProperty,value);
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

