using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace APP.Components.Dto
{

    #region --- Html5 UI

    public enum EmAppWorkflowConditionType
    {
        FieldModified = 2,
        RowAdded = 3,
        RowDeleted = 4,
        RowModified = 5,
    }

   


    // businss log mapping
    public enum EmBLFiledMappingSystemTokenField
    {
        CurrentDatetime = 4,
        CurrentUserID = 5,

        DefaultNewGuid = 7,
        TransactionID = 8,
        Username = 9,
        CurrentPartnerID = 10,


        CalendarRepeatToken = 11,
        CalendarRepeatSetting = 12,
        CalendarEventStartDateTime = 13,
        CalendarEventEndDateTime = 14,

        CreatedByCustomerID = 21,
        LastModifiedByCustomerID = 22,
        CreatedBySupplierID = 23,
        LastModifiedBySupplierID = 24,
        CreatedByClientAgentID = 25,
        LastModifiedByClientAgentID = 26,
        CreatedBySupplierAgentID = 27,
        LastModifiedBySupplierAgentID = 28,

        ApiUploadServerFilePath = 29,
        ApiUploadFileId = 30,
                
        UploadFileByTransactionCommand = 31,

        ApiUploadFtpUserName = 32,
        ApiUploadFtpPassword = 33,

        CurrentUserIPAddress = 34,
    }

    public enum EmAppMessagePlaceHolderToken
    {
        CurrentDatetime = 1,
        CurrentUserName = 2,
        WorkflowtName = 3,
        TaskName = 4,
        TaskStatus = 6,
        //TransactionName = 7,
        //TransactionRID = 8,
        TF = 9,//TransactionField
        UtcTicks = 10,

        RegistrationUserName = 11,
        UserRegisterActivationUrl = 12,

        CurrentPkValue = 13,
        ApplicationURL = 14,

        CurrentCompanyName = 15,

        Encrypt = 100,
        QrLink = 101,

        CurrentUserId = 201,
        CurrentPartnerId = 202,
        Today = 301,


    }

    public enum EmAppTransactionCommandSystemParameterToken
    {
        CurrentUserId = 1,
        CurrentUserName = 2,
        TF = 9,//TransactionField
        GlobalTF = 10,//GlobalTransactionField
    }


    // All Table must have trackfiled as rule
    public enum EmSystemDbTrackField
    {
        AppCreatedByID = 1,
        AppCreatedDate = 2,
        AppModifiedDate = 3,
        AppModifiedByID = 4,
        AppCreatedByCompanyID = 5,
    }


    public enum EmAppGrandChildEditMode
    {
        SubGrid = 1,
        Popup = 2,
    }

    public enum EmAppWorkFlowActionType
    {
        SetActualStart = 1,
        SetActualEnd = 2,
        SetPlannedStart = 3,
        SetPlannedEnd = 4,
        SetNbDays = 5,
        SetTimingDays = 6,
        SetNotes = 7,
        Reject = 8,
        Complete = 9,
        Reopen = 10,
        Start = 11,
        Ignore = 12,
        AppendVendor = 15,
        SetVendor = 16,
        NotifyTaskUser = 17,
        NotifyVendor = 18,
        UpdateTransactionField = 19,
        UpdateTaskField = 21,
        Hold = 22,
        AppendUser = 31,
        AppendCurrentUser = 32,
        SetUser = 33,
        SetCurrentUser = 34,
        DataLoad = 35,
        UpdateNextTransactionForm = 36,
        ExecuteNextTransactionFormDataLoad = 37,
        SendFormMessageToFollowUpUsers = 38,
        SendMessageToTransFieldUserOrRole = 39,
        PluginWebApiCall = 40,
        CallStoredProcedure = 41,
        ExecuteSQLStatement = 42,
        ExecuteFormDataTransfer = 43,
        OpenLinkedSearch = 53,
        StartProject = 101,

    }


    public enum EmAppTransactionCommandType
    {

        ExternalMethodMasterDetail = 38,

        PluginWebApiCall = 40,
        ExecuteSQLStatement = 42,
        ExecuteFormDataTransfer = 43,
        DataModelAllFormulaCalculation = 44,
        ExecuteLoadDataSetToTranscation = 45,
        GenerateMatrix = 46,
        SaveAs = 47,
        Print = 48,
        Save = 49,
        refresh = 50,
        OpenFormCreationWindow = 51,
        CallExternalDllMethod = 52,
        OpenLinkedSearch = 53,
        CloseFormWindow = 54,
        OpenFormEditWindow = 55,

        // DetectTransactionFieldValueChangeBeforeSaveDB = 55,
        // DetectTransactionFieldValueChangeAfterSaveDB = 56,
        CommnadFormulaCalculation = 57,
        CallApiDefaultPublish = 58,
        CallApiOperation = 59,

        SendMessageToTransFieldUserId = 60,
        SendMessageToTransFieldEmailAddress = 61,
        SendMessageToTransFieldPartnerId = 62,

        SendSmsToTransFieldPhoneNumber = 63,
        SendSmsToTransFieldPartnerId = 64,
        IntegrationWebApiCall = 65,

        ImportToDatabaseTableFromJson = 66,
        ImportToDatabaseTableFromExcel= 67,
        ImportToDatabaseTablesFromMultipleJsonFiles = 76,
        ImportToDatabaseTablesFromMultipleExcelFiles = 77,

        ExecuteExternalExeProcess = 68,
       
        QuickCreateUser = 70,

        ImportToDatabaseTableFromRestApiImportSetting = 71,
        ImportToDatabaseTableFromDbToDbImportSetting = 72,

        DownloadFileToServerFolder = 73,

        PrintFromMessageTemplate = 74,

        ConvertFromXmlToJson = 81,
        ConvertBackFromJsonToXml = 82,

        //CreateWindowsSchedulerTask = 91,

        StartProject = 101,
        CompositionCommand = 200,

        MasterDetailDataNotNullValidation = 205,


    }



    public enum EmAppWorkflowTaskField
    {
        DatePlannedStart = 1,
        NbDays = 2,
        DatePlannedEnd = 3,
        DateActualStart = 4,
        DateActualEnd = 5,
        TimingDays = 6,
        ProjectActivityStatus = 7,
        Weight = 8,
        ToleranceDays = 9,
        //DateModelStart = 10,
        //DateModelEnd = 11,
        RequiredPercentage = 12,
        NbHours = 13,
        OriginalLibProjectActivity = 14,
        UnitOfTime = 15,
        AmountOfTime = 16,
        StageStatusFlag = 17,
    }



    public enum EmAppEntityLookupInfoCode
    {
        // Enum 1 - 1000

        EmAppEntityLookupInfoCode = 1,

        EmTransactionOrganizedType = 2,

        EmAppLinkTargetActionType = 3,

        EmAppEntityType = 4,

        EmAppFormScope = 5,

        EmAppFormLayoutType = 6,

        EmAppAggregationFunctionType = 7,

        EmAppFormularType = 8,

        EmAppFormularFunctionType = 9,

        EmAppCriteriaOperatorType = 10,

        EmAppControlType = 11,

        EmAppDataType = 12,

        EmAppImportLanguageType = 13,
        EmAppSecurityObjectType = 14,
        EmAppAdminTheme = 15,
        EmAppClientTheme = 16,
        EmAppApplicationLayoutMode = 17,

        // System Table 1001 - 3000

        AppDataSet = 1001,

        AppDesktop = 1002,
        //
        //AppDesktopItem = 1003,
        //
        //AppEntityEnumValue = 1004,

        AppEntityInfo = 1005,

        AppForm = 1006,
        //
        //AppFormGridLayoutItemBindField = 1007,

        AppFormGroup = 1008,
        //
        //AppFormGroupItem = 1009,
        //
        //AppFormLayoutItem = 1010,
        //
        //AppFormLinkTarget = 1011,

        AppLanguage = 1012,
        //
        //AppLanguageKey = 1013,

        AppListMenu = 1014,

        AppSearch = 1015,
        //
        //AppSearchField = 1016,

        AppSearchSaved = 1017,
        //
        //AppSearchSavedValue = 1018,

        AppSearchView = 1019,
        //
        //AppSearchViewField = 1020,
        //
        //AppSearchViewLinkTarget = 1021,

        AppSecurityFormAction = 1022,
        //
        //AppSecurityFormActionResource = 1023,

        AppSecurityGroup = 1024,
        //
        //AppSecurityGroupMember = 1025,

        AppSecurityLoginAuditor = 1026,

        AppSecurityRegDomain = 1027,
        //
        //AppSecurityRegDomainListMenu = 1028,
        //
        //AppSecuritySysObjGroupUser = 1029,

        AppSecurityUser = 1030,
        //
        //AppSecurityUserListMenu = 1031,

        AppSecurityUserSession = 1032,
        //
        //AppSysLabelLanguage = 1033,

        AppTransaction = 1034,
        //
        //AppTransactionField = 1035,
        //
        //AppTransactionFieldAggFunction = 1036,
        //
        //AppTransactionUnit = 1037,
        //
        //AppTransactionUnitFormula = 1038,

        AppUserAppointment = 1039,


        AppBusinessMgtScope = 1042,
        AppExternalMethodRegister = 1044,
        AppProject = 1045,
        AppWorkFlow = 1046,
        AppCountry = 1047,
        AppProjectWorkFlowTask = 1048,


        AppComOrgLevel = 1049,
        AppComOrgLevelFullName = 1050,


        AppBusienssAssormentNavigation = 1051,

        AppCurrency = 1052,

        AppPartner = 1053,
        AppPartnerSupplier = 1054,
        AppPartnerCustomer = 1055,
        AppEmployee = 1056
    }


    public enum EmAppEntitySearchInfoCode
    {
        [EnumMember]
        AddressEntityView = 1,

    }


    public enum EmTransactionOrganizedType
    {
        [EnumMember]
        MasterDetail = 1,

        [EnumMember]
        List = 3,

        [EnumMember]
        Recursive = 4,

        [EnumMember]
        FolderList = 5,

        //[EnumMember]
        //GroupTransaction = 6,

        [EnumMember]
        ImportDraft = 7,

        [EnumMember]
        WorkflowAutomation = 8,

    }

    public enum EmTransactionCreatedFrom
    {
        [EnumMember]
        Database = 1,

        [EnumMember]
        Excel = 2,

        [EnumMember]
        Json = 3,

    }


    public enum EmAppEntityType
    {       
        SystemDefineTable = 1,       
        Enum = 2,      
        SimpleQuery = 3,       
        SimpleValueList = 4,
    }

    public enum EmAppFormCreationFrom
    {
        FromDB = 1,
        FromScratch = 2,
    }


    public enum EmAppFormLayoutType
    {
        Grid = 1,
        Canvas = 2,
        Flow = 3,
        Flex = 4,
    }


    public enum EmAppAggregationFunctionType
    {
        SUM = 1,
        AVG = 2,
        Min = 3,
        Max = 4,
        RowCount = 5,
        BooleanSum = 6,
        ConcatenateString = 7
    };




    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppFormularType
    {
        Assignment = 1,
        BooleanExpressionWarning = 2,
        BooleanExpressionError = 3,
        SubscribeFromGridColumnAggregation = 4,
        SubscribeFromParentLevelField = 5,
        BooleanExpressionDeleteRow = 6,
        SqlScarlarAssignment = 7,
        SqlTupleAssignment = 8,

        LeadAssignment = 11,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppFormularFunctionType
    {
        [EnumMember]
        Floor = 1,

        [EnumMember]
        Ceiling = 2,

        [EnumMember]
        Round = 3,

        [EnumMember]
        ToLower = 4,

        [EnumMember]
        ToUpper = 5,

        [EnumMember]
        Abs = 6,

        [EnumMember]
        DateDiff = 7,

        [EnumMember]
        Encrypt = 8,

        [EnumMember]
        Decrypt = 9,

    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppLeadFunctionType
    {
        Sum = 1,
        Average = 2,
        Min = 3,
        Max = 4,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppCriteriaOperatorType
    {
        [EnumMember]
        Equals = 0,

        [EnumMember]
        Null = 1,

        [EnumMember]
        NotNull = 2,

        [EnumMember]
        GreaterThan = 3,

        [EnumMember]
        GreaterThanOrEquals = 4,

        [EnumMember]
        LessThan = 5,

        [EnumMember]
        LessThanOrEquals = 6,

        [EnumMember]
        Like = 7,

        [EnumMember]
        NullOrEmpty = 8,

        [EnumMember]
        NotNullOrEmpty = 9,

        [EnumMember]
        StartWith = 10,

        [EnumMember]
        EndWith = 11,

        [EnumMember]
        In = 12,

        [EnumMember]
        NotEqual = 13,

        [EnumMember]
        Between = 14,
    }



    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppControlType
    {
        DDL = 1,
        TextBox = 2,
        Memo = 4,
        Image = 5,
        Grid = 6,
        Date = 7,
        File = 9,
        Label = 10,
        CheckBox = 13,
        Empty = 17,
        Numeric = 20,
        AutoGeneration = 23,
        RGBColorDisplay = 24,
        Video = 25,
        RichText = 26,
        DateTimeDetail = 27,
        Audio = 28,
        PieChart = 29,
        DoughnutChart = 30,
        LinearChart = 31,
        BarChart = 32,
        SearchAndView = 33,
        Time = 34,
        RetrieveData = 35,
        FolderTree = 36,
        ImageBinary = 37,
        AutoComplete = 38,
        RadioButtons = 39,
        DateRange = 40,
        FolderPathDisplay = 41,
        Progress = 42,
        BarCode = 43,
        GoogleMap = 44,
        GoogleAddress = 45,
        Rating = 46,
        TextDisplay = 47,
        SearchAbleDDL = 48,
        YoutubeVideo = 49,
        ExternalImageUrl = 50,
        HtmlContent = 51,
        JsonObject = 52,
        InvalidControlType = 99,

    }


    public enum EmAppFormLayoutItemType
    {
        DDL = 1,
        TextBox = 2,
        Memo = 4,
        Image = 5,
        Grid = 6,
        Date = 7,
        File = 9,
        Label = 10,
        CheckBox = 13,
        Empty = 17,
        Numeric = 20,
        Integer = 21,
        AutoGeneration = 23,
        RGBColorDisplay = 24,
        Video = 25,
        RichText = 26,
        DateTimeDetail = 27,
        Audio = 28,
        PieChart = 29,
        DoughnutChart = 30,
        LinearChart = 31,
        BarChart = 32,
        SearchAndView = 33,
        Time = 34,
        RetrieveData = 35,
        FolderTree = 36,
        ImageBinary = 37,
        AutoComplete = 38,
        RadioButtons = 39,
        DateRange = 40,
        FolderPathDisplay = 41,
        Progress = 42,
        BarCode = 43,
        GoogleMap = 44,
        GoogleAddress = 45,
        YoutubeVideo = 49,
        ExternalImageUrl = 50,
        HtmlContent = 51,
        InvalidControlType = 99,


        LayoutRow = 101,
        Section = 102,
        Content = 103,
        NewItemAddButton = 104,
        Space = 105,
        CommandActionButton = 106,
        TabContainer = 107,
        Tab = 108,
        LinkedSearch = 109,
        TableContainer = 110,
        HtmlContentContainer = 111,
    }



    //[DataContract(Namespace = ContractNamespaces.Dto)]
    //public enum EmAppDataType
    //{
    //    [EnumMember]
    //    String = 1,

    //    [EnumMember]
    //    Int16 = 2,

    //    [EnumMember]
    //    Int32 = 3,

    //    [EnumMember]
    //    Int64 = 4,

    //    [EnumMember]
    //    Decimal = 5,

    //    [EnumMember]
    //    Double = 6,

    //    [EnumMember]
    //    Single = 7,

    //    [EnumMember]
    //    DateTime = 8,

    //    [EnumMember]
    //    Boolean = 9,

    //    [EnumMember]
    //    Guid = 10,

    //    [EnumMember]
    //    ByteArray = 11,

    //    [EnumMember]
    //    TinyInt = 12, // match c# Byte

    //    [EnumMember]
    //    Unknown = 100,
    //}


    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppDataType
    {
        String = 1,


        Integer = 2, // int 32


        Decimal = 3,


        Date = 4,


        Time = 5,


        DateTime = 6,


        Boolean = 7,

        Blob = 8,


        Tinyint = 9,
        Smallint = 10,
        BigInt = 11,


        //Mysql:
        UInt8 = 12,
        UInt16 = 13, // (2 byte 16)
        UInt32 = 14, // int (4 byte)
        UInt64 = 15, // big int (8 byte)

        // need to match sqlserver nvarch(max) or mysql longText
        LongString = 16,

        Guid = 17,


    }







    [DataContract(Namespace = ContractNamespaces.Dto)]
    public enum EmAppImportLanguageType
    {
        [EnumMember]
        Menu = 1,
        [EnumMember]
        Form = 2,
        [EnumMember]
        TransactionField = 3,
        [EnumMember]
        AppLanguageKey = 5
    }


    //[DataContract(Namespace = ContractNamespaces.Dto)]
    //public enum EmAppSecurityObjectType
    //{
    //    [EnumMember]
    //    Form = 1,

    //    [EnumMember]
    //    Transaction = 2,

    //    [EnumMember]
    //    TransactionField = 3,

    //    [EnumMember]
    //    Search = 4,

    //    [EnumMember]
    //    SearchView = 5,
    //}


    public enum EmAppUserType
    {
        SysAdmin = 1, // Reservered id is alwasy =1
        Customer = 3,  // no need regiser ( assign on the running time, it was  invited by othere compnay)
        Supplier = 4, // // no need regiser ( assign on the running time,it was  invited by othere compnay))
        ClientAgent = 5,
        SupplierAgent = 9,
        Employee = 2, // self compnay self employee creation
        SaasCompanyAdmin = 6, // self compnay self register register 
        Unknow = 7, // invite user without self compnay registger
        AllUser = 8,

        CompanyWinScheduleUser = 88,
        Integration = 98,
        CompanyAnonymousUser = 99,
        

    }



    //public enum EmAppBussinessPartnerType
    //{
    //    Customer = 1,
    //    Supplier = 2,
    //}

    public enum EmAppLinkTargetSourceType { SearchView = 2, TransactionUnit = 3 }

    public enum EmAppApplicationSettingCategory
    {
        ServerSetting = 1,
        FileSystem = 2,
        EmailSystem = 3,
        UILayout = 4,
        EShopPageSetting = 5,
        FileFolderSetting = 6,
        MobileSetting = 8,
        GoogleSetting = 9,
        CalendarSettingByUserType = 10,
        UserProfileSettingByUserType = 11,
        SecurityFilterEntityByUserType = 12,
        DefaultLoginPageByUserType = 13,
        PartnerUserAndPartnerMappingRelation = 14,
        TransferPartnerUserRegisterInfoToPartnerExtendTableMapping = 15,
        Figma = 16,
        GeneralSetting = 100,
    }

    // Settings stored in AppMasterDB.AppSystemSetting — one value for the whole installation.
    public enum EmSystemSettings
    {
        Timeout = 2,
        TimeoutWarningGracePeriod = 3,
        ApplicationTutorialUrl = 5,
        ApplicationURL = 10,

        BaseUserDbBackupFilePath = 210,
        UserDbFileDirectoryPath = 211,

        AppVersion = 1000,
        InternalApiRestEndPoint = 1003,
        ApplicationPoolName = 1004,
        IISWebSiteName = 1006,
    }

    // Settings stored in AppTenantDB.AppTenantSetting — one row per tenant company.
    public enum EmTenantSettings
    {
        EnableConfigurationMode = 4,
        SystemEmailFromAddress = 11,
        SystemAgentUser = 12,

        SmtpIntegrationActivation = 189,
        SmtpServer = 190,
        SmtpPort = 191,
        SmtpEnableSSL = 192,
        SmtpUserName = 193,
        SmtpPassword = 194,

        EshopMyAddressesSearch = 301,
        EshopMyOrdersSearch = 302,
        EshopMyWishListSearch = 303,
        EshopUserInfoTransaction = 304,
        EshopNewProductTreeView = 305,
        EshopTreeView = 306,
        EshopTopProductsSearchView = 307,
        EshopOrderDetailTransaction = 308,
        EshopMyAccountDesktop = 309,
        EshopMyAccountExUrl = 311,
        EshopTopNewProductsSearchView = 312,
        EshopDefaultStoreId = 3120,

        StripeGateWaySecretkey = 2001,

        WebPageTemplatePath = 401,

        PublicFileFolderId = 502,
        TempFileFolderId = 503,
        UsersFolderId = 504,
        SystemDefinedFileTransactionId = 505,
        TransactionFileStorageRootFolderId = 506,

        AdminTheme = 1001,
        ClientTheme = 1002,
        ApplicationLayoutMode = 1005,
        WorkflowBatchLogSearchId = 1007,
        WorkflowLogTrackDetailSearchId = 1008,

        DefaultCollapseLeftMenuForAdminUsers = 1021,
        DefaultCollapseLeftMenuForEmployeeUsers = 1022,
        DefaultCollapseLeftMenuForCustomerUsers = 1023,
        DefaultCollapseLeftMenuForSupplierUsers = 1024,
        DefaultCollapseLeftMenuForClientAgentUsers = 1025,
        DefaultCollapseLeftMenuForSupplierAgentUsers = 1029,

        EmployeeEntity = 1301,
        CustomerEntity = 1302,
        CustomerAgentEntity = 1303,
        SupplierEntity = 1304,
        SupplierAgentEntity = 1305,

        GoogleApiKey = 1400,

        AdminCalenarSearch = 1501,
        EmployeeCalendarSearch = 1502,
        CustomerCalendarSearch = 1503,
        SupplierCalendarSearch = 1504,
        ClientAgentCalendarSearch = 1505,
        SupplierAgentCalendarSearch = 1509,

        AdminUserTransaction = 1511,
        EmployeeUserTransaction = 1512,
        CustomerUserTransaction = 1513,
        SupplierUserTransaction = 1514,
        ClientAgentUserTransaction = 1515,
        SupplierAgentUserTransaction = 1519,

        IsCustomerUserToPartnerOneToOneMapping = 1601,
        IsSupplierUserToPartnerOneToOneMapping = 1602,

        CustomerPartnerTransaction = 1611,
        CustomerPartnerDbtableName = 1612,
        CustomerPartnerIdDbfieldName = 1613,
        CustomerPartnerEmailDbfieldName = 1614,
        CustomerPartnerDataTransferId = 1615,

        SupplierPartnerTransaction = 1621,
        SupplierPartnerDbtableName = 1622,
        SupplierPartnerIdDbfieldName = 1623,
        SupplierPartnerEmailDbfieldName = 1624,
        SupplierPartnerDataTransferId = 1625,

        FigmaPersonalAccessToken = 1700,
    }



    public enum EmAppUserDBApplicationSettings
    {
        EnableConfigurationMode = 4,

        SystemEmailFromAddress = 11,
        SystemAgentUser = 12,               // SetupSystemAgentJob(int? systemAgentId)
        SmtpIntegrationActivation = 189,
        SmtpServer = 190,
        SmtpPort = 191,
        SmtpEnableSSL = 192,
        SmtpUserName = 193,
        SmtpPassword = 194,

        ApplicationLayoutMode = 1005,
        //DefaultPageAfterLoginForAdminUsers = 1011,
        //DefaultPageAfterLoginForEmployeeUsers = 1012,
        //DefaultPageAfterLoginForCustomerUsers = 1013,
        //DefaultPageAfterLoginForSupplierUsers = 1014,
        //DefaultPageAfterLoginForClientAgentUsers = 1015,
        //DefaultPageAfterLoginForSupplierAgentUsers = 1019,


        DefaultCollapseLeftMenuForAdminUsers = 1021,
        DefaultCollapseLeftMenuForEmployeeUsers = 1022,
        DefaultCollapseLeftMenuForCustomerUsers = 1023,
        DefaultCollapseLeftMenuForSupplierUsers = 1024,
        DefaultCollapseLeftMenuForClientAgentUsers = 1025,
        DefaultCollapseLeftMenuForSupplierAgentUsers = 1029,

        EmployeeEntity = 1301, //==> point AppSecurityUser , Userdefine EmployeeId will share the smae PK with AppSecurityUser
        CustomerEntity = 1302, // // ===> point to AppBusinessPartner table, studen, customer guest , will use ParterID as Primarykey ( ParterFK will be PK)
        SupplierEntity = 1304, // // 

        CustomerAgentEntity = 1303,
        SupplierAgentEntity = 1305,

        //AgentEntity = 1306,

        GoogleApiKey = 1400,

        //EshopMyAddressesSearch = 301,
        //EshopMyOrdersSearch = 302,
        //EshopMyWishListSearch = 303,
        //EshopUserInfoTransaction = 304,
        //EshopNewProductTreeView = 305,
        //EshopTreeView = 306,
        //EshopTopProductsSearchView = 307,
        //EshopOrderDetailTransaction = 308,
        //EshopMyAccountDesktop = 309,
        //EshopUserDomain = 310,
        //EshopMyAccountExUrl = 311,
        //EshopTopNewProductsSearchView = 312,
        //EshopDefaultStoreId = 3120,

        AdminCalenarSearch = 1501,
        EmployeeCalendarSearch = 1502,
        CustomerCalendarSearch = 1503,
        SupplierCalendarSearch = 1504,
        ClientAgentCalendarSearch = 1505,
        SupplierAgentCalendarSearch = 1509,

        AdminUserTransaction = 1511,
        EmployeeUserTransaction = 1512,
        CustomerUserTransaction = 1513,
        SupplierUserTransaction = 1514,
        ClientAgentUserTransaction = 1515,
        SupplierAgentUserTransaction = 1519,

        FigmaPersonalAccessToken = 1700,
    }


    public enum EmAppViewType
    {
        GridView = 1,
        CardView = 2,
        //CoverFlowView = 3,      
        //BookView = 4,
        PivotView = 5,
        CalendarView = 6,
        ChartView = 7,
        FlatDataSetTreeView = 8,
        EShopCardView = 9,
        EShopProductDetailView = 10,
        EntityView = 11,
        WorkflowView = 12,
        SimpleFormView = 13,
        DatePickerView = 14,

        GanttView = 15,
        SchedulerView = 16,
        GoogleMapView = 17,
        RecursiveDataSetTreeView = 18,
        DateAndTimeCalendarSelectorView = 19,
        EShopOrderListView = 21,
        SliderView = 22,
        HierarchyMasterDetailView = 23,
        //MasterEmbeddedChildView = 24,
        ClusterAnalysisView = 25, //Label: Cluster Analysis View  //Tooltip: Multiple Data Analysis View   
    }

    public enum EmAppDateTimeProperties { FullDate = 1, YearNumber = 2, MonthNumber = 3, DayOfMonthNumber = 4, DayOfWeekNumber = 5, MonthName = 6, DayOfWeekName = 7 }


    public enum EmAppDashboardWidgetItemType
    {
        //ExternalPage = 1,
        InternalPage = 2,
        Directive = 3,
        DirectiveWithParameters = 4,
        //ExternalShortcut = 5,
        InternalShortcut = 6,
        SearchShortcut = 7,
        Folder = 8,
        ClusterAnalysisViewItem = 9,
        WorkflowExecuteButton = 10,

        ResponsiveContainer = 100,
        RowContainer = 101,
        ColumnContainer = 102,
    }

    public enum EmAppListMenuLinkType
    {
        SystemPage = 1,
        WebPopup = 2,
        CallBuiltInFunction = 3,
        WebPage = 9,
        ApplicationPackage = 10,
    }

    public enum EmAppLinkedSearchAction
    {
        AddFormGridRow = 1,
        UpdateFormData = 2,
        ViewSearchResult = 3,
        //UpdateFormByExternalServiceCall = 4,
        //GridRowExclusionSelector = 5,
    }

    //public enum EmAppLinkedSearchDatePickerStaticField
    //{
    //    StartDate = 1,
    //    EndDate = 2,
    //    NbWorkingDays = 3,
    //    NbAllDays = 4,       

    //}



    public enum EmAppLinkedSearchUsageType
    {
        HeaderMenuClick = 1,
        FieldOrCellClick = 2,
    }

    public enum EmAppCascadingSourceType
    {
        RelationalTable = 1,
        ExternalOneToManyMapping = 2,
        ExternalOneToOneMapping = 3,
        QueryStatement = 4,
    }

    public enum EmAppExternalSourceFrom
    {
        StoredProcedure = 1,
        ExternalMethod = 2,
    }

    public enum EmAppMenuRegisterType
    {
        RegionDomain = 1,
        User = 2,
        Organization = 3,
        Role = 4,

    }

    public enum EmAppDataServiceType
    {
        QueryText = 1,
        StoredProcedure = 2,
        PluginWebApiCall = 3,
        IntegrationWebApiCall = 4,
    }

    public enum EmAppDataSetUsageType
    {
        //SearchView = 1,
        //TransactionUnit = 2,
        Default = 1,
        ConvertSimpleObjectToList = 2,
        ErDiagram = 3,
        ExcelTableImportSetting = 4,
        ExcelEntityImportSetting = 5,
        DbToDbTableImportSetting = 6,

        DatabaseTable = 11,
        DatabaseView = 12,
        DatabaseStoredProcedure = 13,
    }

    public enum EmAppSecuritySysObjType
    {
        Transaction = 1,
        TransactionField = 2,
        TransactionUnit = 3,
        Search = 4,
        SearchView = 5,
        Report = 6,
        TransactionUnitLinkedSearch = 7,
        Dashboard = 8,
        TransactionAction = 9,
        TransactionUnitAction = 10,
        Menu = 11,
        TransactionCommand = 12,

    }

    public enum EmAppSearchUsageType
    {
        Management = 1,
        PopulateData = 2,
        Report = 3,
        QuickSearch = 4,
        MyLastModify = 5,
        DataModelTemplate = 6,
        EshopCategorySearch = 7,
    }


    public enum EmAppApplicationSettingValueType
    {
        Unknown = 0,
        Integer = 1,
        List = 2,
        Boolean = 3,
        Text = 4,
        Password = 5,
        ProductFolder = 101,
        EntityFolder = 102,
        ImageFolder = 103,
        FileFolder = 104,
        ProjectFolder = 105,
    }


    public enum EmAppCriteriaType
    {
        Text = 0,
        Entity = 1,
        Date = 2,
        Numeric = 3,
        Boolean = 4,
        Media = 5,
        Integer = 6,
        FolderTree = 7,
        //DateRange = 8,

        GoogleAddress = 45,
    }

    public enum EmAppCriteriaSubType
    {
        DateRange = 1,
    }



    public enum EmAppAdminTheme
    {
        Skin01 = 1,
        Skin02 = 2,
        Skin03 = 3,
        Skin04 = 4,
        Skin05 = 5,
        Skin06 = 6,
        Skin07 = 7,
        Skin08 = 8,
        Skin09 = 9,
        Skin10 = 10,
        Skin11 = 11,
        //PLM = 100,
        Esite = 200,
    }

    public enum EmAppClientTheme
    {
        Skin01 = 1,
        Skin02 = 2,
        Skin03 = 3,
        Skin04 = 4,
        Skin05 = 5,
        Skin06 = 6,
        Skin07 = 7,
        Skin08 = 8,
        Skin09 = 9,
        Skin10 = 10,
        Skin11 = 11,
    }

    public enum EmAppTaskDurationUnit
    {
        Week = 3,
        Day = 4,
        Hour = 5,
        Minute = 6,
    }

    public enum EmAppProjectDirection
    {
        Forward = 1,
        Reverse = 2
    }

    public enum EmAppLanguageKeyType
    {
        SystemLabel = 1,
        Menu = 2,
        TransactionUnit = 3,
        TransactionField = 4,
        LinkTarget = 5,
        LinkedSearch = 6,
        Search = 7,
        SearchField = 8,
        SearchView = 9,
        SearchViewField = 10,
        Unknown = 100,
    }



    public enum EmOrgClassificationLevel
    {
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
        Level5 = 5,
    }


    public enum EmAppChartViewType
    {
        Area = 1,
        Bar = 2,
        Bubble = 3,
        Candlestick = 4,
        Column = 5,
        HighLowOpenClose = 6,
        Line = 7,
        LineSymbols = 8,
        Scatter = 9,
        Spline = 10,
        SplineArea = 11,
        SplineSymbols = 12,
        Pie = 21,
        Donut = 22,
        TreeMap = 23,
        MultipleTypeChart = 100,
    }



    public enum EmAppProjectWorkflowType
    {
        Project = 1,
        BusinessProcessWorkflow = 2,
    }

    public enum EmAppWorkflowTaskStageType
    {
        Start = 1,
        End = 2,
        Input = 3,
        Approval = 4,
    }

    public enum EmAppWorkflowTaskStageStatus
    {
        //Open = 1,
        //Submitted = 2,
        //Approved = 3,
        //Rejected = 4,
        //InQuery = 5,
        //Completed = 6,

        Holding = 1,
        Rejected = 2,
        Started = 3,
        Completed = 4,
        Ignored = 5,
    }


    //public enum EmAppWorkflowTaskInputAction
    //{
    //    Submit = 1,
    //}

    //public enum EmAppWorkflowTaskApprovalAction
    //{
    //    Approve = 1,
    //    Reject = 2,
    //    Query = 3,
    //}



    //public enum EmAppWorkflowActionType
    //{
    //    GoToStep = 1,
    //    UpdateTransactionField = 2,
    //    CreateNotification = 3,
    //    CreateChildWorkflow = 4,
    //    AppendUser = 5,
    //    SetUser = 6,
    //}



    //http://www.conceptdraw.com/How-To-Guide/flowchart-design
    //http://www.conceptdraw.com/How-To-Guide/picture/Designelements-Flowchart.png
    public enum EmAppWorkflowDiagramShapeType
    {
        ProcessStep = 1,
        StartStep = 2,
        EndStep = 3,

        Delay = 4,
        Data = 5,
        Document = 6,
        MultipleDocument = 7,

        Subroutine = 8,
        Preparation = 9,
        Display = 10,
        ManualInput = 11,
        ManualLoop = 12,
        LoopLimit = 13,


        StoredData = 14,
        Connector = 15,
        OffPageConnector = 16,
        Or = 20,
        SummingJunction = 21,

        Collate = 22,
        Sort = 23,
        Merge = 24,
        Database = 25,
        InternalStorage = 26,
        Decision = 30,
    }


    //public enum EmAppSecurityGroupType
    //{
    //    Security = 1,
    //    Project = 2,
    //    Workflow = 3,
    //    Other = 4,
    //}

    public enum EmAppCalendarRecurringType
    {
        ByYear = 1,
        ByMonth = 2,
        ByWeek = 3,
    }

    public enum EmAppMonth
    {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12,
    }

    public enum EmAppDayOfWeek
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        Sunday = 7,
    }

    public enum EmAppCalendarWorkState
    {
        NotWorking = 1,
        Working = 2,
    }

    public enum EmAppGroupUsage
    {
        SecurityGroup = 1,
        ProjectTeam = 2,
        ContactGroup = 3,
        MenuGroup = 4,
    }

    public enum EmAppEntityDatasourceType
    {
        Standalone = 1,
        CascadingByField = 2,
        ForeignUnitAsDatasource = 3,
        SpecialDDLQuery = 4,
    }


    public enum EmAppMessgaeScopeType
    {
        //UserEmail = 1,
        //WorkflowNotification = 2,
        //WorkflowConversation = 3,
        ////TransactionNotification = 4,
        //TransactionBlog = 5,

        Global = 1,
        Workflow = 2,
        //WorkflowConversation = 3,
        //TransactionNotification = 4,
        Transaction = 5,
        Project = 6,
        Task = 7,
        CompanyPublicMessage = 10,

        MessageTemplate = 99,
    }

    public enum EmAppMessgaePostType
    {
        SystemNotification = 1,
        Conversaction = 2,
        UserNotification = 3,
        Drat = 4,

    }



    public enum EmAppViewColumnType
    {
        [EnumMember]
        Data = 0,

        [EnumMember]
        DDL = 1,

        [EnumMember]
        Image = 2,

        [EnumMember]
        ColorSwatch = 3,
    }


    public enum EmAppDateTimeTokenType { Today = 1, Now = 2, Years = 3, Months = 4, Weeks = 5, Days = 6, Hours = 7, Minutes = 8, Seconds = 9, Null = 10 }

    public enum EmAppSearchCriteriaTokenType
    {
        CurrentYear = 2,
        CurrentHalfYear = 3,
        CurrentQuarterYear = 4,
        CurrentMonth = 5,
        CurrentHalfMonth = 6,
        CurrentBiWeek = 7,
        CurrentWeek = 8,
        Today = 10,
        Tomorrow = 11,
        Yesterday = 12,
        Now = 13,

        CurrentUser = 100,

    }

    public enum EmAppDocumentType
    {
        JPG = 1,
        GIF = 2,
        BMP = 3,
        TIF = 4,
        DWG = 5,
        AI = 6,
        EXCEL = 7,
        Video = 8,
        PDF = 9,
        WORD = 10,
        Compressed = 11,
        TXT = 12,
        PNG = 13,
        PSD = 14,
        SVG = 15,
        INDD = 16,
        Unknown = 99,
        PPT = 100
    }


    public enum EmAppAuthenticationMode
    {
        PLM,
        AD,
        Mix

    }



    public enum EmAppSysTransactionActionCode
    {
        LoadDataLoad = 1,
        OpenCpfFlow = 2,
        OpenCommnuication = 3,
        CalculationAndValidation = 4,
        GenerateMatrixGrid = 5,
        Save = 6,
        SaveAs = 7,
        Print = 8,
        Refresh = 9,
        //EditEntities = 10,
        RunUserCommands = 11,
    }

    public enum EmAppSysTransactionUnitActionCode
    {
        AddRow = 1,
        RemoveRow = 2,
        OpenLinkedSearch = 101,
        CreateAction = 201,
        EditAction = 202,
        DeleteAction = 203,
        PreviewAction = 204,
        CallExternalMethodAction = 205,
        EditUserLoginAction = 206,

        CreateFolder = 301,
        DeleteFolder = 302,
        EditFolder = 303,
        EditFolderSecurity = 304,

        UploadFileToFolder = 305,
        PasteFileToFolder = 306,
    }



    //public enum EmAppFolderTransactionUsageType
    //{
    //	File = 1,
    //	Image = 2,
    //	FormData = 3,
    //	Other = 4,
    //}


    public enum EmAppTransBusinessType
    {
        FormData = 1,
        File = 2,
        Image = 3,
        LocalFile = 4,
    }



    public enum EmAppDataServerType
    {
        SqlServer = 1,
        Oracle = 2,
        MySql = 3,
        PostgreSql = 4,
        Db2 = 5,
    }




    public enum EmAppFileFolderCategory
    {
        MyRecentlyFiles = 1, // Logical Folder
        Favorites = 2,      // Logical Folder       
        Company = 3,        // Set in AppSetup
        Private = 4,    // User Profile
        Public = 5,         // Set in AppSetup
        ShareToOthers = 6,  // Logical Folder
        SharedToMe = 7,     // Logical Folder  
        //Tempoary = 8        // Set in AppSetup
        MyRecycleBin = 9,     // Logicl Folder
    }



    public enum EmAppTransNavigationType
    {
        SearchNavigation = 1,
        FolderNavigation = 2,
    }


    public enum EmAppFolderSearchOption
    {
        CurrentFolder = 1,
        AllFolders = 2,
        SubFolders = 3,
        MyOwnFiles = 4,
    }


    public enum EmAppFileNotificationType
    {
        UserDefine = 1,
        FileSharingChanged = 2,
        FileChanged = 3,

    }


    public enum EmAppBuseinssScope
    {
        Transaction = 1,
        Project = 2,
        Workflow = 3,
        Activity = 4,
        SearchView = 5,
        File = 6,
    }

    public enum EmAppGanttDisplayUnit
    {
        Week = 1,
        Month = 2,
        Year = 3,
    }


    public enum EmAppProjectPrivacy
    {
        Individual = 1,
        CrossDomain = 2,
        Organization = 3,
    }

    public enum EmAppProjectPostPrivacy
    {
        Individual = 1,
        CrossDomain = 2,
        Organization = 3,
    }

    public enum EmAppProjectDisplayLayoutType
    {
        TaskList = 1,
        BoardSummary = 3,
    }


    public enum EmAppProjectTaskType
    {
        Task = 1,
        Milestone = 2,
        Deliverable = 3,
        Issue = 4,
    }


    public enum EmAppProjectCostType
    {
        ByRole = 1,
        ByPerson = 2,
    }



    public enum EmAppProjectTaskPriority
    {
        Critical = 1,
        Hight = 2,
        Normal = 3,
        Low = 4,
    }


    public enum EmAppProjectStage
    {
        Planning = 1,
        Processing = 2,
        Completed = 3,
    }

    public enum EmAppProjectTaskStage
    {
        NotStarted = 1,
        Started = 2,
        Completed = 3,
    }

    public enum EmAppProjectTaskProgress
    {
        NotStarted = 1,
        Started = 2,
        HalfwayDone = 3,
        AlmostDone = 4,
        Done = 5,

    }

    public enum EmAppProjectTaskStatus
    {
        Completed = 1,
        OnSchedule = 2,
        Late = 3,
        AtRisk = 4,
        NotAvailable = 5,

    }



    public enum EmAppProjectTaskChangeType
    {
        StartDateChange = 1,
        EndDateChange = 2,
        BothStartDateAndEndDataChange = 3,
        AmountOfTimeChange = 4,
        TimeUnitChange = 5,
    }


    public enum EmApprProjectTaskTimeSheetEntryMethod
    {
        NumberOfHours = 1,
        CalendarTimeRange = 2,
    }




    public enum EmAppTaskSystemDefinedCategory
    {
        ProjectTask = 1,
        WorkflowTask = 2,
        SimpleFormTask = 3,
        UserDefinedFreeTask = 4,
    }

    public enum EmApprTaskViewOption
    {
        Incomplete = 1,
        Completed = 2,
        All = 3,
    }

    public enum EmAppTaskOwnerDeliverPhase
    {
        New = 1,    // When a task is first assigned to you, it will appear under the New Tasks priority section. From there, you have the option to mark your tasks to one of the other priority sections; Today, Upcoming, or Later.
        Today = 2,      //  •Place tasks you’re working on today in the Today section.
        Upcoming = 3,     //  •Place tasks you’re working on this week in the Upcoming section
        Later = 4,      //  •Place tasks you’re working on next week or later in the Later section
    }



    public enum EmAppTaskDueDateType
    {
        NoDueDate = 1,
        Earlier = 2,
        Today = 3,
        ThisWeek = 4,
        NextWeek = 5,
        Later = 6,
    }



    #endregion


    public enum EmAppAuthenticationResult
    {
        NotFound = 1,
        InActive = 2,
        InvalidPassword = 3,
        LoginSucceful = 4,
        SaasUserRegisterNotComplete = 5,
        AccessDenied = 6,
        UserNotLinkedToEStoreUserDB = 7,
        LockedByTooManyWrongPassword = 8,
        NewUserNotActivedByEmail = 9,
    }


    public enum EmAppReportEngineType
    {
        ActiveReport = 1,
        CrystalReport = 2,
    }

    //pivot.js
    //aggregators = (function(tpl)
    //{
    //    return {
    //        "Count": tpl.count(usFmtInt),
    //            "Count Unique Values": tpl.countUnique(usFmtInt),
    //            "List Unique Values": tpl.listUnique(", "),
    //            "Sum": tpl.sum(usFmt),
    //            "Integer Sum": tpl.sum(usFmtInt),
    //            "Average": tpl.average(usFmt),
    //            "Minimum": tpl.min(usFmt),
    //            "Maximum": tpl.max(usFmt),
    //            "Sum over Sum": tpl.sumOverSum(usFmt),
    //            "80% Upper Bound": tpl.sumOverSumBound80(true, usFmt),
    //            "80% Lower Bound": tpl.sumOverSumBound80(false, usFmt),
    //            "Sum as Fraction of Total": tpl.fractionOf(tpl.sum(), "total", usFmtPct),
    //            "Sum as Fraction of Rows": tpl.fractionOf(tpl.sum(), "row", usFmtPct),
    //            "Sum as Fraction of Columns": tpl.fractionOf(tpl.sum(), "col", usFmtPct),
    //            "Count as Fraction of Total": tpl.fractionOf(tpl.count(), "total", usFmtPct),
    //            "Count as Fraction of Rows": tpl.fractionOf(tpl.count(), "row", usFmtPct),
    //            "Count as Fraction of Columns": tpl.fractionOf(tpl.count(), "col", usFmtPct)
    //        };
    //})(aggregatorTemplates);

    public enum EmAppPivotAggregationType
    {
        Count = 1,
        CountUnique = 2,
        ListUniqueValues = 3,
        Sum = 4,
        IntegerSum = 5,
        Average = 6,
        Minimum = 7,
        Maximum = 8,
        SumOverSum = 9,
        UpBound80 = 10,
        LowerBound80 = 11,
        SumAsFractionOfTotal = 12,
        SumAsFractionOfRows = 13,
        SumAsFractionOfColumns = 14,
        CountAsFractionOfTotal = 15,
        CountAsFractionOfRows = 16,
        CountAsFractionOfColumns = 17,
    }

    public enum EmAppWijmoPivotAggregationType
    {
        Avg = 3, // Returns the average value of the numeric values in the group.
        Cnt = 2, // Returns the count of non-null values in the group.
        CntAll = 11, // Returns the count of all values in the group (including nulls).
        First = 12, // Returns the first non-null value in the group.
        Last = 13, // Returns the last non-null value in the group.
        Max = 4, // Returns the maximum value in the group.
        Min = 5, // Returns the minimum value in the group.
        //None = 0,
        Rng = 6, // Returns the difference between the maximum and minimum numeric values in the group.
        Std = 7, // Returns the sample standard deviation of the numeric values in the group (uses the formula based on n-1).
        StdPop = 9, // Returns the population standard deviation of the values in the group (uses the formula based on n).
        Sum = 1, // Returns the sum of the numeric values in the group.
        Var = 8, // Returns the sample variance of the numeric values in the group (uses the formula based on n-1).
        VarPop = 10, // Returns the population variance of the values in the group (uses the formula based on n).

    }


    public enum EmAppExportExcelType
    {
        LanguageKey = 1,
    }

    public enum EmAppNotificationMessageUsageType
    {
        ICSFormatQuery = 1,
        TargetDateQuery = 2,
        NotificationContentQuery = 3,
    }


    public enum EmAppMessageScanPeriod
    {
        Day = 1,
        Hour = 2,
        Minutes = 3,

    }

    public enum EmAppDatePickerSearchViewField
    {
        CalendarDate = 1,
        IsHoliday = 2,
        IsDisabledDay = 3,
        IsSelectedFullDay = 4,
        IsSelectedHalfDay = 5,
        DateRangeType = 6,
    }

    public enum EmAppDockPosition
    {
        Top = 1,
        Right = 2,
        Bottom = 3,
        Left = 4,
    }

    public enum EmAppConversationGroupByType
    {
        GroupByAllUser = 1,
        GroupBySupplierUser = 2,
        GroupByCustomerUser = 3,
        GroupByEmployeeUser = 4,
        GroupBySupplier = 5,
        GroupByCustomer = 6,
        GroupByClientAgent = 7,
        GroupByClientAgentUser = 8,
        GroupBySupplierAgent = 9,
        GroupBySupplierAgentUser = 10,
    }

    public enum EmAppConversationFilterByType
    {
        FilterByCurrentUser = 1,
        FilterByCurrentSupplier = 2,
        FilterByCurrentCustomer = 3,
    }

    public enum EmAppDeviceMenuShowMode
    {
        OnlyForDesktop = 1,
        OnlyForMobile = 2,
        ForBoth = 3,
        Disable = 4,
    }

    public enum EmAppCalendarDateDefineType
    {
        Predefined = 1,
        UserRequested = 2,
    }

    public enum EmAppCalendarDateRangeType
    {
        FullDay = 1,
        HalfDayAM = 2,
        HalfDayPM = 3,
        Range = 4,
    }

    public enum EmAppCalendarViewEventType
    {
        Unspecified = 1,
        Task = 2,
        LeaveDay = 3,
    }

    public enum EmAppCalendarViewEventCompletStage
    {
        NotCompleted = 1,
        Completed = 2,
    }

    public enum EmAppCalendarViewEventLinkType
    {
        Form = 1,
        Project = 2,
    }


    public enum EmAppMassUpdateViewType
    {
        SingleTableUpdate = 1,
        HierarchicalTableUpdate = 2,
    }


    public enum EmAppIntergrationType
    {
        EmbedTranscationToHardingCodeForm = 1,
        ExternalDB = 2,
        ResstAPI = 3,
    }

    public enum EmAppCanlendarMode
    {
        MonthView = 1,
        WeekView = 2,
        DayView = 3,
    }

    public enum EmAppSearchFieldSubControlType
    {
        DDLPullDown = 1,
        DDLDisplayAll = 2,
        TimeZoneTokenInput = 3,
    }

    public enum EmAppLinkTargetUsageType
    {
        SearchViewLinkToForm = 1,
        SearchViewLinkToFormGroup = 2,
        SearchViewLinkToSearch = 3,
        SearchViewLinkToReport = 4,
        SearchViewLinkSystemDefinedPage = 5,

        TransactionUnitLinkToForm = 101, // Example: Open Bom Item
    }

    public enum EmAppLinkTargetActionType
    {
        Edit = 1,
        Create = 2,
        Delete = 3,
        //Refresh = 4,
        Preview = 5,
        CallExternalMethod = 6,
        Report = 7,
        EditUserLogin = 11,
        EditOnPopup = 12,
        CreateFromExistingItem = 13,
        CopyEvent = 14,
        CutEvent = 15,
        PasteEvent = 16,
        ExecuteTransactionCommand = 17,
    }


    public enum EmAppSearchViewLinkTargetActionType
    {
        Edit = 1,
        Create = 2,
        Delete = 3,
        //Refresh = 4,
        Preview = 5,
        //CallExternalMethod = 6,
        //Report = 7,
        //EditUserLogin = 11,
        //EditOnPopup = 12,
        CreateFromExistingItem = 13,
        CopyEvent = 14,
        CutEvent = 15,
        PasteEvent = 16,
    }

    public enum EmAppTransactionUnitLinkTargetActionType
    {
        Create = 2,
        CreateFromExistingItem = 13,
        Edit = 1,

        Preview = 5,
        Delete = 3,

        //CallExternalMethod = 6,       
        //EditUserLogin = 11,
        //EditOnPopup = 12,

    }

    public enum EmAppLinkTargetSourceColumnType
    {
        SearchViewField = 1,
        SearchViewSpecialProperty = 2,
        TransactionField = 3,
    }

    public enum EmAppLinkTargetSystemDefinedPage
    {
        ProjectTaskEditor = 1,
        ProjectGanttEditor = 2,
    }

    public enum EmInternalCodeRegistration
    {
        CalendarNavigationStartDate = 1,
        CalendarNavigationEndDate = 2,
        CalendarNavigatorTriggerSearchFieldOn = 3,
        CalendarNavigatorTriggerSearchFieldOff = 4,
        SchedulerBaseDate = 5,
        EsiteId = 6,
        EsiteMenuCategory = 7,
        UptoUtcTodayDateDiff = 8,

        ClientUserDefinedParam1 = 9,
        ClientUserDefinedParam2 = 10,
        ClientUserDefinedParam3 = 11,
        ClientUserDefinedParam4 = 12,

        WorkflowTransactionId = 21,
        WorkflowTransactionRId = 22,
        WorkflowLogBatchNumber = 23,
    }

    public enum EmInternalCodeRegistrationForESiteOrderList
    {
        OrderId = 101,
        OrderNumber = 102,
        OrderDescription = 103,
        OrderCustomerId = 104,
        OrderSubTotal = 105,
        OrderTotalShipping = 106,
        OrderTotalTax = 107,
        OrderTotalPrice = 108,
        OrderInvoiceId = 109,

        OrderStatus = 120,
        OrderShippingStatus = 121,
        OrderPlacedData = 130,
        OrderShippedData = 131,
        OrderDeliveredData = 132,
        OrderCanceledData = 133,
        OrderExpectedDeliverDate = 135,

        OrderIsBillingAddressSameAsShippingAddress = 150,
        OrderShippingCountry = 151,
        OrderShippingProvince = 152,
        OrderShippingCity = 153,
        OrderShippingAddress1 = 154,
        OrderShippingAddress2 = 155,
        OrderShippingZipCode = 156,
        OrderShippingFirstName = 157,
        OrderShippingLastName = 158,
        OrderShippingEmail = 159,
        OrderShippingPhoneNumber = 160,
        OrderBillingCountry = 161,
        OrderBillingProvince = 162,
        OrderBillingCity = 163,
        OrderBillingAddress1 = 164,
        OrderBillingAddress2 = 165,
        OrderBillingZipCode = 166,
        OrderBillingFirstName = 167,
        OrderBillingLastName = 168,
        OrderBillingEmail = 169,
        OrderBillingPhoneNumber = 170,

        OrderItemId = 250,
        OrderItemDescription1 = 251,
        OrderItemDescription2 = 252,
        OrderItemDescription3 = 253,
        OrderItemDescription4 = 254,
        OrderItemDescription5 = 255,
        OrderItemImageId = 256,
        OrderItemPrice = 261,
        OrderItemQty = 262,
    }

    public enum EmInternalCodeRegistrationForGoogleMapView
    {
        MapMarkerPositionObject = 301,
        MapMarkerLabelText = 302,
    }



    public enum EmAppUserContactType
    {
        Phone = 1,
        Email = 2,
    }

    public enum EmAppProjectPrivilege
    {
        CreateProject = 1,
        OpenProject = 2,
        UpdateProject = 3,
        DeleteProject = 4,
        TaskResourceCandidate = 5,

    }




    //public enum EmAppMassUpdateEditMode
    //{
    //    Added = 1,
    //    Modified = 2,
    //    Deleted = 2,
    //}


    public enum EmAppGlobalServiceAction
    {
        ConfirmNewSaasUserEmailAndCompleteRegistration = 1,
        UnlockSaasUserAccountByEmail = 2,
        UserPasswordRetrieve = 3,
        AddBusinessParternerInvitedUserToCompany = 4,
        ExecuteDataModelInternalCommand = 5,
        ActiveEsitePartnerUserByEmail = 6,
        PartnerEmailChangeVerification = 7,

        ConfirmNewSaasAccountEmailAndCreateUserCompanyDB = 11,
    }


    public enum EmAppUserCategory
    {
        BuiltInUser = 1,  // Master DB User
        SaasUser = 2,
    }

    //public enum EmAppFromLayoutItemBindingType
    //{
    //    TransField = 1,
    //    GridTransUnit = 2,
    //}

    public enum EmAppTransactionUnitLevel
    {
        Root = 1,
        Child = 2,
        Grandchild = 3,
    }

    public enum EmAppApplicationAssetsType
    {
        Form = 1,
        Transaction = 2,
        Workflow = 3,
        Search = 4,
        Dashboard = 5,
        Report = 6,

    }

    public enum EmAppApplicationBuilderSection
    {
        Form = 1,
        Transaction = 2,
        Workflow = 3,
        //Search = 4,
        Dashboard = 5,
        Report = 6,
        Security = 7,
        Collaboration = 8,
        Database = 9,
        Integration = 10,
        MenuSetting = 11,
        DataPresentation = 12,
        DataManipulation = 13,
        ApplicationSetting = 14,
        ListOfValue = 15,
        Website = 16,
        TransactionGroup = 17,


        SearchManagement = 121,
        DataSetManagement = 122,
    }


    public enum EmAppSaasTableFilterOption
    {
        AllTable = 1,
        AllSystemTable = 2,
        ByApplication = 3,
        AllView = 4,
    }

    public enum EmAppSearchViewGridOutputMode
    {
        RegularGrid = 1,
        FinancialGrid = 2,
    }

    public enum EmAppTransactionDataTransferType
    {
        FromDataModelToMessageTemplate = 1,
        FromDataModelToDataModel = 2,
        FromApiToDataModel = 3,
        FromDataModelToApi = 4
    }

    public enum EmAppTransactionGridDisplayType
    {
        RegularGrid = 1,
        TreeGrid = 2,
        PivotEditGrid = 3,
        PivotViewGrid = 4,
        AvailableSelectGridPair = 5,
        MultipleSelectBox = 6,       
        ChildUnitPivotColumns = 7,
    }


    public enum EmAppBarCodeType
    {
        CODE128 = 1,
        CODE128A = 2,
        CODE128B = 3,
        CODE128C = 4,
        EAN13 = 5,
        EAN8 = 6,
        EAN5 = 7,
        EAN2 = 8,
        UPC = 9,
        CODE39 = 11,
        ITF14 = 12,
        MSI = 15,
        MSI10 = 16,
        MSI11 = 17,
        MSI1010 = 18,
        MSI1110 = 19,
        pharmacode = 20,
        codabar = 21,
        QrCode = 22,
    }

    public enum EmAppMobileTopMenuUploadButotnUsageType
    {
        AddFileToFileManagementUserDefaultFolder = 1,
        AttachFileToNewFormPage = 2,
        AttachFileToNewMessage = 3,
    }

    public enum EmAppBuiltInUserGroup
    {
        Default = 1,
        Employee = 2,
        Customer = 3,
        Supplier = 4,
        ClientAgent = 5,
        SupplierAgent = 9,
        CompanyAdmin = 6,
    }

    public enum EmAppSaasUserAvailableCompanyType
    {
        MyCompany = 1,
        BusinessParternerCompany = 2,

    }



    public enum EmAppLinkTargetPageLayoutMode
    {
        NewPage = 1,
        Popup = 2,
    }


    public enum EmAppListSubMenuDisplayType
    {
        DropDownMenu = 1,
        TabGroup = 2,
    }

    public enum EmAppWarningHighlightPriority
    {
        LowPriority = 1,
        MiddlePriority = 2,
        HighPriority = 3,
    }

    public enum EmAppApplicationLayoutMode
    {
        LeftRightTabLayout = 1,
        SingleApplicationTopDownLayout = 2,
        SingleApplicationLeftRightLayout = 3,
    }


    //public enum EmAppWjPivotAggregationType
    //{        
    //    Avg = 3,
    //    Cnt = 2,
    //    CntAll = 11,
    //    First = 12,
    //    Last = 13,
    //    Max = 4,
    //    Min = 5,
    //    None = 0,
    //    Rng = 9,
    //    Std = 7,
    //    StdPop = 9,
    //    Sum = 1,
    //    Var = 8,
    //    VarPop = 10,
    //}


    //public enum EmAppEStoreLayout
    //{
    //    ProductFilterOnTheLeft = 1,
    //    ProductFilterOnTheTop = 2,
    //    SimpleProductList = 3,
    //}

    public enum EmAppEStoreTheme
    {
        Green = 1,
        Red = 2,
    }

    public enum EmAppEsiteLayoutType
    {
        Layout1 = 1,
        Layout2 = 2,
        Layout3 = 3,
        Layout4 = 4,
        Layout5 = 5,
        Layout6 = 6,
        Layout7 = 7,
        Layout8 = 8,
    }



    public enum EmAppShoppingBagFieldType
    {
        // Single 
        ProductMasterId = 1,
        MappingSearchFieldId = 2,


        // Mutiple Desc
        ProductDescription = 3,
        // Mutiple Key  VariantOption
        ProductOptionId = 4,
        ProductOptionDisplayText = 5,



        // Detail filed Sku related ! filed

        SKUId = 11,
        SKUPrice = 12,
        SKUAvailableQty = 13,
        SKUSelectedQty = 14,
        SKUDescription = 15,
        SKUImage = 16,

    }


    public enum EmEstoreLayoutView
    {
        TreeCardListCardDetail = 1,
        TreeCardList = 2,
        CardList = 3,
        CardListCardDetail = 4,


    }


    public enum EmAppESiteApplicationType
    {
        ECommerce = 1,
        NonECommerce = 2,
        //MgtDesktopDefaultSite = 3,
        //MgtMobileDefaultSite = 4,

        NextJsApplication = 3,
    }


    public enum EmAppWebsitePageType
    {
        HTMLPage = 1,
        SiteJavascript = 2,

        // Only for running time load
        NavigationCtrlJavascript = 3,
        RouteStateJavascript = 4,
        SiteCSS = 5,
        Json = 6,
        MgtNavigationCtrlJavascript = 7,
        MgtRouteStateJavascript = 8,

        SiteTypescript = 12,

        Other = 100,
    }

    public enum EmAppExternalLoginType
    {
        Google = 1,
        Facebook = 2,
        Apple = 3,
        Twitter = 4,
        CompanyAPIUser = 88,
        Integration = 98,
        Anonymous = 99,


    }

    public enum EmAppWebPageContainerControlDisplayType
    {
        BlankPage = 1,
        StandardPage = 2,
        WizardSubPage = 3,

        PageHeader = 10,
        PageSection = 11,
        PageFooter = 12,
        LeftAndRightNavButton = 13,

        ItemDefaultContainer = 20,
        HorizontalFlexDiv = 21,
        VerticalFlexDiv = 22,
        ResponsiveRowItem = 23,



        FormConversationBox = 33,


    }

    public enum EmAppWebPageUiControlDisplayType
    {
        TextDisplay = 1,
        InputBox = 2,
        TextArea = 3,
        Checkbox = 4,
        DropdownList = 5,
        ButtonGroupSelection = 6,

        DateSelection = 7,
        TimeSelection = 8,
        DateTimeSelection = 9,

        ImageBox = 10,
        FileBox = 11,
        Video = 12,
        Audio = 13,
        UploadButton = 14,

        GoogleMap = 15,
        GoogleAddress = 16,

        RatingControl = 17,
        YoutubeVideo = 18,
        HtmlContent = 19,

        ItemList = 101,
        Table = 102,
        FlexGrid = 103,
        MultiSelectBox = 104,
        ResponsibleGridLayout = 105,

        SplittedTextBlock = 106,

        CardViewFieldBinding = 999,

    }



    public enum EmAppWebPageSearchViewDisplayType
    {
        CardView = 101,
        Table = 102,
        FlexGrid = 103,
    }


    public enum EmAppCalendarRepeatSimpleSetting
    {
        DoesNotRepeat = 0,
        Daily = 1,
        WeeklyOnThisWeekday = 2,
        MonthlyOnThisWeekAndWeekDay = 3,
        AnnuallyOnThisDate = 4,
        EveryWeekdayMondayToFriday = 5,
        Custom = 6,
    }

    public enum EmAppCalendarRepeatTimeUnit
    {
        Day = 1,
        Week = 2,
        Month = 3,
        Year = 4,
    }

    public enum EmAppCalendarRepeatEndType
    {
        NeverEnd = 1,
        EndOnDate = 2,
        EndAfterNumberOfOccurrences = 3,
    }

    public enum EmAppCalendarRepeatSettingApplyToRange
    {
        ThisEvent = 1,
        ThisAndFollowingEvents = 2,
        AllEvents = 3,
    }

    public enum EmAppMenuItemCategory
    {
        ManagementPage = 1,
        PublicPage = 2,
        SupplierPage = 3,
        ClientPage = 4,
        Component = 5,    
        ResourceImage = 6
    }

    public enum EmAppEsitePageCategory
    {
        BuiltInPage = 1,
        PublicPage = 2,
        SupplierPage = 3,
        ClientPage = 4,
        //Component = 5,
    }

    public enum EmAppYesNo
    {
        Yes = 1,
        No = 2,
    }


    public enum EmAppTransFieldExpressionType
    {
        FieldId = 1,
        UnitId = 2,
        DisplayName = 5,
        DBColumnName = 6,
        Value = 7,
        FilenameDisplay = 8,
        Tooltip = 9,

        IsReadOnly = 11,
        MaxLength = 12,
        IsRequired = 13,
        IsSiblingUnit = 14,
        GoogleMapLocationFieldDBColumnName = 15,
        RatingMaxValue = 16,
        MaxValue = 17,

        ItemSource = 21,
        ErrorMessage = 22,
        UiValidationFunction = 23,
        DdlControlBinding = 24,
        RowItemBinding = 25,
        FieldLevel = 26,
    }

    public enum EmAppSearchViewFieldExpressionType
    {
        ViewFieldId = 1,
        SearchViewId = 2,
        Name = 3,
        EntityId = 4,
        ValueBinding = 5,
    }

    public enum EmAppTransUnitExpressionType
    {
        UnitId = 1,
        DataBaseTableName = 2,
        DisplayName = 3,
        SrcUnitId = 4,
        SubscribeUnitId = 5,

        SrcFieldDBName = 6,

        SubscribeFieldId = 7,
        SubscribeFieldDbName = 8,
        SubscribeFieldDisplayName = 9,

        SrcDisplayFieldDbName = 10,

        IsChildUnitExclusiveForOwner = 11,

        ItemFormatter = 21,
        ChildItemsPath = 22,
        GridStyleClass = 23,

        ParentUnitId = 24,
    }


    public enum EmAppWebsiteComponentType
    {
        UI = 1,
        Container = 11,

        FormFieldDDL = 101,
        FormFieldText = 102,
        FormFieldMemo = 103,
        FormFieldCheckbox = 104,
        FormFieldImage = 105,
        FormFieldFile = 106,
        FormFieldAudio = 107,
        FormFieldVideo = 108,
        FormFieldDate = 109,
        FormFieldTime = 110,
        FormFieldDateTimeDetail = 111,
        FormFieldNumeric = 112,
        FormFieldBarCode = 113,
        FormFieldGoogleMap = 114,
        FormFieldGoogleAddress = 115,
        FormFieldRating = 116,
        FormFieldTextDisplay = 117,
        FormFieldYoutubeVideo = 118,
        FormFieldExternalImageUrl = 119,


        FormGridChildUnit = 190,
        FormGridChildUnitAvailableSelect = 191,
        //FormGridGrandChildUnit = 192,
        //FormGridGrandChildUnitAvailableSelect = 193,
        //FormGridColumnDDL = 201,
        //FormGridColumnText = 202,
        //FormGridColumnMemo = 203,
        //FormGridColumnCheckbox = 204,
        //FormGridColumnImage = 205,
        //FormGridColumnFile = 206,
        //FormGridColumnAudio = 207,
        //FormGridColumnVideo = 208,
        //FormGridColumnDate = 209,
        //FormGridColumnTime = 210,
        //FormGridColumnDateTimeDetail = 211,
        //FormGridColumnNumeric = 212,        

        SearchGridView = 301,
        SearchCardView = 302,
        SearchFlatDataSetTreeView = 303,

        SearchCardViewFieldDDL = 401,
        SearchCardViewFieldText = 402,
        SearchCardViewFieldMemo = 403,
        SearchCardViewFieldCheckbox = 404,
        SearchCardViewFieldImage = 405,
        SearchCardViewFieldFile = 406,
        SearchCardViewFieldAudio = 407,
        SearchCardViewFieldVideo = 408,
        SearchCardViewFieldDate = 409,
        SearchCardViewFieldTime = 410,
        SearchCardViewFieldDateTimeDetail = 411,
        SearchCardViewFieldNumeric = 412,
        SearchCardViewFieldBarCode = 413,
        SearchCardViewFieldRating = 416,
        SearchCardViewFieldTextDisplay = 417,
        SearchCardViewFieldYoutubeVideo = 418,
        SearchCardViewFieldExternalImageUrl = 419,



    }


    public enum EmAppWebsiteComponentSubPartType
    {
        FormFieldContainer = 100,
        FormFieldLabelFixed = 101,
        FormFieldLabelAutoShow = 102,
        FormFieldErrorMessage = 103,
        FormFieldDdlButtonGroup = 104,
        FormFieldDdlComboBox = 105,
        FormFieldDdlReadOnlyText = 106,
        FormFieldTextInputBox = 107,
        FormFieldTextReadOnlyText = 108,
    }


    public enum EmAppEsiteThirdPartControl
    {
        Wijmo = 1,
        DayPilot = 2,
    }

    public enum EmAppTransactionCrudType
    {
        Create = 1,
        Read = 2,
        Update = 3,
        Delete = 4,
    }

    public enum EmAppSchemaDataSetMappingNodeProcessMode
    {
        CreateTable = 1,
        RollUpToParent = 2,
        SerializeToParent = 3,
        MapToExistingTable = 4,
        Ignore = 0,
    }

    public enum EmAppJsonNodeType
    {
        Object = 1,
        Array = 2,
        Property = 3,
    }


    public enum EmAppSecurityGroupInernalCode
    {
        AllUsers = 8,
        AllEmpoyee = 2,
        AllCustomer = 3,
        AllSupplier = 4,
        AllClientAgent = 5,
        AllSupplierAgent = 9,
        CustomerAdmin = 31,
        SupplierAdmin = 41,
        ClientAgentAdmin = 51,
        SupplierAgentAdmin = 91,
    }

    public enum EmAppDesktopLayoutType
    {
        Canvas = 1,
        Flex = 2,
    }

    public enum EmAppTransactionTemplateItemType
    {
        MainItem = 1,
        TemplateHeader = 2,
    }

    /// <summary>
    /// Controls how a Main Item's template header transactions are rendered at runtime.
    /// Show: load and expand; Hide: do not load until the user manually expands; Collapsed: load but start collapsed.
    /// </summary>
    public enum EmAppTemplateHeaderVisibility
    {
        Show = 1,
        Hide = 2,
        Collapsed = 3,
    }

    public enum EmAppWijmoOperator
    {
        Equals = 0,
        DoesNotEqual = 1,
        GreaterThan = 2,
        GreaterThanOrEqualTo = 3,
        LessThan = 4,
        LessThanOrEquaTo = 5,
        BeginsWith = 6,
        EndsWith = 7,
        Contains = 8,
        DoesNotContain = 9,
    }

    public enum EmAppImportStatus
    {
        Draft = 1,
        Released = 2,

    }
    public enum EmAppIntergrationSettingParameterUsageType
    {
        ApiOperation = 1,
        JsonFileTableImportSetting = 2,
        ApiTableImportSetting = 3,
    }

    public enum EmAppModuleConfigTable
    {
        AppListMenu = 1,

        AppReport = 2,
        AppEntityInfo = 3,
        AppEntitySimpleListValue = 4,
        AppDataSet = 5,
        AppDataSetParameter = 6,
        AppSearch = 7,
        AppSearchField = 8,
        AppSearchParameter = 9,
        AppSearchSaved = 10,
        AppSearchSavedValue = 11,
        AppSearchView = 12,
        AppSearchViewField = 13,
        AppViewLinkedSeaechOrUrl = 14,

        AppTransaction = 15,

        AppTransactionUnit = 16,

        AppTransactionField = 17,
        AppTransactionFieldAggFunction = 18,

        AppTransactionUnitDeleteFlow = 19,
        AppTransactionUnitFormula = 20,
        AppConditionalAction = 21,

        AppFormLinkTarget = 22,

        AppTransactionUnitLinkedSearch = 23,
        AppTransactionUnitSearchFieldMapping = 24,
        AppTransactionUnitSearchViewFieldMapping = 25,

        AppTransactionDataTransferSetting = 26,
        AppTransactionSaveAsMapping = 27,

        AppTransactionNavigation = 28,

        AppTransactionDataLoad = 29,
        AppTranscationDataLoadFieldMapping = 30,

        AppTranscationReport = 31,

        AppMessage = 32,

        AppProjectOrWorkFlow = 33,
        AppProjectTaskPredecessor = 34,
        AppProjectWorkFlowAction = 35,
        AppProjectWorkFlowCondition = 36,
        AppProjectWorkFlowTask = 37,
        AppForm = 38,
        AppFormLayoutItem = 39,

        AppIntergrationSetting = 40,
        AppIntergrationSettingParameter = 41,

        AppWebAPIDataExchangeSetting = 42,
        AppWinScheduleSetting = 43,

        AppApplicationAssetsItem = 44,

        AppSetup = 45,

        AppEsite = 46,
        AppEsiteCatalogue = 47,
        AppESiteNavMenu = 48,
        AppESitePages = 49,
    }

    public enum EmAppApiPayloadDataType
    {
        JSON = 1,
        FileByteArray = 2,
        ServerFilePath = 3,        
    }

    public enum EmAppDbToDbImportSourceType
    {
        DatabaseTable = 1,
        DataSet = 2,
    }

    public enum EmAppBuiltInQueryType
    {
        Insert = 1,
        Select = 2,
        Update = 3,
        Delete = 4,
    }

    public enum EmAppBatchCommandSourceFrom
    {
        DataSet = 1,
        Search = 2,
        ChildUnit = 3,
    }
       

    public enum EmAppValidationResultPreference
    {
        ShowResultDetails = 1,
        ShowResultStatusOnly = 2,       
    }
    public enum EmAppCommandLoggingPreference
    {
        LogResultDetails = 1,
        LogResultStatusOnly = 2,
        DoNotLog = 3,
    }

    public enum EmAppTransactionScopeUsage
    {
        BusinessUiApplication = 1,
        WorkflowAutomation = 2,
        
    }

    public enum EmAppLinkTargetApplyToRowRangeType
    {
        IndividualRow = 1,
        SelectedRows = 2,
        IrrelevantRow = 3,

    }

    public enum EmAppDataFieldStoreMode
    {
        DatabaseTable = 1,
        TemporaryField = 2,
        ExtendTable = 3,
    }

    public enum EmAppWorkflowProgressStatus
    {
        Started = 1,
        Completed = 2,       
        StoppedWithError = 3,
        ForceAbortedByUser = 4,
    }

    public enum EmAppCommandProgressStatus
    {
        Running = 1,
        Completed = 2,
        StoppedWithError = 3,
        ForceAbortedByUser = 4,
        Ignored = 5,
        CompletedWithError = 6,
    }

    public enum EmAppApiSystemEnvironmentVariable
    {
        BaseUrl = 1,
        Authorization = 2,     
    }


    //public enum EmAppApiDataStructureNodeType
    //{
    //    Object = 1,
    //    Array = 2,
    //}



    //public enum EmAppEsiteComponentType
    //{
    //    RegularUIComponent = 1,
    //    DataFieldComponent = 2,
    //    SearchViewComponent = 3,
    //}

    //public enum EmAppEsiteComponentType
    //{
    //    RegularUIComponent = 1,
    //    DataFieldComponent = 2,
    //    SearchViewComponent = 3,
    //}

    public static class AppSystemConstants
    {
        /// <summary>
        /// Reserved AppCompanyId for the host/platform company. Always 1.
        /// </summary>
        public const int HostCompanyId = 1;
    }
}