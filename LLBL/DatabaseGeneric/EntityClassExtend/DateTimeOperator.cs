using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using APP.LBL;

using SD.LLBLGen.Pro.ORMSupportClasses;

namespace APP.LBL.EntityClasses
{
    [Serializable]
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
