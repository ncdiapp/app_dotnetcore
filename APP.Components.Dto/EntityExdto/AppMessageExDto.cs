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
    /// DTO class for the  Extend Relation Entity 'AppMessage'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppMessageExDto : AppMessageDto 
    {

     	#region Static Name Properties Declaration
    
            public static readonly string AppConditionalActionListProperty = ObjectInfoHelper.GetName<AppMessageExDto,  ObservableSet<AppConditionalActionExDto>>(o=>o.AppConditionalActionList);
            public static readonly string AppMessageDeletedListProperty = ObjectInfoHelper.GetName<AppMessageExDto,  ObservableSet<AppMessageDeletedExDto>>(o=>o.AppMessageDeletedList);
            public static readonly string AppMessageUserReceivedListProperty = ObjectInfoHelper.GetName<AppMessageExDto,  ObservableSet<AppMessageUserReceivedExDto>>(o=>o.AppMessageUserReceivedList);
            public static readonly string AppProjectWorkFlowActionListProperty = ObjectInfoHelper.GetName<AppMessageExDto,  ObservableSet<AppProjectWorkFlowActionExDto>>(o=>o.AppProjectWorkFlowActionList); 
            public static readonly string ForeignAppProjectOrWorkFlowProperty = ObjectInfoHelper.GetName<AppMessageExDto,  AppProjectOrWorkFlowExDto>(o=>o.ForeignAppProjectOrWorkFlowExDto);
            public static readonly string ForeignAppProjectWorkFlowTaskProperty = ObjectInfoHelper.GetName<AppMessageExDto,  AppProjectWorkFlowTaskExDto>(o=>o.ForeignAppProjectWorkFlowTaskExDto);
            public static readonly string ForeignAppSecurityGroupProperty = ObjectInfoHelper.GetName<AppMessageExDto,  AppSecurityGroupExDto>(o=>o.ForeignAppSecurityGroupExDto); 

        
        #endregion
	
	
        public AppMessageExDto()
        {        
        }
		
		protected override void OnInitialize()
		{
			base.OnInitialize();
            AppConditionalActionList = new  ObservableSet<AppConditionalActionExDto>();
            AppMessageDeletedList = new  ObservableSet<AppMessageDeletedExDto>();
            AppMessageUserReceivedList = new  ObservableSet<AppMessageUserReceivedExDto>();
            AppProjectWorkFlowActionList = new  ObservableSet<AppProjectWorkFlowActionExDto>(); 
			 OnInitialized();
   		}
		 partial void OnInitialized();
   
        #region  Ex Relation Entity Dto Properties



        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppConditionalActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppConditionalActionExDto> AppConditionalActionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppConditionalActionExDto>>(AppConditionalActionListProperty);    
            }
            set
            {
				SetValue(AppConditionalActionListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppMessageDeletedEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppMessageDeletedExDto> AppMessageDeletedList
        {
            get
            {
			    return  GetValue<ObservableSet<AppMessageDeletedExDto>>(AppMessageDeletedListProperty);    
            }
            set
            {
				SetValue(AppMessageDeletedListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppMessageUserReceivedEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppMessageUserReceivedExDto> AppMessageUserReceivedList
        {
            get
            {
			    return  GetValue<ObservableSet<AppMessageUserReceivedExDto>>(AppMessageUserReceivedListProperty);    
            }
            set
            {
				SetValue(AppMessageUserReceivedListProperty,value);
            }
        }

        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppProjectWorkFlowActionEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
       [DataMember(EmitDefaultValue=false),EditableMemberAttribute]
       public  ObservableSet<AppProjectWorkFlowActionExDto> AppProjectWorkFlowActionList
        {
            get
            {
			    return  GetValue<ObservableSet<AppProjectWorkFlowActionExDto>>(AppProjectWorkFlowActionListProperty);    
            }
            set
            {
				SetValue(AppProjectWorkFlowActionListProperty,value);
            }
        }

		
		

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppProjectOrWorkFlowEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppProjectOrWorkFlowExDto ForeignAppProjectOrWorkFlowExDto
        {
            get
            {
			    return  GetValue<AppProjectOrWorkFlowExDto>(ForeignAppProjectOrWorkFlowProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppProjectOrWorkFlowProperty,value);
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

        /// <summary> Gets the Entity Dto  with the related entities of type 'AppSecurityGroupEntity' which are related to this entity via a relation of type 'm:1'.
         /// If the Entity Dto  hasn't been fetched yet, It returned will be empty.</summary>
     	[DataMember(EmitDefaultValue=false)]
        public  AppSecurityGroupExDto ForeignAppSecurityGroupExDto
        {
            get
            {
			    return  GetValue<AppSecurityGroupExDto>(ForeignAppSecurityGroupProperty ) ;    
            }
            set
            {
				SetValue(ForeignAppSecurityGroupProperty,value);
            }
        }	



        #endregion
        
    }
}

