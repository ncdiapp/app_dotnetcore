using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
  

    public partial class AppSecurityUserDto
    {
        public static readonly string ConfirmPasswordProperty = ObjectInfoHelper.GetName<AppSecurityUserDto, string>(o => o.ConfirmPassword);
        public static readonly string IsSelectedProperty = ObjectInfoHelper.GetName<AppSecurityUserDto, bool>(o => o.IsSelected);

        public static readonly string ImageFolderProperty = ObjectInfoHelper.GetName<AppSecurityUserDto, string>(o => o.ImageFolder);
        public static readonly string FileFolderProperty = ObjectInfoHelper.GetName<AppSecurityUserDto, string>(o => o.FileFolder);
        
        /// <summary> The ConfirmPassword property of the PdmSecurityWebUserDto</summary>
        [DataMember(EmitDefaultValue = false)]
        public string ConfirmPassword
        {
            get { return GetValue<string>(ConfirmPasswordProperty); }
            set { SetValue(ConfirmPasswordProperty, value); }
        }


        [DataMember(EmitDefaultValue = false)]
        public string OrganizationPath
        {
            get;
            set;
        }


        private static Dictionary<string, string> _DictCultroInfo;

        public static Dictionary<string, string> DictCultroInfo
        {
            get
            {
                if (_DictCultroInfo == null)
                {
                    _DictCultroInfo = new Dictionary<string, string>();

                    _DictCultroInfo.Add("en-AU", "Australia");
                    _DictCultroInfo.Add("en-BZ", "Belize");
                    _DictCultroInfo.Add("en-CA", "Canada English");
                    _DictCultroInfo.Add("en-CB", "Caribbean");
                    _DictCultroInfo.Add("en-IE", "Ireland");
                    _DictCultroInfo.Add("en-JM", "Jamaica");
                    _DictCultroInfo.Add("en-NZ", "New Zealand");
                    _DictCultroInfo.Add("en-PH", "Philippines");
                    _DictCultroInfo.Add("en-ZA", "South Africa");
                    _DictCultroInfo.Add("en-TT", "Trinidad and Tobago");
                    _DictCultroInfo.Add("en-GB", "United Kingdom");
                    _DictCultroInfo.Add("en-US", "United States");
                    _DictCultroInfo.Add("en-ZW", "Zimbabwe");
                    _DictCultroInfo.Add("fr-BE", "Belgium");
                    _DictCultroInfo.Add("fr-CA", "Canada French");
                    _DictCultroInfo.Add("fr-FR", "France ");
                    _DictCultroInfo.Add("fr-LU", "Luxembourg");
                    _DictCultroInfo.Add("fr-MC", "Monaco");
                    _DictCultroInfo.Add("fr-CH", "Switzerland");
                    _DictCultroInfo.Add("es-AR", "Argentina ");
                    _DictCultroInfo.Add("es-BO", "Bolivia");
                    _DictCultroInfo.Add("es-CL", "Chile");
                    _DictCultroInfo.Add("es-CO", "Colombia");
                    _DictCultroInfo.Add("es-CR", "Costa Rica");
                    _DictCultroInfo.Add("es-DO", "Dominican Republic");
                    _DictCultroInfo.Add("es-EC", "Ecuador");
                    _DictCultroInfo.Add("es-SV", "El Salvador ");
                    _DictCultroInfo.Add("es-GT", "Guatemala");
                    _DictCultroInfo.Add("es-HN", "Honduras ");
                    _DictCultroInfo.Add("es-MX", "Mexico ");
                    _DictCultroInfo.Add("es-NI", "Nicaragua");
                    _DictCultroInfo.Add("es-PA", "Panama ");
                    _DictCultroInfo.Add("es-PY", "Paraguay ");
                    _DictCultroInfo.Add("es-PE", "Peru ");
                    _DictCultroInfo.Add("es-PR", "Puerto Rico ");
                    _DictCultroInfo.Add("es-ES", "Spain");
                    _DictCultroInfo.Add("es-UY", "Uruguay ");
                    _DictCultroInfo.Add("es-VE", "Venezuela");
                    _DictCultroInfo.Add("tr", "Turkish");

                    _DictCultroInfo.Add("zh-HK", "Hong Kong SAR");
                    _DictCultroInfo.Add("zh-MO", "Macau SAR");
                    _DictCultroInfo.Add("zh-CN", "China");
                    _DictCultroInfo.Add("zh-CHS", "Simplified");
                    _DictCultroInfo.Add("zh-SG", "Singapore ");
                    _DictCultroInfo.Add("zh-TW", "Taiwan China");
                    _DictCultroInfo.Add("zh-CHT", "Traditional");
                }

                return _DictCultroInfo;
            }
        }

        /// <summary> The User Devisions property for display info</summary>
        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> UserDivisions
        {
            get { return GetValue(() => UserDivisions); }
            set { SetValue(() => UserDivisions, value); }
        }

        /// <summary> The User UserGroups property for display infor</summary>
        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> UserGroups
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String UserGroupString
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> UserProjectTeams
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String UserProjectTeamString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> UserContactGroups
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSelected
        {
            get { return GetValue(() => IsSelected); }
            set { SetValue(() => IsSelected, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public string ImageFolder
        {
            get { return GetValue(() => ImageFolder); }
            set { SetValue(() => ImageFolder, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public string FileFolder
        {
            get { return GetValue(() => FileFolder); }
            set { SetValue(() => FileFolder, value); }
        }

        public Dictionary<string, string> DictPdmSetup
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DomainDispaly
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean?  IsExternalUserSelfRegistration
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UserIdFromUI
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsInvitingCompanyUser
        {
            get;
            set;
        }

       


        [DataMember(EmitDefaultValue = false)]
        public int? InvitedByCompanyId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? InvitedByPartnerId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? InvitationId
        {
            get;
            set;
        }

        

        [DataMember(EmitDefaultValue = false)]
        public int? NewUserPartnerType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string NewUserPartnerName
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? NewBusinessAccountId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RegisterFromEsiteId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EmExternalSigninType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ExternalAcessToken
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedActiveSaasUserByEmail
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedActivePartnerUserByEmail
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PostEmailActivationRedirectUrl
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsNewCompanyUser
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsQuickRegistration
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RedirectToEsiteId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string LogoImgUrl
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string MessageTempalte
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? MessageTempalteId
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string FirstName
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public string LastName
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public string Phone
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> UserContactEmails
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? UserTransactionId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppBusinessPartnerDto PartnerDtoInCurrentCompany
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string ApplicationName
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public string ApplicationCode
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string CompanyName
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public string CompanySize
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public DateTime TokenExpirationDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string AccessToken
        {
            get;
            set;
        }

    }
}