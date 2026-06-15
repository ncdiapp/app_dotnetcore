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
    /// DTO class for the  Extend Relation Entity 'AppDataSetParameter'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDataSetParameterExDto : AppDataSetParameterDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppSearchParameterListProperty = ObjectInfoHelper.GetName<AppDataSetParameterExDto,  ObservableSet<AppSearchParameterExDto>>(o=>o.AppSearchParameterList); 
            public static readonly string ForeignAppDataSetProperty = ObjectInfoHelper.GetName<AppDataSetParameterExDto,  AppDataSetExDto>(o=>o.ForeignAppDataSetExDto); 

        
        #endregion
	
	
        public AppDataSetParameterExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppSearchParameterList = new  ObservableSet<AppSearchParameterExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSearchParameterEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSearchParameterExDto> AppSearchParameterList
        {
            get
            {
			    return  GetValue<ObservableSet<AppSearchParameterExDto>>(AppSearchParameterListProperty);    
            }
            set
            {
				SetValue(AppSearchParameterListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppDataSetExDto ForeignAppDataSetExDto
        {
            get
            {
			    return  GetValue<AppDataSetExDto>(ForeignAppDataSetProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppDataSetProperty,value);
            }
        }	



        #endregion
        
    }
}

