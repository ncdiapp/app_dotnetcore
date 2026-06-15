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
    public partial class ApiDataStructureNodeDto
    {       

		[DataMember(EmitDefaultValue = false)]
		public string Name 
		{
			get;
			set;
		}

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SchemaTypeName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string NodePath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string AbsolutePath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DataBindingPath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsArray
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsObject
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSimpleList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<ApiDataStructureNodeDto> Children
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSelected
        {
            get;
            set;
        }

      
        public bool HasSimpleProperties
        {
            get;
            set;
        }




        //[DataMember(EmitDefaultValue = false)]
        //public ApiDataStructureNodeDto ParentNode
        //{
        //    get;
        //    set;
        //}
    }
}