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
    /// DTO class for the  Extend Relation Entity 'AppTransactionUnitExtendField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitExtendFieldExDto : AppTransactionUnitExtendFieldDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionUnitExtendFieldValueListProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldExDto,  ObservableSet<AppTransactionUnitExtendFieldValueExDto>>(o=>o.AppTransactionUnitExtendFieldValueList); 
            public static readonly string ForeignAppTransactionUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldExDto,  AppTransactionUnitExDto>(o=>o.ForeignAppTransactionUnitExDto); 

        
        #endregion
	
	
        public AppTransactionUnitExtendFieldExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionUnitExtendFieldValueList = new  ObservableSet<AppTransactionUnitExtendFieldValueExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionUnitExtendFieldValueEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionUnitExtendFieldValueExDto> AppTransactionUnitExtendFieldValueList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionUnitExtendFieldValueExDto>>(AppTransactionUnitExtendFieldValueListProperty);    
            }
            set
            {
				SetValue(AppTransactionUnitExtendFieldValueListProperty,value);
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

