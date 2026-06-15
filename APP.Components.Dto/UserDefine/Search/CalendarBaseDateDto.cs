using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APP.Framework;
using System.Runtime.Serialization;

namespace APP.Components.Dto
{
    public class CalendarBaseDateDto : ObservableObject
    {
        //Day
        //Day_Desc
        //Week
        //Week_Desc
        //Bi_Week
        //Bi_Week_Desc
        //Hlf_Month
        //Hlf_Month_Desc
        //Month
        //Month_Desc
        //Quarter
        //Quarter_Desc
        //Pln_Hlf_Yr
        //Pln_Hlf_Yr_Desc
        //Pln_Yr
        //Pln_Yr_Desc
        //Fiscal_Hlf_Yr
        //Fiscal_Hlf_Yr_Desc
        //Fiscal_Yr
        //Fiscal_Yr_Desc
        //Range_Period
        //Range_Period_Desc

        [DataMember]
        public int? Day { get; set; }

        [DataMember]
        public DateTime? Day_Desc { get; set; }

        [DataMember]
        public string Week { get; set; }

        [DataMember]
        public string Week_Desc { get; set; }

        [DataMember]
        public string Bi_Week { get; set; }

        [DataMember]
        public string Bi_Week_Desc { get; set; }

        [DataMember]
        public string Hlf_Month { get; set; }

        [DataMember]
        public string Hlf_Month_Desc { get; set; }

        [DataMember]
        public string Month { get; set; }

        [DataMember]
        public string Month_Desc { get; set; }

        [DataMember]
        public string Quarter { get; set; }

        [DataMember]
        public string Quarter_Desc { get; set; }

        [DataMember]
        public string Pln_Hlf_Yr { get; set; }

        [DataMember]
        public string Pln_Hlf_Yr_Desc { get; set; }

        [DataMember]
        public string Pln_Yr { get; set; }

        [DataMember]
        public string Pln_Yr_Desc { get; set; }

        [DataMember]
        public string Fiscal_Hlf_Yr { get; set; }

        [DataMember]
        public string Fiscal_Hlf_Yr_Desc { get; set; }

        [DataMember]
        public string Fiscal_Yr { get; set; }

        [DataMember]
        public string Fiscal_Yr_Desc { get; set; }

        [DataMember]
        public string Range_Period { get; set; }

        [DataMember]
        public string Range_Period_Desc { get; set; }

    }
}
