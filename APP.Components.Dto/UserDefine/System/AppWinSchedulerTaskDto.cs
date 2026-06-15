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

    public partial class AppWinSchedulerTaskDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TransactionRId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TaskName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TaskFolder
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FullPath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string NextRunTime
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Status
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string LastRunTime
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Schedule
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Author
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TaskToRun
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string StartIn
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RunAsUser
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ScheduleType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string StartTime
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string StartDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string EndDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Days
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Months
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RepeatEvery
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RepeatUntilTime
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RepeatUntilDuration
        {
            get;
            set;
        }


       
    }
   
}

