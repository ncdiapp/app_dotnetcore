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

    public partial class AppProjectOrWorkFlowExDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowTaskExDto[] RootTreeList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<string>> DictGuidKeyPredecessorList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int TimeZoneOffset
        {
            get;
            set;
        }

        public string DurationDisplay
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public List<AppProjectWorkFlowPathDto> PathList
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        public List<AppProjectTaskPredecessorExDto> PredecessorList
        {
            get;
            set;
        }

        //  [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, Dictionary<int, List<AppProjectWorkFlowActionExDto>>> DictFromTaskIdToTaskIdAndActionList
        {
            get;
            set;
        }



        public Dictionary<int, AppCalendarExDto> DictUserIdUserCalendar
        {
            get;
            set;
        }

        public Dictionary<int, AppCalendarExDto> DictUserIdCopmanyCalendar
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowTaskExDto CurrentTask
        {
            get;
            set;
        }


       

        [DataMember(EmitDefaultValue = false)]
        public DateTime? CalcProjectStartDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? CalcProjectEndDate
        {
            get;
            set;
        }

        public Dictionary<int, AppTransactionExDto> DictTransIdAndTransExDto
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> WorkflowTransactionLookUpList
        {
            get;
            set;
        }


		[DataMember(EmitDefaultValue = false)]
		public int?  ExtractMainTaskId
		{
			get;
			set;
		}

        public int? ExcludeCalculationTaskId
        {
            get;
            set;
        }

	}
}

