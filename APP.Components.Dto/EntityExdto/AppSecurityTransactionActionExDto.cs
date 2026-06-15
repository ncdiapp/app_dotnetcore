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
    /// DTO class for the  Extend Relation Entity 'AppSecurityTransactionAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityTransactionActionExDto : AppSecurityTransactionActionDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSecurityFormActionResourceListProperty = ObjectInfoHelper.GetName<AppSecurityTransactionActionExDto,  ObservableSet<AppSecurityTransactionActionResourceExDto>>(o=>o.AppSecurityFormActionResourceList); 
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppSecurityTransactionActionExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppSecurityTransactionActionExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSecurityFormActionResourceList = new  ObservableSet<AppSecurityTransactionActionResourceExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityTransactionActionResourceEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityTransactionActionResourceExDto> AppSecurityFormActionResourceList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityTransactionActionResourceExDto>>(AppSecurityFormActionResourceListProperty);    
            }
            set
            {
				SetValue(AppSecurityFormActionResourceListProperty,value);
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

