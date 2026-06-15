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
    /// DTO class for the  Extend Relation Entity 'AppTransactionUnitFormula'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitFormulaExDto : AppTransactionUnitFormulaDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitFormulaExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto);
            public static readonly string ForeignAppTransactionUnit_Property = ObjectInfoHelper.GetName<AppTransactionUnitFormulaExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnit_ExDto); 

        
        #endregion
	
	
        public AppTransactionUnitFormulaExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionUnitEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionUnitExDto ForeignAppTransactionUnit_ExDto
        {
            get
            {
			    return  GetValue<AppTransactionUnitExDto>(ForeignAppTransactionUnit_Property ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionUnit_Property,value);
            }
        }	



        #endregion
        
    }
}

