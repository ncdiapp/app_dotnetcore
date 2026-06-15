using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class CalendarRepeatSettingDto
    {

        [DataMember(EmitDefaultValue = false)]
        public int? RepeatSimpleSettingValue
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? NbOccurrencesPerTimeUnit
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RepeatTimeUnit
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsWeekdayOnly
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RepeatMonthlySettingValue
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> RepeatWeeklySelectedWeekdays
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RepeatEndType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? RepeatEndDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RepeatEndAfterNbOccurrences
        {
            get;
            set;
        }
    }

}