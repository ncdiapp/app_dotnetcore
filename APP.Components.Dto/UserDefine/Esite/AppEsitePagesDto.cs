using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;
using System.ComponentModel;

namespace APP.Components.EntityDto
{
    public partial class AppEsitePagesDto
    {

        [DataMember]
        public string PageTypeDisplay
        {
            get;
            set;
        }

        [DataMember]
        public bool IsDataModelPage
        {
            get;
            set;
        }

        [DataMember]
        public bool IsSearchViewPage
        {
            get;
            set;
        }

        [DataMember]
        public bool IsHtmlLayoutOnly
        {
            get;
            set;
        }

        [DataMember]
        public System.String FileCode
        {
            get; set;
        }

        [DataMember]
        public System.String Description
        {
            get; set;
        }


        [DataMember]
        public System.String Extension
        {
            get; set;
        }

        [DataMember]
        public bool IsNewFile
        {
            get; set;
        }

        //[DataMember]
        //public System.DateTime? AppCreatedDate
        //{
        //    get; set;
        //}

        //[DataMember]
        //public System.DateTime? AppModifiedDate
        //{
        //    get; set;
        //}


        [DataMember(EmitDefaultValue = false)]
        public AppEsiteStyleSheetUpdateDto StyleSheetUpdateDto
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CreatedFromFilePath
        {
            get; set;
        }

        [DataMember]
        public bool IsComponent
        {
            get;
            set;
        }


        [DataMember]
        public string ComponentType
        {
            get;
            set;
        }

        [DataMember]
        public string ComponentSubType
        {
            get;
            set;
        }

        [DataMember]
        public string ComponentSubTypeId
        {
            get;
            set;
        }

        [DataMember]
        public string ComponentName
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public bool IsStaticSitePage { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsStaticSiteSearchPage { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public bool IsStaticSiteDataModelPage { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string MoveToParentFolderPath { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public AppEsitePageAttributeDto PageAttribute
        {
            get;
            set;
        }


        [DataMember]
        public string LogicKey
        {
            get;
            set;
        }

        [DataMember]
        public string FigmaCanvasId
        {
            get;
            set;
        }

        [DataMember]
        public string FigmaFrameId
        {
            get;
            set;
        }

        [DataMember]
        public float? SizeScaleFactor
        {
            get;
            set;
        }

        //[DataMember]
        //public List<string> FontFamilyList
        //{
        //    get;
        //    set;
        //}
        //

        [DataMember]
        public bool IsAutoConvertAbsolutePositionToStatic
        {
            get;
            set;
        }


        [DataMember]
        public bool IsAutoConvertInlineStyleToTailwind
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public string FigmaPersonalAccessToken
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FigmaFileUrlOrId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictSeoSettingKeyAndValue
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSeoSettingChanged
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<string> PathParameterList
        {
            get;
            set;
        }

    }

    public partial class AppEsitePageAttributeDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string PageTitle
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PageDescription
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
        public int? PageTitleTransFieldId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? PageDescriptionTransFieldId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchEngineKeywordsTransFieldId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? OgImageUrlTransFieldId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool NeedToGenerateStaticSearchDetailViewPages { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string StaticSiteSearchDetailViewPageFileName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? StaticSiteSearchDetailViewPagePkViewColumnId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string PageText1
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PageText2
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PageText3
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PageText4
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PageText5
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string,string> DictThemeParameterAndValue
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, bool> DictScreenSizeCodeAndIsActive
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppEsitePagesDto> AssociatedPageList
        {
            get;
            set;
        }
        

    }

}