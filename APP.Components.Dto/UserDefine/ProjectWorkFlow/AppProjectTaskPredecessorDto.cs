using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppProjectTaskPredecessor'.
    /// </summary>

    public partial class AppProjectTaskPredecessorDto  
    {

        [DataMember]
        public Guid? PredecessorGuid
        {
            get;
            set;
        }

        [DataMember]
        public Guid? CurrentTaskGuid
        {
            get;
            set;
        }



		//[DataMember(EmitDefaultValue = false)]
		public AppProjectWorkFlowTaskExDto AppProjectWorkFlowTaskPredecessorExDto
		{
			get;set;
		}

		public AppProjectWorkFlowTaskExDto AppProjectWorkFlowTaskExDto
		{
			get;set;
		}


		
        //public List<AppProjectWorkFlowActionExDto> GoToNextStepConditionActionList
        //{
        //    get;
        //    set;
        //}



        [DataMember]
        public string PathLabel
        {
            get;
            set;
        }

    }
}

