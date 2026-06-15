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
    /// DTO class for the  Extend Relation Entity 'AppProjectTaskResource'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTaskResourceExDto : AppProjectTaskResourceDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppProjectTaskResourcePlannedHoursListProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceExDto,  ObservableSet<AppProjectTaskResourcePlannedHoursExDto>>(o=>o.AppProjectTaskResourcePlannedHoursList); 
            public static readonly string ForeignAppProjectWorkFlowTaskProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTaskExDto); 

        
        #endregion
	
	
        public AppProjectTaskResourceExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppProjectTaskResourcePlannedHoursList = new  ObservableSet<AppProjectTaskResourcePlannedHoursExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectTaskResourcePlannedHoursEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectTaskResourcePlannedHoursExDto> AppProjectTaskResourcePlannedHoursList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectTaskResourcePlannedHoursExDto>>(AppProjectTaskResourcePlannedHoursListProperty);    
            }
            set
            {
				SetValue(AppProjectTaskResourcePlannedHoursListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectWorkFlowTaskEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectWorkFlowTaskExDto ForeignAppProjectWorkFlowTaskExDto
        {
            get
            {
			    return  GetValue<AppProjectWorkFlowTaskExDto>(ForeignAppProjectWorkFlowTaskProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectWorkFlowTaskProperty,value);
            }
        }	



        #endregion
        
    }
}

