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
    /// DTO class for the  Extend Relation Entity 'AppBusinessMgtScope'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusinessMgtScopeExDto : AppBusinessMgtScopeDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSearchListProperty = ObjectInfoHelper.GetName<AppBusinessMgtScopeExDto,  ObservableSet<AppSearchExDto>>(o=>o.AppSearchList);
            public static readonly string AppTransactionListProperty = ObjectInfoHelper.GetName<AppBusinessMgtScopeExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransactionList); 
 

        
        #endregion
	
	
        public AppBusinessMgtScopeExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSearchList = new  ObservableSet<AppSearchExDto>();
            AppTransactionList = new  ObservableSet<AppTransactionExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchExDto> AppSearchList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchExDto>>(AppSearchListProperty);    
            }
            set
            {
				SetValue(AppSearchListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionExDto> AppTransactionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionExDto>>(AppTransactionListProperty);    
            }
            set
            {
				SetValue(AppTransactionListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

