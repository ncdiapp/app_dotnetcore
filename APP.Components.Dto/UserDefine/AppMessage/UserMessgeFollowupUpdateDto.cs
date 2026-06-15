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
   
    public partial class UserMessgeFollowupUpdateDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<int> FollowupUserIdList
        {
            get;
            set;
        }        

        [DataMember(EmitDefaultValue = false)]
        public EmAppMessgaeScopeType? ScopeType
        {
            get;
            set;
        }

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
        public int? TaskId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? ProjectOrWorkflowId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ProjectTeamId
        {
            get;
            set;
        }
    }


}

