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
    /// DTO class for the  Extend Relation Entity 'AppTransactionUnitSearchFieldMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitSearchFieldMappingExDto : AppTransactionUnitSearchFieldMappingDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSearchFieldProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchFieldMappingExDto,  AppSearchFieldExDto>(o=>o.ForeignAppSearchFieldExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchFieldMappingExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchFieldMappingExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto);
            public static readonly string ForeignAppTransactionUnitLinkedSearchProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchFieldMappingExDto,  AppTransactionUnitLinkedSearchExDto>(o=>o.ForeignAppTransactionUnitLinkedSearchExDto); 

        
        #endregion
	
	
        public AppTransactionUnitSearchFieldMappingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitLinkedSearchEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitLinkedSearchExDto ForeignAppTransactionUnitLinkedSearchExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitLinkedSearchExDto>(ForeignAppTransactionUnitLinkedSearchProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnitLinkedSearchProperty,value);
            }
        }	



        #endregion
        
    }
}

