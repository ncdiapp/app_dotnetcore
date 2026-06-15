using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Com.Visual2000.LBL.EntityExtendEnum
{
    public static class ContractNamespaces
    {
        public const string Core = "http://visual-2000.com/plms/";
        public const string Dto = Core + "dto/";
    }


    internal enum EmBlockFormulaType { Assignment = 1, BooleanExpression = 2, };

    //internal enum EmBlockFunctionType { Floor = 1, Ceiling = 2, Round = 3, ToLower = 4, ToUpper = 5, Abs = 6 }

    internal enum EmDateTimeTokenType { Today = 1, Now = 2, Years = 3, Months = 4, Weeks = 5, Days = 6, Hours = 7, Minutes = 8, Seconds = 9, Null = 10, }

    //internal enum EmAggregationFunctionType { SUM = 1, AVG = 2, Min = 3, Max = 4, RowCount = 5, BooleanSum = 6, };

    #region --- Administration Module Enumerations

    internal enum EmSecurityType
    {
        Group,
        User
    }

    
    internal enum EmViewType
    {
    
        GridView = 0,
     
        CardView = 1,

        CoverFlowView = 2,
   
        BookView = 3,
    }

 
    internal enum EmDomainType
    {
        [EnumMember]
        Anonymous = 0,
        [EnumMember]
        Vendor = 1,
        [EnumMember]
        Customer = 2,
        [EnumMember]
        Agent = 3,
        [EnumMember]
        Employee = 4,
        [EnumMember]
        AppAdmin = 5,
        [EnumMember]
        SysAdmin = 6
    }


    internal enum EmApplicationSettingValueType
    {
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Integer = 1,
        [EnumMember]
        List = 2,
        [EnumMember]
        Boolean = 3,
        [EnumMember]
        Text = 4,
    }


    internal enum EmNotificationType
    {
        [EnumMember]
        Undefined = 0,
        [EnumMember]
        Product = 1,
        [EnumMember]
        Quotes = 2,
        [EnumMember]
        Samples = 3,
        [EnumMember]
        Workflow = 4,
        [EnumMember]
        Message = 5,
        [EnumMember]
        Blogs = 6,
        [EnumMember]
        PrintJob = 7,
    }

  
    internal enum EmNotificationStatus
    {
     
        Read,
      
        Unread
    }

  
    internal enum EmNotificationEditorView
    {
        [EnumMember]
        New,
        [EnumMember]
        Forward,
        [EnumMember]
        Reply,
        [EnumMember]
        ReplyAll
    }

    internal enum    EmNotificationRootType
    {
        Inbox = 1,
        SentItems = 2,
        Outbox = 3,
    }

    internal enum    EmNotificationFolderType
    {
        Folders = 1,
        SearchFolders = 2,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmApplicationSettingCode
    {
        [EnumMember]
        DefaultWareHouse,
        [EnumMember]
        WorkingCurrency,
        [EnumMember]
        DefaultDimension,
        [EnumMember]
        DefaultCategory,
        [EnumMember]
        EnableProductTabChangePrompt,
        [EnumMember]
        DefaultMessageForRequestedQuotes,
        [EnumMember]
        DefaultMessageForSubmmitedQuotes,
        [EnumMember]
        IsPLMRunStandAlone,
        [EnumMember]
        ProductCopyDefaultPrefixCode,
        [EnumMember]
        NumberOfItemPerList,
        [EnumMember]
        DefaultImageReferenceTemplate,
        [EnumMember]
        ProductToolsBarFloating,
        [EnumMember]
        ApplicationUrl,
        [EnumMember]
        WebServiceUrl,
        [EnumMember]
        SmtpServer,
        [EnumMember]
        SystemEmailFromAddress,
        [EnumMember]
        SystemEmailFromDisplay,
        [EnumMember]
        ConvertPath,
        [EnumMember]
        ConvertApplication,
        [EnumMember]
        AutoSaveTechPack,
        [EnumMember]
        DefaultFileReferenceTemplate,
        [EnumMember]
        NextQuoteCode,
        [EnumMember]
        ReportJobExportPath,
        [EnumMember]
        ShowRelatedEntityCountInFolderTree,
        [EnumMember]
        DefaultDivision,
        [EnumMember]
        DynamicReportRepositoryPath,
        [EnumMember]
        EnableFolderSecurity,
        [EnumMember]
        EnablePrintDivisionHeader,
        [EnumMember]
        AutoSubscribeCreator,
        [EnumMember]
        AutoSubscribeManager,
        [EnumMember]
        AutoSubscribeVendor,
        [EnumMember]
        DefaultFolderSecurityOwnerRead,
        [EnumMember]
        DefaultFolderSecurityOwnerWrite,
        [EnumMember]
        DefaultFolderSecurityOwnerList,
        [EnumMember]
        DefaultFolderSecurityOwnerModify,
        [EnumMember]
        DefaultFolderSecurityOwnerSecurity,
        [EnumMember]
        AutoAddFolderSecurityOwner,
        [EnumMember]
        MaxReferencedObjectsForDisplay,
        [EnumMember]
        PLMDWDBConnection,
        [EnumMember]
        PLMAPPDBConnection,
        [EnumMember]
        PLMDWRealTimeTurnOnOff,
        [EnumMember]
        AutoAddParentFolderSecurityResource,
        [EnumMember]
        DynamicReportWebViewURL,
        [EnumMember]
        AdministratorEmailAddress,
        [EnumMember]
        EnableBackgroundColorIfFitOutOfTolerance,
        [EnumMember]
        EnableBackgroundColorIfFitWithinTolerance,
        [EnumMember]
        EnableFitMatchBackGroudColor,
        [EnumMember]
        EnableBackgroundColorIfPOMIsCriticalPoint,
        [EnumMember]
        EnableNotification,
        [EnumMember]
        ERPExchangeTab,
        [EnumMember]
        IntegratedWithV2kERP,
        [EnumMember]
        DefaultPOMUnitOfMeasure,
        [EnumMember]
        EnableColorCopyShorcutForAdminOnly,
        [EnumMember]
        EnableProductCodeAutoGeneration,
        [EnumMember]
        DefaultPOMUnitOfMeasureCentemeterScale,
        [EnumMember]
        ImageConvertDensity,
        [EnumMember]
        VendorSampleRequestDefaultTemplate,
        [EnumMember]
        VendorSampleRequestDefaultMassUpdateView,
        [EnumMember]
        VendorQuoteRequestDefaultTemplate,
        [EnumMember]
        VendorQuoteRequestDefaultMassUpdateView,
        [EnumMember]
        IntergrationWithVisualRetail,
        [EnumMember]
        LicenseKey,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmMenuRegisterType
    {
        [EnumMember]
        RegionDomain,
        [EnumMember]
        User
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmListMenuItemType
    {
        [EnumMember]
        Web = 0,
        [EnumMember]
        System = 1,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmMassUpdateType
    {
        [EnumMember]
        TabField = 1,
        [EnumMember]
        DynamicMatrix = 3,
        [EnumMember]
        RegularGrid = 2,
        //[EnumMember]
        //TechPackTabGridView = 4,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmAutoNumberConcatenationType
    {
        [EnumMember]
        BlockField = 1,
        [EnumMember]
        AutoNumber = 2,
        [EnumMember]
        ConstantString = 3,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmAutoNumberStartSideType
    {
        [EnumMember]
        Left = 1,
        [EnumMember]
        Right = 2,
    }

    #endregion --- Administration Module Enumerations

    #region --- Design Module Enumerations

    internal enum    EmDataType
    {
        [EnumMember]
        String = 1,

        [EnumMember]
        Integer = 2,

        [EnumMember]
        Double = 3,

        [EnumMember]
        Decimal = 4,

        [EnumMember]
        DateTime = 5,

        [EnumMember]
        Date = 6,

        [EnumMember]
        Boolean = 7,

        [EnumMember]
        UserDefine = 8,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmBlockSubitemValidatorType
    {
        [EnumMember]
        RequiredFields = 1,
        [EnumMember]
        Others = 2
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmBlockEditType
    {
        [EnumMember]
        Regular,
        [EnumMember]
        Relational
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmNewRowViewPosition
    {
        [EnumMember]
        Top = 1,
        [EnumMember]
        Bottom = 2,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmGridType
    {
        [EnumMember]
        RegularGrid = 1,
        [EnumMember]
        ProductBasedGrid = 2,
        [EnumMember]
        DynamicMatrixGrid = 3,

        [EnumMember]
        LinePlanningGrid = 4,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmControlType
    {
        [EnumMember]
        CheckBox = 13,
        [EnumMember]
        Date = 7,
        [EnumMember]
        DDL = 1,
        [EnumMember]
        Empty = 17,
        [EnumMember]
        File = 9,
        [EnumMember]
        Grid = 6,
        [EnumMember]
        Image = 5,
        [EnumMember]
        Label = 10,
        [EnumMember]
        Memo = 4,
        [EnumMember]
        TextBox = 2,
        [EnumMember]
        Numeric = 20,
        [EnumMember]
        AutoGeneration = 23,
        [EnumMember]
        RGBColorDisplay = 24,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmGridColumnType
    {
        [EnumMember]
        CheckBox = 13,
        [EnumMember]
        TextBox = 2,
        [EnumMember]
        File = 9,
        [EnumMember]
        Date = 7,
        [EnumMember]
        DDL = 1,
        [EnumMember]
        Image = 5,
        [EnumMember]
        Numeric = 20,
        [EnumMember]
        RGBColorDisplay = 24,

        [EnumMember]
        LinePlanningGridColumn = 25,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmSpecialColumnType
    {
        [EnumMember]
        RegularColumn = 1,
        [EnumMember]
        PointToExternalDCUDataSourceKeyColumn = 2,
        [EnumMember]
        ProductGridSimpleDCU = 3,
        [EnumMember]
        ProductGridKeyDCUColumn = 4,
        [EnumMember]
        CurrentRefSimpleDCU = 5,
        [EnumMember]
        CurrentRefKeyDCUColumn = 6,
        [EnumMember]
        DynamicMatrixKeyColumn = 7,
        [EnumMember]
        ForeignKeyDependentColumn = 8,
        [EnumMember]
        UnknownTypeColumn = 20,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmDataSourceType
    {
        [EnumMember]
        DDL = 1,
        [EnumMember]
        Text = 2,
        [EnumMember]
        Number = 3,
        [EnumMember]
        DateTime = 4
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmFormularType
    {
        [EnumMember]
        Assignment = 1,
        [EnumMember]
        BooleanExpression = 2,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmFormularFunctionType
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
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmGridMetaColumnAggFunctionType
    {
        [EnumMember]
        Sum = 1,
        [EnumMember]
        Avg = 2,
        [EnumMember]
        Min = 3,
        [EnumMember]
        Max = 4,
        [EnumMember]
        RowCount = 5,
        [EnumMember]
        BooleanSum = 6,
    }

   // internal enum    EmDateTimeTokenType { Today = 1, Now = 2, Years = 3, Months = 4, Weeks = 5, Days = 6, Hours = 7, Minutes = 8, Seconds = 9, Null = 10 }

    //internal enum    EmBlockFormulaType { Assignment = 1, BooleanExpression = 2, };

    //internal enum    EmBlockFunctionType { Floor = 1, Ceiling = 2, Round = 3, ToLower = 4, ToUpper = 5, Abs = 6 }

    //internal enum    EmDateTimeTokenType { Today = 1, Now = 2, Years = 3, Months = 4, Weeks = 5, Days = 6, Hours = 7, Minutes = 8, Seconds = 9, Null = 10 }

    //internal enum    EmAggregationFunctionType { SUM = 1, AVG = 2, Min = 3, Max = 4, RowCount = 5, BooleanSum = 6, };

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmAlignmentType
    {
        [EnumMember]
        Left = 1,
        [EnumMember]
        Right = 2,
        [EnumMember]
        Center = 3,
        [EnumMember]
        Justify = 4,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmEntityCode
    {
        [EnumMember]
        ProductClass,
        [EnumMember]
        ProductType,
        [EnumMember]
        CompanyDivision,
        [EnumMember]
        Collection,
        [EnumMember]
        Group,
        [EnumMember]
        SellingPeriod,
        [EnumMember]
        Dimension,
        [EnumMember]
        SizeRun,
        [EnumMember]
        Sketch,
        [EnumMember]
        Composition,
        //[EnumMember]
        //Color,
        //[EnumMember]
        //BomTemplate,
        [EnumMember]
        BodyPart,
        [EnumMember]
        BodyType,
        [EnumMember]
        Vendor,
        [EnumMember]
        SystemProductStatusType,
        //[EnumMember]
        //Fabric,
        //[EnumMember]
        //V2kProduct,
        //[EnumMember]
        //StitchPoint,
        //[EnumMember]
        //SewInstruction,
        [EnumMember]
        Employee,
        [EnumMember]
        Agent,
        [EnumMember]
        Country,
        //[EnumMember]
        //SewStitchTemplate,
        //[EnumMember]
        //CustomerStandard,
        [EnumMember]
        BodyTypeGroup,
        [EnumMember]
        SizeRunDetail,
        [EnumMember]
        Customer,
        //[EnumMember]
        //CareInstruction,
        [EnumMember]
        ContentLabel,
        [EnumMember]
        UnitOfMeasure,
        [EnumMember]
        Currency,
        [EnumMember]
        CompositionType,
        [EnumMember]
        Fiber,
        [EnumMember]
        CieWarehouse,
        [EnumMember]
        DimentionDetail,
        [EnumMember]
        Package,
        //[EnumMember]
        //SystemSubjectDetail,
        [EnumMember]
        CompanySetup,
        [EnumMember]
        HTS,
        [EnumMember]
        PdmProductColor,
        [EnumMember]
        RGBColor,
        [EnumMember]
        PDMUser,
        [EnumMember]
        ProductManagerUser,
        [EnumMember]
        ProductSize,
        [EnumMember]
        ProductDim,
        //[EnumMember]
        //EmPriceByType,
        [EnumMember]
        ProductPackage,
        [EnumMember]
        Component,
        [EnumMember]
        ProductWarehouse,
        [EnumMember]
        ProductCategory,
        //[EnumMember]
        //ReferenceTemplate,
        //[EnumMember]
        //EmSpecFitStatus,
        [EnumMember]
        TemplateAttributeDetail,
        //[EnumMember]
        //ProductCatalog,
        [EnumMember]
        V2kLabel,
        [EnumMember]
        PDMTab,
        [EnumMember]
        ProductClassGroup,
        [EnumMember]
        PDMSecurityGroup,
        [EnumMember]
        MassUpdateView,

        [EnumMember]
        PDMBlock,
        [EnumMember]
        PDMDocumentView,
        [EnumMember]
        PDMEntity,
        [EnumMember]
        PDMGrid,
        [EnumMember]
        PDMItem,
        [EnumMember]
        PDMLanguage,
        [EnumMember]
        PDMListMenu,
        [EnumMember]
        PDMLPTemplate,
        [EnumMember]
        PDMNotification,
        [EnumMember]
        PDMPrintJob,
        [EnumMember]
        PDMProduct,
        [EnumMember]
        PDMReferenceView,
        [EnumMember]
        PDMReportWebPublish,
        [EnumMember]
        PDMSearchTemplate,
        [EnumMember]
        PDMSecurityControlorList,
        [EnumMember]
        PDMSecurityPermission,
        [EnumMember]
        PDMSecurityRegDomain,
        [EnumMember]
        PDMSEFolder,
        [EnumMember]
        PDMSetup,
        [EnumMember]
        PDMTemplateTabLibReferenceSetting,
        [EnumMember]
        PDMV2kPrefixType,
        [EnumMember]
        PDMBlockSubItem,
        [EnumMember]
        PDMGridMetaColumn
    }

    #endregion --- Design Module Enumerations

    #region --- Collaboration Module Enumerations

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmFolderType
    {
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Product = 1,
        [EnumMember]
        Color = 2,
        [EnumMember]
        SampleRequest = 3,
        [EnumMember]
        DataSource = 4,
        [EnumMember]
        BodyType = 5,
        [EnumMember]
        File = 6,
        [EnumMember]
        Sketch = 7,
        [EnumMember]
        Quote = 8,
        [EnumMember]
        Vendor = 9,
        [EnumMember]
        Template = 10,
        [EnumMember]
        Tab = 11,
        [EnumMember]
        Block = 12,
        [EnumMember]
        Grid = 13,
        [EnumMember]
        BodyPart = 14,
        [EnumMember]
        LinePlanning = 15,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmFolderSecurityType
    {
        [EnumMember]
        Role,
        [EnumMember]
        User
    }

    #endregion --- Collaboration Module Enumerations

    #region --- Reference Module Enumerations

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmQuoteStaus
    {
        [EnumMember]
        Open = 1,
        [EnumMember]
        Approved = 2,
        [EnumMember]
        Rejected = 3,
        [EnumMember]
        Dropped = 4,
        [EnumMember]
        Submitted = 5,
        [EnumMember]
        Requested = 11,
        [EnumMember]
        Received = 12
    }

    //internal enum    EmRefTxType { ProductDevelopment = 1, QuoteRequest = 2, SampleRequest = 3 ,QCRequest=4, Production=5, LibReference=6,ImageReference=7
    //FileReference=8,ConceptReference=9,VendorReference=10,CopyTabValueHolderReference=20}

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmRefTxType
    {
        [EnumMember]
        ProductDevelopment = 1,
        [EnumMember]
        QuoteRequest = 2,
        [EnumMember]
        SampleRequest = 3,
        [EnumMember]
        QCRequest = 4,
        [EnumMember]
        Production = 5,
        [EnumMember]
        LibReference = 6,
        [EnumMember]
        ImageReference = 7,
        [EnumMember]
        FileReference = 8,

        [EnumMember]
        BomConceptReference = 9,

        [EnumMember]
        VendorReference = 10,

        [EnumMember]
        LinePlanningReference = 100,

        [EnumMember]
        LinePlanningConceptual = 101,

        [EnumMember]
        CopyTabReference = 200,
    }

    #endregion --- Reference Module Enumerations

    #region---  User Setting

    internal enum    EmUserFavoriteType
    {
        Unknown = 0,
        SearchTemplate = 1,
        Folder = 2,
        SearchTemplateSaved = 3
    }

    #endregion

    #region --- Search Module Enumerations

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmViewColumnType
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

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EMReferenceSearchStaticField
    {
        [EnumMember]
        ID = 1,

        [EnumMember]
        MasterReferenceID = 7,
        [EnumMember]
        Status = 16,
        [EnumMember]
        CreatedBy = 17,
        [EnumMember]
        CreatedDate = 18,
        [EnumMember]

        LastRevisedBy = 23,
        [EnumMember]
        LastRevisedByDate = 24,
        [EnumMember]
        ApprovedBy = 19,
        [EnumMember]
        ApprovedDate = 20,
        [EnumMember]
        // QuoteCode = 50,
        QuoteVendor = 51,
        [EnumMember]
        QuoteTargetReferenceID = 52,
        [EnumMember]
        QuoteStatus = 53,
        [EnumMember]
        ProductManager = 55,

        [EnumMember]
        DivRefNumber = 56,
        [EnumMember]
        ProductVendor = 57,
        [EnumMember]
        MerchPlan = 60,
        [EnumMember]
        MerchPlanStatus = 61,
        [EnumMember]

        // for view purpose

        ReferenceQuoteCount = 62,
        [EnumMember]

        RefTxType = 1000,
        [EnumMember]

        SketchFolder = 1004,
        [EnumMember]

        ReferenceFolder = 2000,

        // ?? what is enum6
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmCriteriaOperatorType
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
    internal enum    EmCriteriaType
    {
        [EnumMember]
        Text = 0,
        [EnumMember]
        Entity = 1,
        [EnumMember]
        Date = 2,
        [EnumMember]
        Numeric = 3,
        [EnumMember]
        Boolean = 4,
        [EnumMember]
        Media = 5,
        [EnumMember]
        Integer = 6,
    }

    internal enum    EMViewDisplayType
    {
        GridView = 0,
        CardView = 1,
        CoverFlowView = 2,
        BookView = 3,
        //TabFields = 2,
        //DynamicMatrixBlock = 3,
        //RegularGrid = 4,
        //TabGridView = 5,
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    internal enum    EmSearchPageEditNumber
    {
        [EnumMember]
        P025 = 0,
        [EnumMember]
        P050 = 1,
        [EnumMember]
        P100 = 2,
        [EnumMember]
        P150 = 3,
        [EnumMember]
        P200 = 4,
        [EnumMember]
        P250 = 5,
        [EnumMember]
        P350 = 6,
        [EnumMember]
        P400 = 7,
    }

    #endregion

    #region --------- Point of measure

    internal enum    EmUnitType { INCH = 1, CM = 2 }

    #endregion

    #region------ Application Settings

    internal enum    EmApplicationSettings
    {
        EnableProductFolderSecurity = 1,
        NumberOfItemPerList = 2,
        MainMenuState = 4,
        ProjectEditorGridLayout = 5,
        ApplicationUrl = 8,
        SmtpServer = 10,
        SystemEmailFromAddress = 11,
        SystemEmailFromDisplay = 12,
        ConvertPath = 13,
        ConvertApplication = 14,
        AutoSaveTechPack = 15,
        DefaultFileReferenceTemplate = 16,
        DefaultImageReferenceTemplate = 17,
        IsPLMRunStandAlone = 19,
        DefaultSavedSearch = 20,
        AutoExecuteSearch = 21,
        UserDesktop = 22,
        ReportJobExportPath = 23,
        DefaultMessageForRequestedQuotes = 24,
        ShowRelatedEntityCountInFolderTree = 25,
        TimestampsInfo = 26,
        ProjectTemplateEditorGridLayout = 27,
        ActivityManagementGridLayout = 28,
        DefaultDivision = 29,
        EnableFolderSecurity = 30,
        AutoSubscribeCreator = 31,
        AutoSubscribeManager = 32,
        AutoSubscribeVendor = 33,

        DefaultFolderSecurityOwnerRead = 34,
        DefaultFolderSecurityOwnerWrite = 35,
        DefaultFolderSecurityOwnerList = 36,
        DefaultFolderSecurityOwnerModify = 37,
        AutoAddFolderSecurityOwner = 38,
        DefaultFolderSecurityOwnerSecurity = 39,
        MaxReferencedObjectsForDisplay = 40,
        AutoAddParentFolderSecurityResource = 41,
        DynamicReportRepositoryPath = 42,
        DynamicReportWebViewURL = 43,
        AdministratorEmailAddress = 44,
        HomePageLayout = 45,
        HomePageProjectActivitiesSummaryViewType = 46,
        HomePageNotificationSummaryViewType = 47,
        DefaultPOMUnitOfMeasure = 48,
        EnableColorCopyShorcutForAdminOnly = 49,
        ImageConvertDensity = 50,
        LicenseKey = 51,
        Visual2000MdePath = 52,
        ScmApplicationURL = 53,
        VibeApplicationURL = 54,
        BiApplicationURL = 55,
        WmsApplicationURL = 56,
        VbiApplicationURL = 57,

        SystemLoginEmailNotification = 58,
        SystemPasswordEmailNotification = 59,
        SystemDomainEmailNotification = 60,
        SystemPortSSLEmailNotification = 61,
        SystemInboxEmailNotification = 62,

        AutoUploadSendEmailError = 63,
        AutoUploadFolderImage = 64,
        AutoUploadCreateTreeFolder = 65,

        SfaApplicationURL = 66,
        CrmApplicationURL = 67,
    }

    #endregion

    #region------- Image Document

    internal enum    EmDocumentType { JPG = 1, GIF = 2, BMP = 3, TIF = 4, DWG = 5, AI = 6, EXCEL = 7, Video = 8, PDF = 9, WORD = 10, Compressed = 11, TXT = 12, PNG = 13, PSD = 14, Unknown = 99 }

    internal enum    EmImageEditType
    {
        DownloadNotEdit = 1,
        DownloadAndEdit = 2,
        DownloadAndEditAnnotation = 3,
        OnlineEdit = 4,
        DownloadFile = 5,
        ImagePreview = 6,
    }

    #endregion

    internal enum    EmFolderClipboardAction
    {
        Copy = 0,
        Cut = 1,
        CopyStructure = 2,
    }

    internal enum    EmColorOperationAction
    {
        Move,
        Copy,
        CopyAsNew,
    }

    internal enum    EmWhichControlUseViewControl
    {
        FolderTreeControl,
        SearchCriteriaControl
    }

    internal enum    EmDocumentInfoState
    {
        None = 0,
        Saving = 1,
        SaveOK = 2,
        SaveFailed = 3,
        FileFailed = 4,
    }

    internal enum    EmUserEditorViewType
    {
        Profile = 0,
        Division = 1,
        Security = 2,
    }

    internal enum    EmProductDirection
    {
        Forward = 1,
        Reverse = 2,
    }

    internal enum    EmReferenceMenuItemType
    {
        IsTemplate,
        IsTab,
        IsCopyTab
    }

    internal enum    EmIsSystemDefine
    {
        IsSystemDefine = 3000,
    }

    /// <summary>
    ///
    /// </summary>

   

}
