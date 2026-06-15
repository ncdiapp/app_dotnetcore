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
    /// DTO class for the  Extend Relation Entity 'AppWebApiProvider'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWebApiProviderExDto : AppWebApiProviderDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppWebApiConfigListProperty = ObjectInfoHelper.GetName<AppWebApiProviderExDto,  ObservableSet<AppWebApiConfigExDto>>(o=>o.AppWebApiConfigList); 
 

        
        #endregion
	
	
        public AppWebApiProviderExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppWebApiConfigList = new  ObservableSet<AppWebApiConfigExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppWebApiConfigEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppWebApiConfigExDto> AppWebApiConfigList
        {
            get
            {
			    return  GetValue<ObservableSet<AppWebApiConfigExDto>>(AppWebApiConfigListProperty);    
            }
            set
            {
				SetValue(AppWebApiConfigListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

