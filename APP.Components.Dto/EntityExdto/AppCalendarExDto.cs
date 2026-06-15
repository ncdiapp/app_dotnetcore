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
    /// DTO class for the  Extend Relation Entity 'AppCalendar'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCalendarExDto : AppCalendarDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppCalendarRecurringDayListProperty = ObjectInfoHelper.GetName<AppCalendarExDto,  ObservableSet<AppCalendarRecurringDayExDto>>(o=>o.AppCalendarRecurringDayList);
            public static readonly string AppCalendarSpecificDayListProperty = ObjectInfoHelper.GetName<AppCalendarExDto,  ObservableSet<AppCalendarSpecificDayExDto>>(o=>o.AppCalendarSpecificDayList);
            public static readonly string AppSecurityUser_ListProperty = ObjectInfoHelper.GetName<AppCalendarExDto,  ObservableSet<AppSecurityUserExDto>>(o=>o.AppSecurityUser_List); 
            public static readonly string ForeignAppSecurityUserProperty = ObjectInfoHelper.GetName<AppCalendarExDto,  AppSecurityUserExDto>(o=>o.ForeignAppSecurityUserExDto); 

        
        #endregion
	
	
        public AppCalendarExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppCalendarRecurringDayList = new  ObservableSet<AppCalendarRecurringDayExDto>();
            AppCalendarSpecificDayList = new  ObservableSet<AppCalendarSpecificDayExDto>();
            AppSecurityUser_List = new  ObservableSet<AppSecurityUserExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCalendarRecurringDayEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCalendarRecurringDayExDto> AppCalendarRecurringDayList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCalendarRecurringDayExDto>>(AppCalendarRecurringDayListProperty);    
            }
            set
            {
				SetValue(AppCalendarRecurringDayListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppCalendarSpecificDayEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppCalendarSpecificDayExDto> AppCalendarSpecificDayList
        {
            get
            {
			    return  GetValue<ObservableSet<AppCalendarSpecificDayExDto>>(AppCalendarSpecificDayListProperty);    
            }
            set
            {
				SetValue(AppCalendarSpecificDayListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppSecurityUserExDto> AppSecurityUser_List
        {
            get
            {
			    return  GetValue<ObservableSet<AppSecurityUserExDto>>(AppSecurityUser_ListProperty);    
            }
            set
            {
				SetValue(AppSecurityUser_ListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityUserEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityUserExDto ForeignAppSecurityUserExDto
        {
            get
            {
			    return  GetValue<AppSecurityUserExDto>(ForeignAppSecurityUserProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityUserProperty,value);
            }
        }	



        #endregion
        
    }
}

