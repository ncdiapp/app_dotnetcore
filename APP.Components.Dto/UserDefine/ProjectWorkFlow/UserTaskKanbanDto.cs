using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class UserTaskKanbanDto
    {
        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppProjectPerspectiveViewExDto> KanbanColumnList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectPerspectiveTaskExDto> KanbanAvailableTaskList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppProjectPerspectiveTaskExDto> KanbanSelectedTaskList
        {
            get;
            set;
        }

       
    }
}
