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
    /// DTO class for the  Extend Relation Entity 'AppFormGroup'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormGroupExDto : AppFormGroupDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionGroupItemListProperty = ObjectInfoHelper.GetName<AppFormGroupExDto,  ObservableSet<AppFormGroupItemExDto>>(o=>o.AppTransactionGroupItemList); 
 

        
        #endregion
	
	
        public AppFormGroupExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionGroupItemList = new  ObservableSet<AppFormGroupItemExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppFormGroupItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppFormGroupItemExDto> AppTransactionGroupItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppFormGroupItemExDto>>(AppTransactionGroupItemListProperty);    
            }
            set
            {
				SetValue(AppTransactionGroupItemListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

