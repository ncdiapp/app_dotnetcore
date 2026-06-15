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
    /// DTO class for the  Extend Relation Entity 'AppTransactionFieldAggFunction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionFieldAggFunctionExDto : AppTransactionFieldAggFunctionDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionFieldListProperty = ObjectInfoHelper.GetName<AppTransactionFieldAggFunctionExDto,  ObservableSet<AppTransactionFieldExDto>>(o=>o.AppTransactionFieldList); 
            public static readonly string ForeignAppTransactionField_Property = ObjectInfoHelper.GetName<AppTransactionFieldAggFunctionExDto,  AppTransactionFieldExDto>(o=>o.ForeignAppTransactionField_ExDto); 

        
        #endregion
	
	
        public AppTransactionFieldAggFunctionExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionFieldList = new  ObservableSet<AppTransactionFieldExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionFieldEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionFieldExDto> AppTransactionFieldList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionFieldExDto>>(AppTransactionFieldListProperty);    
            }
            set
            {
				SetValue(AppTransactionFieldListProperty,value);
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



        #endregion
        
    }
}

