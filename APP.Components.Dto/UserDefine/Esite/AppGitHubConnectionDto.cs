
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Framework.Collections;

namespace APP.Components.EntityDto
{
    public partial class AppGitHubConfigDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string RepoName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Description
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RepoUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Branch
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RepoUsername
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RepoOwnerId
        {
            get;
            set;
        }

        

        [DataMember(EmitDefaultValue = false)]
        public string GithubToken
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string LocalRootFolderPath
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string LocalFolderPath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string LocalFilePath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsPublic
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ESiteId
        {
            get;
            set;
        }

       
    }

}
