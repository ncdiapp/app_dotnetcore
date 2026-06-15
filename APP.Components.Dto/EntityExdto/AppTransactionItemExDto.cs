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
    /// DTO class for the  Extend Relation Entity 'AppTransactionItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionItemExDto : AppTransactionItemDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionGroupItemListProperty = ObjectInfoHelper.GetName<AppTransactionItemExDto,  ObservableSet<AppTransactionGroupItemExDto>>(o=>o.AppTransactionGroupItemList); 
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppTransactionItemExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppTransactionItemExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionGroupItemList = new  ObservableSet<AppTransactionGroupItemExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionGroupItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionGroupItemExDto> AppTransactionGroupItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionGroupItemExDto>>(AppTransactionGroupItemListProperty);    
            }
            set
            {
				SetValue(AppTransactionGroupItemListProperty,value);
            }
        }

		
		

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



        #endregion
        
    }
}

