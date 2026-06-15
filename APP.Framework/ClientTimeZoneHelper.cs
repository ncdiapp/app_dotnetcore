using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APP.Framework;
//using APP.Persistence.Common;

namespace APP.Framework
{
    /// <summary>
    ///  Currnet Login User Time zone convert
    /// </summary>
    public class ClientTimeZoneHelper
    {


        private static readonly Dictionary<string, string> DictTimeZoneShortKey = new Dictionary<string, string>();


      

        public static DateTime? ConvertClientToUTCDateTime(DateTime? clientDateTime)
        {
            return TimeZoneHelper.ConvertClientToUTCDateTime(clientDateTime, ServerContext.Instance.CurrentUserTimeZoneKey);
        }

        public static DateTime? ConvertUTCToClientDateTime(DateTime? utcDateTime)
        {
            return TimeZoneHelper.ConvertUTCToClientDateTime(utcDateTime, ServerContext.Instance.CurrentUserTimeZoneKey);
        }

        public static DateTime ConvertUTCToClientDateTime(DateTime utcDateTime)
        {
            return TimeZoneHelper.ConvertUTCToClientDateTime(utcDateTime, ServerContext.Instance.CurrentUserTimeZoneKey);
        }

        public static DateTime ConvertClientToUTCDateTime(DateTime clientDateTime)
        {
            return TimeZoneHelper.ConvertClientToUTCDateTime(clientDateTime, ServerContext.Instance.CurrentUserTimeZoneKey);
        }

        public static bool IsClientUsingTimeZone
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ServerContext.Instance.CurrentUserTimeZoneKey);
            }
        }
        // short_name + utcOffsie (key)
        //  MS.net timezoneKye (value)
        //https://www.timeanddate.com/time/zones/
        //https://gist.github.com/redoPop/3915761
        //var zone = new Date().toLocaleTimeString('en-us',{timeZoneName:'short'}).split(' ')[2] 
        static ClientTimeZoneHelper()

        {


         

            // Only inlucde north mrecia
            DictTimeZoneShortKey["AKST"] = "Alaska Standard Time";
            DictTimeZoneShortKey["AST"]= "Atlantic Standard Time";
            DictTimeZoneShortKey["AT"] = "Atlantic Time";
            DictTimeZoneShortKey["CDT"] = "Central Daylight Time";
            DictTimeZoneShortKey["CST"] = "Central Standard Time";
            DictTimeZoneShortKey["CT"] = "Central Time";
           
            DictTimeZoneShortKey["EGST"] = "Eastern Greenland Summer Time";
            DictTimeZoneShortKey["EGT"] = "East Greenland Time";

            DictTimeZoneShortKey["EST"] = "Eastern Standard Time";
            DictTimeZoneShortKey["EDT"] = "Eastern Daylight Time";
            DictTimeZoneShortKey["ET"] = "Eastern Time";

            DictTimeZoneShortKey["GT"] = "Greenwich Time";
            DictTimeZoneShortKey["HDT"] = "Hawaii-Aleutian Daylight Time";
            DictTimeZoneShortKey["HST"] = "Hawaii Standard Time";
            DictTimeZoneShortKey["MDT"] = "Mountain Daylight Time";
            DictTimeZoneShortKey["MST"] = "Mountain Standard Time";
            DictTimeZoneShortKey["MT"] = "Mountain Time";
            DictTimeZoneShortKey["NDT"] = "Newfoundland Daylight Time";
            DictTimeZoneShortKey["NST"] = "Newfoundland Standard Time";
            DictTimeZoneShortKey["PDT"] = "Pacific Daylight Time";
            DictTimeZoneShortKey["PMDT"] = "Pierre & Miquelon Daylight Time";
            DictTimeZoneShortKey["PMST"] = "Pierre &Miquelon Standard Time";
            DictTimeZoneShortKey["PST"] = "Pacific Standard Time";
            DictTimeZoneShortKey["PT"] = "Pacific Time";

            //DictTimeZone["WGST"] = "Western Greenland Summer Time";
            //DictTimeZone["WGT"] = "West Greenland Time";
            //DictTimeZone["ADT"] = "Arabia Daylight Time";
            //DictTimeZone["ALMT"] = "Alma - Ata Time";
            //DictTimeZone["AFT"] = " Afghanistan Time";
            //DictTimeZone["AMST"] = "Armenia Summer Time";
            //DictTimeZone["AMT"] = "Armenia Time";
            //DictTimeZone["ANAST"] = "Anadyr Summer Time";
            //DictTimeZone["ANAT"] = "Anadyr Time";
            //DictTimeZone["AQTT"] = "Aqtobe Time";
            //DictTimeZone["AST"] = "Arabia Standard Time";
            //DictTimeZone["AZST"] = "Azerbaijan Summer Time";
            //DictTimeZone["AZT"] = "Azerbaijan Time";
            //DictTimeZone["BNT"] = "Brunei Darussalam Time";
            //DictTimeZone["BST"] = "Bangladesh Standard Time";
            //DictTimeZone["BTT"] = "Bhutan Time";
            //DictTimeZone["CHOST"] = "Choibalsan Summer Time";
            //DictTimeZone["CHOT"] = "Choibalsan Time";
            //DictTimeZone["CST"] = "China Standard Time";
//GET Georgia Standard Time
//GST Gulf Standard Time
//HKT Hong Kong Time
//HOVST Hovd Summer Time
//HOVT Hovd Time
//ICT Indochina Time
//IDT Israel Daylight Time
//IRDT Iran Daylight Time
//IRKST Irkutsk Summer Time
//IRKT Irkutsk Time
//IRST    Iran Standard Time
//IST India Standard Time
//IST Israel Standard Time
//JST Japan Standard Time
//KGT Kyrgyzstan Time
//KRAST Krasnoyarsk Summer Time
//KRAT Krasnoyarsk Time
//KST Korea Standard Time
//MAGST   Magadan Summer Time
//MAGT    Magadan Time
//MMT Myanmar Time
//    MCK – Moscow Time
//MVT Maldives Time
//MYT Malaysia Time
//NOVST Novosibirsk Summer Time
//NOVT Novosibirsk Time
//NPT Nepal Time
//OMSST Omsk Summer Time
//OMST Omsk Standard Time
//ORAT Oral Time
//PETST   Kamchatka Summer Time
//PETT    Kamchatka Time
//PHT Philippine Time
//PKT Pakistan Standard Time
//PYT Pyongyang Time
//QYZT Qyzylorda Time
//SAKT    Sakhalin Time
//SGT Singapore Time
//SRET    Srednekolymsk Time
//TJT Tajikistan Time
//TLT East Timor Time
//TMT Turkmenistan Time
//TRT Turkey Time
//ULAST   Ulaanbaatar Summer Time
//ULAT    Ulaanbaatar Time
//UZT Uzbekistan Time
//VLAST   Vladivostok Summer Time
//VLAT    Vladivostok Time
//WIB Western Indonesian Time
//WIT Eastern Indonesian Time
//WITA Central Indonesian Time
//YAKST Yakutsk Summer Time
//YAKT Yakutsk Time
//YEKST   Yekaterinburg Summer Time
//YEKT    Yekaterinburg Time


        }


    }
}
