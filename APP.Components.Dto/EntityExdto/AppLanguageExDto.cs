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
    /// DTO class for the  Extend Relation Entity 'AppLanguage'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppLanguageExDto : AppLanguageDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppLanguageKeyListProperty = ObjectInfoHelper.GetName<AppLanguageExDto,  ObservableSet<AppLanguageKeyExDto>>(o=>o.AppLanguageKeyList); 
 

        
        #endregion
	
	
        public AppLanguageExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppLanguageKeyList = new  ObservableSet<AppLanguageKeyExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppLanguageKeyEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppLanguageKeyExDto> AppLanguageKeyList
        {
            get
            {
			    return  GetValue<ObservableSet<AppLanguageKeyExDto>>(AppLanguageKeyListProperty);    
            }
            set
            {
				SetValue(AppLanguageKeyListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

