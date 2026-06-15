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
    /// <summary>
    /// DTO class for the entity 'AppSefolder'.
    /// </summary>


    public class BaseAppCatalogueTreeDto
    {





        [DataMember(EmitDefaultValue = false)]
        public object Id
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public object ParentId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object UiId
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public object TreeViewEntityId
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public object TreeViewEntityId
        {
            get;
            set;
        }


        // 1| 2| 3| 4| 5
        [DataMember(EmitDefaultValue = false)]
        public string BranchPath
        {
            get;
            set;
        }

        // 1| 2| 3| 4| 5
        [DataMember(EmitDefaultValue = false)]
        public int TreeLevel
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? Sort
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Name
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? ImageId
        {
            get;
            set;
        }

        //key1:OptionLevel  Key2:rowidentityKey
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictOptionCheckedLevel
        {
            get;
            set;
        }

        public int CountContent
        {
            get;
            set;
        }

        public int? StoreId
        {
            get;
            set;
        }

        public bool IsWithOptionFilter
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public int? columnId_NodeId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? columnId_NodeDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? columnId_NodeImage
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSelectedByDefault
        {
            get;
            set;
        }
    }
}

