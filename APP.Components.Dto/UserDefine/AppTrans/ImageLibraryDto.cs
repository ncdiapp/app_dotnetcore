using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class ImageLibraryDto
    {
        public static readonly string ImageIconFolderPath24 = "Resources/Images/SystemIcons/24/";
        public static readonly string ImageIconFolderIframePath24 = "../" + ImageIconFolderPath24;

        public static readonly string ImageIconFolderPath64 = "Resources/Images/SystemIcons/64/";
        public static readonly string ImageIconFolderIframePath64 = "../" + ImageIconFolderPath64;

        public static readonly string DefaultIconName64 = "Download From Cloud-64.png";



        //private static readonly Dictionary<string, string> _dictIconNameFileName24 = new Dictionary<string, string>();
        //private static readonly Dictionary<string, string> _dictIconNameFileName64 = new Dictionary<string, string>();

        //static ImageLibraryDto()
        //{

        //    InitialImageDictionary();
         
        //}
        //public static Dictionary<string, string> DictIconNameFileName24
        //{
        //    get
        //    {
              
        //        return _dictIconNameFileName24;
        //    }
        //}
        //public static Dictionary<string, string> DictIconNameFileName64
        //{
        //    get
        //    {
              
        //        return _dictIconNameFileName64;
        //    }
        //}



        //private static void InitialImageDictionary()
        //{
          

        //    _dictIconNameFileName24.Add("ActiveDirectory", "Active Directory-24.png");
        //    _dictIconNameFileName24.Add("ActivityFeed", "Activity Feed-24.png");
        //    _dictIconNameFileName24.Add("AddProperty", "Add Property-24.png");
        //    _dictIconNameFileName24.Add("AddtoFavorites", "Add to Favorites-24.png");
        //    _dictIconNameFileName24.Add("AdministrativeTools", "Administrative Tools-24.png");
        //    _dictIconNameFileName24.Add("AdwareFree", "Adware Free-24.png");
        //    _dictIconNameFileName24.Add("AndroidOS", "Android OS-24.png");
        //    _dictIconNameFileName24.Add("Android", "Android-24.png");
        //    _dictIconNameFileName24.Add("Attach", "Attach-24.png");
        //    _dictIconNameFileName24.Add("BacktoDraft", "Back to Draft-24.png");
        //    _dictIconNameFileName24.Add("Bell", "Bell-24.png");
        //    _dictIconNameFileName24.Add("Block", "Block-24.png");
        //    _dictIconNameFileName24.Add("Bluetooth", "Bluetooth-24.png");
        //    _dictIconNameFileName24.Add("Bug", "Bug-24.png");
        //    _dictIconNameFileName24.Add("Camera", "Camera-24.png");
        //    _dictIconNameFileName24.Add("Checklist", "Checklist-24.png");
        //    _dictIconNameFileName24.Add("Code", "Code-24.png");
        //    _dictIconNameFileName24.Add("CommandLine", "Command Line-24.png");
        //    _dictIconNameFileName24.Add("Console", "Console-24.png");
        //    _dictIconNameFileName24.Add("ControlPanel", "Control Panel-24.png");
        //    _dictIconNameFileName24.Add("CopyLink", "Copy Link-24.png");
        //    _dictIconNameFileName24.Add("DeleteProperty", "Delete Property-24.png");
        //    _dictIconNameFileName24.Add("DeleteShield", "Delete Shield-24.png");
        //    _dictIconNameFileName24.Add("Delete", "Delete-24.png");
        //    _dictIconNameFileName24.Add("DeletedMessage", "Deleted Message-24.png");
        //    _dictIconNameFileName24.Add("Domain", "Domain-24.png");
        //    _dictIconNameFileName24.Add("DownloadFromFtp", "Download From Ftp-24.png");
        //    _dictIconNameFileName24.Add("Download", "Download-24.png");
        //    _dictIconNameFileName24.Add("EditProperty", "Edit Property-24.png");
        //    _dictIconNameFileName24.Add("Edit", "Edit-24.png");
        //    _dictIconNameFileName24.Add("Email", "Email-24.png");
        //    _dictIconNameFileName24.Add("Enter", "Enter-24.png");
        //    _dictIconNameFileName24.Add("Error", "Error-24.png");
        //    _dictIconNameFileName24.Add("Exit", "Exit-24.png");
        //    _dictIconNameFileName24.Add("FilmReel", "Film Reel-24.png");
        //    _dictIconNameFileName24.Add("Film", "Film-24.png");
        //    _dictIconNameFileName24.Add("ForgotPassword", "Forgot Password-24.png");
        //    _dictIconNameFileName24.Add("Form", "Form-24.png");
        //    _dictIconNameFileName24.Add("ForwardMessage", "Forward Message-24.png");
        //    _dictIconNameFileName24.Add("Ftp", "Ftp-24.png");
        //    _dictIconNameFileName24.Add("FullImage", "Full Image-24.png");
        //    _dictIconNameFileName24.Add("GeneralOCR", "General OCR-24.png");
        //    _dictIconNameFileName24.Add("GroupMessage", "Group Message-24.png");
        //    _dictIconNameFileName24.Add("Help", "Help-24.png");
        //    _dictIconNameFileName24.Add("HighImportance", "High Importance-24.png");
        //    _dictIconNameFileName24.Add("Info", "Info-24.png");
        //    _dictIconNameFileName24.Add("InstallingUpdates", "Installing Updates-24.png");
        //    _dictIconNameFileName24.Add("IosPhotos", "Ios Photos-24.png");
        //    _dictIconNameFileName24.Add("iPad", "iPad-24.png");
        //    _dictIconNameFileName24.Add("iPhone", "iPhone-24.png");
        //    _dictIconNameFileName24.Add("Key2", "Key 2-24.png");
        //    _dictIconNameFileName24.Add("Kindle", "Kindle-24.png");
        //    _dictIconNameFileName24.Add("LargeIcons", "Large Icons-24.png");
        //    _dictIconNameFileName24.Add("LevelUp", "Level Up-24.png");
        //    _dictIconNameFileName24.Add("Link", "Link-24.png");
        //    _dictIconNameFileName24.Add("List2", "List 2-24.png");
        //    _dictIconNameFileName24.Add("MacClient", "Mac Client-24.png");
        //    _dictIconNameFileName24.Add("MacOS", "Mac OS-24.png");
        //    _dictIconNameFileName24.Add("MediumIcons", "Medium Icons-24.png");
        //    _dictIconNameFileName24.Add("Menu", "Menu-24.png");
        //    _dictIconNameFileName24.Add("Message", "Message-24.png");
        //    _dictIconNameFileName24.Add("Monitor", "Monitor-24.png");
        //    _dictIconNameFileName24.Add("More", "More-24.png");
        //    _dictIconNameFileName24.Add("MultipleDevices", "Multiple Devices-24.png");
        //    _dictIconNameFileName24.Add("NavigationToolbarBottom", "Navigation Toolbar Bottom-24.png");
        //    _dictIconNameFileName24.Add("NavigationToolbarLeft", "Navigation Toolbar Left-24.png");
        //    _dictIconNameFileName24.Add("NavigationToolbarTop", "Navigation Toolbar Top -24.png");
        //    _dictIconNameFileName24.Add("NewMessage", "New Message-24.png");
        //    _dictIconNameFileName24.Add("Note", "Note-24.png");
        //    _dictIconNameFileName24.Add("Notebook", "Notebook-24.png");
        //    _dictIconNameFileName24.Add("Ok", "Ok-24.png");
        //    _dictIconNameFileName24.Add("OnlineSupport", "Online Support-24.png");
        //    _dictIconNameFileName24.Add("OpeninBrowser", "Open in Browser-24.png");
        //    _dictIconNameFileName24.Add("Outline", "Outline-24.png");
        //    _dictIconNameFileName24.Add("Pin", "Pin-24.png");
        //    _dictIconNameFileName24.Add("Plugin", "Plugin-24.png");
        //    _dictIconNameFileName24.Add("Print", "Print-24.png");
        //    _dictIconNameFileName24.Add("Private", "Private-24.png");
        //    _dictIconNameFileName24.Add("Public", "Public-24.png");
        //    _dictIconNameFileName24.Add("QuestionShield", "Question Shield-24.png");
        //    _dictIconNameFileName24.Add("Questions", "Questions-24.png");
        //    _dictIconNameFileName24.Add("Rating", "Rating-24.png");
        //    _dictIconNameFileName24.Add("RegistryEditor", "Registry Editor-24.png");
        //    _dictIconNameFileName24.Add("Restart", "Restart-24.png");
        //    _dictIconNameFileName24.Add("Restrict", "Restrict-24.png");
        //    _dictIconNameFileName24.Add("RightUp2", "Right Up 2-24.png");
        //    _dictIconNameFileName24.Add("Saveas", "Save as-24.png");
        //    _dictIconNameFileName24.Add("Save", "Save-24.png");
        //    _dictIconNameFileName24.Add("SearchProperty", "Search Property-24.png");
        //    _dictIconNameFileName24.Add("Server", "Server-24.png");
        //    _dictIconNameFileName24.Add("Settings3", "Settings 3-24.png");
        //    _dictIconNameFileName24.Add("ShowProperty", "Show Property-24.png");
        //    _dictIconNameFileName24.Add("SoftwareInstaller", "Software Installer-24.png");
        //    _dictIconNameFileName24.Add("SourceCode", "Source Code-24.png");
        //    _dictIconNameFileName24.Add("Stack", "Stack-24.png");
        //    _dictIconNameFileName24.Add("Start", "Start-24.png");
        //    _dictIconNameFileName24.Add("Support", "Support-24.png");
        //    _dictIconNameFileName24.Add("SystemInformation", "System Information-24.png");
        //    _dictIconNameFileName24.Add("SystemReport", "System Report-24.png");
        //    _dictIconNameFileName24.Add("SystemTask", "System Task-24.png");
        //    _dictIconNameFileName24.Add("Unpin", "Unpin-24.png");
        //    _dictIconNameFileName24.Add("UploadToFtp", "Upload To Ftp-24.png");
        //    _dictIconNameFileName24.Add("Upload", "Upload-24.png");
        //    _dictIconNameFileName24.Add("UserMenu", "User Menu-24.png");
        //    _dictIconNameFileName24.Add("Variable", "Variable-24.png");
        //    _dictIconNameFileName24.Add("VirtualMachine2", "Virtual Machine 2-24.png");
        //    _dictIconNameFileName24.Add("VirtualMachine", "Virtual Machine-24.png");
        //    _dictIconNameFileName24.Add("VirusFree", "Virus Free-24.png");
        //    _dictIconNameFileName24.Add("WiFi", "Wi-Fi-24.png");
        //    _dictIconNameFileName24.Add("WindowsLogo", "Windows Logo-24.png");
        //    _dictIconNameFileName24.Add("ZoomIn", "Zoom In-24.png");
        //    _dictIconNameFileName24.Add("ZoomOut", "Zoom Out-24.png");



        //    _dictIconNameFileName64.Add("ActiveDirectory", "Active Directory-64.png");
        //    _dictIconNameFileName64.Add("ActivityFeed", "Activity Feed-64.png");
        //    _dictIconNameFileName64.Add("AddCamera", "Add Camera-64.png");
        //    _dictIconNameFileName64.Add("AddRule", "Add Rule-64.png");
        //    _dictIconNameFileName64.Add("AddressBook", "Address Book-64.png");
        //    _dictIconNameFileName64.Add("AdministrativeTools", "Administrative Tools-64.png");
        //    _dictIconNameFileName64.Add("Agreement", "Agreement-64.png");
        //    _dictIconNameFileName64.Add("Android", "Android-64.png");
        //    _dictIconNameFileName64.Add("Answers", "Answers-64.png");
        //    _dictIconNameFileName64.Add("AppointmentReminders", "Appointment Reminders-64.png");
        //    _dictIconNameFileName64.Add("Approval", "Approval-64.png");
        //    _dictIconNameFileName64.Add("AreaChart", "Area Chart-64.png");
        //    _dictIconNameFileName64.Add("Assistant", "Assistant-64.png");
        //    _dictIconNameFileName64.Add("Attach", "Attach-64.png");
        //    _dictIconNameFileName64.Add("BarChart", "Bar Chart-64.png");
        //    _dictIconNameFileName64.Add("Block", "Block-64.png");
        //    _dictIconNameFileName64.Add("Blog", "Blog-64.png");
        //    _dictIconNameFileName64.Add("Bookmark", "Bookmark-64.png");
        //    _dictIconNameFileName64.Add("Briefcase", "Briefcase-64.png");
        //    _dictIconNameFileName64.Add("BusinessContact", "Business Contact-64.png");
        //    _dictIconNameFileName64.Add("ButtingIn", "Butting In-64.png");
        //    _dictIconNameFileName64.Add("Calendar", "Calendar-64.png");
        //    _dictIconNameFileName64.Add("Callback", "Callback-64.png");
        //    _dictIconNameFileName64.Add("Checklist", "Checklist-64.png");
        //    _dictIconNameFileName64.Add("CircledPlay", "Circled Play-64.png");
        //    _dictIconNameFileName64.Add("Class", "Class-64.png");
        //    _dictIconNameFileName64.Add("Classroom", "Classroom-64.png");
        //    _dictIconNameFileName64.Add("Code", "Code-64.png");
        //    _dictIconNameFileName64.Add("Collaboration", "Collaboration-64.png");
        //    _dictIconNameFileName64.Add("Collect", "Collect-64.png");
        //    _dictIconNameFileName64.Add("ComboChart", "Combo Chart-64.png");
        //    _dictIconNameFileName64.Add("CommandLine", "Command Line-64.png");
        //    _dictIconNameFileName64.Add("Comments", "Comments-64.png");
        //    _dictIconNameFileName64.Add("ConferenceCall", "Conference Call-64.png");
        //    _dictIconNameFileName64.Add("Conference", "Conference-64.png");
        //    _dictIconNameFileName64.Add("Console", "Console-64.png");
        //    _dictIconNameFileName64.Add("Contacts", "Contacts-64.png");
        //    _dictIconNameFileName64.Add("Courses", "Courses-64.png");
        //    _dictIconNameFileName64.Add("Curriculum", "Curriculum-64.png");
        //    _dictIconNameFileName64.Add("DeleteLink", "Delete Link-64.png");
        //    _dictIconNameFileName64.Add("Delete", "Delete-64.png");
        //    _dictIconNameFileName64.Add("DeletedMessage", "Deleted Message-64.png");
        //    _dictIconNameFileName64.Add("Diploma1", "Diploma 1-64.png");
        //    _dictIconNameFileName64.Add("Diploma2", "Diploma 2-64.png");
        //    _dictIconNameFileName64.Add("Document", "Document-64.png");
        //    _dictIconNameFileName64.Add("Domain", "Domain-64.png");
        //    _dictIconNameFileName64.Add("DoughnutChart", "Doughnut Chart-64.png");
        //    _dictIconNameFileName64.Add("DownloadFromCloud", "Download From Cloud-64.png");
        //    _dictIconNameFileName64.Add("Download", "Download-64.png");
        //    _dictIconNameFileName64.Add("Edit", "Edit-64.png");
        //    _dictIconNameFileName64.Add("Elective", "Elective-64.png");
        //    _dictIconNameFileName64.Add("Exam", "Exam-64.png");
        //    _dictIconNameFileName64.Add("Expired", "Expired-64.png");
        //    _dictIconNameFileName64.Add("External", "External-64.png");
        //    _dictIconNameFileName64.Add("FAQ", "FAQ-64.png");
        //    _dictIconNameFileName64.Add("Feedback", "Feedback-64.png");
        //    _dictIconNameFileName64.Add("File", "File-64.png");
        //    _dictIconNameFileName64.Add("FinePrint", "Fine Print-64.png");
        //    _dictIconNameFileName64.Add("FlowChart", "Flow Chart-64.png");
        //    _dictIconNameFileName64.Add("Folder", "Folder-64.png");
        //    _dictIconNameFileName64.Add("GraduationCap", "Graduation Cap-64.png");
        //    _dictIconNameFileName64.Add("Help", "Help-64.png");
        //    _dictIconNameFileName64.Add("HighPriority", "High Priority-64.png");
        //    _dictIconNameFileName64.Add("Home", "Home-64.png");
        //    _dictIconNameFileName64.Add("Idea", "Idea-64.png");
        //    _dictIconNameFileName64.Add("ImageFile", "Image File-64.png");
        //    _dictIconNameFileName64.Add("Info", "Info-64.png");
        //    _dictIconNameFileName64.Add("Inspection", "Inspection-64.png");
        //    _dictIconNameFileName64.Add("Internal", "Internal-64.png");
        //    _dictIconNameFileName64.Add("Invisible", "Invisible-64.png");
        //    _dictIconNameFileName64.Add("Invite", "Invite-64.png");
        //    _dictIconNameFileName64.Add("Key2", "Key 2-64.png");
        //    _dictIconNameFileName64.Add("Key", "Key-64.png");
        //    _dictIconNameFileName64.Add("Leave", "Leave-64.png");
        //    _dictIconNameFileName64.Add("LevelUp", "Level Up-64.png");
        //    _dictIconNameFileName64.Add("Library", "Library-64.png");
        //    _dictIconNameFileName64.Add("LikeFilled", "Like Filled-64.png");
        //    _dictIconNameFileName64.Add("LineChart", "Line Chart-64.png");
        //    _dictIconNameFileName64.Add("Link", "Link-64.png");
        //    _dictIconNameFileName64.Add("LowPriority", "Low Priority-64.png");
        //    _dictIconNameFileName64.Add("LowVolume", "Low Volume-64.png");
        //    _dictIconNameFileName64.Add("Manager", "Manager-64.png");
        //    _dictIconNameFileName64.Add("MediumPriority", "Medium Priority-64.png");
        //    _dictIconNameFileName64.Add("Message", "Message-64.png");
        //    _dictIconNameFileName64.Add("MindMap", "Mind Map-64.png");
        //    _dictIconNameFileName64.Add("Minus", "Minus-64.png");
        //    _dictIconNameFileName64.Add("MSAccess", "MS Access-64.png");
        //    _dictIconNameFileName64.Add("MSProject", "MS Project-64.png");
        //    _dictIconNameFileName64.Add("MultipleInputs", "Multiple Inputs-64.png");
        //    _dictIconNameFileName64.Add("NegativeDynamic", "Negative Dynamic-64.png");
        //    _dictIconNameFileName64.Add("News", "News-64.png");
        //    _dictIconNameFileName64.Add("NumberedList", "Numbered List-64.png");
        //    _dictIconNameFileName64.Add("Offline", "Offline-64.png");
        //    _dictIconNameFileName64.Add("Online", "Online-64.png");
        //    _dictIconNameFileName64.Add("OpenFolder", "Open Folder-64.png");
        //    _dictIconNameFileName64.Add("OrgUnit", "Org Unit-64.png");
        //    _dictIconNameFileName64.Add("Organization", "Organization-64.png");
        //    _dictIconNameFileName64.Add("Overtime", "Overtime-64.png");
        //    _dictIconNameFileName64.Add("ParallelTasks", "Parallel Tasks-64.png");
        //    _dictIconNameFileName64.Add("Password1", "Password 1-64.png");
        //    _dictIconNameFileName64.Add("Pause", "Pause-64.png");
        //    _dictIconNameFileName64.Add("Picture", "Picture-64.png");
        //    _dictIconNameFileName64.Add("PieChart", "Pie Chart-64.png");
        //    _dictIconNameFileName64.Add("Pin", "Pin-64.png");
        //    _dictIconNameFileName64.Add("Planner", "Planner-64.png");
        //    _dictIconNameFileName64.Add("Play", "Play-64.png");
        //    _dictIconNameFileName64.Add("Plus", "Plus-64.png");
        //    _dictIconNameFileName64.Add("PositiveDynamic", "Positive Dynamic-64.png");
        //    _dictIconNameFileName64.Add("Privacy", "Privacy-64.png");
        //    _dictIconNameFileName64.Add("Process", "Process-64.png");
        //    _dictIconNameFileName64.Add("Prototype", "Prototype-64.png");
        //    _dictIconNameFileName64.Add("Questions", "Questions-64.png");
        //    _dictIconNameFileName64.Add("Ratings", "Ratings-64.png");
        //    _dictIconNameFileName64.Add("ReadMessage", "Read Message-64.png");
        //    _dictIconNameFileName64.Add("RecurringAppointmentException", "Recurring Appointment Exception-64.png");
        //    _dictIconNameFileName64.Add("RecurringAppointment", "Recurring Appointment-64.png");
        //    _dictIconNameFileName64.Add("Repeat", "Repeat-64.png");
        //    _dictIconNameFileName64.Add("ReportCard", "Report Card-64.png");
        //    _dictIconNameFileName64.Add("RestrictionShield", "Restriction Shield-64.png");
        //    _dictIconNameFileName64.Add("Rules", "Rules-64.png");
        //    _dictIconNameFileName64.Add("ScatterPlot", "Scatter Plot-64.png");
        //    _dictIconNameFileName64.Add("School", "School-64.png");
        //    _dictIconNameFileName64.Add("Search", "Search-64.png");
        //    _dictIconNameFileName64.Add("SerialTasks", "Serial Tasks-64.png");
        //    _dictIconNameFileName64.Add("Services", "Services-64.png");
        //    _dictIconNameFileName64.Add("Settings", "Settings-64.png");
        //    _dictIconNameFileName64.Add("Share", "Share-64.png");
        //    _dictIconNameFileName64.Add("Signature", "Signature-64.png");
        //    _dictIconNameFileName64.Add("SmartphoneTablet", "Smartphone Tablet-64.png");
        //    _dictIconNameFileName64.Add("SoftwareBox", "Software Box-64.png");
        //    _dictIconNameFileName64.Add("SoftwareInstaller", "Software Installer-64.png");
        //    _dictIconNameFileName64.Add("SourceCode", "Source Code-64.png");
        //    _dictIconNameFileName64.Add("StarFilled", "Star Filled-64.png");
        //    _dictIconNameFileName64.Add("Statistics", "Statistics-64.png");
        //    _dictIconNameFileName64.Add("Stop", "Stop-64.png");
        //    _dictIconNameFileName64.Add("Student", "Student-64.png");
        //    _dictIconNameFileName64.Add("Students", "Students-64.png");
        //    _dictIconNameFileName64.Add("Support", "Support-64.png");
        //    _dictIconNameFileName64.Add("Survey", "Survey-64.png");
        //    _dictIconNameFileName64.Add("Synchronize", "Synchronize-64.png");
        //    _dictIconNameFileName64.Add("Timeline", "Timeline-64.png");
        //    _dictIconNameFileName64.Add("ToDo", "To Do-64.png");
        //    _dictIconNameFileName64.Add("TodoList", "Todo List-64.png");
        //    _dictIconNameFileName64.Add("Toolbox", "Toolbox-64.png");
        //    _dictIconNameFileName64.Add("Training", "Training-64.png");
        //    _dictIconNameFileName64.Add("Trash", "Trash-64.png");
        //    _dictIconNameFileName64.Add("TreeStructure", "Tree Structure-64.png");
        //    _dictIconNameFileName64.Add("University", "University-64.png");
        //    _dictIconNameFileName64.Add("Unlock", "Unlock-64.png");
        //    _dictIconNameFileName64.Add("Unpin", "Unpin-64.png");
        //    _dictIconNameFileName64.Add("UploadtotheCloud", "Upload to the Cloud-64.png");
        //    _dictIconNameFileName64.Add("UrgentMessage", "Urgent Message-64.png");
        //    _dictIconNameFileName64.Add("UserFemale", "User Female-64.png");
        //    _dictIconNameFileName64.Add("UserGroup", "User Group-64.png");
        //    _dictIconNameFileName64.Add("VIP", "VIP-64.png");
        //    _dictIconNameFileName64.Add("VirtualMachine2", "Virtual Machine 2-64.png");
        //    _dictIconNameFileName64.Add("Visible", "Visible-64.png");
        //    _dictIconNameFileName64.Add("VoicePresentation", "Voice Presentation-64.png");
        //    _dictIconNameFileName64.Add("Work", "Work-64.png");
        //    _dictIconNameFileName64.Add("YouTubePlay", "YouTube Play-64.png");
        //}




    }
}
