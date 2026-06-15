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
    /// DTO class for the  Extend Relation Entity 'AppWebApiConfig'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWebApiConfigExDto : AppWebApiConfigDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppDataSetListProperty = ObjectInfoHelper.GetName<AppWebApiConfigExDto,  ObservableSet<AppDataSetExDto>>(o=>o.AppDataSetList);
            public static readonly string AppTransactionListProperty = ObjectInfoHelper.GetName<AppWebApiConfigExDto,  ObservableSet<AppTransactionExDto>>(o=>o.AppTransactionList);
            public static readonly string AppWebApiParamsHeaderSettigListProperty = ObjectInfoHelper.GetName<AppWebApiConfigExDto,  ObservableSet<AppWebApiParamsHeaderSettigExDto>>(o=>o.AppWebApiParamsHeaderSettigList); 
            public static readonly string ForeignAppWebApiProviderProperty = ObjectInfoHelper.GetName<AppWebApiConfigExDto,  AppWebApiProviderExDto>(o=>o.ForeignAppWebApiProviderExDto); 

        
        #endregion
	
	
        public AppWebApiConfigExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppDataSetList = new  ObservableSet<AppDataSetExDto>();
            AppTransactionList = new  ObservableSet<AppTransactionExDto>();
            AppWebApiParamsHeaderSettigList = new  ObservableSet<AppWebApiParamsHeaderSettigExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppDataSetEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppDataSetExDto> AppDataSetList
        {
            get
            {
			    return  GetValue<ObservableSet<AppDataSetExDto>>(AppDataSetListProperty);    
            }
            set
            {
				SetValue(AppDataSetListProperty,value);
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

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppWebApiParamsHeaderSettigEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppWebApiParamsHeaderSettigExDto> AppWebApiParamsHeaderSettigList
        {
            get
            {
			    return  GetValue<ObservableSet<AppWebApiParamsHeaderSettigExDto>>(AppWebApiParamsHeaderSettigListProperty);    
            }
            set
            {
				SetValue(AppWebApiParamsHeaderSettigListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppWebApiProviderEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppWebApiProviderExDto ForeignAppWebApiProviderExDto
        {
            get
            {
			    return  GetValue<AppWebApiProviderExDto>(ForeignAppWebApiProviderProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppWebApiProviderProperty,value);
            }
        }	



        #endregion
        
    }
}

