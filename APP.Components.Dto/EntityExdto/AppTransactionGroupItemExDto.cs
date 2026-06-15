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
    /// DTO class for the  Extend Relation Entity 'AppTransactionGroupItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionGroupItemExDto : AppTransactionGroupItemDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppTransactionGroupProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemExDto,  AppTransactionGroupExDto>(o=>o.ForeignAppTransactionGroupExDto);
            public static readonly string ForeignAppTransactionItemProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemExDto,  AppTransactionItemExDto>(o=>o.ForeignAppTransactionItemExDto); 

        
        #endregion
	
	
        public AppTransactionGroupItemExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionGroupEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionGroupExDto ForeignAppTransactionGroupExDto
        {
            get
            {
			    return  GetValue<AppTransactionGroupExDto>(ForeignAppTransactionGroupProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionGroupProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppTransactionItemEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppTransactionItemExDto ForeignAppTransactionItemExDto
        {
            get
            {
			    return  GetValue<AppTransactionItemExDto>(ForeignAppTransactionItemProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionItemProperty,value);
            }
        }	



        #endregion
        
    }
}

