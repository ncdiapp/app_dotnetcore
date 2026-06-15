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
    /// DTO class for the  Extend Relation Entity 'AppTransactionSaveAsMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionSaveAsMappingExDto : AppTransactionSaveAsMappingDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto);
            public static readonly string ForeignAppTransactionDataTransferSettingProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingExDto,  AppTransactionDataTransferSettingExDto>(o=>o.ForeignAppTransactionDataTransferSettingExDto);
            public static readonly string ForeignAppTransactionField_Property = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField_ExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppTransactionSaveAsMappingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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
        public  AppTransactionFieldExDto ForeignAppTransactionField_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionFieldExDto>(ForeignAppTransactionField_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionField_Property,value);
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

