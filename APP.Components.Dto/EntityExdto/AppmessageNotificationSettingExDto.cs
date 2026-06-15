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
    /// DTO class for the  Extend Relation Entity 'AppmessageNotificationSetting'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppmessageNotificationSettingExDto : AppmessageNotificationSettingDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppmessageNotificationSetting_ListProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingExDto,  ObservableSet<AppmessageNotificationSettingExDto>>(o=>o.AppmessageNotificationSetting_List); 
            public static readonly string ForeignAppmessageNotificationSettingProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingExDto,  AppmessageNotificationSettingExDto>(o=>o.ForeignAppmessageNotificationSettingExDto);
            public static readonly string ForeignAppTransactionProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingExDto,  AppTransactionExDto>(o=>o.ForeignAppTransactionExDto); 

        
        #endregion
	
	
        public AppmessageNotificationSettingExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppmessageNotificationSetting_List = new  ObservableSet<AppmessageNotificationSettingExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppmessageNotificationSettingEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppmessageNotificationSettingExDto> AppmessageNotificationSetting_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppmessageNotificationSettingExDto>>(AppmessageNotificationSetting_ListProperty);    
            }
            set
            {
				SetValue(AppmessageNotificationSetting_ListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppmessageNotificationSettingEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppmessageNotificationSettingExDto ForeignAppmessageNotificationSettingExDto
        {
            get
            {
			    return  GetValue<AppmessageNotificationSettingExDto>(ForeignAppmessageNotificationSettingProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppmessageNotificationSettingProperty,value);
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

