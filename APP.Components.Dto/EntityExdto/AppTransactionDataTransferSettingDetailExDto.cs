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
    /// DTO class for the  Extend Relation Entity 'AppTransactionDataTransferSettingDetail'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionDataTransferSettingDetailExDto : AppTransactionDataTransferSettingDetailDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppTransactionDataTransferSettingProperty = ObjectInfoHelper.GetName<AppTransactionDataTransferSettingDetailExDto,  AppTransactionDataTransferSettingExDto>(o=>o.ForeignAppTransactionDataTransferSettingExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppTransactionDataTransferSettingDetailExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto); 

        
        #endregion
	
	
        public AppTransactionDataTransferSettingDetailExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionDataTransferSettingEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionDataTransferSettingExDto ForeignAppTransactionDataTransferSettingExDto
        {
            get
            {
			    return  GetValue<AppTransactionDataTransferSettingExDto>(ForeignAppTransactionDataTransferSettingProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionDataTransferSettingProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionFieldExDto ForeignAppTransactionFieldExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionFieldProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionFieldProperty,value);
            }
        }	



        #endregion
        
    }
}

