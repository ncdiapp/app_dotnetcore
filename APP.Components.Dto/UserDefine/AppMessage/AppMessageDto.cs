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

    public partial class AppMessageDto
    {
        public static readonly string ConversationMessageSeperateToken = "║";
        public static readonly string ConversationMessagePropertySeperateToken = "₸"; //Subject₸Message₸AppCreatedDate₸AppCreatedById

        public static readonly string SubjectField = "Subject";


        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToSendFullDetail
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CreateDateString
        {
            get
            {
                if (AppCreatedDate != null)
                {
                    return AppCreatedDate.ToString();
                }
                return string.Empty;
            }
        }


        [DataMember(EmitDefaultValue = false)]
        public string CreateByUserName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsRead
        {
            get;
            set;
        }

        public List<AppFileExDto> AttachmentFileDtoList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> AttachmentFieldIds
        {

            get
            {

                List<int> fileIdList = new List<int>();

                if (!string.IsNullOrEmpty(this.AttachmentFileToken))
                {

                    string[] fieIds = AttachmentFileToken.Split("|".ToCharArray());
                    foreach (string fileIdString in fieIds)
                    {
                        int? fileId = ControlTypeValueConverter.ConvertValueToInt(fileIdString);
                        if (fileId.HasValue)
                        {
                            fileIdList.Add(fileId.Value);
                        }
                    }
                }

                if (ConversationMessageList != null && ConversationMessageList.Count > 0)
                {
                    ConversationMessageList.ForAll(o => fileIdList.AddRange(o.AttachmentFieldIds));
                }

                return fileIdList.Distinct().ToList();

            }
        }



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictAttachmentFileIdAndDisplay
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? MessageUserReceivedId
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<AppMessageDto> ConversationMessageList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppMessageDto NewConversationMessage
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsConversationMemberChanged
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<AppFileOrFolderShareToOtherDto> FileshareOtherList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> ToUserIdList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int Sort
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAttachFile
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? LinkToProjectId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToSendMessageAfterFileSharing
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsAttachFormPrintDoc
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAttachAllFormFiles
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsForceUseGlobalSetting
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public string SubGroupId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SubGroupName
        {
            get;
            set;
        }
    }


}

