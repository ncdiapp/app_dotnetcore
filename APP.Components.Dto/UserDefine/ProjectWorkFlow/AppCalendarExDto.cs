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

    public class AppCalendarViewExDto
    {

        public int? CalenarId
        {
            get; set;
        }

        public int? UserId
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? ViewStartDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? ViewEndDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppCalendarSpecificDayDto> CalendarDaysList
        {
            get;
            set;
        }

        

    }


}

