using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using System.Text;
using APP.LBL.DatabaseSpecific;
using System.Data.SqlClient;
using APP.Framework;

using Twilio;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using RestSharp;
using System.Dynamic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace App.BL
{


    public static class EmailHelper
    {

        //dlZ3S3hyRf2PV7UdSQbCb2:APA91bFUv14BD_t_IgwOjt5DLIjbxwNpNZQ2wBuK1dZ3ESJJykKnNAl2JjLKfT-3rgPqCmtxVvoKyM7-aTXfu0bVOhXIbALUlew7U9qYGav8ghGbpsKtEoviAwizO_d4QiN7M08ZCLTK
        public static void PushToClient(string clientDeviceId, string body)
        {
            var client = new RestClient(string.Format("https://fcm.googleapis.com/fcm/send"));
            var request = new RestSharp.RestRequest() { Method = RestSharp.Method.Post };
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "key=AAAAxbO7zG0:APA91bEs0WKBANxgDhbcu4pvg9r8g7i8663LnRCg8ubiaPDQZManboj4qnGO1TD-mMYjABT1zrYvBOk4awhFP9M45LKj-hg_OiEV9cX5GNBu9t-c-IiCMCYv-XBVR7YRrfIwNsL0ILy0");

            dynamic objectOtPass = new ExpandoObject();
            objectOtPass.notification = new { title = "Fit Concierge notification", body = body };
            objectOtPass.to = clientDeviceId;

            string serializeedjson = JsonConvert.SerializeObject(objectOtPass);

            request.AddParameter("application/json; charset=utf-8", serializeedjson, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;

            var response = client.ExecuteAsync(request).GetAwaiter().GetResult();
        }

        //https://www.twilio.com/docs/sms/send-messages
        //https://www.twilio.com/docs/libraries/csharp-dotnet/details
        public static void PushToClientSMS(string phoneNumber, string body)
        {

            phoneNumber = ClearUpPhoneNo(phoneNumber);

            string accountSid = System.Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? string.Empty;
            string authToken = System.Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? string.Empty;
            string FromTwilioPhoneNumber = "+15052786246";

            body = " Fit Concierge: " + body;

            Twilio.TwilioClient.Init(accountSid, authToken);

            try
            {
                var message = MessageResource.Create(
                    body: body,
                    to: new Twilio.Types.PhoneNumber("+" + phoneNumber),
                    from: new Twilio.Types.PhoneNumber(FromTwilioPhoneNumber)

                );

                AppLogger.Warn("PushToClientSMS: phone:" + phoneNumber + " Messge body" + body);
            }
            catch (ApiException e)
            {
                // Console.WriteLine(e.Message);
                // Console.WriteLine($"Twilio Error {e.Code} - {e.MoreInfo}");
                AppLogger.Error("PushToClientSMS:" + e);
            }

        }

        private static string ClearUpPhoneNo(string orgiNumber)
        {
            // REMOVE space
            // orgiNumber= Regex.Replace(orgiNumber, @"\s+", "");

            // remove sapce,-,( ) <> #
            orgiNumber = Regex.Replace(orgiNumber, @"(\s+|-|\(|\)|<|>|#)", "");

            // international ( phone)
            if (orgiNumber.StartsWith("+"))
            {
                orgiNumber = orgiNumber.Remove(0);

            }
            else // north amreccia 
            {
                if (orgiNumber.Length == 10)
                {
                    orgiNumber = "1" + orgiNumber;
                }

            }



            return orgiNumber;

        }
        //  SystemEmailFromAddress = 11,
        //SmtpIntegrationActivation = 189,
        //SmtpServer = 190,
        //SmtpPort = 191,
        //SmtpEnableSSL = 192,
        //SmtpUserName = 193,
        //SmtpPassword = 194,

        //appHostUrl paramountstudy1:funstudy
        //paramountstudy1@gmail.com 
        //     // https://myaccount.google.com/security#connectedapps need to set google acount  Allow less secure apps: ON
        //public static void SmtpGamilSend(string smtpUsarename, string smtpPassword, string fromEamilAddress, string toEmailAdress,string subject,string body)
        //{
        //    // 465, 587
        //    SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
        //    {
        //        Credentials = new NetworkCredential(smtpUsarename, smtpPassword),
        //        EnableSsl = true
        //    };
        //    client.Send(fromEamilAddress, toEmailAdress, subject, body);
        //    //  Console.WriteLine("Sent");
        //    //    Console.ReadLine();

        //}




        public static bool SmtpEamilSend(string toEmailAdress, string subject, string body, List<int> attachmentFileIds = null, System.Net.Mail.Attachment reservationAttachment = null)
        {
            bool isNeedToConvertDateTime = false;

            if (subject.Contains("[" + EmAppMessagePlaceHolderToken.UtcTicks.ToString())
                || body.Contains("[" + EmAppMessagePlaceHolderToken.UtcTicks.ToString()))
            {
                isNeedToConvertDateTime = true;
            }

            //body = "<table class='RichTextMessageBody' border='0' cellspacing='0' width='100%'><tr><td></td><td width='600'><div style='width: 600px;max-width: 100%;'>"
            //    + body + "</div></td><td></td></tr></table>" + "\n"
            //    + "<style>.RichTextMessageBody img, .RichTextMessageBody iframe{max-width: 100% !important;}</style>";

            body = "<div class='RichTextMessageBody' style='width: 100%;'>" + "\n"
                + "<table style='border-collapse:collapse;width:100%;background-color:#f5f5f5;' cellspacing='0' cellpadding='0' border='0'><tr><td>" + "\n"
                + body + "\n"
                + "</div>" + "\n"
                + "</td></tr></table>" + "\n"
                + "<style>.RichTextMessageBody img, .RichTextMessageBody iframe {max-width: 100% !important;}</style>";


            MailMessage smtpmessage = new MailMessage();

            //smtpmessage.Subject = subject;
            //smtpmessage.Body = body;

            smtpmessage.IsBodyHtml = true;

            int? agentUserId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemAgentUser);
            if (agentUserId.HasValue)
            {
                try
                {
                    string agentUsername = AppCacheManagerBL.GetOneAppSecurityUserEntityFromCache(agentUserId.Value).UserName;
                    smtpmessage.From = new MailAddress(AppTenantSettingBL.GetStringValue(EmTenantSettings.SystemEmailFromAddress), agentUsername);
                }
                catch (Exception ex)
                {
                    smtpmessage.From = new MailAddress(AppTenantSettingBL.GetStringValue(EmTenantSettings.SystemEmailFromAddress));
                }
            }
            else
            {
                smtpmessage.From = new MailAddress(AppTenantSettingBL.GetStringValue(EmTenantSettings.SystemEmailFromAddress));
            }




            List<string> toEmailAddressList = toEmailAdress.Split(';').ToList();

            //toEmailAddressList.Add("abaha001@hotmail.com");

            //foreach (string aToEmailAddress in toEmailAddressList)
            //{
            //    if (!string.IsNullOrEmpty(aToEmailAddress.Trim()))
            //    {
            //        smtpmessage.To.Add(new MailAddress(aToEmailAddress.Trim()));
            //    }
            //}




            if (!attachmentFileIds.IsEmpty())
            {
                foreach (int fileId in attachmentFileIds)
                {
                    byte[] buffer = null;
                    string fileName = "";
                    
                    GetAppFileContentFromId(fileId, ref buffer, ref fileName, true);

                    if (!buffer.IsEmpty())
                    {

                        Stream stream = new MemoryStream(buffer);
                        System.Net.Mail.Attachment aAttachment = new System.Net.Mail.Attachment(stream, fileName);
                        smtpmessage.Attachments.Add(aAttachment);
                    }



                }
            }

            //List<ReservationDto> reservations = new List<ReservationDto>();
            //ReservationDto aReservation = new ReservationDto();
            //aReservation.BeginDate = DateTime.Today;
            //aReservation.BeginDate = DateTime.Now;
            //aReservation.Location = "Classroom";
            //aReservation.Summary = "Weekly meeting, Please bring notes Subject";
            //aReservation.DetailsHTML = "Weekly meeting, Please bring notes";
            //reservations.Add(aReservation);

            //ReservationDto aReservation2 = new ReservationDto();
            //aReservation2.BeginDate = DateTime.Now.AddHours(1);
            //aReservation2.BeginDate = DateTime.Now.AddHours(2);
            //aReservation2.Location = "Classroom";
            //aReservation2.Summary = "Training 123 Subject";
            //aReservation2.DetailsHTML = "Training 123";
            //reservations.Add(aReservation2);

            //System.Net.Mail.Attachment reservationAttachment = ConvertReservationToAttahment(reservations);
            //smtpmessage.Attachments.Add(reservationAttachment);

            if (reservationAttachment != null)
            {
                smtpmessage.Attachments.Add(reservationAttachment);
            }

            //new MailAddress(systemEmailReceiver.Email); ;
            string smtpAddress = AppTenantSettingBL.GetStringValue(EmTenantSettings.SmtpServer);
            int? portNumber = AppTenantSettingBL.GetIntValue(EmTenantSettings.SmtpPort);
            bool enableSSL = AppTenantSettingBL.GetBoolValue(EmTenantSettings.SmtpEnableSSL);
            string smtpUserName = AppTenantSettingBL.GetStringValue(EmTenantSettings.SmtpUserName);
            string smtppassword = AppTenantSettingBL.GetStringValue(EmTenantSettings.SmtpPassword);

            if (!string.IsNullOrWhiteSpace(smtpAddress) && !string.IsNullOrWhiteSpace(smtpUserName) && !string.IsNullOrWhiteSpace(smtppassword))
            {
                if (!portNumber.HasValue)
                {
                    portNumber = 587;
                }

                try
                {


                    using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber.Value))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(smtpUserName, smtppassword);
                        smtp.EnableSsl = enableSSL;

                        var allUsers = AppSecurityUserBL.DictAllUserDto.Values;

                        ////Start Debug ***
                        //toEmailAddressList.Clear();
                        //toEmailAddressList.Add("ncditest1@gmail.com");
                        //toEmailAddressList.Add("xianghao_hu@hotmail.com");
                        ////End Debug ***

                        foreach (string aToEmailAddress in toEmailAddressList)
                        {
                            if (!string.IsNullOrEmpty(aToEmailAddress.Trim()))
                            {
                                smtpmessage.To.Clear();
                                smtpmessage.To.Add(new MailAddress(aToEmailAddress.Trim()));

                                if (isNeedToConvertDateTime)
                                {
                                    string timeZoneKey = string.Empty;
                                    var matchedUser = allUsers.FirstOrDefault(o => !string.IsNullOrEmpty(o.Email) && o.Email.Trim().ToLower() == aToEmailAddress.Trim().ToLower());

                                    if (matchedUser != null && !string.IsNullOrWhiteSpace(matchedUser.TimeZoneInfoToken))
                                    {
                                        timeZoneKey = matchedUser.TimeZoneInfoToken;
                                    }

                                    smtpmessage.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(subject, timeZoneKey);
                                    smtpmessage.Body = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(body, timeZoneKey);
                                }
                                else
                                {
                                    smtpmessage.Subject = subject;
                                    smtpmessage.Body = body;
                                }

                                smtp.Send(smtpmessage);
                            }
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }


            }



            return false;

        }

        public static void GetAppFileContentFromId(int fileId, ref byte[] buffer, ref string fileName, bool addFileIdToFileName = true)
        {
            var appFileExDto = AppFileBL.RetrieveOneLatestAppFileExDto(fileId);

            if (appFileExDto != null)
            {


                var fileCode = appFileExDto.FileCode;

                string iniFileIdString = appFileExDto.Id.ToString();

                if (appFileExDto.InitialFileId.HasValue)
                {
                    iniFileIdString = appFileExDto.InitialFileId.Value.ToString();
                }


                EmAppDocumentType docType = (EmAppDocumentType)appFileExDto.FileType;


                fileName = appFileExDto.FileCode;

                if (addFileIdToFileName) {
                    fileName = AppFileBL.fileIdPrefix + iniFileIdString + "_" + appFileExDto.FileCode; 
                }

                if (docType == EmAppDocumentType.PDF
                           || docType == EmAppDocumentType.EXCEL
                           || docType == EmAppDocumentType.WORD
                           || docType == EmAppDocumentType.TXT
                           || docType == EmAppDocumentType.PPT
                       )
                {
                    buffer = appFileExDto.FileContent;

                }
                else
                {
                    if (!string.IsNullOrEmpty(appFileExDto.OriginalFilePath))
                    {

                        var fileFullPathName = AppCompanyBL.GetMyCompanyImagePath() + appFileExDto.OriginalFilePath;
                        buffer = StreamHelper.FileToByteArray(fileFullPathName);


                    }

                }
            }
        }

        // 993 for IMAP; to 995 for POP); Use the ...

        public static void ReadImap()
        {
#if NETFRAMEWORK
            //"IT-MAYFLY.visual-2000.com",

            var gmailmailRepository = new MailRepository(
                                    "imap.gmail.com",
                                    993,
                                    true,
                                    "yourEmailAddress@gmail.com",
                                    "yourPassword"
                                );

            //must use upper case INBOX,

            var emailListUnread = gmailmailRepository.GetUnreadMails("INBOX");

            foreach (ActiveUp.Net.Mail.Message email in emailListUnread)
            {
                Console.WriteLine("<p>{0}: {1}</p><p>{2}</p>", email.From, email.Subject, email.BodyHtml.Text);
                if (email.Attachments.Count > 0)
                {
                    foreach (ActiveUp.Net.Mail.MimePart attachment in email.Attachments)
                    {
                        //Console.WriteLine("<p>Attachment: {0} {1}</p>", attachment.ContentName, attachment.ContentType.MimeType);
                    }
                }
            }
#endif
        }

        public static System.Net.Mail.Attachment GenerateICSAttahment(int transcationId, object rootValueId)
        {
            //EmNotificationMessageUsageType : 1 ( Calendor)

            string queryCalendType = @"select NotificationQuery from [APPMessageNotificationSetting] where [TranscationID]=@TranscationID and MessageUsageType=1  ";
            var paramterList = new List<SqlParameter>();

            paramterList.Add(new SqlParameter("@TranscationID", transcationId));

            string qeryContyent = "";

            using (DataAccessAdapter adapter = new DataAccessAdapter(ServerContext.Instance.CurrentUserDbConnectionString))
            {
                qeryContyent = adapter.ExecuteScalarQuery(queryCalendType, paramterList) as string;

            }

            List<ReservationDto> Reservationss = new List<BL.ReservationDto>();

            if (!string.IsNullOrWhiteSpace(qeryContyent))
            {

                var paramterContentList = new List<SqlParameter>();

                paramterContentList.Add(new SqlParameter(ReservationDto.ParameterRootValueID, rootValueId));

                using (DataAccessAdapter adapter = new DataAccessAdapter(ServerContext.Instance.CurrentUserDbConnectionString))
                {
                    var datatable = adapter.ExecuteDataTableRetrievalQuery(qeryContyent, paramterContentList);
                    Reservationss = ConvertReserVationDataTableToReserVationDto(datatable);


                }
            }

            return GenerateICSAttahmentWithRefervationDto(Reservationss);

        }

        private static List<ReservationDto> ConvertReserVationDataTableToReserVationDto(DataTable datatable)
        {
            List<ReservationDto> Reservationss = new List<ReservationDto>();

            foreach (DataRow row in datatable.Rows)
            {
                var reservation = new ReservationDto();
                reservation.Summary = row[ReservationDto.SummaryField].ToString();
                reservation.BeginDate = row[ReservationDto.BeginDateField] as DateTime?;
                reservation.EndDate = row[ReservationDto.EndDateField] as DateTime?;

                if (reservation.BeginDate.HasValue)
                {
                    reservation.BeginDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(reservation.BeginDate.Value);
                }

                if (reservation.EndDate.HasValue)
                {
                    reservation.EndDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(reservation.EndDate.Value);
                }

                reservation.Location = row[ReservationDto.LocationField].ToString();
                reservation.Details = row[ReservationDto.DetailsFild].ToString();
                reservation.DetailsHTML = row[ReservationDto.DetailsHTMLFild].ToString();
                Reservationss.Add(reservation);

            }

            return Reservationss;
        }

        private static System.Net.Mail.Attachment GenerateICSAttahmentWithRefervationDto(List<ReservationDto> Reservations)
        {
            //List<Reservation> Reservationss = new List<Reservation>();

            //var reservation  =new Reservation() { BeginDate = System.DateTime.Now, EndDate = System.DateTime.Now.AddHours(24), Location = "9660 park", Summary = "porje mangamnet meeting" };

            if (Reservations.IsEmpty())
            {
                return null;
            }

            MemoryStream ms = NewMethod(Reservations);
            System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(ms, "event.ics", "text/calendar");
            return attachment;
        }

        private static MemoryStream NewMethod(List<ReservationDto> Reservations)
        {
            string icsContent = ConvertReserVationtoIcsContent(Reservations);
            var calendarBytes = Encoding.UTF8.GetBytes(icsContent);
            MemoryStream ms = new MemoryStream(calendarBytes);
            return ms;
        }

        public static string ConvertReserVationtoIcsContent(List<ReservationDto> Reservations)
        {
            StringBuilder sb = new StringBuilder();
            string DateFormat = "yyyyMMddTHHmmssZ";
            string now = DateTime.Now.ToUniversalTime().ToString(DateFormat);
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("PRODID:-//Compnay Inc//Product Application//EN");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("METHOD:PUBLISH");
            foreach (var res in Reservations)
            {
                DateTime dtStart = Convert.ToDateTime(res.BeginDate);
                DateTime dtEnd = Convert.ToDateTime(res.EndDate);
                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine("DTSTART:" + dtStart.ToUniversalTime().ToString(DateFormat));
                sb.AppendLine("DTEND:" + dtEnd.ToUniversalTime().ToString(DateFormat));
                sb.AppendLine("DTSTAMP:" + now);
                sb.AppendLine("UID:" + Guid.NewGuid());
                sb.AppendLine("CREATED:" + now);
                sb.AppendLine("X-ALT-DESC;FMTTYPE=text/html:" + res.DetailsHTML);
                sb.AppendLine("DESCRIPTION:" + res.Details);
                sb.AppendLine("LAST-MODIFIED:" + now);
                sb.AppendLine("LOCATION:" + res.Location);
                sb.AppendLine("SEQUENCE:0");
                sb.AppendLine("STATUS:CONFIRMED");
                sb.AppendLine("SUMMARY:" + res.Summary);
                sb.AppendLine("TRANSP:OPAQUE");
                sb.AppendLine("END:VEVENT");
            }
            sb.AppendLine("END:VCALENDAR");
            string icsContent = sb.ToString();
            return icsContent;
        }
    }


    public class ReservationDto
    {

        public static readonly string SummaryField = "Summary";
        public static readonly string LocationField = "Location";
        public static readonly string BeginDateField = "BeginDate";
        public static readonly string EndDateField = "EndDate";
        public static readonly string DetailsFild = "Details";
        public static readonly string DetailsHTMLFild = "DetailsHTML";

        public static readonly string ParameterRootValueID = "@RootValueID";


        public DateTime? BeginDate
        { get; set; }
        public DateTime? EndDate { get; set; }
        public string Location { get; set; }
        public string Summary { get; set; }
        public string Details { get; set; }
        public string DetailsHTML { get; set; }


    }

}