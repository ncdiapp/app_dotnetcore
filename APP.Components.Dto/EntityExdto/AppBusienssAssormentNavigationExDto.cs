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
    /// DTO class for the  Extend Relation Entity 'AppBusienssAssormentNavigation'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusienssAssormentNavigationExDto : AppBusienssAssormentNavigationDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionGroupListProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationExDto,  ObservableSet<AppTransactionGroupExDto>>(o=>o.AppTransactionGroupList); 
 

        
        #endregion
	
	
        public AppBusienssAssormentNavigationExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionGroupList = new  ObservableSet<AppTransactionGroupExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionGroupEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionGroupExDto> AppTransactionGroupList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionGroupExDto>>(AppTransactionGroupListProperty);    
            }
            set
            {
				SetValue(AppTransactionGroupListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

