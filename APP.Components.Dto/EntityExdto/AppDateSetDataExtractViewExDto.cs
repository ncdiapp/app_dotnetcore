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
    /// DTO class for the  Extend Relation Entity 'AppDateSetDataExtractView'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDateSetDataExtractViewExDto : AppDateSetDataExtractViewDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto); 

        
        #endregion
	
	
        public AppDateSetDataExtractViewExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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



        #endregion
        
    }
}

