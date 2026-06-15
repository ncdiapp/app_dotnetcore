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
    /// DTO class for the  Extend Relation Entity 'AppModuleLibRegister'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppModuleLibRegisterExDto : AppModuleLibRegisterDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppCompanyOrderModuleListProperty = ObjectInfoHelper.GetName<AppModuleLibRegisterExDto,  ObservableSet<AppCompanyOrderModuleExDto>>(o=>o.AppCompanyOrderModuleList); 
 

        
        #endregion
	
	
        public AppModuleLibRegisterExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppCompanyOrderModuleList = new  ObservableSet<AppCompanyOrderModuleExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCompanyOrderModuleEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCompanyOrderModuleExDto> AppCompanyOrderModuleList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCompanyOrderModuleExDto>>(AppCompanyOrderModuleListProperty);    
            }
            set
            {
				SetValue(AppCompanyOrderModuleListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

