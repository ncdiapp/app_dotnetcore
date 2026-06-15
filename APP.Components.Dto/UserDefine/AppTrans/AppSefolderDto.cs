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


    public partial class AppSefolderDto
    {

        [DataMember(EmitDefaultValue = false)]
        public AppSefolderDto[] Children
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int CountContent
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int CountContentSubTotal
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsFolderReadonly
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string FolderPath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UiId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ParentUiId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EsiteId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RelativePathDisplay
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public List<string> FileNameList
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public AppFolderOtherSettingsDto DragDropPostProcessSetting
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DisplayName
        {
            get;
            set;
        }
    }



    public partial class AppFolderOtherSettingsDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public int? OperationTriggerConditonType
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public int? OperationType
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public int? DataSetId
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? TransactionId
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public int? CommandId
        //{
        //    get;
        //    set;
        //}

    }
}

