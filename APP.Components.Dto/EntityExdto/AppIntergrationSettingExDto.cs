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
    /// DTO class for the  Extend Relation Entity 'AppIntergrationSetting'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppIntergrationSettingExDto : AppIntergrationSettingDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppIntergrationSettingParameterListProperty = ObjectInfoHelper.GetName<AppIntergrationSettingExDto,  ObservableSet<AppIntergrationSettingParameterExDto>>(o=>o.AppIntergrationSettingParameterList); 
 

        
        #endregion
	
	
        public AppIntergrationSettingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppIntergrationSettingParameterList = new  ObservableSet<AppIntergrationSettingParameterExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppIntergrationSettingParameterEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppIntergrationSettingParameterExDto> AppIntergrationSettingParameterList
        {
            get
            {
			    return  GetValue<ObservableSet<AppIntergrationSettingParameterExDto>>(AppIntergrationSettingParameterListProperty);    
            }
            set
            {
				SetValue(AppIntergrationSettingParameterListProperty,value);
            }
        }

		
		
	



        #endregion
        
    }
}

