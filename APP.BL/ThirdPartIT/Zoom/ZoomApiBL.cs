using Newtonsoft.Json;

using RestSharp;
using System.Dynamic;
using System;

//namespace App.BL
//{
//    public static class ZoomApiBL
//    {

//          public const  string ZoomWebJWTPorverConfigName = "ZoomWebAppApi";

//        // public const string ZoomMobileAppApiProviderName = "ZoomMobileAppApi";


//        // mapping https://marketplace.zoom.us/docs/api-reference/zoom-api/meetings/meetingcreate

//        public static void GetUserMeeting(string zoomHostRoomEmailAccount = "sean_z@hotmail.com", string apiProviderName= ZoomWebJWTPorverConfigName)
//        {
//            string jwtToken = GetToken(apiProviderName);

//            RestClient clientget = new RestClient(string.Format("https://api.zoom.us/v2/users/{0}/meetings", zoomHostRoomEmailAccount));
//            var requestget = new RestRequest(Method.GET);
//            requestget.AddHeader("authorization", "Bearer " + jwtToken);

//            // requestget.AddHeader("authorization", "Bearer " + jwtToken);
//            IRestResponse response1 = clientget.Execute(requestget);
//        }

//        //System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
//        public static dynamic CreateMetting(

//             string zoomHostRoomEmailAccount = "sean_z@hotmail.com",
//              string start_time = "2020-12-28T13:30:15Z" ,
//              string duration = "60", 
//              string password="1234",
//              string topic = "topic ",
//              string apiProviderName = ZoomWebJWTPorverConfigName

//              )
//        {
//            string jwtToken = GetToken(apiProviderName);

//            var client = new RestClient(string.Format("https://api.zoom.us/v2/users/{0}/meetings", zoomHostRoomEmailAccount));
//            var request = new RestSharp.RestRequest(RestSharp.Method.POST);


//            request.AddHeader("content-type", "application/json");
//            request.AddHeader("authorization", "Bearer " + jwtToken);

//            dynamic objectOtPass = new ExpandoObject();
//            objectOtPass.start_time = start_time; 
//            objectOtPass.duration = duration;
//            // objectOtPass.schedule_for = schedule_for;
//            objectOtPass.password = password;
//            objectOtPass.topic = topic;

//            dynamic meetingSettings = new ExpandoObject();
//            objectOtPass.settings = meetingSettings;
//            // start video when the hosts the meeting
//            meetingSettings.host_video = true;
//            meetingSettings.participant_video = false;
//            meetingSettings.cn_meeting = false;
//            meetingSettings.in_meeting = false;
//            meetingSettings.join_before_host = false;
//            meetingSettings.mute_upon_entry = false;

//            // "host_video": "boolean",
//            //"participant_video": "boolean",
//            //"cn_meeting": "boolean",
//            //"in_meeting": "boolean",
//            //"join_before_host": "boolean",
//            //"mute_upon_entry": "boolean",
//            //"watermark": "boolean",
//            //"use_pmi": "boolean",
//            //"approval_type": "integer",
//            //"registration_type": "integer",
//            //"audio": "string",
//            //"auto_recording": "string",
//            //"enforce_login": "boolean",
//            //"enforce_login_domains": "string",
//            //"alternative_hosts": "string",
//            //"global_dial_in_countries": [
//            //  "string"
//            //],
//            //"registrants_email_notification": "boolean"



//            // dynamic config = JsonConvert.DeserializeObject<ExpandoObject>(crateMesstin, new ExpandoObjectConverter());


//            string serializeedjson = JsonConvert.SerializeObject(objectOtPass);

//            request.AddParameter("application/json; charset=utf-8", serializeedjson, ParameterType.RequestBody);
//            request.RequestFormat = DataFormat.Json;

//            // need to update fit_seassion
//            var response = client.Execute(request);

//            dynamic toReturn = JsonConvert.DeserializeObject(response.Content);

//            // meeting Id
//            //toReturn.password
//            //  toReturn.


//            return toReturn;


//        }

//        public static void CreateUser(string userEmailAddress,string firstName, string lastname, string apiProviderName = ZoomWebJWTPorverConfigName)
//        {
//            string jwtToken = GetToken(apiProviderName);
//            var client = new RestClient("https://api.zoom.us/v2/users");
//            var request = new RestSharp.RestRequest(RestSharp.Method.POST);


//            request.AddHeader("content-type", "application/json");
//            request.AddHeader("authorization", "Bearer " + jwtToken);


//            request.RequestFormat = DataFormat.Json;

//            dynamic objectOtPass = new ExpandoObject();
//            objectOtPass.action = "create";
//            objectOtPass.user_info = new { email = userEmailAddress, type = 1, first_name = firstName, last_name = lastname };

//            string serializeedjson = JsonConvert.SerializeObject(objectOtPass);

//            request.AddParameter("application/json; charset=utf-8", serializeedjson, ParameterType.RequestBody);
//            request.RequestFormat = DataFormat.Json;

//            var response = client.Execute(request);
//        }
//        private static string GetToken( string apiProviderName)
//        {
//            var apientity = AppWebApiProviderBL.GetOneApiProviderEntity(apiProviderName);
//            string jwtToken = JwtTokenHelperBL.CreateJWTToken(apientity.ApiKey, apientity.ApiSecret, 60);
//            return jwtToken;
//        }

//        // this is only for zoom sdk ( mobile app)
//        public static dynamic PrepareZoomSignatureRuntimeParameters(dynamic zoomMeetingObj)
//        {
          

//          // var  constskdKey = "PLKsISsJTZQlcOYuixCvvUHywyAtqUQxGzF3";
//          // var  constsdkSecret = "4aaMNbex5nLhW0zc37XyVdOL4rPhpkZSMorM";

//            if (zoomMeetingObj != null)
//            {
//                if (zoomMeetingObj.mn != null && zoomMeetingObj.role != null)
//                {        
//                    var apientity = AppWebApiProviderBL.GetOneApiProviderEntity(ZoomWebJWTPorverConfigName);

//                    string signature = JwtTokenHelperBL.GetWebUrlMeetingSignature(apientity.ApiKey, apientity.ApiSecret, (string)zoomMeetingObj.mn, (string)zoomMeetingObj.role);
//                    string apiKey = apientity.ApiKey;

//                    zoomMeetingObj.signature = signature;
//                    zoomMeetingObj.apiKey = apiKey;                    
//                }
//            }

//            return zoomMeetingObj;
//        }

//        public static dynamic GetZoomMeetingJwtToken(dynamic zoomMeetingObj)
//        {
            
//            if (zoomMeetingObj != null)
//            {
//                if (zoomMeetingObj.ApiKey != null && zoomMeetingObj.ApiSecret != null && zoomMeetingObj.mn != null && zoomMeetingObj.role != null)
//                {

//                    try
//                    {
//                        string signature = JwtTokenHelperBL.GetWebUrlMeetingSignature((string)zoomMeetingObj.ApiKey, (string)zoomMeetingObj.ApiSecret, (string)zoomMeetingObj.mn, (string)zoomMeetingObj.role);
//                        return new { isSuccessful = true, Signature = signature };
//                    }
//                    catch
//                    {
//                        return new { isSuccessful = false, Signature = "" };
//                    }
                   
//                }
//            }

//             return new { isSuccessful = false, Signature = "" }; ;
//        }

//        //{"uuid":"jhop1xgHTuuZO2FlkEdkcg==","id":84204626739,"host_id":"WObtmfYUSnWMH-ZOczGuDQ","host_email":"sean_z@hotmail.com","topic":"Zoom Meeting","type":2,"status":"waiting","start_time":"2020-11-18T02:53:43Z","duration":60,"timezone":"America/New_York","agenda":"agenda ","created_at":"2020-11-18T02:53:43Z","start_url":"https://us02web.zoom.us/s/84204626739?zak=eyJ6bV9za20iOiJ6bV9vMm0iLCJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJjbGllbnQiLCJ1aWQiOiJXT2J0bWZZVVNuV01ILVpPY3pHdURRIiwiaXNzIjoid2ViIiwic3R5IjoxMDAsIndjZCI6InVzMDIiLCJjbHQiOjAsInN0ayI6Ilk0ODY3MV8xUC1PT0d1U0p6Q2Nzb1BValFPS081Vjhyc2wzSXJybGh0TmsuQmdVZ2NFOUNlRVpYUjNoSFRITktOVGRNZVRJdk16Rlpiek00VEU1V1ZIbFlaMmxBTjJNMVlUUTJNVFkzT0RobE5tTTNPREl4T0dNMllqaGlZbVk1WkdReFl6UTVPRGhqWlRCbE1qQmlNR0ZqTTJRek56ZGlabU0wTVRObVptSmtZVEV6WWdBZ1UyaFBlR0pzVGtka05FTldlbEJNUmpsVFJrZEtlVUpYYm5kWmNFNU1MMFVBQkhWek1ESSIsImV4cCI6MTYwNTY3NTIyMywiaWF0IjoxNjA1NjY4MDIzLCJhaWQiOiJZVlBta0VhT1JHYW5UcWFIY1BnZGx3IiwiY2lkIjoiIn0.amspz0LqC9H8-iT5ZfdfjbJMo6TmP22DYAJ5cUWJjqM","join_url":"https://us02web.zoom.us/j/84204626739?pwd=SW1IdmFVY1VsaXlRRVlESGxlV1Zsdz09","password":"1234","h323_password":"1234","pstn_password":"1234","encrypted_password":"SW1IdmFVY1VsaXlRRVlESGxlV1Zsdz09","settings":{"host_video":false,"participant_video":false,"cn_meeting":false,"in_meeting":false,"join_before_host":false,"mute_upon_entry":false,"watermark":false,"use_pmi":false,"approval_type":2,"audio":"both","auto_recording":"none","enforce_login":false,"enforce_login_domains":"","alternative_hosts":"","close_registration":false,"show_share_button":false,"allow_multiple_devices":false,"registrants_confirmation_email":true,"waiting_room":true,"request_permission_to_unmute_participants":false,"global_dial_in_countries":["US"],"global_dial_in_numbers":[{"country_name":"US","number":"+1 3462487799","type":"toll","country":"US"},{"country_name":"US","number":"+1 6699006833","type":"toll","country":"US"},{"country_name":"US","number":"+1 9294362866","type":"toll","country":"US"},{"country_name":"US","number":"+1 2532158782","type":"toll","country":"US"},{"country_name":"US","number":"+1 3017158592","type":"toll","country":"US"},{"country_name":"US","number":"+1 3126266799","type":"toll","country":"US"}],"registrants_email_notification":true,"meeting_authentication":false,"encryption_type":"enhanced_encryption"}}


//        //        {
//        //  "type": "object",
//        //  "description": "Meeting object",
//        //  "properties": {
//        //    "id": {
//        //      "type": "integer",
//        //      "description": "[Meeting ID](https://support.zoom.us/hc/en-us/articles/201362373-What-is-a-Meeting-ID-): Unique identifier of the meeting in \"**long**\" format(represented as int64 data type in JSON), also known as the meeting number.",
//        //      "format": "int64"
//        //    },
//        //    "assistant_id": {
//        //      "type": "string",
//        //      "description": "Unique identifier of the scheduler who scheduled this meeting on behalf of the host. This field is only returned if you used \"schedule_for\" option in the [Create a Meeting API request](https://marketplace.zoom.us/docs/api-reference/zoom-api/meetings/meetingcreate)."
//        //    },
//        //    "host_email": {
//        //      "type": "string",
//        //      "description": "Email address of the meeting host.",
//        //      "format": "email"
//        //    },
//        //    "registration_url": {
//        //      "type": "string",
//        //      "description": "URL using which registrants can register for a meeting. This field is only returned for meetings that have enabled registration."
//        //    },
//        //    "topic": {
//        //      "type": "string",
//        //      "description": "Meeting topic",
//        //      "maxLength": 200
//        //    },
//        //    "type": {
//        //      "type": "integer",
//        //      "description": "Meeting Type",
//        //      "default": 2,
//        //      "enum": [
//        //        1,
//        //        2,
//        //        3,
//        //        8
//        //      ],
//        //      "x-enum-descriptions": [
//        //        "Instant Meeting",
//        //        "Scheduled Meeting",
//        //        "Recurring Meeting with no fixed time",
//        //        "Recurring Meeting with fixed time"
//        //      ]
//        //    },
//        //    "start_time": {
//        //      "type": "string",
//        //      "format": "date-time",
//        //      "description": "Meeting start date-time in UTC/GMT. Example: \"2020-03-31T12:02:00Z\""
//        //    },
//        //    "duration": {
//        //      "type": "integer",
//        //      "description": "Meeting duration"
//        //    },
//        //    "timezone": {
//        //      "type": "string",
//        //      "description": "Timezone to format start_time"
//        //    },
//        //    "created_at": {
//        //      "type": "string",
//        //      "format": "date-time",
//        //      "description": "The date and time at which this meeting was created."
//        //    },
//        //    "agenda": {
//        //      "type": "string",
//        //      "description": "Agenda"
//        //    },
//        //    "start_url": {
//        //      "type": "string",
//        //      "description": "URL to start the meeting. This URL should only be used by the host of the meeting and **should not be shared with anyone other than the host** of the meeting as anyone with this URL will be able to login to the Zoom Client as the host of the meeting."
//        //    },
//        //    "join_url": {
//        //      "type": "string",
//        //      "description": "URL for participants to join the meeting. This URL should only be shared with users that you would like to invite for the meeting."
//        //    },
//        //    "password": {
//        //      "type": "string",
//        //      "description": "Meeting password. Password may only contain the following characters: `[a-z A-Z 0-9 @ - _ * !]`\n\nIf \"Require a password when scheduling new meetings\" setting has been **enabled** **and** [locked](https://support.zoom.us/hc/en-us/articles/115005269866-Using-Tiered-Settings#locked) for the user, the password field will be autogenerated in the response even if it is not provided in the API request. \n\n\n"
//        //    },
//        //    "h323_password": {
//        //      "type": "string",
//        //      "description": "H.323/SIP room system password"
//        //    },
//        //    "pmi": {
//        //      "type": "integer",
//        //      "description": "Personal Meeting Id. Only used for scheduled meetings and recurring meetings with no fixed time.",
//        //      "format": "int64"
//        //    },
//        //    "tracking_fields": {
//        //      "type": "array",
//        //      "description": "Tracking fields",
//        //      "items": {
//        //        "type": "object",
//        //        "properties": {
//        //          "field": {
//        //            "type": "string",
//        //            "description": "Label of the tracking field."
//        //          },
//        //          "value": {
//        //            "type": "string",
//        //            "description": "Value for the field."
//        //          },
//        //          "visible": {
//        //            "type": "boolean",
//        //            "description": "Indicates whether the [tracking field](https://support.zoom.us/hc/en-us/articles/115000293426-Scheduling-Tracking-Fields) is visible in the meeting scheduling options in the Zoom Web Portal or not.\n\n`true`: Tracking field is visible. <br>\n\n`false`: Tracking field is not visible to the users in the meeting options in the Zoom Web Portal but the field was used while scheduling this meeting via API. An invisible tracking field can be used by users while scheduling meetings via API only. "
//        //          }
//        //        }
//        //      }
//        //    },
//        //    "occurrences": {
//        //      "type": "array",
//        //      "description": "Array of occurrence objects.",
//        //      "items": {
//        //        "type": "object",
//        //        "description": "Occurence object. This object is only returned for Recurring Webinars.",
//        //        "properties": {
//        //          "occurrence_id": {
//        //            "type": "string",
//        //            "description": "Occurrence ID: Unique Identifier that identifies an occurrence of a recurring webinar. [Recurring webinars](https://support.zoom.us/hc/en-us/articles/216354763-How-to-Schedule-A-Recurring-Webinar) can have a maximum of 50 occurrences."
//        //          },
//        //          "start_time": {
//        //            "type": "string",
//        //            "format": "date-time",
//        //            "description": "Start time."
//        //          },
//        //          "duration": {
//        //            "type": "integer",
//        //            "description": "Duration."
//        //          },
//        //          "status": {
//        //            "type": "string",
//        //            "description": "Occurrence status."
//        //          }
//        //        }
//        //      }
//        //    },
//        //    "settings": {
//        //      "type": "object",
//        //      "description": "Meeting settings.",
//        //      "properties": {
//        //        "host_video": {
//        //          "type": "boolean",
//        //          "description": "Start video when the host joins the meeting."
//        //        },
//        //        "participant_video": {
//        //          "type": "boolean",
//        //          "description": "Start video when participants join the meeting."
//        //        },
//        //        "cn_meeting": {
//        //          "type": "boolean",
//        //          "description": "Host meeting in China.",
//        //          "default": false
//        //        },
//        //        "in_meeting": {
//        //          "type": "boolean",
//        //          "description": "Host meeting in India.",
//        //          "default": false
//        //        },
//        //        "join_before_host": {
//        //          "type": "boolean",
//        //          "description": "Allow participants to join the meeting before the host starts the meeting. Only used for scheduled or recurring meetings.",
//        //          "default": false
//        //        },
//        //        "mute_upon_entry": {
//        //          "type": "boolean",
//        //          "description": "Mute participants upon entry.",
//        //          "default": false
//        //        },
//        //        "watermark": {
//        //          "type": "boolean",
//        //          "description": "Add watermark when viewing a shared screen.",
//        //          "default": false
//        //        },
//        //        "use_pmi": {
//        //          "type": "boolean",
//        //          "description": "Use a personal meeting ID. Only used for scheduled meetings and recurring meetings with no fixed time.",
//        //          "default": false
//        //        },
//        //        "approval_type": {
//        //          "type": "integer",
//        //          "default": 2,
//        //          "description": "Enable registration and set approval for the registration. Note that this feature requires the host to be of **Licensed** user type. **Registration cannot be enabled for a basic user.** <br><br>\n\n`0` - Automatically approve.<br>`1` - Manually approve.<br>`2` - No registration required.",
//        //          "enum": [
//        //            0,
//        //            1,
//        //            2
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Automatically Approve",
//        //            "Manually Approve",
//        //            "No Registration Required"
//        //          ]
//        //        },
//        //        "registration_type": {
//        //          "type": "integer",
//        //          "description": "Registration type. Used for recurring meeting with fixed time only. <br>`1` Attendees register once and can attend any of the occurrences.<br>`2` Attendees need to register for each occurrence to attend.<br>`3` Attendees register once and can choose one or more occurrences to attend.",
//        //          "default": 1,
//        //          "enum": [
//        //            1,
//        //            2,
//        //            3
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Attendees register once and can attend any of the occurrences",
//        //            "Attendees need to register for each occurrence to attend",
//        //            "Attendees register once and can choose one or more occurrences to attend"
//        //          ]
//        //        },
//        //        "audio": {
//        //          "type": "string",
//        //          "description": "Determine how participants can join the audio portion of the meeting.<br>`both` - Both Telephony and VoIP.<br>`telephony` - Telephony only.<br>`voip` - VoIP only.",
//        //          "default": "both",
//        //          "enum": [
//        //            "both",
//        //            "telephony",
//        //            "voip"
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Both Telephony and VoIP",
//        //            "Telephony only",
//        //            "VoIP only"
//        //          ]
//        //        },
//        //        "auto_recording": {
//        //          "type": "string",
//        //          "description": "Automatic recording:<br>`local` - Record on local.<br>`cloud` -  Record on cloud.<br>`none` - Disabled.",
//        //          "default": "none",
//        //          "enum": [
//        //            "local",
//        //            "cloud",
//        //            "none"
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Record to local device",
//        //            "Record to cloud",
//        //            "No Recording"
//        //          ]
//        //        },
//        //        "enforce_login": {
//        //          "type": "boolean",
//        //          "description": "Only signed in users can join this meeting.\n\n**This field is deprecated and will not be supported in the future.**  <br><br>As an alternative, use the \"meeting_authentication\", \"authentication_option\" and \"authentication_domains\" fields to understand the [authentication configurations](https://support.zoom.us/hc/en-us/articles/360037117472-Authentication-Profiles-for-Meetings-and-Webinars) set for the meeting."
//        //        },
//        //        "enforce_login_domains": {
//        //          "type": "string",
//        //          "description": "Only signed in users with specified domains can join meetings.\n\n**This field is deprecated and will not be supported in the future.**  <br><br>As an alternative, use the \"meeting_authentication\", \"authentication_option\" and \"authentication_domains\" fields to understand the [authentication configurations](https://support.zoom.us/hc/en-us/articles/360037117472-Authentication-Profiles-for-Meetings-and-Webinars) set for the meeting."
//        //        },
//        //        "alternative_hosts": {
//        //          "type": "string",
//        //          "description": "Alternative host's emails or IDs: multiple values separated by a comma."
//        //        },
//        //        "close_registration": {
//        //          "type": "boolean",
//        //          "description": "Close registration after event date",
//        //          "default": false
//        //        },
//        //        "waiting_room": {
//        //          "type": "boolean",
//        //          "description": "Enable waiting room",
//        //          "default": false
//        //        },
//        //        "global_dial_in_countries": {
//        //          "type": "array",
//        //          "description": "List of global dial-in countries",
//        //          "items": {
//        //            "type": "string"
//        //          }
//        //        },
//        //        "global_dial_in_numbers": {
//        //          "type": "array",
//        //          "description": "Global Dial-in Countries/Regions",
//        //          "items": {
//        //            "type": "object",
//        //            "properties": {
//        //              "country": {
//        //                "type": "string",
//        //                "description": "Country code. For example, BR."
//        //              },
//        //              "country_name": {
//        //                "type": "string",
//        //                "description": "Full name of country. For example, Brazil."
//        //              },
//        //              "city": {
//        //                "type": "string",
//        //                "description": "City of the number, if any. For example, Chicago."
//        //              },
//        //              "number": {
//        //                "type": "string",
//        //                "description": "Phone number. For example, +1 2332357613."
//        //              },
//        //              "type": {
//        //                "type": "string",
//        //                "description": "Type of number. ",
//        //                "enum": [
//        //                  "toll",
//        //                  "tollfree"
//        //                ]
//        //              }
//        //            }
//        //          }
//        //        },
//        //        "contact_name": {
//        //          "type": "string",
//        //          "description": "Contact name for registration"
//        //        },
//        //        "contact_email": {
//        //          "type": "string",
//        //          "description": "Contact email for registration"
//        //        },
//        //        "registrants_confirmation_email": {
//        //          "type": "boolean",
//        //          "description": "Send confirmation email to registrants upon successful registration."
//        //        },
//        //        "registrants_email_notification": {
//        //          "type": "boolean",
//        //          "description": "Send email notifications to registrants about approval, cancellation, denial of the registration. The value of this field must be set to true in order to use the `registrants_confirmation_email` field."
//        //        },
//        //        "meeting_authentication": {
//        //          "type": "boolean",
//        //          "description": "`true`- Only authenticated users can join meetings."
//        //        },
//        //        "authentication_option": {
//        //          "type": "string",
//        //          "description": "Meeting authentication option id."
//        //        },
//        //        "authentication_domains": {
//        //          "type": "string",
//        //          "description": "If user has configured [\"Sign Into Zoom with Specified Domains\"](https://support.zoom.us/hc/en-us/articles/360037117472-Authentication-Profiles-for-Meetings-and-Webinars#h_5c0df2e1-cfd2-469f-bb4a-c77d7c0cca6f) option, this will list the domains that are authenticated."
//        //        },
//        //        "authentication_name": {
//        //          "type": "string",
//        //          "description": "Authentication name set in the [authentication profile](https://support.zoom.us/hc/en-us/articles/360037117472-Authentication-Profiles-for-Meetings-and-Webinars#h_5c0df2e1-cfd2-469f-bb4a-c77d7c0cca6f)."
//        //        },
//        //        "interpreters": {
//        //          "type": "array",
//        //          "description": "Information associated with the interpreter.",
//        //          "items": {
//        //            "type": "object",
//        //            "properties": {
//        //              "email": {
//        //                "type": "string",
//        //                "description": "Email address of the interpreter.",
//        //                "format": "email"
//        //              },
//        //              "languages": {
//        //                "type": "string",
//        //                "description": "Languages for interpretation. The string must contain two [country Ids](https://marketplace.zoom.us/docs/api-reference/other-references/abbreviation-lists#countries) separated by a comma. \n\nFor example, if the language is to be interpreted from English to Chinese, the value of this field should be \"US,CN\"."
//        //              }
//        //            }
//        //          }
//        //        },
//        //        "show_share_button": {
//        //          "type": "boolean",
//        //          "description": "Show social share buttons on the meeting registration page.\nThis setting only works for meetings that require [registration](https://support.zoom.us/hc/en-us/articles/211579443-Setting-up-registration-for-a-meeting)."
//        //        },
//        //        "allow_multiple_devices": {
//        //          "type": "boolean",
//        //          "description": "Allow attendees to join the meeting from multiple devices. This setting only works for meetings that require [registration](https://support.zoom.us/hc/en-us/articles/211579443-Setting-up-registration-for-a-meeting)."
//        //        },
//        //        "encryption_type": {
//        //          "type": "string",
//        //          "description": "Choose between enhanced encryption and [end-to-end encryption](https://support.zoom.us/hc/en-us/articles/360048660871) when starting or a meeting. When using end-to-end encryption, several features (e.g. cloud recording, phone/SIP/H.323 dial-in) will be **automatically disabled**. <br><br>The value of this field can be one of the following:<br>\n`enhanced_encryption`: Enhanced encryption. Encryption is stored in the cloud if you enable this option. <br>\n\n`e2ee`: [End-to-end encryption](https://support.zoom.us/hc/en-us/articles/360048660871). The encryption key is stored in your local device and can not be obtained by anyone else. Enabling this setting also **disables** the following features: join before host, cloud recording, streaming, live transcription, breakout rooms, polling, 1:1 private chat, and meeting reactions.",
//        //          "enum": [
//        //            "enhanced_encryption",
//        //            "e2ee"
//        //          ]
//        //        }
//        //      }
//        //    },
//        //    "recurrence": {
//        //      "type": "object",
//        //      "description": "Recurrence object. Use this object only for a meeting with type `8` i.e., a recurring meeting with fixed time. ",
//        //      "required": [
//        //        "type"
//        //      ],
//        //      "properties": {
//        //        "type": {
//        //          "type": "integer",
//        //          "description": "Recurrence meeting types:<br>`1` - Daily.<br>`2` - Weekly.<br>`3` - Monthly.",
//        //          "enum": [
//        //            1,
//        //            2,
//        //            3
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Daily",
//        //            "Weekly",
//        //            "Monthly"
//        //          ]
//        //        },
//        //        "repeat_interval": {
//        //          "type": "integer",
//        //          "description": "Define the interval at which the meeting should recur. For instance, if you would like to schedule a meeting that recurs every two months, you must set the value of this field as `2` and the value of the `type` parameter as `3`. \n\nFor a daily meeting, the maximum interval you can set is `90` days. For a weekly meeting the maximum interval that you can set is  of `12` weeks. For a monthly meeting, there is a maximum of `3` months.\n\n"
//        //        },
//        //        "weekly_days": {
//        //          "type": "string",
//        //          "description": "Use this field **only if you're scheduling a recurring meeting of type** `2` to state which day(s) of the week the meeting should repeat. <br> <br> The value for this field could be a number between `1` to `7` in string format. For instance, if the meeting should recur on Sunday, provide `\"1\"` as the value of this field.<br><br> **Note:** If you would like the webinar to occur on multiple days of a week, you should provide comma separated values for this field. For instance, if the meeting should recur on Sundays and Tuesdays provide `\"1,3\"` as the value of this field.\n\n <br>`1`  - Sunday. <br>`2` - Monday.<br>`3` - Tuesday.<br>`4` -  Wednesday.<br>`5` -  Thursday.<br>`6` - Friday.<br>`7` - Saturday.",
//        //          "enum": [
//        //            "1",
//        //            "2",
//        //            "3",
//        //            "4",
//        //            "5",
//        //            "6",
//        //            "7"
//        //          ],
//        //          "default": "1"
//        //        },
//        //        "monthly_day": {
//        //          "type": "integer",
//        //          "description": "Use this field **only if you're scheduling a recurring meeting of type** `3` to state which day in a month, the meeting should recur. The value range is from 1 to 31.\n\nFor instance, if you would like the meeting to recur on 23rd of each month, provide `23` as the value of this field and `1` as the value of the `repeat_interval` field. Instead, if you would like the meeting to recur every three months, on 23rd of the month, change the value of the `repeat_interval` field to `3`.",
//        //          "default": 1
//        //        },
//        //        "monthly_week": {
//        //          "type": "integer",
//        //          "description": "Use this field **only if you're scheduling a recurring meeting of type** `3` to state the week of the month when the meeting should recur. If you use this field, **you must also use the `monthly_week_day` field to state the day of the week when the meeting should recur.** <br>`-1` - Last week of the month.<br>`1` - First week of the month.<br>`2` - Second week of the month.<br>`3` - Third week of the month.<br>`4` - Fourth week of the month.",
//        //          "enum": [
//        //            -1,
//        //            1,
//        //            2,
//        //            3,
//        //            4
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Last week",
//        //            "First week",
//        //            "Second week",
//        //            "Third week",
//        //            "Fourth week"
//        //          ]
//        //        },
//        //        "monthly_week_day": {
//        //          "type": "integer",
//        //          "description": "Use this field **only if you're scheduling a recurring meeting of type** `3` to state a specific day in a week when the monthly meeting should recur. To use this field, you must also use the `monthly_week` field. \n\n<br>`1` - Sunday.<br>`2` - Monday.<br>`3` - Tuesday.<br>`4` -  Wednesday.<br>`5` - Thursday.<br>`6` - Friday.<br>`7` - Saturday.",
//        //          "enum": [
//        //            1,
//        //            2,
//        //            3,
//        //            4,
//        //            5,
//        //            6,
//        //            7
//        //          ],
//        //          "x-enum-descriptions": [
//        //            "Sunday",
//        //            "Monday",
//        //            "Tuesday",
//        //            "Wednesday",
//        //            "Thursday",
//        //            "Friday",
//        //            "Saturday"
//        //          ]
//        //        },
//        //        "end_times": {
//        //          "type": "integer",
//        //          "description": "Select how many times the meeting should recur before it is canceled. (Cannot be used with \"end_date_time\".)",
//        //          "default": 1,
//        //          "maximum": 50
//        //        },
//        //        "end_date_time": {
//        //          "type": "string",
//        //          "description": "Select the final date on which the meeting will recur before it is canceled. Should be in UTC time, such as 2017-11-25T12:00:00Z. (Cannot be used with \"end_times\".)",
//        //          "format": "date-time"
//        //        }
//        //      }
//        //    }
//        //  }
//        //}


//        static string crateMesstin = @"
//{
//  ""topic"": ""sean Test "",
//  ""type"": ""2"",
//  ""start_time"": ""2020-10-31T12:30:00"",
//  ""duration"": ""100"",
//  ""schedule_for"": ""sean_z@hotmail.com"",
//  ""timezone"": ""America/Montreal"",
//  ""password"": ""1234"",
//  ""agenda"": ""api intergration""
 
 
//}

//";

//    }



//}