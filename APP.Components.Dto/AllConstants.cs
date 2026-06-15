using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Components.Dto
{
    //public class AllConstants
    //{
    //    public const string WidgetTop = "Top";
    //    public const string WidgetLeft = "Left";
    //    public const string WidgetWidth = "Width";
    //    public const string WidgetHeight = "Height";
    //    public const string WidgetZIndex = "ZIndex";
    //    public const string WidgetFontSize = "FontSize";
    //    public const string WidgetFontFamily = "FontFamily";
    //    public const string WidgetFontColor = "FontColor";
    //    public const string WidgetBackgroundColor = "BackgroundColor";
    //    public const string WidgetOpacity = "Opacity";
    //    public const string WidgetShowLabel = "ShowLabel";
    //    public const string WidgetCustomValue = "CustomValue";
    //    public const string WidgetCustomControlType = "CustomControlType";

    //    public const string FolderSeparatorToken = "###&&&";
    //    public const string FolderSeparator = "/";
    //}

    public class TransactionFieldInternalCode
    {
        ////public const string NotificationSendTrigger = "NotificationSendTriggert";
        //public const string NotificationSubject = "NotificationSubject";
        //public const string NotificationTo = "NotificationTo";
        //public const string NotificationCc = "NotificationCc";
        //public const string NotificationBcc = "NotificationBcc";
        //public const string NotificationBody = "NotificationBody";
        ////public const string NotificationAttachedFileIdString = "NotificationAttachedFileIdString";       
        //public const string NotificationTargetDate = "NotificationTargetDate";
    }


    public class TransactionDataTransferRegister
    {
        public class AppMessageTransfer
        {
            public static readonly string TransferType = "AppMessageTransfer";

            public static readonly string Subject = "Subject";
            public static readonly string Message = "Message";
            // public static readonly string FromEmail = "FromEmail";
            public static readonly string ToList = "ToList";
            // public static readonly string Cclist = "Cclist";
            // public static readonly string Bcclist = "Bcclist";
            public static readonly string ReminderTargetDate = "ReminderTargetDate";
            public static readonly string IsEnableReminder = "IsEnableReminder";
            public static readonly string ReminderMinutes = "ReminderMinutes";
            // public static readonly string AttachedFileId = "AttachedFileId";           
        }

    }


    
}
