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
    /// DTO class for the  Extend Relation Entity 'AppTransactionUnitSearchViewFieldMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitSearchViewFieldMappingExDto : AppTransactionUnitSearchViewFieldMappingDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppSearchViewFieldProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingExDto,  AppSearchViewFieldExDto>(o=>o.ForeignAppSearchViewFieldExDto);
            public static readonly string ForeignAppTransactionFieldProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionFieldExDto);
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto);
            public static readonly string ForeignAppTransactionUnitLinkedSearchProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingExDto,  AppTransactionUnitLinkedSearchExDto>(o=>o.ForeignAppTransactionUnitLinkedSearchExDto); 

        
        #endregion
	
	
        public AppTransactionUnitSearchViewFieldMappingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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

