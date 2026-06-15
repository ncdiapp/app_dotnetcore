
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
    public partial class AppEsiteDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string CurrentUserSessionId
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string SitePublishedBaseUrl
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public string SitePublishedLoginUrl
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public int? SiteNavMenuSearchID
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public int? SiteNavMenuSearchID
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string RequestHostServerPath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SiteTemplateId
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public int? PublictSiteLandingPageLayoutId
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public int? CustomerSiteLandingPageLayoutId
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public int? SupplierSiteLandingPageLayoutId
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public string RootFolderPath
        {
            get;
            set;
        }
                

        [DataMember(EmitDefaultValue = false)]
        public EsiteLayoutSettingDto LayoutSetting
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> MediaWidthList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public EsiteAttributeDto EsiteAttribute
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppEsiteTemplateRegisterDto SiteTemplateRegisterDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<string> ThirdPartControlThemeNameList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> ComponentTagList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ComponentConfigText
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowCustomerRegister
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowSupplierRegister
        {
            get;
            set;
        }
    }

    public class EsiteLayoutSettingDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsModified
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int TotalWidthRanges
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BreakWidth1
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BreakWidth2
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BreakWidth3
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BreakWidth4
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? BreakWidth5
        {
            get;
            set;
        }
    }

    public class EsiteAttributeDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? DesignPreviewCustomerPartnerId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DesignPreviewSupplierPartnerId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SearchEngineKeywords
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string OgImageUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string MgtSiteBaseUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TemplateCode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppEsiteThemParameterDto> GlobalSiteThemeParameterList
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string GlobalSiteThemeParameterPreviewHtmlContent
        //{
        //    get;
        //    set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public List<AppEsiteUserDefinedJsFunctionDto> UserDefinedJsFunctionList
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public List<LookupItemDto> FigmaTemplateList
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public List<string> FontFamilyList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppGitHubConfigDto GitHubRepositoryInfo
        {
            get;
            set;
        }
    }
}
