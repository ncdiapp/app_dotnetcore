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
    /// DTO class for the  Extend Relation Entity 'AppTransactionGroup'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionGroupExDto : AppTransactionGroupDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppTransactionGroupItemListProperty = ObjectInfoHelper.GetName<AppTransactionGroupExDto,  ObservableSet<AppTransactionGroupItemExDto>>(o=>o.AppTransactionGroupItemList);
            public static readonly string AppTransactionGroupSessionListProperty = ObjectInfoHelper.GetName<AppTransactionGroupExDto,  ObservableSet<AppTransactionGroupSessionExDto>>(o=>o.AppTransactionGroupSessionList); 
            public static readonly string ForeignAppBusienssAssormentNavigationProperty = ObjectInfoHelper.GetName<AppTransactionGroupExDto,  AppBusienssAssormentNavigationExDto>(o=>o.ForeignAppBusienssAssormentNavigationExDto); 

        
        #endregion
	
	
        public AppTransactionGroupExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppTransactionGroupItemList = new  ObservableSet<AppTransactionGroupItemExDto>();
            AppTransactionGroupSessionList = new  ObservableSet<AppTransactionGroupSessionExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionGroupItemEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionGroupItemExDto> AppTransactionGroupItemList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionGroupItemExDto>>(AppTransactionGroupItemListProperty);    
            }
            set
            {
				SetValue(AppTransactionGroupItemListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionGroupSessionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppTransactionGroupSessionExDto> AppTransactionGroupSessionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppTransactionGroupSessionExDto>>(AppTransactionGroupSessionListProperty);    
            }
            set
            {
				SetValue(AppTransactionGroupSessionListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppBusienssAssormentNavigationEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppBusienssAssormentNavigationExDto ForeignAppBusienssAssormentNavigationExDto
        {
            get
            {
			    return  GetValue<AppBusienssAssormentNavigationExDto>(ForeignAppBusienssAssormentNavigationProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppBusienssAssormentNavigationProperty,value);
            }
        }	



        #endregion
        
    }
}

