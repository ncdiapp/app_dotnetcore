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


    public partial class AppProjectWorkFlowTaskDto
    {

        public string ProjectName { get; set; }

        [DataMember]
        public string ResourceDisplay
        {
            get; set;
        }



        [DataMember]
        public DateTime DueDate
        {
            get; set;
        }

        [DataMember]
        public string DueDateDisplay
        {
            get; set;
        }        


        [DataMember]
        public int? EmAppTaskSystemDefinedCategory
        {
            get; set;
        }


        [DataMember]
        public string CategoryDetailDisplay
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int TimeZoneOffset
        {
            get;
            set;
        }

        [DataMember]
        public int? WorkflowTransactionId
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string WorkflowTransactionRId
        {
            get;
            set;
        }      

        [DataMember(EmitDefaultValue = false)]
        public double TotalDays
        {
            get
            {
                if (DatePlannedStart.HasValue && DatePlannedEnd.HasValue)
                {
                    return (DatePlannedEnd.Value - DatePlannedStart.Value).TotalDays;
                }

                return 0;
            }
            set {

            }
        }


       

    }
}

