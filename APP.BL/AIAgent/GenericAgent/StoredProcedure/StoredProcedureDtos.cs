using System;
using System.Collections.Generic;

namespace App.BL.AIAgent.GenericAgent.StoredProcedure
{
    public sealed class StoredProcedureListItem
    {
        public string Schema { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public sealed class StoredProcedureParameterDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Direction { get; set; }
        public int? MaxLength { get; set; }
        public int Ordinal { get; set; }
        public bool HasDefault { get; set; }
    }

    public sealed class StoredProcedureDetailDto
    {
        public int DataSourceId { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string Engine { get; set; }
        public List<StoredProcedureParameterDto> Parameters { get; set; } = new List<StoredProcedureParameterDto>();
        public string Definition { get; set; }
        public string UsageHint { get; set; }
    }
}
