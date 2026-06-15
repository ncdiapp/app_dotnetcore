using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Components.Dto
{
    public struct DateTimeOperator
    {
        public string Operator, DateTimeUnit, Amount;

        public DateTimeOperator(string aOperator, string aDateTimeUnit, string aAmount)
        {
            Operator = aOperator;
            DateTimeUnit = aDateTimeUnit;
            Amount = aAmount;


        }



    }
}
