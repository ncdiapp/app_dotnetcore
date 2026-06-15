using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{


    public partial class AppProjectWorkFlowTaskExDto
    {


        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowTaskExDto[] Children
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public Guid? MainTaskGuId
        {
            get;
            set;
        }

        public List<AppProjectWorkFlowTaskExDto> Sucessors { get; set; }




		[DataMember]
		public bool IsExternalChildSumaryTask
		{
			get;
			set;
		}

		//[DataMember]
		//public int CompletePercent
		//{
		//	get;
		//	set;
		//}

        [DataMember]
        public EmAppProjectTaskStage? EmAppProjectTaskStage
        {
            get;
            set;
        }

        [DataMember]
        public string TaskStageDisplay
        {
            get;
            set;
        }

        [DataMember]
        public string TaskProgressDisplay
        {
            get;
            set;
        }

        [DataMember]
        public string TaskStatusDisplay
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? CalStartDate
        {
            get;
            set;
        }

        [DataMember]
        public DateTime? CalEndDate
        {
            get;
            set;
        }

        [DataMember]
        public double CalDurationDays
        {
            get;
            set;
        }


        [DataMember]
        public bool IsCriticalPathTask
        {
            get;
            set;
        }

        [DataMember]
        public bool IsLastTask
        {
            get;
            set;
        }


        public List<string> NewSystemConversationMessageList
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsActiveForCurrentUser { get; set; }
    }
}

