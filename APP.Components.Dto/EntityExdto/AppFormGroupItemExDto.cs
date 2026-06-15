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
    /// DTO class for the  Extend Relation Entity 'AppFormGroupItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormGroupItemExDto : AppFormGroupItemDto 
    {

     	#region Static Name Properties Declaration
    
 
            public static readonly string ForeignAppFormProperty = ObjectInfoHelper.GetName<AppFormGroupItemExDto,  AppFormExDto>(o=>o.ForeignAppFormExDto);
            public static readonly string ForeignAppTransactionGroupProperty = ObjectInfoHelper.GetName<AppFormGroupItemExDto,  AppFormGroupExDto>(o=>o.ForeignAppTransactionGroupExDto); 

        
        #endregion
	
	
        public AppFormGroupItemExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties




		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormExDto ForeignAppFormExDto
        {
            get
            {
			    return  GetValue<AppFormExDto>(ForeignAppFormProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppFormProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppFormGroupEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppFormGroupExDto ForeignAppTransactionGroupExDto
        {
            get
            {
			    return  GetValue<AppFormGroupExDto>(ForeignAppTransactionGroupProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppTransactionGroupProperty,value);
            }
        }	



        #endregion
        
    }
}

