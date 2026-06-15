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
    public class AppProjectWorkloadInputParameterDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? RangeStartDateId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RangeEndDateId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> UserIdList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> ProjectIdList
        {
            get;
            set;
        }
    }

    public class AppProjectWorkloadPivotDto
    {       

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<DateTime, List<AppProjectDateColumnDto>> DictWeekGroupAndDateList
        {
            get;
            set;
        }

        
        //public List<AppProjectTaskResourcePlannedHoursExDto> OrgTaskResourcePlannedHoursExDtoList
        //{
        //    get;
        //    set;
        //}

        public List<ProjectWorkloadPivotDataRowDto> ProjectWorkloadPivotRowDtoList
        {
            get;
            set;
        }

    }



    public class AppProjectDateColumnDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? DateId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? WorkDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DateDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime WeekGroupDate
        {
            get;
            set;
        }
    }

    public class ProjectWorkloadPivotDataRowDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TaskResourceId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? UserId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TaskId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ProjectId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double? TotalHours
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, double?> DictDateIdAndWorkhour
        {
            get;
            set;
        }
    }
}

