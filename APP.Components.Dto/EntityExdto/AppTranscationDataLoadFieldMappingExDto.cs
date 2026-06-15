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
    /// DTO class for the  Extend Relation Entity 'AppTranscationDataLoadFieldMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTranscationDataLoadFieldMappingExDto : AppTranscationDataLoadFieldMappingDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppTransactionDataLoadProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingExDto,  AppTransactionDataLoadExDto>(o=>o.ForeignAppTransactionDataLoadExDto); 

        
        #endregion
	
	
        public AppTranscationDataLoadFieldMappingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionDataLoadEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionDataLoadExDto ForeignAppTransactionDataLoadExDto
        {
            get
            {
			    return  GetValue<AppTransactionDataLoadExDto>(ForeignAppTransactionDataLoadProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionDataLoadProperty,value);
            }
        }	



        #endregion
        
    }
}

