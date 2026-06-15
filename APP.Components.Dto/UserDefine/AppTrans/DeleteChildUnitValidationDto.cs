using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public class DeleteChildUnitValidationDto
    {
        public int? UnitId
        {
            get;
            set;
        }

        public List<object> PrimaryKeyValues
        {
            get;
            set;
        }
    }
}
