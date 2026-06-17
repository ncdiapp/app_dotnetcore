/****** Object:  Table [dbo].[AppAiChatHistory]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppAiChatHistory](
	[MainTopicID] [int] IDENTITY(1,1) NOT NULL,
	[MainTopic] [nvarchar](2000) NULL,
	[Description] [nvarchar](max) NULL,
	[UserId] [int] NULL,
	[IsAchieved] [bit] NULL,
	[JsonContent] [nvarchar](max) NULL,
	[TopicScope] [int] NULL,
	[TargetId] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppAiChatHistory] PRIMARY KEY CLUSTERED 
(
	[MainTopicID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppApplicationAssetsItem]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppApplicationAssetsItem](
	[AssetsItemID] [int] IDENTITY(1,1) NOT NULL,
	[ApplicationID] [int] NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[FormID] [int] NULL,
	[TransactionID] [int] NULL,
	[ProjectWorkflowID] [int] NULL,
	[SearchID] [int] NULL,
	[ReportID] [int] NULL,
	[DesktopID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppApplicationConfigurationItem] PRIMARY KEY CLUSTERED 
(
	[AssetsItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppBusienssAssormentNavigation]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppBusienssAssormentNavigation](
	[AssotmentnavigationID] [int] IDENTITY(1,1) NOT NULL,
	[AssormentName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[MainTraisactionID] [int] NULL,
	[MainProjectTemplateID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppBusienssAssormentNavigation] PRIMARY KEY CLUSTERED 
(
	[AssotmentnavigationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppBusinessMgtScope]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppBusinessMgtScope](
	[BusinessScopeID] [int] IDENTITY(1,1) NOT NULL,
	[ScopeName] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[Sort] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppBusinessScope] PRIMARY KEY CLUSTERED 
(
	[BusinessScopeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppBusinessScopeTag]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppBusinessScopeTag](
	[ScopeTagID] [int] IDENTITY(1,1) NOT NULL,
	[TagName] [nvarchar](50) NULL,
	[TagIconStyle] [nvarchar](200) NULL,
	[EmTagBusienssScope] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppBusinessScopeTag] PRIMARY KEY CLUSTERED 
(
	[ScopeTagID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppCalendar]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppCalendar](
	[CalendarID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[IsCompanyCalendar] [bit] NULL,
	[IsCompanyDefaultCalendar] [bit] NULL,
	[UserID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppCalendar] PRIMARY KEY CLUSTERED 
(
	[CalendarID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppCalendarBaseDate]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppCalendarBaseDate](
	[Day] [int] NOT NULL,
	[Day_Desc] [datetime] NULL,
	[Week] [nvarchar](255) NULL,
	[Week_Desc] [nvarchar](255) NULL,
	[Bi_Week] [nvarchar](255) NULL,
	[Bi_Week_Desc] [nvarchar](255) NULL,
	[Hlf_Month] [nvarchar](255) NULL,
	[Hlf_Month_Desc] [nvarchar](255) NULL,
	[Month] [nvarchar](255) NULL,
	[Month_Desc] [nvarchar](255) NULL,
	[Quarter] [nvarchar](255) NULL,
	[Quarter_Desc] [nvarchar](255) NULL,
	[Pln_Hlf_Yr] [nvarchar](255) NULL,
	[Pln_Hlf_Yr_Desc] [nvarchar](255) NULL,
	[Pln_Yr] [nvarchar](255) NULL,
	[Pln_Yr_Desc] [nvarchar](255) NULL,
	[Fiscal_Hlf_Yr] [nvarchar](255) NULL,
	[Fiscal_Hlf_Yr_Desc] [nvarchar](255) NULL,
	[Fiscal_Yr] [nvarchar](255) NULL,
	[Fiscal_Yr_Desc] [nvarchar](255) NULL,
	[Range_Period] [nvarchar](255) NULL,
	[Range_Period_Desc] [nvarchar](255) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppCalendarBaseDate] PRIMARY KEY CLUSTERED 
(
	[Day] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppCalendarRecurringDay]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppCalendarRecurringDay](
	[RecurringDayID] [int] IDENTITY(1,1) NOT NULL,
	[CalendarID] [int] NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[DateTokenType] [int] NULL,
	[Month] [int] NULL,
	[DayOfMonth] [int] NULL,
	[DayOfWeek] [int] NULL,
	[WorkStatus] [int] NULL,
	[RecurringStartDate] [datetime] NOT NULL,
	[RecurringEndDate] [datetime] NULL,
	[RecurringType] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppCalendarRecurringDay] PRIMARY KEY CLUSTERED 
(
	[RecurringDayID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppCalendarSpecificDay]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppCalendarSpecificDay](
	[CalendarDayID] [int] IDENTITY(1,1) NOT NULL,
	[CalendarID] [int] NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[WorkStatus] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[EmDateDefineType] [int] NULL,
	[EmDateRangeType] [int] NULL,
	[UserDefined1] [nvarchar](2000) NULL,
	[UserDefined2] [nvarchar](2000) NULL,
	[UserDefined3] [nvarchar](2000) NULL,
	[UserDefined4] [bit] NULL,
 CONSTRAINT [PK_AppCalendarDay] PRIMARY KEY CLUSTERED 
(
	[CalendarDayID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppChartOfAccount]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppChartOfAccount](
	[ChartOfAccountID] [int] IDENTITY(1,1) NOT NULL,
	[AccountCode] [nvarchar](20) NOT NULL,
	[Name] [nvarchar](80) NULL,
	[Description] [nvarchar](80) NULL,
	[AccountType] [nvarchar](15) NOT NULL,
	[HeaderType] [nvarchar](20) NULL,
	[NodeLevel] [decimal](18, 6) NULL,
	[CurrentOpeningBalance] [decimal](18, 6) NULL,
	[SignDebitCredit] [nvarchar](10) NULL,
	[IsPosted] [bit] NOT NULL,
	[SystemTimeStamp] [timestamp] NULL,
	[StartBalance] [decimal](18, 6) NULL,
	[IsBalanceTransfered] [bit] NOT NULL,
	[GroupAccountID] [int] NULL,
	[GroupLevel] [nvarchar](10) NULL,
	[ChartOfAccountType] [int] NULL,
	[IsInvesting] [bit] NOT NULL,
	[IsOperating] [bit] NOT NULL,
	[IsFinancing] [bit] NOT NULL,
	[IntAccountActivity] [int] NULL,
	[TaxID] [int] NULL,
	[IsInactive] [bit] NULL,
	[TagCode] [varchar](20) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppChartOfAccount] PRIMARY KEY CLUSTERED 
(
	[ChartOfAccountID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 85, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppComOrganization]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppComOrganization](
	[OrganizationID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NULL,
	[ShortName] [nvarchar](100) NULL,
	[FullName] [nvarchar](200) NULL,
	[ClassificationLevel] [int] NULL,
	[ContactPerson] [nvarchar](50) NULL,
	[Telphone] [nvarchar](20) NULL,
	[IsActive] [bit] NULL,
	[Memo] [nvarchar](500) NULL,
	[BelongToID] [int] NULL,
	[Adress] [nvarchar](500) NULL,
	[LeaderID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[UserTypeEm] [int] NULL,
	[AppCompanyID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppCompanyOrganization] PRIMARY KEY CLUSTERED 
(
	[OrganizationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppComOrgLevel]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppComOrgLevel](
	[OrgLevelID] [int] IDENTITY(1,1) NOT NULL,
	[ClassificationLevel] [int] NULL,
	[CodeNum] [nchar](10) NULL,
	[LevelName] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[FullName] [nvarchar](200) NULL,
	[AppCompanyID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppComOrgLevel] PRIMARY KEY CLUSTERED 
(
	[OrgLevelID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppConditionalAction]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppConditionalAction](
	[ActionID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[TransactionID] [int] NULL,
	[ConditionUnitID] [int] NULL,
	[BooleanConditionFieldID] [int] NULL,
	[LockingTransactionFieldID] [int] NULL,
	[LockingFieldUnitID] [int] NULL,
	[IsLockingTransaction] [bit] NULL,
	[LockingTransactionUnitID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[BooleanConditionFormula] [nvarchar](500) NULL,
	[NotificationTemplateMessgeID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsLockForSpecailEditPrivilege] [bit] NULL,
	[UITriggerTransactionFieldID] [int] NULL,
	[NeedToHideTransactionFieldID] [int] NULL,
 CONSTRAINT [PK_AppConditionalAction] PRIMARY KEY CLUSTERED 
(
	[ActionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppCurrentUserFavouriteFolderOrFile]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppCurrentUserFavouriteFolderOrFile](
	[FavouriteFileID] [int] IDENTITY(1,1) NOT NULL,
	[CurrentUserID] [int] NULL,
	[FiledID] [int] NULL,
	[FolderID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppCurrentUserFavouriteFolderOrFile] PRIMARY KEY CLUSTERED 
(
	[FavouriteFileID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppDataSet]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppDataSet](
	[DataSetID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[QueryText] [nvarchar](max) NULL,
	[QueryType] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[DataSourceFrom] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[BaseDataSetID] [int] NULL,
	[SaasApplicationID] [int] NULL,
	[UsageTypeID] [int] NULL,
	[BaseTableName] [nvarchar](200) NULL,
	[WebApiConfigID] [int] NULL,
	[RestApiHeaderKeyValue] [nvarchar](4000) NULL,
	[RestApiQueryParameterKeyValue] [nvarchar](4000) NULL,
	[HttpMethod] [int] NULL,
	[OtherSettings] [nvarchar](max) NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppDataSet] PRIMARY KEY CLUSTERED 
(
	[DataSetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppDataSetParameter]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppDataSetParameter](
	[ParameterID] [int] IDENTITY(1,1) NOT NULL,
	[DataSetID] [int] NULL,
	[ParameterName] [nvarchar](50) NULL,
	[DataType] [nvarchar](50) NULL,
	[DirectionInOut] [nchar](10) NULL,
	[DefautValue] [nvarchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppDataSetParameter] PRIMARY KEY CLUSTERED 
(
	[ParameterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppDateSetDataExtractView]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppDateSetDataExtractView](
	[ExtractViewID] [int] IDENTITY(1,1) NOT NULL,
	[Description] [nvarchar](200) NULL,
	[DataSetID] [int] NULL,
	[DBFiledName] [nvarchar](200) NULL,
	[IsGroup] [bit] NULL,
	[AggFunction] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppDateSetDataExtractView] PRIMARY KEY CLUSTERED 
(
	[ExtractViewID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppDesktop]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppDesktop](
	[DesktopID] [int] IDENTITY(1,1) NOT NULL,
	[DesktopName] [nvarchar](200) NULL,
	[Description] [nvarchar](2000) NULL,
	[IsGlobalDefault] [bit] NULL,
	[LayoutType] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[SaasApplicationID] [int] NULL,
	[OtherSettings] [nvarchar](max) NULL,
	[IsUserDesktop] [bit] NULL,
	[UserDesktopUserId] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
	[UserFavoriteList] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppDesktop] PRIMARY KEY CLUSTERED 
(
	[DesktopID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppDesktopItem]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppDesktopItem](
	[DesktopItemID] [int] IDENTITY(1,1) NOT NULL,
	[DesktopID] [int] NULL,
	[WidgetItemType] [int] NULL,
	[FlowOrGridLayoutSortOrder] [int] NULL,
	[StyleLayoutInfo] [nvarchar](2000) NULL,
	[DomElementTag] [nvarchar](2000) NULL,
	[ParameterKeyValue] [nvarchar](2000) NULL,
	[DisplayTitle] [nvarchar](500) NULL,
	[RowIndex] [int] NULL,
	[ColumnIndex] [int] NULL,
	[RowSpan] [int] NULL,
	[ColumnSpan] [int] NULL,
	[GridLayoutParentID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppDesktopItem] PRIMARY KEY CLUSTERED 
(
	[DesktopItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEmployee]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEmployee](
	[EmployeeID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NOT NULL,
	[HireDate] [datetime] NULL,
	[VacationHours] [int] NULL,
	[Gender] [int] NULL,
	[SalariedFlag] [bit] NULL,
	[SickLeaveHours] [int] NULL,
	[CurrentFlag] [bit] NULL,
	[JobTitle] [nvarchar](50) NULL,
	[BirthDate] [datetime] NULL,
	[MaritalStatus] [bit] NULL,
	[NationalIDNumber] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppEmployee] PRIMARY KEY CLUSTERED 
(
	[EmployeeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEntityEnumValue]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEntityEnumValue](
	[EnumValueID] [int] IDENTITY(1,1) NOT NULL,
	[EntityInfoID] [int] NOT NULL,
	[EnumKey] [int] NOT NULL,
	[EnumValue] [nvarchar](200) NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppEntityEnumValue] PRIMARY KEY CLUSTERED 
(
	[EnumValueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEntityInfo]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEntityInfo](
	[EntityInfoID] [int] IDENTITY(1,1) NOT NULL,
	[EntityCode] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[EntityType] [int] NULL,
	[TableName] [nvarchar](100) NULL,
	[IdentityField] [nvarchar](100) NULL,
	[DisplayFiled1] [nvarchar](100) NULL,
	[DisplayFiled3] [nvarchar](100) NULL,
	[DisplayFiled2] [nvarchar](100) NULL,
	[QueryText] [nvarchar](3000) NULL,
	[DataSourceFrom] [int] NULL,
	[IsSystemDefine] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[SchemaOwner] [nvarchar](50) NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsSharedbyMutipleCompany] [bit] NULL,
	[ColorCodeField] [nvarchar](100) NULL,
	[SaasApplicationID] [int] NULL,
	[PartnerFilterFiled] [nvarchar](100) NULL,
	[ExternalKeyField] [nvarchar](500) NULL,
	[OtherSettings] [nvarchar](max) NULL,
	[IdentityCoumnDataType] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppLookItemInfo] PRIMARY KEY CLUSTERED 
(
	[EntityInfoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEntitySimpleListValue]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEntitySimpleListValue](
	[SimpleListValueID] [int] IDENTITY(1,1) NOT NULL,
	[EntityInfoID] [int] NOT NULL,
	[Sort] [int] NOT NULL,
	[Code] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](500) NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[InternalKey] [int] NULL,
 CONSTRAINT [PK_AppEntitySimpleListValue] PRIMARY KEY CLUSTERED 
(
	[SimpleListValueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppErpColor]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppErpColor](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](4000) NULL,
	[Desc] [nvarchar](4000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppErpColor] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppErpSizeRangeDetail]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppErpSizeRangeDetail](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](4000) NULL,
	[Description] [nvarchar](4000) NULL,
	[Sort] [int] NULL,
	[SizeRangeID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppSizeRangeDetail] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEsite]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEsite](
	[EsiteID] [int] IDENTITY(1,1) NOT NULL,
	[Sort] [int] NULL,
	[Name] [nvarchar](500) NULL,
	[Description] [nvarchar](2000) NULL,
	[EmAppEsiteTheme] [int] NULL,
	[LogoImageID1] [int] NULL,
	[LogoImageID2] [int] NULL,
	[IsAllowGuestCheckout] [bit] NULL,
	[MyOrderListSearchID] [int] NULL,
	[CustomerInfoDataModelID] [int] NULL,
	[CustomerInfoDBTableName] [nvarchar](500) NULL,
	[CustomerInfoCustomerIdDBFieldName] [nvarchar](500) NULL,
	[CustomerInfoEmailDBFieldName] [nvarchar](500) NULL,
	[CustomerInfoDataTransferID] [int] NULL,
	[SaveCustomerInfoPostActionID] [int] NULL,
	[CustomerShippingAddressSearchID] [int] NULL,
	[CustomerShippingAddressDataModelID] [int] NULL,
	[CustomerShippingAddressDataTransferID] [int] NULL,
	[SaveCustomerShippingAddressPostActionID] [int] NULL,
	[CustomerOrderListSearchID] [int] NULL,
	[OrderDataModelID] [int] NULL,
	[InvoiceDataModelId] [int] NULL,
	[InvoiceReportId] [int] NULL,
	[OrderDataTransferID] [int] NULL,
	[SaveOrderPostActionID] [int] NULL,
	[CustomerPaymentHistorySearchID] [int] NULL,
	[CustomerPaymentHistoryDataModelID] [int] NULL,
	[CustomerPaymentHistoryDataTransferID] [int] NULL,
	[PaymentSuccessfulPostActionActionID] [int] NULL,
	[PaymentFailedPostActionActionID] [int] NULL,
	[EnablePaypalPayment] [bit] NULL,
	[PaypalPayment_ApiBaseURL] [nvarchar](500) NULL,
	[PaypalPayment_SB_CLIENT_ID] [nvarchar](500) NULL,
	[PaypalPayment_DefaultCurrencyCode] [nvarchar](500) NULL,
	[EnableVisaPayment] [bit] NULL,
	[VisaPayment_ApiBaseURL] [nvarchar](500) NULL,
	[VisaPayment_ApiParam1] [nvarchar](500) NULL,
	[VisaPayment_ApiParam2] [nvarchar](500) NULL,
	[VisaPayment_ApiParam3] [nvarchar](500) NULL,
	[VisaPayment_ApiParam4] [nvarchar](500) NULL,
	[VisaPayment_ApiParam5] [nvarchar](500) NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[MasteSiteHostLayoutHtmlContent] [nvarchar](max) NULL,
	[EmApplicationType] [int] NULL,
	[MasteSiteCustNavigationJavaScripControl] [nvarchar](max) NULL,
	[SaasApplicationID] [int] NULL,
	[SupplierInfoDataModelID] [int] NULL,
	[SupplierInfoDBTableName] [nvarchar](500) NULL,
	[SupplierInfoIdDBFieldName] [nvarchar](500) NULL,
	[SupplierInfoEmailDBFieldName] [nvarchar](500) NULL,
	[SupplierInfoDataTransferID] [nvarchar](500) NULL,
	[SaveSupplierInfoPostActionID] [nvarchar](500) NULL,
	[SitePublishedBaseUrl] [nvarchar](500) NULL,
	[SitePublishedLoginUrl] [nvarchar](500) NULL,
	[StartPage] [nvarchar](500) NULL,
	[SiteNavMenuSearchID] [int] NULL,
	[DesignPreviewCustomerPartnerId] [int] NULL,
	[DesignPreviewSupplierPartnerId] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppESite] PRIMARY KEY CLUSTERED 
(
	[EsiteID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEsiteCatalogue]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEsiteCatalogue](
	[EsiteCatalogueID] [int] IDENTITY(1,1) NOT NULL,
	[EsiteID] [int] NULL,
	[Sort] [int] NULL,
	[Name] [nvarchar](100) NULL,
	[Description] [nvarchar](1000) NULL,
	[EmAppEStoreLayout] [int] NULL,
	[EmAppEStoreTheme] [int] NULL,
	[TreeNavigationViewID] [int] NULL,
	[CatalogCardViewID] [int] NULL,
	[CatalogCardDetailID] [int] NULL,
	[SaasApplicationID] [int] NULL,
	[IsActive] [bit] NULL,
	[IsDefault] [bit] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppESiteCatalogue] PRIMARY KEY CLUSTERED 
(
	[EsiteCatalogueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppESiteNavMenu]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppESiteNavMenu](
	[MenuID] [int] IDENTITY(1,1) NOT NULL,
	[ParentID] [int] NULL,
	[EsiteID] [int] NULL,
	[Name] [nvarchar](100) NULL,
	[Description] [nvarchar](2000) NULL,
	[Sort] [int] NULL,
	[IconName] [nvarchar](50) NULL,
	[RouteCode] [nvarchar](50) NULL,
	[Link] [nvarchar](500) NULL,
	[LinkParam1] [nvarchar](500) NULL,
	[LinkParam2] [nvarchar](500) NULL,
	[LinkCategory] [int] NULL,
	[LinkType] [int] NULL,
	[EmDeviceMenuShowMode] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppESiteNavMenu] PRIMARY KEY CLUSTERED 
(
	[MenuID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppESitePages]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppESitePages](
	[PageID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NULL,
	[EMResourceContentType] [int] NULL,
	[ResourceContent] [nvarchar](max) NULL,
	[LoadOrder] [int] NULL,
	[IsActive] [bit] NULL,
	[MetaDesciption] [nvarchar](250) NULL,
	[UrlAndHandle] [nvarchar](100) NULL,
	[EsiteID] [int] NULL,
	[TransactionID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsDefault] [bit] NULL,
	[ControllerName] [nvarchar](500) NULL,
	[SearchID] [int] NULL,
	[SearchViewID] [int] NULL,
	[IsMasterLayoutPage] [bit] NULL,
	[PageJsMethod] [nvarchar](max) NULL,
	[PageCssStyle] [nvarchar](max) NULL,
	[NavigationCtrlJavascript] [nvarchar](max) NULL,
	[FileFullPath] [nvarchar](800) NULL,
	[DesignLayout] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppESitePages] PRIMARY KEY CLUSTERED 
(
	[PageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppEStore]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppEStore](
	[EStoreID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NULL,
	[Description] [nvarchar](1000) NULL,
	[EmAppEStoreLayout] [int] NULL,
	[EmAppEStoreTheme] [int] NULL,
	[TreeNavigationViewID] [int] NULL,
	[CatalogCardViewID] [int] NULL,
	[CatalogCardDetailID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[SaasApplicationID] [int] NULL,
	[IsActive] [bit] NULL,
	[IsDefault] [bit] NULL,
 CONSTRAINT [PK_AppEStore] PRIMARY KEY CLUSTERED 
(
	[EStoreID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppExternalMethodRegister]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppExternalMethodRegister](
	[MethodRegisterID] [int] IDENTITY(1,1) NOT NULL,
	[MethodDisplayName] [nvarchar](100) NULL,
	[AssemblyName] [nvarchar](500) NULL,
	[TypeName] [nvarchar](500) NULL,
	[MethodName] [nvarchar](500) NULL,
	[InputParameterList] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppExternalMethodRegister] PRIMARY KEY CLUSTERED 
(
	[MethodRegisterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFile]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFile](
	[FileID] [int] IDENTITY(1,1) NOT NULL,
	[FileCode] [nvarchar](200) NULL,
	[Description] [nvarchar](100) NULL,
	[FolderID] [int] NULL,
	[FileType] [int] NULL,
	[Extension] [nvarchar](50) NULL,
	[OriginalFilePath] [nvarchar](100) NULL,
	[ThumbnailFilePath] [nvarchar](100) NULL,
	[RegularImageFilepath] [nvarchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[FileContent] [varbinary](max) NULL,
	[InitialFileID] [int] NULL,
	[CheckoutByID] [int] NULL,
	[CheckoutDate] [datetime] NULL,
	[Comments] [nvarchar](4000) NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[ClientLastWriteTick] [bigint] NULL,
 CONSTRAINT [PK_AppFile] PRIMARY KEY CLUSTERED 
(
	[FileID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFileOrFolderShareToOther]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFileOrFolderShareToOther](
	[SharingID] [int] IDENTITY(1,1) NOT NULL,
	[FolderID] [int] NULL,
	[FileID] [int] NULL,
	[ShareToOtherUserID] [int] NULL,
	[ShareToOtherRoleID] [int] NULL,
	[IsCanWrite] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppFileOrFolderShareToOther] PRIMARY KEY CLUSTERED 
(
	[SharingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFileTypeView]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFileTypeView](
	[FileTypeViewID] [int] IDENTITY(1,1) NOT NULL,
	[FileTypeEnumID] [int] NULL,
	[SearchViewID] [int] NULL,
	[IsDefault] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppFileTypeView] PRIMARY KEY CLUSTERED 
(
	[FileTypeViewID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppForm]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppForm](
	[FormID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](1000) NULL,
	[LayoutType] [int] NULL,
	[FormScope] [int] NULL,
	[SystemDefineRouteState] [nvarchar](500) NULL,
	[RouteParamter1] [nvarchar](100) NULL,
	[RouteParamter2] [nvarchar](100) NULL,
	[RouteParamter3] [nvarchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[SearchViewID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[DefaultWidth] [nvarchar](50) NULL,
	[DefaultHight] [nvarchar](50) NULL,
	[SaasApplicationID] [int] NULL,
 CONSTRAINT [PK_pdmForm] PRIMARY KEY CLUSTERED 
(
	[FormID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFormGridLayoutItemBindField]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFormGridLayoutItemBindField](
	[LayoutBindFieldID] [int] IDENTITY(1,1) NOT NULL,
	[FormLayoutID] [int] NULL,
	[TransactionField] [int] NULL,
	[AliasName] [nvarchar](300) NULL,
	[Width] [int] NULL,
	[Height] [int] NULL,
	[Visible] [bit] NULL,
	[ChildTranscationUnitID] [int] NULL,
	[GrandChildTranscationUnitID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppFormGridLayoutItemBindField] PRIMARY KEY CLUSTERED 
(
	[LayoutBindFieldID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFormGroup]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFormGroup](
	[FormGroupID] [int] IDENTITY(1,1) NOT NULL,
	[GroupName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransFormGroup] PRIMARY KEY CLUSTERED 
(
	[FormGroupID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFormGroupItem]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFormGroupItem](
	[GroupItemID] [int] IDENTITY(1,1) NOT NULL,
	[FromGroupID] [int] NULL,
	[FormID] [int] NULL,
	[FlowOrder] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransFormGroupItem] PRIMARY KEY CLUSTERED 
(
	[GroupItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFormLayoutItem]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFormLayoutItem](
	[FormLayoutItemID] [int] IDENTITY(1,1) NOT NULL,
	[FormID] [int] NULL,
	[WidgetItemType] [int] NULL,
	[FlowOrGridLayoutSortOrder] [int] NULL,
	[StyleLayoutInfo] [nvarchar](2000) NULL,
	[DomElementTag] [nvarchar](2000) NULL,
	[ParameterKeyValue] [nvarchar](max) NULL,
	[DisplayTitle] [nvarchar](500) NULL,
	[RowIndex] [int] NULL,
	[ColumnIndex] [int] NULL,
	[RowSpan] [int] NULL,
	[ColumnSpan] [int] NULL,
	[UIGridLayoutParentID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[GridTransactionUnitID] [int] NULL,
	[AutoExcuteSearchID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[SearchViewFieldID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[CurrentHostID] [nvarchar](50) NULL,
	[ParentHostID] [nvarchar](50) NULL,
 CONSTRAINT [PK_pdmFormLayoutItem] PRIMARY KEY CLUSTERED 
(
	[FormLayoutItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppFormLinkTarget]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppFormLinkTarget](
	[LinkTargetID] [int] IDENTITY(1,1) NOT NULL,
	[SearchViewID] [int] NULL,
	[SourceColumn1] [nvarchar](100) NULL,
	[SourceColumn2] [nvarchar](100) NULL,
	[SourceColumn3] [nvarchar](100) NULL,
	[TargetColumn1] [nvarchar](100) NULL,
	[TargetColumn2] [nvarchar](100) NULL,
	[TargetColumn3] [nvarchar](100) NULL,
	[NavigationActionName] [nvarchar](200) NULL,
	[ActionType] [int] NULL,
	[IsReadonly] [bit] NULL,
	[TransactionUnitID] [int] NULL,
	[LinkTargetTransactionID] [int] NULL,
	[RowDisplayDbField] [nvarchar](100) NULL,
	[SourceConditionColumn] [nvarchar](100) NULL,
	[ConditionWarningMessage] [nvarchar](800) NULL,
	[GroupName] [nvarchar](100) NULL,
	[LinkTargetTransactionGroupID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[LinkTargetSearchID] [int] NULL,
	[LinkTargetUsageType] [int] NULL,
	[SourceColumnType] [int] NULL,
	[LinkTargetUrlOrRouteCode] [nvarchar](500) NULL,
	[LayoutDisplayMode] [int] NULL,
	[SourceViewColumnID1] [int] NULL,
	[SourceViewColumnID2] [int] NULL,
	[SourceViewColumnID3] [int] NULL,
	[TargetSearchFieldID1] [int] NULL,
	[TargetSearchFieldID2] [int] NULL,
	[TargetSearchFieldID3] [int] NULL,
	[RowDisplayViewColumnID] [int] NULL,
	[SourceConditionViewColumnID] [int] NULL,
	[ConditionTransFieldID] [int] NULL,
	[DataTransferSettingID] [int] NULL,
	[Sort] [int] NULL,
	[OpennedFormAutoExecuteCommandID] [int] NULL,
	[IsPopup] [bit] NULL,
	[PopupWidth] [int] NULL,
	[PopupHeight] [int] NULL,
	[IconName] [nvarchar](500) NULL,
	[OtherSettings] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppFormLinkTarget] PRIMARY KEY CLUSTERED 
(
	[LinkTargetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppGeneralJournal]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppGeneralJournal](
	[JournalID] [int] IDENTITY(1,1) NOT NULL,
	[JournalCode] [nvarchar](20) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ReferenceDesc] [nvarchar](100) NULL,
	[PostedDate] [datetime] NOT NULL,
	[IsPosted] [bit] NOT NULL,
	[SystemTimeStamp] [timestamp] NULL,
	[CreatedByID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppGeneralJournal] PRIMARY KEY CLUSTERED 
(
	[JournalID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppGeneralJournalDetail]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppGeneralJournalDetail](
	[JournalDetailID] [int] IDENTITY(0,1) NOT NULL,
	[Debit] [decimal](18, 6) NULL,
	[Credit] [decimal](18, 6) NULL,
	[Memo] [nvarchar](100) NULL,
	[SystemTimeStamp] [timestamp] NULL,
	[AccDivisionID] [int] NULL,
	[AccDeptID] [int] NULL,
	[JournalID] [int] NULL,
	[TaxID] [int] NULL,
	[TaxAmount] [decimal](19, 6) NULL,
	[JournalCode] [nvarchar](20) NOT NULL,
	[AccountCode] [nvarchar](20) NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppGeneralJournalDetail] PRIMARY KEY CLUSTERED 
(
	[JournalDetailID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppIntergrationSetting]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppIntergrationSetting](
	[IntergrationSettingID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[InternalCode] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[DataSourceRegisterID] [int] NULL,
	[RestAPIURL] [nvarchar](200) NULL,
	[APICredentialConfig] [nvarchar](max) NULL,
	[IntergrationType] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_pdmIntergrationSetting] PRIMARY KEY CLUSTERED 
(
	[IntergrationSettingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppIntergrationSettingParameter]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppIntergrationSettingParameter](
	[SettingParameterID] [int] IDENTITY(1,1) NOT NULL,
	[IntergrationSettingID] [int] NULL,
	[MappingInternalCode] [nvarchar](100) NULL,
	[InternalFiledName] [nvarchar](100) NULL,
	[ExternalFieldName] [nvarchar](100) NULL,
	[TranscationID] [int] NULL,
	[TranscationFieID] [int] NULL,
	[DefaultValue] [nvarchar](max) NULL,
	[ValidationRule] [nvarchar](500) NULL,
	[ActionCode] [nvarchar](200) NULL,
	[ActionDescription] [nvarchar](500) NULL,
	[JsonQuery] [nvarchar](max) NULL,
	[WhereClauseFormat] [nvarchar](500) NULL,
	[IsSimpleQuery] [bit] NULL,
	[JsonSampleData] [nvarchar](max) NULL,
	[JsonSchema] [nvarchar](max) NULL,
	[SchemaDataSetMapping] [nvarchar](max) NULL,
	[HttpMethd] [nvarchar](20) NULL,
	[DataSourceID] [int] NULL,
	[SchemaFromDataSetMapping] [nvarchar](max) NULL,
	[PostProcessScript] [nvarchar](max) NULL,
	[APIConfigParameters] [nvarchar](max) NULL,
	[TablePrefix] [nvarchar](400) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppIntergrationSettingParameter] PRIMARY KEY CLUSTERED 
(
	[SettingParameterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppItemBase]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppItemBase](
	[ItemBaseID] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](50) NOT NULL,
	[Desc1] [nvarchar](200) NULL,
	[Desc2] [nvarchar](500) NULL,
	[ParentID] [int] NULL,
	[CopyFromID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[CustomerID] [int] NULL,
	[SupplierID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppItemBase] PRIMARY KEY CLUSTERED 
(
	[ItemBaseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppListMenu]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppListMenu](
	[MenuID] [int] IDENTITY(1,1) NOT NULL,
	[ParentID] [int] NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](2000) NULL,
	[IconName] [nvarchar](50) NULL,
	[RouteCode] [nvarchar](50) NULL,
	[Link] [nvarchar](500) NULL,
	[Sort] [int] NULL,
	[LinkType] [int] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsSharedbyMutipleCompany] [bit] NULL,
	[EmDeviceMenuShowMode] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
	[DisplayModeMenuOrTab] [int] NULL,
	[TransactionID] [int] NULL,
	[SearchID] [int] NULL,
	[ReportID] [int] NULL,
	[ProjectID] [int] NULL,
	[DesktopID] [int] NULL,
	[LinkParam1] [nvarchar](500) NULL,
	[LinkParam2] [nvarchar](500) NULL,
	[IconName2] [nvarchar](200) NULL,
	[ModuleRegisterID] [int] NULL,
	[EsiteID] [int] NULL,
	[EmAppMenuItemCategory] [int] NULL,
 CONSTRAINT [PK_AppListMenu] PRIMARY KEY CLUSTERED 
(
	[MenuID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppListMenuTemp]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppListMenuTemp](
	[MenuID] [int] IDENTITY(1,1) NOT NULL,
	[ParentID] [int] NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](2000) NULL,
	[IconName] [nvarchar](50) NULL,
	[RouteCode] [nvarchar](50) NULL,
	[Link] [nvarchar](500) NULL,
	[Sort] [int] NULL,
	[LinkType] [int] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsSharedbyMutipleCompany] [bit] NULL,
	[EmDeviceMenuShowMode] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppMessage]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppMessage](
	[MessageID] [int] IDENTITY(1,1) NOT NULL,
	[Subject] [nvarchar](500) NOT NULL,
	[Message] [nvarchar](max) NULL,
	[ReplyMsgToID] [int] NULL,
	[FromEmail] [nvarchar](200) NULL,
	[ToList] [nvarchar](max) NOT NULL,
	[CCList] [nvarchar](max) NULL,
	[BCCList] [nvarchar](max) NULL,
	[IsDraft] [bit] NULL,
	[IsPredefinedTemplate] [bit] NULL,
	[TransactionID] [int] NULL,
	[TransactionRootValueID] [nvarchar](200) NULL,
	[ProjectActivityID] [int] NULL,
	[ProjectTeamID] [int] NULL,
	[ProjectID] [int] NULL,
	[MsgUniqueID] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AttachmentFileToken] [nvarchar](4000) NULL,
	[MessagePostType] [int] NULL,
	[MessgaeScopeType] [int] NULL,
	[ReminderMinutes] [int] NULL,
	[IsEnableReminder] [bit] NULL,
	[ReminderTargetDate] [datetime] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[TransactionGroupID] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppMessage] PRIMARY KEY CLUSTERED 
(
	[MessageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppMessageDeleted]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppMessageDeleted](
	[DeleteMessageID] [int] IDENTITY(1,1) NOT NULL,
	[MessageID] [int] NOT NULL,
	[SenderEmail] [nvarchar](200) NULL,
	[ReceivedEmail] [nvarchar](200) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppMessageDeleted] PRIMARY KEY CLUSTERED 
(
	[DeleteMessageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[APPMessageNotificationSetting]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[APPMessageNotificationSetting](
	[NotificationSettingID] [int] IDENTITY(1,1) NOT NULL,
	[TranscationID] [int] NULL,
	[ProejctID] [int] NULL,
	[SettingName] [nvarchar](500) NULL,
	[NotificationQuery] [nvarchar](max) NULL,
	[MessageUsageType] [int] NULL,
	[MessageTemplateID] [int] NULL,
	[MessageTemplate] [nvarchar](max) NULL,
	[EmScanPeriod] [int] NULL,
	[NotificationQueryContentSettingID] [int] NULL,
	[AlertSpanTime] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [date] NULL,
	[AppModifiedDate] [date] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_APPMessageNotificationSetting] PRIMARY KEY CLUSTERED 
(
	[NotificationSettingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppMessageUserReceived]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppMessageUserReceived](
	[MessageUserReceivedID] [int] IDENTITY(1,1) NOT NULL,
	[MessageID] [int] NOT NULL,
	[ReadDate] [datetime] NULL,
	[ReceivedEmail] [nvarchar](200) NOT NULL,
	[IsSentByEmailServer] [bit] NULL,
	[ReceivedByID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppMessageUserReceived] PRIMARY KEY CLUSTERED 
(
	[MessageUserReceivedID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppMiscJsonData]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppMiscJsonData](
	[DataID] [int] IDENTITY(1,1) NOT NULL,
	[UsageType] [int] NULL,
	[Name] [nvarchar](2000) NULL,
	[Description] [nvarchar](max) NULL,
	[TargetId] [int] NULL,
	[JsonContent] [nvarchar](max) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppMiscJsonData] PRIMARY KEY CLUSTERED 
(
	[DataID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppPorjectWorkFlowTaskTimeSheet]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppPorjectWorkFlowTaskTimeSheet](
	[FlowTaskTimeSheetID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectWorkFlowTaskID] [int] NULL,
	[StartTime] [time](7) NULL,
	[EndTime] [time](7) NULL,
	[TimeSpan] [float] NULL,
	[HourByRate] [float] NULL,
	[ApprovedByID] [int] NULL,
	[ApprovedByDate] [datetime] NULL,
	[comments] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[DateID] [int] NULL,
	[ResourceUserID] [int] NULL,
 CONSTRAINT [PK_AppPorjectWorkFlowTaskTimeSheet] PRIMARY KEY CLUSTERED 
(
	[FlowTaskTimeSheetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectOrTaskTranscation]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectOrTaskTranscation](
	[ProejctTaskTranscationID] [int] IDENTITY(1,1) NOT NULL,
	[ProejctID] [int] NULL,
	[ProjectTaskID] [int] NULL,
	[TranscationID] [int] NULL,
	[TransactionRID] [nvarchar](200) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectOrTaskTranscation] PRIMARY KEY CLUSTERED 
(
	[ProejctTaskTranscationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectOrWorkFlow]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectOrWorkFlow](
	[ProjectID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[ProjectDirectionID] [int] NOT NULL,
	[ParentProjectID] [int] NULL,
	[DateModelStart] [datetime] NULL,
	[DateModelEnd] [datetime] NULL,
	[DateAborted] [datetime] NULL,
	[ProjectPathID] [int] NOT NULL,
	[IsPredefined] [bit] NOT NULL,
	[IsActive] [bit] NULL,
	[TransactionID] [int] NULL,
	[TransactionRID] [nvarchar](200) NULL,
	[TimeUnit] [int] NULL,
	[ProjectWorkflowType] [int] NULL,
	[ProjectTeamID] [int] NULL,
	[ProjectSumaryTaskID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[ProjectPlannedCost] [float] NULL,
	[ProjectActualCost] [float] NULL,
	[ProjectLeaderID] [int] NULL,
	[CurrencyID] [int] NULL,
	[IsNeedBudget] [bit] NULL,
	[Duration] [float] NULL,
	[DisplayLayoutType] [int] NULL,
	[CompanyID] [int] NULL,
	[EmPrivacy] [int] NULL,
	[ParticipatedDmainID] [nvarchar](500) NULL,
	[DateActualStart] [datetime] NULL,
	[DateActualEnd] [datetime] NULL,
	[EmCostType] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[DatePlannedStart] [datetime] NULL,
	[DatePlannedEnd] [datetime] NULL,
	[ProjectModelBugestCost] [float] NULL,
	[RuntimeOriginalProjectID] [int] NULL,
	[CompletedPercent] [float] NULL,
	[RequireTaskCompletedPercentAsCompleProject] [float] NULL,
	[DefaultGanttDisplayUnit] [int] NULL,
	[IsChildProjectAllowParentTtrickleDown] [bit] NULL,
	[IsChildProjectAllowChildBubbleUpParent] [bit] NULL,
	[ProjectLogoImageID] [int] NULL,
	[PlannedWorkHours] [float] NULL,
	[ActualWorkHours] [float] NULL,
	[PlannedResourceCost] [float] NULL,
	[ActualResourceCost] [float] NULL,
	[SaasApplicationID] [int] NULL,
	[TransactionGroupID] [int] NULL,
 CONSTRAINT [PK_pdmProject] PRIMARY KEY NONCLUSTERED 
(
	[ProjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectPerspectiveTask]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectPerspectiveTask](
	[PerspectiveTaskID] [int] IDENTITY(1,1) NOT NULL,
	[PerspectiveSectionID] [int] NULL,
	[ProjectWorkFlowTaskID] [int] NULL,
	[DisplayOrder] [int] NULL,
	[AddtionTaskNotes] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectPerspectiveTask] PRIMARY KEY CLUSTERED 
(
	[PerspectiveTaskID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectPerspectiveView]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectPerspectiveView](
	[PerspectiveSectionID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NOT NULL,
	[ViewSectionName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[DisplayOrder] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectPerspectiveView] PRIMARY KEY CLUSTERED 
(
	[PerspectiveSectionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectPortfolio]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectPortfolio](
	[PortfolioID] [int] IDENTITY(1,1) NOT NULL,
	[PortfilioName] [nvarchar](50) NULL,
	[Description] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NOT NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectPortfolio] PRIMARY KEY CLUSTERED 
(
	[PortfolioID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectPortfolioBoard]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectPortfolioBoard](
	[BoardItmeID] [int] IDENTITY(1,1) NOT NULL,
	[PortfolioID] [int] NULL,
	[SummaryProjectID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectPortfolioBoard] PRIMARY KEY CLUSTERED 
(
	[BoardItmeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectPrivacy]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectPrivacy](
	[ProvacyID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectID] [int] NULL,
	[UserID] [int] NULL,
	[RoleID] [int] NULL,
	[ProjectTeamID] [int] NULL,
	[IsAllowedToEdit] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectPrivacy] PRIMARY KEY CLUSTERED 
(
	[ProvacyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectPrivilegeLibrary]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectPrivilegeLibrary](
	[ProjectPrivilegeID] [int] IDENTITY(1,1) NOT NULL,
	[EmAppProjectPrivilegeCode] [nvarchar](100) NULL,
	[Description] [nvarchar](2000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectPrivilegeLibrary] PRIMARY KEY CLUSTERED 
(
	[ProjectPrivilegeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectRole]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectRole](
	[ProjectRoleID] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[RoleRate] [float] NULL,
	[CurrencyID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectRole] PRIMARY KEY CLUSTERED 
(
	[ProjectRoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectRolePrivilege]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectRolePrivilege](
	[ProjectRoleActionID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectRoleID] [int] NULL,
	[ProjectPrivilegeID] [int] NULL,
	[Description] [nvarchar](2000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectRolePrivilege] PRIMARY KEY CLUSTERED 
(
	[ProjectRoleActionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectSnapshot]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectSnapshot](
	[ProjectSnapshotID] [int] IDENTITY(1,1) NOT NULL,
	[ManProejctID] [int] NULL,
	[SnapshotName] [nvarchar](50) NULL,
	[CopyProejctID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectSnapshot] PRIMARY KEY CLUSTERED 
(
	[ProjectSnapshotID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskCheckList]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskCheckList](
	[TaskCheckListID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectTaskID] [int] NULL,
	[CheckItemDesc] [nvarchar](100) NULL,
	[IsPass] [bit] NULL,
	[Comments] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTaskCheckList] PRIMARY KEY CLUSTERED 
(
	[TaskCheckListID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskExpense]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskExpense](
	[TaskExpenseID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectTaskID] [int] NULL,
	[Category] [int] NULL,
	[Notes] [nvarchar](500) NULL,
	[ExpenseAmount] [float] NULL,
	[ApprovedBy] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTaskExpense] PRIMARY KEY CLUSTERED 
(
	[TaskExpenseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskPredecessor]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskPredecessor](
	[ProjectActivityPredecessorID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectWorkFlowTaskID] [int] NOT NULL,
	[PredecessorID] [int] NOT NULL,
	[PathUILayout] [nvarchar](1000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_pdmProjectActivityPredecessor] PRIMARY KEY CLUSTERED 
(
	[ProjectActivityPredecessorID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskResource]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskResource](
	[TaskResourceID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectWorkFlowTaskID] [int] NOT NULL,
	[UserID] [int] NULL,
	[RoleID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[PlannedWorkHours] [float] NULL,
 CONSTRAINT [PK_pdmTAProjectActivityResource] PRIMARY KEY CLUSTERED 
(
	[TaskResourceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskResourcePlannedHours]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskResourcePlannedHours](
	[PlannedWorkHourID] [int] IDENTITY(1,1) NOT NULL,
	[TaskResourceID] [int] NULL,
	[DateID] [int] NULL,
	[PlannedWorkHours] [float] NULL,
	[StartTime] [time](7) NULL,
	[EndTime] [time](7) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTaskWorkHourPlanned] PRIMARY KEY CLUSTERED 
(
	[PlannedWorkHourID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskTag]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskTag](
	[TaskTagID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectWorkFlowTaskID] [int] NULL,
	[ScopeTagID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTaskTag] PRIMARY KEY CLUSTERED 
(
	[TaskTagID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTaskTimeLog]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTaskTimeLog](
	[TaksTimeLogID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectTaskID] [int] NULL,
	[TeamMemberID] [int] NULL,
	[TiemSpanHours] [float] NULL,
	[RateByHour] [float] NULL,
	[ApprovedBy] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTaskTimeLog] PRIMARY KEY CLUSTERED 
(
	[TaksTimeLogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTeam]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTeam](
	[ProejctTeamID] [int] IDENTITY(1,1) NOT NULL,
	[TeamName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[IsPrefinedTeam] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[EmPrivacy] [int] NULL,
	[ParticipatedDmainID] [nvarchar](500) NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTeam] PRIMARY KEY CLUSTERED 
(
	[ProejctTeamID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTeamMember]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTeamMember](
	[TeamMemberID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectTeamID] [int] NULL,
	[UserID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[ProjectID] [int] NULL,
	[EmCostType] [int] NULL,
	[PersonalRate] [float] NULL,
	[CurrencyID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTeamMember] PRIMARY KEY CLUSTERED 
(
	[TeamMemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTeamMemberRole]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTeamMemberRole](
	[TeamMemberRoleID] [int] IDENTITY(1,1) NOT NULL,
	[TeamMemberID] [int] NULL,
	[ProjectRoleID] [int] NULL,
	[RoleRate] [float] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTeamMemberRole] PRIMARY KEY CLUSTERED 
(
	[TeamMemberRoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectTemplateResource]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectTemplateResource](
	[TemplateResouceID] [int] IDENTITY(1,1) NOT NULL,
	[ProejctID] [int] NULL,
	[ResourceName] [nvarchar](100) NULL,
	[Description] [nvarchar](300) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppProjectTemplateResource] PRIMARY KEY CLUSTERED 
(
	[TemplateResouceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectWorkFlowAction]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectWorkFlowAction](
	[WorkFlowActionID] [int] IDENTITY(1,1) NOT NULL,
	[WorkFlowConditionID] [int] NULL,
	[Name] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[ActionType] [int] NULL,
	[UpdateActionTransactionFieldID] [int] NULL,
	[FormulaExpression] [nvarchar](max) NULL,
	[NextWorkFlowID] [int] NULL,
	[RowIdentity] [uniqueidentifier] NOT NULL,
	[NextTransactionRID] [int] NULL,
	[NextTransactionID] [int] NULL,
	[NextProjectID] [int] NULL,
	[ExcutionDateTime] [datetime] NULL,
	[ExcutedByID] [int] NULL,
	[NotificationSubject] [nvarchar](500) NULL,
	[NotificationMessage] [nvarchar](max) NULL,
	[NotificationDestination] [nvarchar](500) NULL,
	[NotificationDestinationUserIDTransactionFiledID] [int] NULL,
	[PathUILayout] [nvarchar](1000) NULL,
	[ActionFlowOrder] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[NotificationDestinationRoleIDTransactionFiledID] [int] NULL,
	[MessageContentQueryDataSetID] [int] NULL,
	[DataSetQeuryString] [nvarchar](max) NULL,
	[TransactionID] [int] NULL,
	[TransactionUnitID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[ChangeTypeForTransactionUnitField] [int] NULL,
	[MessageTemplateID] [int] NULL,
	[IsNeedToAttachForm] [bit] NULL,
	[IsNeedToAttachAllFormFiles] [bit] NULL,
	[DataLoadID] [int] NULL,
	[ActionGUID] [uniqueidentifier] NULL,
	[CommandTransactionID] [int] NULL,
	[CommandConditionTransactionFieldID] [int] NULL,
	[DataTransferSettingID] [int] NULL,
	[OtherOptions] [nvarchar](4000) NULL,
	[CommandUIOption] [nvarchar](4000) NULL,
	[CallBackCommandID] [int] NULL,
	[CommandSearchViewID] [int] NULL,
	[CommandConditionExpression] [nvarchar](2000) NULL,
 CONSTRAINT [PK_AppProjectWorkFlowAction] PRIMARY KEY CLUSTERED 
(
	[WorkFlowActionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectWorkFlowCondition]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectWorkFlowCondition](
	[WorkFlowConditionID] [int] IDENTITY(1,1) NOT NULL,
	[ProjectWorkFlowTaskID] [int] NULL,
	[Name] [nvarchar](50) NULL,
	[Description] [nvarchar](500) NULL,
	[FormulaExpression] [nvarchar](4000) NULL,
	[ConditionTransactionFieldID] [int] NULL,
	[ConditionTypeID] [int] NULL,
	[ConditionPredictValue] [nvarchar](500) NULL,
	[RowIdentity] [uniqueidentifier] NOT NULL,
	[ProjectID] [int] NULL,
	[TriggerFlowOrder] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[MonitorChildUnitID] [int] NULL,
	[ConditionGUID] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppProjectWorkFlowCondition] PRIMARY KEY CLUSTERED 
(
	[WorkFlowConditionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppProjectWorkFlowTask]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppProjectWorkFlowTask](
	[ProjectWorkFlowTaskID] [int] IDENTITY(1,1) NOT NULL,
	[DatePlannedStart] [datetime] NULL,
	[NbDays] [decimal](18, 4) NULL,
	[DatePlannedEnd] [datetime] NULL,
	[DateActualStart] [datetime] NULL,
	[DateActualEnd] [datetime] NULL,
	[CompletedByID] [int] NULL,
	[ActivityID] [int] NULL,
	[PhaseID] [int] NULL,
	[IsAutoStart] [bit] NULL,
	[SeverityID] [int] NULL,
	[Notes] [nvarchar](4000) NULL,
	[Sort] [int] NULL,
	[ProjectID] [int] NULL,
	[IsFixedPlannedDate] [bit] NULL,
	[TimingDays] [float] NULL,
	[IsAutoComplete] [bit] NULL,
	[IsMilestone] [bit] NULL,
	[ProjectActivityStatusID] [int] NULL,
	[Name] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[Weight] [float] NULL,
	[ToleranceDays] [float] NULL,
	[IsDependent] [bit] NULL,
	[NbHours] [float] NULL,
	[OriginalLibProjectActivityID] [int] NULL,
	[RowIdentity] [uniqueidentifier] NULL,
	[IsActive] [bit] NULL,
	[MainTaskID] [int] NULL,
	[UnitOfTime] [int] NULL,
	[AmountOfTime] [float] NULL,
	[DiagramShapeType] [int] NULL,
	[StageType] [int] NULL,
	[StageUILayout] [nvarchar](1000) NULL,
	[StageStatusFlag] [int] NULL,
	[IsProjectSumaryTask] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[TaskPlannedCost] [float] NULL,
	[TaskActualCost] [float] NULL,
	[CurrencyCode] [nvarchar](10) NULL,
	[IsBillable] [bit] NULL,
	[BillRateByHour] [float] NULL,
	[EmPriority] [int] NULL,
	[ProjectSectionID] [int] NULL,
	[EmTaskType] [int] NULL,
	[EmCostType] [int] NULL,
	[ProjectRoleID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[TransactionID] [int] NULL,
	[TransactionRID] [nvarchar](200) NULL,
	[DateModelStart] [datetime] NULL,
	[DateModelEnd] [datetime] NULL,
	[ActrualNeedAmountHours] [float] NULL,
	[TaskOwnerID] [int] NULL,
	[EmAppTaskOwnerDeliverPhase] [int] NULL,
	[RequirChildrenCompletedPercentAsTaskComple] [float] NULL,
	[CompletedPercent] [float] NULL,
	[TimeSheetEntryMethod] [int] NULL,
	[PlannedWorkHours] [float] NULL,
	[ActualWorkHours] [float] NULL,
	[PlannedResourceCost] [float] NULL,
	[ActualResourceCost] [float] NULL,
	[ProgressID] [int] NULL,
 CONSTRAINT [PK_AppProjectWorkFlowTask] PRIMARY KEY NONCLUSTERED 
(
	[ProjectWorkFlowTaskID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppReport]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppReport](
	[ReportID] [int] IDENTITY(1,1) NOT NULL,
	[DataSourceID] [int] NULL,
	[ReportName] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[ReportFileName] [nvarchar](400) NULL,
	[IsActive] [bit] NULL,
	[ReportEngineType] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[SaasApplicationID] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppReport] PRIMARY KEY CLUSTERED 
(
	[ReportID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppRouteState]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppRouteState](
	[RouteStateID] [int] IDENTITY(1,1) NOT NULL,
	[StateCode] [nvarchar](150) NULL,
	[PageRelativeUrl] [nvarchar](200) NULL,
	[ControllerName] [nvarchar](200) NULL,
	[TemplateUrl] [nvarchar](200) NULL,
	[NoSecurityControl] [bit] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppRouteState] PRIMARY KEY CLUSTERED 
(
	[RouteStateID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearch]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearch](
	[SearchID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](250) NULL,
	[Type] [int] NOT NULL,
	[IsBuiltIn] [bit] NULL,
	[WhereUsedSearchID] [int] NULL,
	[SearchViewID] [int] NULL,
	[IsAutoExecute] [bit] NOT NULL,
	[DataSetID] [int] NULL,
	[FilterByCurrentUserMappingField] [nvarchar](100) NULL,
	[FilterByCurrentUserDomainTypeMappingField] [nvarchar](100) NULL,
	[FilterByCurrentUserRoleMappingField] [nvarchar](100) NULL,
	[BusinessScopeID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[FolderTransactionID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsHideAllToolsBar] [bit] NULL,
	[SaasApplicationID] [int] NULL,
	[IsForPublicAcesss] [bit] NULL,
	[IsFilterByUserTypeEntity] [bit] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_SearchID] PRIMARY KEY CLUSTERED 
(
	[SearchID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchField]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchField](
	[SearchFieldID] [int] IDENTITY(1,1) NOT NULL,
	[SearchID] [int] NULL,
	[Sort] [int] NULL,
	[PositionRow] [int] NULL,
	[PositionColumn] [int] NULL,
	[OperationID] [int] NULL,
	[DisplayText] [nvarchar](250) NULL,
	[IsVisible] [bit] NOT NULL,
	[DefaultValue] [nvarchar](1000) NULL,
	[IsReadOnly] [bit] NOT NULL,
	[IsAutoPopulate] [bit] NOT NULL,
	[ParentFieldID] [int] NULL,
	[IsLoadOnDemand] [bit] NULL,
	[SysTableFiledPath] [nvarchar](800) NULL,
	[ControlType] [int] NULL,
	[EntityID] [int] NULL,
	[DataType] [int] NULL,
	[IsFilterByCurrentUser] [bit] NULL,
	[SysTableFiledFullPath] [nvarchar](800) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsChangedAutoExecute] [bit] NULL,
	[StartValueEntityField] [nvarchar](800) NULL,
	[EndValueEntityField] [nvarchar](800) NULL,
	[StartValueDataSetField] [nvarchar](800) NULL,
	[EndValueDataSetField] [nvarchar](800) NULL,
	[SubControlType] [int] NULL,
	[CascadingRelationTable] [nvarchar](500) NULL,
	[CascadingRelationTableParentKeyField] [nvarchar](500) NULL,
	[CascadingRelationTableChildKeyField] [nvarchar](500) NULL,
	[IsAllowMultipleSelect] [bit] NULL,
	[MasterEntityFieldlID] [int] NULL,
	[InnerEntitySubscribeFiled] [nvarchar](100) NULL,
	[IsSkipSearch] [bit] NULL,
	[DataRetrieveType] [int] NULL,
	[CascadingRelationTableSchemaOwner] [nvarchar](500) NULL,
	[AppExternalSourceFrom] [int] NULL,
	[DdlQueryText] [nvarchar](4000) NULL,
	[WhereClauseExpress] [nvarchar](1000) NULL,
	[EmInternalCodeRegistration] [int] NULL,
 CONSTRAINT [PK_AppSearchFielD] PRIMARY KEY CLUSTERED 
(
	[SearchFieldID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchParameter]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchParameter](
	[SearchparameterID] [int] IDENTITY(1,1) NOT NULL,
	[ParameterID] [int] NULL,
	[SearchFieldID] [int] NULL,
	[DefaultValue] [nvarchar](100) NULL,
	[SearchID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSearchParameter] PRIMARY KEY CLUSTERED 
(
	[SearchparameterID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchSaved]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchSaved](
	[SearchSavedID] [int] IDENTITY(1,1) NOT NULL,
	[SearchID] [int] NOT NULL,
	[SavedSearchName] [nvarchar](500) NOT NULL,
	[UserID] [int] NOT NULL,
	[SystemTimeStamp] [timestamp] NOT NULL,
	[IsDefault] [bit] NULL,
	[IsAutoExecute] [bit] NULL,
	[GroupID] [int] NULL,
	[DefaultSearchViewID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSearchSaved] PRIMARY KEY CLUSTERED 
(
	[SearchSavedID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchSavedValue]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchSavedValue](
	[SearchSavedValueID] [int] IDENTITY(1,1) NOT NULL,
	[SearchSavedID] [int] NOT NULL,
	[SearchFieldID] [int] NOT NULL,
	[SearchValue] [nvarchar](1000) NULL,
	[OperationID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSearchSavedValue] PRIMARY KEY CLUSTERED 
(
	[SearchSavedValueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchView]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchView](
	[SearchViewID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](2500) NULL,
	[NoSecurity] [bit] NOT NULL,
	[GridOutputMode] [int] NOT NULL,
	[Options] [int] NOT NULL,
	[ViewType] [int] NOT NULL,
	[WhereUsedDefaultViewId] [int] NULL,
	[PivotOrChartSetting] [nvarchar](max) NULL,
	[ColumnCount] [int] NOT NULL,
	[RowPerPage] [int] NOT NULL,
	[IsFilterByCurrentUser] [bit] NULL,
	[DataSetID] [int] NULL,
	[ChartInnerRadius] [decimal](18, 2) NULL,
	[ChartType] [int] NULL,
	[CatalogueSearchID] [int] NULL,
	[EntityInternalCode] [nvarchar](100) NULL,
	[TransactionID] [int] NULL,
	[ProductDetaiViewMapUnitID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsMasterEditInSamePage] [nchar](10) NULL,
	[AppRestResourceUri] [nvarchar](1000) NULL,
	[AppRestResourceUriDisplay] [nvarchar](1000) NULL,
	[NbFrozenColumn] [int] NULL,
	[UpdateTransctionID] [int] NULL,
	[UpdateTransctionRootFieldName] [nvarchar](50) NULL,
	[UpdateChildParentFKFieldName] [nvarchar](50) NULL,
	[UpdateBaseTranscationUnitID] [int] NULL,
	[IsMassUpdateView] [bit] NULL,
	[IsAllowAddRow] [bit] NULL,
	[IsAllowDeleteRow] [bit] NULL,
	[IsAllowUpdateRow] [bit] NULL,
	[CanlendarDefaultViewMode] [int] NULL,
	[IsEnableCalendarMonthView] [bit] NULL,
	[IsEnableCalendarWeekView] [bit] NULL,
	[IsEnableCalendarDayView] [bit] NULL,
	[IsEnableCalendarNavigator] [bit] NULL,
	[IsDisableClientTimeConvert] [bit] NULL,
	[SaasApplicationID] [int] NULL,
	[IsForPublicAcesss] [bit] NULL,
	[IsFilterByUserTypeEntity] [bit] NULL,
	[CalendarStartHour] [int] NULL,
	[CalendarEndHour] [int] NULL,
	[FilterSearchID] [int] NULL,
	[HierachyParentViewID] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_SearchViewID] PRIMARY KEY CLUSTERED 
(
	[SearchViewID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchViewField]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchViewField](
	[SearchViewFieldID] [int] IDENTITY(1,1) NOT NULL,
	[SearchViewID] [int] NOT NULL,
	[IsVisible] [bit] NOT NULL,
	[DisplayText] [nvarchar](250) NULL,
	[Sort] [int] NULL,
	[SysTableFiledPath] [nvarchar](800) NULL,
	[ControlType] [int] NULL,
	[EntityID] [int] NULL,
	[DataType] [int] NULL,
	[IsGroupBy] [bit] NULL,
	[GroupByLevel] [int] NULL,
	[AggregationFunctionType] [int] NULL,
	[IsFilterByCurrentUser] [bit] NULL,
	[IsMapToChartX] [bit] NULL,
	[IsMapToChartY] [bit] NULL,
	[ChartYMappingOrder] [int] NULL,
	[TreeLevel] [int] NULL,
	[IsTreeNodeID] [bit] NULL,
	[IsTreeNodeDisplay] [bit] NULL,
	[MappingSearchFieldID] [int] NULL,
	[IsTreeNodeDesc] [bit] NULL,
	[IsTreeNodeImageUrl] [bit] NULL,
	[ProductDetaiMapTransFiledID] [int] NULL,
	[IsUserDefined1] [bit] NULL,
	[IsUserDefined2] [bit] NULL,
	[IsUserDefined3] [bit] NULL,
	[IsUserDefined4] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[IsFileFoderID] [bit] NULL,
	[IsTransRootID] [bit] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[Width] [int] NULL,
	[RowNumber] [int] NULL,
	[ColumnNumber] [int] NULL,
	[OrderByLevel] [int] NULL,
	[IsDescOrder] [bit] NULL,
	[MassUpdateTransactionFieldID] [int] NULL,
	[IsReadOnly] [bit] NULL,
	[PullCriteriaAsDefaultValueSearchFieldID] [int] NULL,
	[EmInternalCodeRegistration] [int] NULL,
	[IsPartnerFilterFiled] [bit] NULL,
	[JoinToParentViewFieldID] [int] NULL,
	[IsCalulationField] [bit] NULL,
	[OtherSettings] [nvarchar](max) NULL,
 CONSTRAINT [PK_SearchViewFieldID] PRIMARY KEY CLUSTERED 
(
	[SearchViewFieldID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchViewReport]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchViewReport](
	[SearchViewReportID] [int] IDENTITY(1,1) NOT NULL,
	[SearchViewID] [int] NULL,
	[ReportID] [int] NULL,
	[ReportDisplayName] [nvarchar](200) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSearchViewReport] PRIMARY KEY CLUSTERED 
(
	[SearchViewReportID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSearchViewReportParamterMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSearchViewReportParamterMapping](
	[ParamterMappingID] [int] IDENTITY(1,1) NOT NULL,
	[SearchViewReportID] [int] NULL,
	[SearchViewFieldID] [int] NULL,
	[ReportParamterName] [nvarchar](200) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSearchViewReportParamterMapping] PRIMARY KEY CLUSTERED 
(
	[ParamterMappingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityEntityAction]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityEntityAction](
	[EntityActionID] [int] IDENTITY(1,1) NOT NULL,
	[ActionCode] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](150) NULL,
	[NoSecurityControl] [bit] NOT NULL,
	[TransactionID] [int] NULL,
	[RouteStateID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsSharedbyMutipleCompany] [bit] NULL,
 CONSTRAINT [PK_AppSecurityEntityAction] PRIMARY KEY CLUSTERED 
(
	[EntityActionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityGroup]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityGroup](
	[GroupID] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
	[GroupName] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](250) NULL,
	[LoginEvent] [nvarchar](50) NULL,
	[InternalCode] [nvarchar](80) NULL,
	[IsBuiltIn] [bit] NULL,
	[GroupUsage] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[OrganizationID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsSharedbyMutipleCompany] [bit] NULL,
	[DefaultDesktopID] [int] NULL,
	[RoleUserTypeID] [int] NULL,
	[BusinessPartnerID] [int] NULL,
 CONSTRAINT [PK_AppSecurityGroup] PRIMARY KEY CLUSTERED 
(
	[GroupID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityGroupMember]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityGroupMember](
	[RoleMemberID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[UserID] [int] NOT NULL,
	[IsDefault] [bit] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSecurityGroupMember] PRIMARY KEY CLUSTERED 
(
	[RoleMemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityRegDomainListMenu]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityRegDomainListMenu](
	[DomainMenuID] [int] IDENTITY(1,1) NOT NULL,
	[MenuID] [int] NOT NULL,
	[DomainID] [int] NULL,
	[SystemTimeStamp] [timestamp] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[OrganizationID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSecurityRegDomainListMenu] PRIMARY KEY CLUSTERED 
(
	[DomainMenuID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecuritySysObjGroupUser]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecuritySysObjGroupUser](
	[SecurityRightID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NULL,
	[UserID] [int] NULL,
	[TransactionID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[SearchID] [int] NULL,
	[SearchViewID] [int] NULL,
	[RouteStateID] [int] NULL,
	[TransactionUnitLinkedSearchId] [int] NULL,
	[IsInVisible] [bit] NULL,
	[IsUnSaveAble] [bit] NULL,
	[IsSpecialPermission] [bit] NULL,
	[TransactionUnitID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[OrganizationID] [int] NULL,
	[DesktopID] [int] NULL,
	[UserActionTransactionID] [int] NULL,
	[UserActionTransactionCode] [int] NULL,
	[UserActionTransactionUnitID] [int] NULL,
	[UserActionTransactionUnitCode] [int] NULL,
	[FormLinkTargetID] [int] NULL,
	[ReportID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[EmUserType] [int] NULL,
	[IsIgnoreFilterBy] [bit] NULL,
	[IsDefault] [bit] NULL,
	[IsNeedSpecailEditPrivilege] [bit] NULL,
	[CommandId] [int] NULL,
 CONSTRAINT [PK_AppSecurityGroupUserRight] PRIMARY KEY CLUSTERED 
(
	[SecurityRightID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityTransactionAction]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityTransactionAction](
	[TransactionActionID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[TransactionID] [int] NULL,
	[ActionType] [int] NULL,
	[IsNeedSecurityControl] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSecurityTransactionAction] PRIMARY KEY CLUSTERED 
(
	[TransactionActionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityTransactionActionResource]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityTransactionActionResource](
	[AppTransactionActionResourceID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionActionID] [int] NULL,
	[GroupID] [int] NULL,
	[UserID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSecurityTransactionActionResource] PRIMARY KEY CLUSTERED 
(
	[AppTransactionActionResourceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityUserListMenu]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityUserListMenu](
	[UserMenuID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[MenuID] [int] NULL,
	[GroupID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSecurityUserListMenu] PRIMARY KEY CLUSTERED 
(
	[UserMenuID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSecurityUserRolePrevilege]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSecurityUserRolePrevilege](
	[UserPrevilegeID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[RoleID] [int] NULL,
	[EntityActionID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSecurityUserRolePrevilege] PRIMARY KEY CLUSTERED 
(
	[UserPrevilegeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSEFolder]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSEFolder](
	[FolderID] [int] IDENTITY(1,1) NOT NULL,
	[FolderType] [int] NULL,
	[Name] [nvarchar](200) NOT NULL,
	[Description] [nvarchar](500) NULL,
	[ParentID] [int] NULL,
	[IsSystemFolder] [bit] NULL,
	[DefaultViewID] [int] NULL,
	[TransactionID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[OtherSettings] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppSEFolder] PRIMARY KEY CLUSTERED 
(
	[FolderID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSEFolderResource]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSEFolderResource](
	[FolderResourceID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[RoleID] [int] NULL,
	[FolderID] [int] NOT NULL,
	[IsReadOnly] [bit] NULL,
	[IsAllowedToEditSecurity] [bit] NOT NULL,
	[SystemTimeStamp] [timestamp] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppSEFolderResource] PRIMARY KEY CLUSTERED 
(
	[FolderResourceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppService]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppService](
	[ServiceId] [int] IDENTITY(1,1) NOT NULL,
	[ServiceCode] [nvarchar](15) NOT NULL,
	[Description1] [nvarchar](60) NULL,
	[Description2] [nvarchar](60) NULL,
	[Posted] [bit] NOT NULL,
	[Discontinued] [bit] NOT NULL,
	[ServiceTypeId] [int] NULL,
	[Memo] [nvarchar](2000) NULL,
	[UnitMeasureId] [int] NULL,
	[UnitCost] [decimal](18, 6) NULL,
	[CurrencyId] [int] NULL,
	[IsTaxable1] [bit] NOT NULL,
	[IsTaxable2] [bit] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppService] PRIMARY KEY CLUSTERED 
(
	[ServiceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppStorePages]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppStorePages](
	[PageID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NULL,
	[HtmlContent] [nvarchar](max) NULL,
	[IsActive] [bit] NULL,
	[MetaDesciption] [nvarchar](250) NULL,
	[UrlAndHandle] [nvarchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppSysLabelLanguage]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppSysLabelLanguage](
	[SysLableLanguageID] [int] IDENTITY(1,1) NOT NULL,
	[LanguageID] [int] NULL,
	[MenuID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[FormID] [int] NULL,
	[LanguageText] [nvarchar](500) NULL,
	[TransactionUnitLinkedSearchId] [int] NULL,
	[LinkTargetID] [int] NULL,
	[SearchViewFieldID] [int] NULL,
	[SearchFieldID] [int] NULL,
	[TransactionUnitID] [int] NULL,
	[SearchViewID] [int] NULL,
	[SearchID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[ApplicationID] [int] NULL,
 CONSTRAINT [PK_AppSysLabelLanguage] PRIMARY KEY CLUSTERED 
(
	[SysLableLanguageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransaction]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransaction](
	[TransactionID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionName] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[NeedToCheckRowVersion] [bit] NULL,
	[TransactionOrganizedType] [int] NULL,
	[PostProcessStoreProcedure] [nvarchar](max) NULL,
	[ListFilterWhereClause] [nvarchar](500) NULL,
	[IsReadOnly] [bit] NULL,
	[FormID] [int] NULL,
	[BusinessScopeID] [int] NULL,
	[PrintFormID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[IsEnableFolderSecurity] [bit] NULL,
	[IsSystemBuitIn] [bit] NULL,
	[IsNeedToSetCriticalPathTrackFlow] [bit] NULL,
	[IsNeedToSetComunication] [bit] NULL,
	[FolderTransactionID] [int] NULL,
	[FolderUsageType] [int] NULL,
	[DataSourceFrom] [int] NULL,
	[EmAppTransBusinessType] [int] NULL,
	[MgtRootFolderID] [int] NULL,
	[LogicalDisplayEntityID] [int] NULL,
	[TransactionFileStorageRootFolderID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsExclusiveForOwner] [bit] NULL,
	[MasterWorkflowID] [int] NULL,
	[MasterTransactionID] [int] NULL,
	[EmGrandChildEditMode] [int] NULL,
	[ConversationBoxDockPosition] [int] NULL,
	[PreSaveValidationMethod] [nvarchar](500) NULL,
	[IsPhysicalModelTableCreated] [bit] NULL,
	[IsAllowSaveAs] [bit] NULL,
	[FormTitleDisplayFieldID] [int] NULL,
	[IsShowSaveButton] [bit] NULL,
	[IsShowCalculateButton] [bit] NULL,
	[IsShowPrintButton] [bit] NULL,
	[SaasApplicationID] [int] NULL,
	[IsForPublicAcesss] [bit] NULL,
	[EmNotificaionMethod] [int] NULL,
	[NotificationSetting] [nvarchar](2000) NULL,
	[WebApiConfigID] [int] NULL,
	[GlobalGuid] [uniqueidentifier] NULL,
 CONSTRAINT [PK_AppTransaction] PRIMARY KEY CLUSTERED 
(
	[TransactionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionDataLoad]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionDataLoad](
	[DataLoadID] [int] IDENTITY(1,1) NOT NULL,
	[DataSetID] [int] NULL,
	[TransactionID] [int] NULL,
	[LoadName] [nvarchar](50) NULL,
	[Description] [nvarchar](100) NULL,
	[LoadOrder] [int] NULL,
	[TransactionUnitID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsAutoExcutedWhenOpenEditForm] [bit] NULL,
	[IsAutoExecuteBeforeIntialCscading] [bit] NULL,
 CONSTRAINT [PK_AppTransactionDataLoad] PRIMARY KEY CLUSTERED 
(
	[DataLoadID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionDataTransferSetting]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionDataTransferSetting](
	[DataTransferSettingID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionID] [int] NULL,
	[InternalCode] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[TransferTypeID] [int] NULL,
	[DestinationTransactionID] [int] NULL,
 CONSTRAINT [PK_AppTransactionDataTransferSetting] PRIMARY KEY CLUSTERED 
(
	[DataTransferSettingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionField]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionField](
	[TransactionFieldID] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
	[TransactionUnitID] [int] NOT NULL,
	[DisplayName] [nvarchar](500) NOT NULL,
	[DataBaseFieldName] [nvarchar](500) NOT NULL,
	[ControlType] [int] NOT NULL,
	[DataType] [int] NULL,
	[EntityID] [int] NULL,
	[InternalCode] [nvarchar](50) NULL,
	[NeedValidator] [bit] NULL,
	[ValidatorType] [int] NULL,
	[NBDecimal] [int] NULL,
	[SortOrder] [int] NULL,
	[MaxCharLegnth] [int] NULL,
	[DDLParentLevelID] [int] NULL,
	[AutoIncrementSeed] [int] NULL,
	[AutoIncrementPrefix] [nvarchar](200) NULL,
	[AutoIncrementLastID] [int] NULL,
	[IsNeedLog] [bit] NULL,
	[IsAllowEmpty] [bit] NULL,
	[ToolTip] [nvarchar](500) NULL,
	[IsConvertToUpperCase] [bit] NULL,
	[DefaultValue] [nvarchar](500) NULL,
	[CascadingRelationTable] [nvarchar](500) NULL,
	[CascadingRelationTableParentKeyField] [nvarchar](500) NULL,
	[CascadingRelationTableChildKeyField] [nvarchar](500) NULL,
	[MasterEntityFieldlID] [int] NULL,
	[InnerEntitySubscribeFiled] [nvarchar](100) NULL,
	[DisplayWidth] [nvarchar](50) NULL,
	[IsReadonly] [bit] NULL,
	[ChildUnitSubscribeParentFieldID] [int] NULL,
	[ParentUnitSubscribeChildAggFunctionID] [int] NULL,
	[IsGridUseAvailableEntitySource] [bit] NULL,
	[IsUnique] [bit] NULL,
	[IsGroupBy] [bit] NULL,
	[GroupByLevel] [int] NULL,
	[MatrixKeyTransactionFieldId] [int] NULL,
	[IsPrimaryKey] [bit] NOT NULL,
	[IsLinkToParentPrimaryKey] [bit] NOT NULL,
	[IsVisible] [bit] NULL,
	[IsFilterByCurrentUser] [bit] NULL,
	[DataRetrieveType] [int] NULL,
	[SystemVariableEnumCode] [nvarchar](100) NULL,
	[MatrixForeignKeyFieldID] [int] NULL,
	[IsPivotRow] [bit] NULL,
	[IsPivotColumn] [bit] NULL,
	[AppExternalSourceFrom] [int] NULL,
	[DdlQueryText] [nvarchar](4000) NULL,
	[WhereClauseExpress] [nvarchar](1000) NULL,
	[DdlForeignUnitID] [int] NULL,
	[DdlForeignUnitDisplayDbFieds] [nvarchar](1000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[FileControlTypeFolderTransactionID] [int] NULL,
	[LinkToParentPrimaryKeyFieldID] [int] NULL,
	[RowIdentityGuid] [uniqueidentifier] NULL,
	[MaxNumber] [decimal](38, 0) NULL,
	[CascadingRelationTableSchemaOwner] [nvarchar](500) NULL,
	[MappingEmSystemTokenField] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsLogicalDisplay] [bit] NULL,
	[IsChangeTrigerNotification] [bit] NULL,
	[SiblingUnitLogicalKeyFieldID] [int] NULL,
	[IsFieldExclusiveForOwner] [bit] NULL,
	[IsAllowEditOnMobileRowPopup] [bit] NULL,
	[EmInternalCodeRegistration] [int] NULL,
	[HostFormLayoutItemID] [int] NULL,
	[IsPivotValue] [bit] NULL,
	[PivotAggregationType] [int] NULL,
	[ControlTypeParam1] [nvarchar](500) NULL,
	[ControlTypeParam2] [nvarchar](500) NULL,
	[ControlTypeParam3] [nvarchar](500) NULL,
	[IsPrintVisible] [bit] NULL,
	[OnChangeTriggerToCommandID] [int] NULL,
	[IsTempVariable] [bit] NULL,
	[MappingToAvailableSourceUnitTransactionFieldID] [int] NULL,
	[IsStoreToExtendTable] [bit] NULL,
	[LabelCascadingRelationTable] [nvarchar](500) NULL,
	[LabelCascadingRelationTableParentKeyField] [nvarchar](500) NULL,
	[InnerEntityLabelSubscribeFiled] [nvarchar](100) NULL,
 CONSTRAINT [PK_AppTransactionField] PRIMARY KEY CLUSTERED 
(
	[TransactionFieldID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionFieldAggFunction]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionFieldAggFunction](
	[FieldAggFunctionID] [int] IDENTITY(1,1) NOT NULL,
	[AggregationFunctionType] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionFieldAggFunction] PRIMARY KEY CLUSTERED 
(
	[FieldAggFunctionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionGroup]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionGroup](
	[TransactionGroupID] [int] IDENTITY(1,1) NOT NULL,
	[AssotmentnavigationID] [int] NULL,
	[GroupName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[IsDefaultGroup] [bit] NULL,
	[GroupSortOrder] [int] NULL,
	[EmBuseinssScope] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[SaasApplicationID] [int] NULL,
 CONSTRAINT [PK_AppTransactionGroup] PRIMARY KEY CLUSTERED 
(
	[TransactionGroupID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionGroupItem]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionGroupItem](
	[GroupItemID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionGroupID] [int] NULL,
	[TransactionItemID] [int] NULL,
	[TransactionCaculationFlowOrder] [int] NULL,
	[TransactionLayoutOrder] [int] NULL,
	[IsCrossGroupSharedHeader] [bit] NULL,
	[IsGroupSharedHeader] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[UserDefined1] [nvarchar](200) NULL,
	[UserDefined2] [nvarchar](200) NULL,
	[UserDefined3] [nvarchar](200) NULL,
	[UserDefined4] [nvarchar](200) NULL,
	[UserDefined5] [nvarchar](200) NULL,
	[TransID] [int] NULL,
 CONSTRAINT [PK_AppTransactionGroupItem] PRIMARY KEY CLUSTERED 
(
	[GroupItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionGroupSession]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionGroupSession](
	[TransactionGroupSessionID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionGroupID] [int] NOT NULL,
	[SessionGroupName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[SaasApplicationID] [int] NULL,
 CONSTRAINT [PK_TransactionGroupSession] PRIMARY KEY CLUSTERED 
(
	[TransactionGroupSessionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionItem]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionItem](
	[AppTransactionItemID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionID] [int] NULL,
	[TransactionItemName] [nvarchar](500) NULL,
	[Description] [nvarchar](1000) NULL,
	[CategoryID] [int] NULL,
	[Tag] [nvarchar](1000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionItem] PRIMARY KEY CLUSTERED 
(
	[AppTransactionItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionNavigation]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionNavigation](
	[TransNavigationID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionID] [int] NULL,
	[QuickSearchID] [int] NULL,
	[FolderViewID] [int] NULL,
	[IsDefaultView] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionNavigation] PRIMARY KEY CLUSTERED 
(
	[TransNavigationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionPostProcess]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionPostProcess](
	[PostProcessID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[TransactionID] [int] NULL,
	[ProcessFlow] [int] NULL,
	[PostStoreProcedureName] [nvarchar](500) NULL,
	[ExternalCommand] [nvarchar](500) NULL,
	[InternalMethod] [nvarchar](500) NULL,
	[RootUnitID] [int] NULL,
	[ParameterOptions] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionPostProcess] PRIMARY KEY CLUSTERED 
(
	[PostProcessID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionSaveAsMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionSaveAsMapping](
	[MappingID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[TransactionID] [int] NULL,
	[MappingUnitId] [int] NULL,
	[SourceFiledID] [int] NULL,
	[TargetFiledID] [int] NULL,
	[IsBlankTargetField] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[DataTransferSettingID] [int] NULL,
	[JsonPropertyPathName] [nvarchar](200) NULL,
 CONSTRAINT [PK_AppTransactionSaveAsMapping] PRIMARY KEY CLUSTERED 
(
	[MappingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnit]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnit](
	[TransactionUnitID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionID] [int] NULL,
	[UnitDisplayName] [nvarchar](200) NULL,
	[DataBaseTableName] [nvarchar](200) NULL,
	[TransactionFlow] [int] NULL,
	[ParentTransactionUnitID] [int] NULL,
	[IsReadOnly] [bit] NULL,
	[IsMatrixUnit] [bit] NULL,
	[IsSynchToDatabaseTable] [bit] NULL,
	[IsMatrixPivotUnit] [bit] NULL,
	[IsMasterSiblingUnit] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[IsPrimaryKeyIdentityInsert] [bit] NOT NULL,
	[SchemaOwner] [nvarchar](50) NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsExclusiveForOwner] [bit] NULL,
	[IsDisableAddButton] [bit] NULL,
	[IsDisableDeleteButton] [bit] NULL,
	[BaseDataBaseTableName] [nvarchar](200) NULL,
	[TransactionUnitIentityGuid] [uniqueidentifier] NULL,
	[TreeViewKeyField] [nvarchar](200) NULL,
	[TreeViewParentKeyField] [nvarchar](200) NULL,
	[EmGridViewDisplayType] [int] NULL,
	[ImageHeight] [int] NULL,
	[IsUsedForLoadingAvailableSource] [bit] NULL,
	[AvailableSourceFilterWhereClause] [nvarchar](1000) NULL,
	[AvailableSourceFilterByParentTransactionFieldID] [int] NULL,
	[AvailableSourceMatchToParentUnitTransactionFieldId] [int] NULL,
	[MinRowCount] [int] NULL,
	[MaxRowCount] [int] NULL,
	[DataSourceQuery] [nvarchar](4000) NULL,
	[AvailableSourceUnitID] [int] NULL,
 CONSTRAINT [PK_AppTransactionUnit] PRIMARY KEY CLUSTERED 
(
	[TransactionUnitID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitDeleteFlow]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitDeleteFlow](
	[DeleteFlowID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitID] [int] NULL,
	[RelativeTableName] [nvarchar](200) NULL,
	[RelativeForeignKeyName] [nvarchar](200) NULL,
	[IsForcedDelete] [bit] NULL,
	[IsSetEmpty] [bit] NULL,
	[IsNotAllowedDeleteWithMsg] [bit] NULL,
	[DeleteFlowPriority] [int] NULL,
	[WarningMessage] [nvarchar](200) NULL,
	[StoredProcedureName] [nvarchar](200) NULL,
	[SpParameterOptions] [nvarchar](500) NULL,
	[DeleteValidationStoredProcedureName] [nvarchar](200) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionUnitDeleteFlow] PRIMARY KEY CLUSTERED 
(
	[DeleteFlowID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitExtendFieldValue]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitExtendFieldValue](
	[UnitExtendFieldValueID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitID] [int] NULL,
	[UnitExtendFiledID] [int] NULL,
	[UnitPKValue] [nvarchar](100) NULL,
	[ValueText] [nvarchar](max) NULL,
	[AppCreatedById] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedById] [int] NULL,
	[AppCreatedByCompanyId] [int] NULL,
 CONSTRAINT [PK_AppTransactionUnitExtendFieldValue] PRIMARY KEY CLUSTERED 
(
	[UnitExtendFieldValueID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitFormula]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitFormula](
	[TransactionUnitFormulaID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitID] [int] NULL,
	[CaculationFlowSort] [int] NULL,
	[FormulaExpression] [nvarchar](4000) NOT NULL,
	[WarningMessage] [nvarchar](4000) NULL,
	[FunctionType] [int] NULL,
	[OperationType] [int] NULL,
	[ConditionFieldID] [int] NULL,
	[SwitchTrueFalseType] [bit] NULL,
	[ChildTransactionUnitID] [int] NULL,
	[SystemTimeStamp] [timestamp] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[WarningHighlightTransFieldID] [int] NULL,
	[WarningHighlightStyleID] [int] NULL,
	[FormulaName] [nvarchar](500) NULL,
	[ApplyToScope] [int] NULL,
	[SearchViewId] [int] NULL,
 CONSTRAINT [PK_pdmTransactionUnitFormula] PRIMARY KEY CLUSTERED 
(
	[TransactionUnitFormulaID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitJoin]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitJoin](
	[UnitJoinID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitID] [int] NULL,
	[FromJointKeyTransactionFieldID] [int] NULL,
	[JoinToDataBaseTableName] [nvarchar](50) NULL,
	[JoinToSchemaOwner] [nvarchar](50) NULL,
	[JoinToKeyDataBaseFieldName] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionUnitJoin] PRIMARY KEY CLUSTERED 
(
	[UnitJoinID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitJoinSelectColumnMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitJoinSelectColumnMapping](
	[MappingID] [int] IDENTITY(1,1) NOT NULL,
	[UnitJoinID] [int] NULL,
	[FromJointPlaceHolderTransactionFieldID] [int] NULL,
	[JoinToSeelctDataBaseFieldName] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransactionUnitJoinSelectColumnMapping] PRIMARY KEY CLUSTERED 
(
	[MappingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitLinkedSearch]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitLinkedSearch](
	[TransactionUnitLinkedSearchId] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitId] [int] NOT NULL,
	[SearchId] [int] NULL,
	[SearchSaveID] [int] NULL,
	[SearchViewId] [int] NOT NULL,
	[Name] [nvarchar](100) NULL,
	[Action] [int] NULL,
	[IsSingleSelectedRow] [bit] NULL,
	[Description] [nvarchar](500) NULL,
	[UsageType] [int] NULL,
	[GroupName] [nvarchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[IsNeedPreValidation] [bit] NULL,
	[IsNeedPostValidation] [bit] NULL,
	[CallbackRestResourceUri] [nvarchar](1000) NULL,
	[TargetTransactionID] [int] NULL,
	[ConditionTransFieldID] [int] NULL,
	[CallBackCommandID] [int] NULL,
	[Sort] [int] NULL,
	[IsPopup] [bit] NULL,
	[PopupWidth] [int] NULL,
	[PopupHeight] [int] NULL,
	[IconName] [nvarchar](500) NULL,
	[OtherSettings] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppTransactionUnitLinkedSearch] PRIMARY KEY CLUSTERED 
(
	[TransactionUnitLinkedSearchId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitSearchFieldMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitSearchFieldMapping](
	[TransactionUnitSearchFieldMappingId] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitLinkedSearchId] [int] NOT NULL,
	[TransactionFieldId] [int] NOT NULL,
	[SearchFieldId] [int] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[TargetUnitID] [int] NULL,
	[TargetTransactionFieldDBName] [nvarchar](200) NULL,
 CONSTRAINT [PK_AppTransactionUnitSearchFieldMapping] PRIMARY KEY CLUSTERED 
(
	[TransactionUnitSearchFieldMappingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransactionUnitSearchViewFieldMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping](
	[TransactionUnitSearchViewFieldMappingId] [int] IDENTITY(1,1) NOT NULL,
	[TransactionUnitLinkedSearchId] [int] NOT NULL,
	[TransactionFieldId] [int] NULL,
	[SearchViewFieldId] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[ExternalAppFieldMappingCode] [nvarchar](50) NULL,
	[IsUnique] [bit] NULL,
	[TargetUnitID] [int] NULL,
	[TargetTransactionFieldDBName] [nvarchar](200) NULL,
 CONSTRAINT [PK_AppTransactionUnitSearchViewFieldMapping] PRIMARY KEY CLUSTERED 
(
	[TransactionUnitSearchViewFieldMappingId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTransAuditTrailLog]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTransAuditTrailLog](
	[AuditTrailLogID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionID] [int] NULL,
	[RootValueID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[RowIdentityName] [nvarchar](500) NULL,
	[TraiLogAction] [nvarchar](100) NULL,
	[ModifiedValueBefor] [nvarchar](4000) NULL,
	[ModifiedValueAfter] [nvarchar](4000) NULL,
	[BatchNoID] [bigint] NULL,
	[UnitID] [int] NULL,
	[ChildUnitRowValueID] [nvarchar](50) NULL,
	[GrandChildUnitRowValueID] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTransAuditTrailLog] PRIMARY KEY CLUSTERED 
(
	[AuditTrailLogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTranscationDataLoadFieldMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTranscationDataLoadFieldMapping](
	[FieldMappingID] [int] IDENTITY(1,1) NOT NULL,
	[DataLoadID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[DBColumnName] [nvarchar](100) NULL,
	[IsConditionMapping] [bit] NULL,
	[WhereClause] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTranscationDataLoadFieldMapping] PRIMARY KEY CLUSTERED 
(
	[FieldMappingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTranscationReport]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTranscationReport](
	[TransctionReportID] [int] IDENTITY(1,1) NOT NULL,
	[TranscationID] [int] NULL,
	[ReportID] [int] NULL,
	[ReportDisplayName] [nvarchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTranscationReport] PRIMARY KEY CLUSTERED 
(
	[TransctionReportID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTrasactionSnapShot]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTrasactionSnapShot](
	[TrasactionSnapShotID] [int] IDENTITY(1,1) NOT NULL,
	[TransactionID] [int] NULL,
	[RootValueID] [int] NULL,
	[TransactionFieldID] [int] NULL,
	[BatchNoID] [bigint] NULL,
	[UnitID] [int] NULL,
	[ChildUnitRowValueID] [nvarchar](50) NULL,
	[GrandChildUnitRowValueID] [nvarchar](50) NULL,
	[SnapShotValue] [nvarchar](4000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTrasactionSnapShot] PRIMARY KEY CLUSTERED 
(
	[TrasactionSnapShotID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppTrascationRecycleBin]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppTrascationRecycleBin](
	[RecycleBinID] [int] IDENTITY(1,1) NOT NULL,
	[TranscationID] [int] NULL,
	[RootKeyValueID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppTrascationRecycleBin] PRIMARY KEY CLUSTERED 
(
	[RecycleBinID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUserAppointment]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUserAppointment](
	[UserAppointmentId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Subject] [nvarchar](300) NULL,
	[Body] [nvarchar](max) NULL,
	[DateStart] [datetime] NOT NULL,
	[DateEnd] [datetime] NOT NULL,
	[IsAllDay] [bit] NOT NULL,
	[Importance] [int] NOT NULL,
	[Location] [nvarchar](300) NULL,
	[OriginalAppointmentID] [int] NULL,
	[UserCC] [nvarchar](2000) NULL,
	[SystemTimeStamp] [timestamp] NOT NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppUserAppointment] PRIMARY KEY CLUSTERED 
(
	[UserAppointmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUserDefineTheme]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUserDefineTheme](
	[ThemeID] [int] IDENTITY(1,1) NOT NULL,
	[ThemeName] [nvarchar](500) NULL,
	[Description] [nvarchar](2000) NULL,
	[ThemeDetails] [nvarchar](max) NULL,
	[IsForAllUsers] [bit] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
 CONSTRAINT [PK_AppUserDefineTheme] PRIMARY KEY CLUSTERED 
(
	[ThemeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUserMessgeFollowup]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUserMessgeFollowup](
	[MessageFollowupID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[TransactionID] [int] NULL,
	[TransactionRootValueID] [nvarchar](200) NULL,
	[ProjectActivityID] [int] NULL,
	[ProjectTeamID] [int] NULL,
	[ProjectID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[RoleID] [int] NULL,
 CONSTRAINT [PK_AppUserMessgeFollowup] PRIMARY KEY CLUSTERED 
(
	[MessageFollowupID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUserSkill]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUserSkill](
	[UserSkillID] [int] IDENTITY(1,1) NOT NULL,
	[SkillItemID] [int] NULL,
	[UserID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppUserSkill] PRIMARY KEY CLUSTERED 
(
	[UserSkillID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUserSkillList]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUserSkillList](
	[SkillItemID] [int] IDENTITY(1,1) NOT NULL,
	[SkillName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[SkillLevel] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[SkillType] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppUserSkillList] PRIMARY KEY CLUSTERED 
(
	[SkillItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppVersionEditionModule]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppVersionEditionModule](
	[EditionModuleItemID] [int] IDENTITY(1,1) NOT NULL,
	[MenuID] [int] NULL,
	[EmApplicationVersionEdition] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppVersionEditionModule] PRIMARY KEY CLUSTERED 
(
	[EditionModuleItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppViewFiledSearchFiledMapping]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppViewFiledSearchFiledMapping](
	[MappingID] [int] IDENTITY(1,1) NOT NULL,
	[SearchViewID] [int] NULL,
	[SearchViewFiledID] [int] NULL,
	[SearchID] [int] NULL,
	[SearchFieldID] [int] NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppViewFiledSearchFiledMapping] PRIMARY KEY CLUSTERED 
(
	[MappingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppViewLinkedSeaechOrUrl]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppViewLinkedSeaechOrUrl](
	[SearchViewLinkSearchID] [int] IDENTITY(1,1) NOT NULL,
	[SearchViewID] [int] NULL,
	[LinkTargetUrlOrRouteCode] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[LayoutDisplayMode] [int] NULL,
	[SourceViewColumnID1] [int] NULL,
	[SourceViewColumnID2] [int] NULL,
	[SourceViewColumnID3] [int] NULL,
	[TargetSearchFieldID1] [int] NULL,
	[TargetSearchFieldID2] [int] NULL,
	[TargetSearchFieldID3] [int] NULL,
	[DisplayText] [nvarchar](500) NULL,
	[Sort] [int] NULL,
	[LinkTargetSearchID] [int] NULL,
	[IsPopup] [bit] NULL,
	[PopupWidth] [int] NULL,
	[PopupHeight] [int] NULL,
	[IconName] [nvarchar](500) NULL,
	[RowDisplayViewColumnID] [int] NULL,
	[SourceConditionViewColumnID] [int] NULL,
	[OtherSettings] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppViewLinkedSeaechOrUrl] PRIMARY KEY CLUSTERED 
(
	[SearchViewLinkSearchID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppWebApiConfig]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppWebApiConfig](
	[WebApiConfigID] [int] IDENTITY(1,1) NOT NULL,
	[WebApiName] [nvarchar](100) NULL,
	[Description] [nvarchar](500) NULL,
	[WebApiProviderID] [int] NULL,
	[WebApiBaseUrl] [nvarchar](500) NULL,
	[PathParameterName] [nvarchar](50) NULL,
	[WebMethod] [int] NULL,
	[WebApiFullUrlFormat] [nvarchar](1000) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppExternalWebAPIConfig] PRIMARY KEY CLUSTERED 
(
	[WebApiConfigID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppWebAPIDataExchangeSetting]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppWebAPIDataExchangeSetting](
	[ActionID] [int] IDENTITY(1,1) NOT NULL,
	[ActionCode] [nvarchar](200) NULL,
	[ActionDescription] [nvarchar](500) NULL,
	[JsonQuery] [nvarchar](max) NULL,
	[WhereClauseFormat] [nvarchar](500) NULL,
	[IsSimpleQuery] [bit] NULL,
	[JsonSampleData] [nvarchar](max) NULL,
	[JsonSchema] [nvarchar](max) NULL,
	[SchemaDataSetMapping] [nvarchar](max) NULL,
	[HttpMethd] [nvarchar](20) NULL,
	[DataSourceID] [int] NULL,
	[SchemaFromDataSetMapping] [nvarchar](max) NULL,
	[PostProcessScript] [nvarchar](max) NULL,
	[APIConfigParameters] [nvarchar](4000) NULL,
	[TablePrefix] [nvarchar](50) NULL,
 CONSTRAINT [PK_AppWebAPIDataExchangeSetting] PRIMARY KEY CLUSTERED 
(
	[ActionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppWebApiParamsHeaderSettig]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppWebApiParamsHeaderSettig](
	[ParamHeaderID] [int] IDENTITY(1,1) NOT NULL,
	[WebApiConfigID] [int] NULL,
	[KeyValueType] [int] NULL,
	[KeyName] [nvarchar](50) NULL,
	[Value] [nvarchar](50) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppExternalWebApiParamsHeaderSettig] PRIMARY KEY CLUSTERED 
(
	[ParamHeaderID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppWebApiProvider]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppWebApiProvider](
	[WebApiPorviderID] [int] IDENTITY(1,1) NOT NULL,
	[ProviderName] [nvarchar](100) NULL,
	[ApiKey] [nvarchar](200) NULL,
	[ApiSecret] [nvarchar](200) NULL,
	[AuthorizationType] [int] NULL,
	[AuthorizationTypePrefix] [nchar](100) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
 CONSTRAINT [PK_AppWebApiPorvider] PRIMARY KEY CLUSTERED 
(
	[WebApiPorviderID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppWinScheduleSetting]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppWinScheduleSetting](
	[WinScheduleSeetingID] [int] IDENTITY(1,1) NOT NULL,
	[JobCode] [nvarchar](50) NOT NULL,
	[Descprtion] [nvarchar](500) NULL,
	[SqlConnectionString] [nvarchar](1000) NULL,
	[JobScript] [nvarchar](4000) NULL,
	[SqlParamterList] [nvarchar](500) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[JobType] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[CommandID] [int] NULL,
	[TranscationID] [int] NULL,
 CONSTRAINT [PK_AppWinScheduleSetting] PRIMARY KEY CLUSTERED 
(
	[WinScheduleSeetingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppWorkflowAutomation]    Script Date: 5/12/2026 10:04:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppWorkflowAutomation](
	[WorkflowAutomationId] [int] IDENTITY(1,1) NOT NULL,
	[WorkflowTransactionId] [int] NULL,
	[Name] [nvarchar](4000) NULL,
	[Description] [nvarchar](max) NULL,
	[Notes] [nvarchar](max) NULL,
	[StartTime] [datetime] NULL,
	[EndTime] [datetime] NULL,
	[WorkflowProgressStatus] [nvarchar](2000) NULL,
	[OtherSetting] [nvarchar](max) NULL,
	[AppCreatedByID] [int] NULL,
	[AppCreatedDate] [datetime] NULL,
	[AppModifiedDate] [datetime] NULL,
	[AppModifiedByID] [int] NULL,
	[AppCompanyID] [int] NULL,
	[AppCreatedByCompanyID] [int] NULL,
	[UserDefinedField1] [nvarchar](max) NULL,
	[UserDefinedField2] [nvarchar](max) NULL,
	[UserDefinedField3] [nvarchar](max) NULL,
	[UserDefinedField4] [nvarchar](max) NULL,
	[UserDefinedField5] [nvarchar](max) NULL,
	[UserDefinedField6] [nvarchar](max) NULL,
	[UserDefinedField7] [nvarchar](max) NULL,
	[UserDefinedField8] [nvarchar](max) NULL,
 CONSTRAINT [PK_AppWorkflowAutomation] PRIMARY KEY CLUSTERED 
(
	[WorkflowAutomationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] ADD  CONSTRAINT [DF_pdmTAProject_IsPredefined]  DEFAULT ((0)) FOR [IsPredefined]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] ADD  CONSTRAINT [DF_pdmTAProject_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] ADD  CONSTRAINT [DF_AppProjectOrWorkFlow_IsAllowParentTopDownCascading]  DEFAULT ((1)) FOR [IsChildProjectAllowParentTtrickleDown]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] ADD  CONSTRAINT [DF_AppProjectOrWorkFlow_IsAllowChildBottomUpCascading]  DEFAULT ((1)) FOR [IsChildProjectAllowChildBubbleUpParent]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmProjectActivity_NbDays]  DEFAULT ((0)) FOR [NbDays]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmProjectActivity_IsAutoStart]  DEFAULT ((0)) FOR [IsAutoStart]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmProjectActivity_Sort]  DEFAULT ((0)) FOR [Sort]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmProjectActivity_IsFixedDate]  DEFAULT ((0)) FOR [IsFixedPlannedDate]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmTAProjectActivity_TimingDays]  DEFAULT ((0)) FOR [TimingDays]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmProjectActivity_IsAutoComplete]  DEFAULT ((0)) FOR [IsAutoComplete]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_pdmProjectActivity_IsMilestone]  DEFAULT ((0)) FOR [IsMilestone]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] ADD  CONSTRAINT [DF_AppProjectWorkFlowTask_IsProjectSumaryTask]  DEFAULT ((0)) FOR [IsProjectSumaryTask]
GO
ALTER TABLE [dbo].[AppRouteState] ADD  CONSTRAINT [DF_AppRouteState_NoSecurityControl]  DEFAULT ((0)) FOR [NoSecurityControl]
GO
ALTER TABLE [dbo].[AppSecurityEntityAction] ADD  CONSTRAINT [DF_AppSecurityEntityAction_NoSecurityControl]  DEFAULT ((0)) FOR [NoSecurityControl]
GO
ALTER TABLE [dbo].[AppSecurityGroup] ADD  CONSTRAINT [DF_AppSecurityGroup_GroupUsage]  DEFAULT ((1)) FOR [GroupUsage]
GO
ALTER TABLE [dbo].[AppTransaction] ADD  DEFAULT ((0)) FOR [IsSystemBuitIn]
GO
ALTER TABLE [dbo].[AppTransactionField] ADD  CONSTRAINT [DF_AppTransactionField_IsPrimaryKey]  DEFAULT ((0)) FOR [IsPrimaryKey]
GO
ALTER TABLE [dbo].[AppTransactionField] ADD  CONSTRAINT [DF_AppTransactionField_IsLinkToParentPrimaryKey]  DEFAULT ((0)) FOR [IsLinkToParentPrimaryKey]
GO
ALTER TABLE [dbo].[AppTransactionField] ADD  CONSTRAINT [DF_AppTransactionField_IsVisible]  DEFAULT ((1)) FOR [IsVisible]
GO
ALTER TABLE [dbo].[AppTransactionField] ADD  DEFAULT ((0)) FOR [IsTempVariable]
GO
ALTER TABLE [dbo].[AppTransactionUnit] ADD  CONSTRAINT [DF_AppTransactionUnit_IsSynchToDatabaseTable]  DEFAULT ((1)) FOR [IsSynchToDatabaseTable]
GO
ALTER TABLE [dbo].[AppTransactionUnit] ADD  CONSTRAINT [DF_AppTransactionUnit_IsPrimaryKeyIdentityInsert]  DEFAULT ((1)) FOR [IsPrimaryKeyIdentityInsert]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppDesktop] FOREIGN KEY([DesktopID])
REFERENCES [dbo].[AppDesktop] ([DesktopID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppDesktop]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppForm] FOREIGN KEY([FormID])
REFERENCES [dbo].[AppForm] ([FormID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppForm]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppListMenu] FOREIGN KEY([ApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppListMenu]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppProjectOrWorkFlow] FOREIGN KEY([ProjectWorkflowID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppReport] FOREIGN KEY([ReportID])
REFERENCES [dbo].[AppReport] ([ReportID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppReport]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppSearch]
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppApplicationConfigurationItem_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppApplicationAssetsItem] CHECK CONSTRAINT [FK_AppApplicationConfigurationItem_AppTransaction]
GO
ALTER TABLE [dbo].[AppCalendarRecurringDay]  WITH NOCHECK ADD  CONSTRAINT [FK_AppCalendarRecurringDay_AppCalendar] FOREIGN KEY([CalendarID])
REFERENCES [dbo].[AppCalendar] ([CalendarID])
GO
ALTER TABLE [dbo].[AppCalendarRecurringDay] CHECK CONSTRAINT [FK_AppCalendarRecurringDay_AppCalendar]
GO
ALTER TABLE [dbo].[AppCalendarSpecificDay]  WITH NOCHECK ADD  CONSTRAINT [FK_AppCalendarDay_AppCalendar] FOREIGN KEY([CalendarID])
REFERENCES [dbo].[AppCalendar] ([CalendarID])
GO
ALTER TABLE [dbo].[AppCalendarSpecificDay] CHECK CONSTRAINT [FK_AppCalendarDay_AppCalendar]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppMessage] FOREIGN KEY([NotificationTemplateMessgeID])
REFERENCES [dbo].[AppMessage] ([MessageID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppMessage]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransaction]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionField] FOREIGN KEY([BooleanConditionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionField]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionField_NeedToHideTransactionFieldID] FOREIGN KEY([NeedToHideTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionField_NeedToHideTransactionFieldID]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionField1] FOREIGN KEY([LockingTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionField1]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionField2] FOREIGN KEY([UITriggerTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionField2]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionUnit] FOREIGN KEY([LockingTransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionUnit_ConditionUnitID] FOREIGN KEY([ConditionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionUnit_ConditionUnitID]
GO
ALTER TABLE [dbo].[AppConditionalAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppConditionalAction_AppTransactionUnit1] FOREIGN KEY([LockingFieldUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppConditionalAction] CHECK CONSTRAINT [FK_AppConditionalAction_AppTransactionUnit1]
GO
ALTER TABLE [dbo].[AppCurrentUserFavouriteFolderOrFile]  WITH NOCHECK ADD  CONSTRAINT [FK_AppCurrentUserFavouriteFolderOrFile_AppFile] FOREIGN KEY([FiledID])
REFERENCES [dbo].[AppFile] ([FileID])
GO
ALTER TABLE [dbo].[AppCurrentUserFavouriteFolderOrFile] CHECK CONSTRAINT [FK_AppCurrentUserFavouriteFolderOrFile_AppFile]
GO
ALTER TABLE [dbo].[AppCurrentUserFavouriteFolderOrFile]  WITH NOCHECK ADD  CONSTRAINT [FK_AppCurrentUserFavouriteFolderOrFile_AppSEFolder] FOREIGN KEY([FolderID])
REFERENCES [dbo].[AppSEFolder] ([FolderID])
GO
ALTER TABLE [dbo].[AppCurrentUserFavouriteFolderOrFile] CHECK CONSTRAINT [FK_AppCurrentUserFavouriteFolderOrFile_AppSEFolder]
GO
ALTER TABLE [dbo].[AppDataSet]  WITH NOCHECK ADD  CONSTRAINT [FK_AppDataSet_AppDataSet] FOREIGN KEY([BaseDataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
GO
ALTER TABLE [dbo].[AppDataSet] CHECK CONSTRAINT [FK_AppDataSet_AppDataSet]
GO
ALTER TABLE [dbo].[AppDataSet]  WITH CHECK ADD  CONSTRAINT [FK_AppDataSet_AppExternalWebAPIConfig] FOREIGN KEY([WebApiConfigID])
REFERENCES [dbo].[AppWebApiConfig] ([WebApiConfigID])
GO
ALTER TABLE [dbo].[AppDataSet] CHECK CONSTRAINT [FK_AppDataSet_AppExternalWebAPIConfig]
GO
ALTER TABLE [dbo].[AppDataSet]  WITH NOCHECK ADD  CONSTRAINT [FK_AppDataSet_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppDataSet] CHECK CONSTRAINT [FK_AppDataSet_AppListMenu]
GO
ALTER TABLE [dbo].[AppDataSetParameter]  WITH NOCHECK ADD  CONSTRAINT [FK_AppDataSetParameter_AppDataSet] FOREIGN KEY([DataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
GO
ALTER TABLE [dbo].[AppDataSetParameter] CHECK CONSTRAINT [FK_AppDataSetParameter_AppDataSet]
GO
ALTER TABLE [dbo].[AppDateSetDataExtractView]  WITH NOCHECK ADD  CONSTRAINT [FK_AppDateSetDataExtractView_AppDataSet] FOREIGN KEY([DataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
GO
ALTER TABLE [dbo].[AppDateSetDataExtractView] CHECK CONSTRAINT [FK_AppDateSetDataExtractView_AppDataSet]
GO
ALTER TABLE [dbo].[AppDesktop]  WITH NOCHECK ADD  CONSTRAINT [FK_AppDesktop_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppDesktop] CHECK CONSTRAINT [FK_AppDesktop_AppListMenu]
GO
ALTER TABLE [dbo].[AppDesktopItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppDesktopItem_AppDesktop] FOREIGN KEY([DesktopID])
REFERENCES [dbo].[AppDesktop] ([DesktopID])
GO
ALTER TABLE [dbo].[AppDesktopItem] CHECK CONSTRAINT [FK_AppDesktopItem_AppDesktop]
GO
ALTER TABLE [dbo].[AppEntityEnumValue]  WITH NOCHECK ADD  CONSTRAINT [FK_AppEntityEnumValue_AppEntityInfo] FOREIGN KEY([EntityInfoID])
REFERENCES [dbo].[AppEntityInfo] ([EntityInfoID])
GO
ALTER TABLE [dbo].[AppEntityEnumValue] CHECK CONSTRAINT [FK_AppEntityEnumValue_AppEntityInfo]
GO
ALTER TABLE [dbo].[AppEntityInfo]  WITH NOCHECK ADD  CONSTRAINT [FK_AppEntityInfo_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppEntityInfo] CHECK CONSTRAINT [FK_AppEntityInfo_AppListMenu]
GO
ALTER TABLE [dbo].[AppEntitySimpleListValue]  WITH NOCHECK ADD  CONSTRAINT [FK_AppEntitySimpleListValue_AppEntityInfo] FOREIGN KEY([EntityInfoID])
REFERENCES [dbo].[AppEntityInfo] ([EntityInfoID])
GO
ALTER TABLE [dbo].[AppEntitySimpleListValue] CHECK CONSTRAINT [FK_AppEntitySimpleListValue_AppEntityInfo]
GO
ALTER TABLE [dbo].[AppEsite]  WITH NOCHECK ADD  CONSTRAINT [FK_AppEsite_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppEsite] CHECK CONSTRAINT [FK_AppEsite_AppListMenu]
GO
ALTER TABLE [dbo].[AppEsiteCatalogue]  WITH CHECK ADD  CONSTRAINT [FK_AppESiteCatalogue_AppESite] FOREIGN KEY([EsiteID])
REFERENCES [dbo].[AppEsite] ([EsiteID])
GO
ALTER TABLE [dbo].[AppEsiteCatalogue] CHECK CONSTRAINT [FK_AppESiteCatalogue_AppESite]
GO
ALTER TABLE [dbo].[AppEsiteCatalogue]  WITH CHECK ADD  CONSTRAINT [FK_AppESiteCatalogue_AppSearchView] FOREIGN KEY([TreeNavigationViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppEsiteCatalogue] CHECK CONSTRAINT [FK_AppESiteCatalogue_AppSearchView]
GO
ALTER TABLE [dbo].[AppEsiteCatalogue]  WITH CHECK ADD  CONSTRAINT [FK_AppESiteCatalogue_AppSearchView1] FOREIGN KEY([CatalogCardViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppEsiteCatalogue] CHECK CONSTRAINT [FK_AppESiteCatalogue_AppSearchView1]
GO
ALTER TABLE [dbo].[AppEsiteCatalogue]  WITH CHECK ADD  CONSTRAINT [FK_AppESiteCatalogue_AppSearchView2] FOREIGN KEY([CatalogCardDetailID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppEsiteCatalogue] CHECK CONSTRAINT [FK_AppESiteCatalogue_AppSearchView2]
GO
ALTER TABLE [dbo].[AppESiteNavMenu]  WITH CHECK ADD  CONSTRAINT [FK_AppESiteNavMenu_AppEsite] FOREIGN KEY([EsiteID])
REFERENCES [dbo].[AppEsite] ([EsiteID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppESiteNavMenu] CHECK CONSTRAINT [FK_AppESiteNavMenu_AppEsite]
GO
ALTER TABLE [dbo].[AppESitePages]  WITH CHECK ADD  CONSTRAINT [FK_AppESitePages_AppEsite] FOREIGN KEY([EsiteID])
REFERENCES [dbo].[AppEsite] ([EsiteID])
GO
ALTER TABLE [dbo].[AppESitePages] CHECK CONSTRAINT [FK_AppESitePages_AppEsite]
GO
ALTER TABLE [dbo].[AppESitePages]  WITH CHECK ADD  CONSTRAINT [FK_AppESitePages_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppESitePages] CHECK CONSTRAINT [FK_AppESitePages_AppTransaction]
GO
ALTER TABLE [dbo].[AppEStore]  WITH CHECK ADD  CONSTRAINT [FK_AppEStore_AppSearchView] FOREIGN KEY([TreeNavigationViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppEStore] CHECK CONSTRAINT [FK_AppEStore_AppSearchView]
GO
ALTER TABLE [dbo].[AppEStore]  WITH CHECK ADD  CONSTRAINT [FK_AppEStore_AppSearchView1] FOREIGN KEY([CatalogCardViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppEStore] CHECK CONSTRAINT [FK_AppEStore_AppSearchView1]
GO
ALTER TABLE [dbo].[AppEStore]  WITH CHECK ADD  CONSTRAINT [FK_AppEStore_AppSearchView2] FOREIGN KEY([CatalogCardDetailID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppEStore] CHECK CONSTRAINT [FK_AppEStore_AppSearchView2]
GO
ALTER TABLE [dbo].[AppFile]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFile_AppSEFolder] FOREIGN KEY([FolderID])
REFERENCES [dbo].[AppSEFolder] ([FolderID])
GO
ALTER TABLE [dbo].[AppFile] CHECK CONSTRAINT [FK_AppFile_AppSEFolder]
GO
ALTER TABLE [dbo].[AppFileOrFolderShareToOther]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFileOrFolderShareToOther_AppFile] FOREIGN KEY([FileID])
REFERENCES [dbo].[AppFile] ([FileID])
GO
ALTER TABLE [dbo].[AppFileOrFolderShareToOther] CHECK CONSTRAINT [FK_AppFileOrFolderShareToOther_AppFile]
GO
ALTER TABLE [dbo].[AppFileOrFolderShareToOther]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFileOrFolderShareToOther_AppSecurityGroup] FOREIGN KEY([ShareToOtherRoleID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppFileOrFolderShareToOther] CHECK CONSTRAINT [FK_AppFileOrFolderShareToOther_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppFileOrFolderShareToOther]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFileOrFolderShareToOther_AppSEFolder] FOREIGN KEY([FolderID])
REFERENCES [dbo].[AppSEFolder] ([FolderID])
GO
ALTER TABLE [dbo].[AppFileOrFolderShareToOther] CHECK CONSTRAINT [FK_AppFileOrFolderShareToOther_AppSEFolder]
GO
ALTER TABLE [dbo].[AppFileTypeView]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFileTypeView_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppFileTypeView] CHECK CONSTRAINT [FK_AppFileTypeView_AppSearchView]
GO
ALTER TABLE [dbo].[AppForm]  WITH NOCHECK ADD  CONSTRAINT [FK_AppForm_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppForm] CHECK CONSTRAINT [FK_AppForm_AppListMenu]
GO
ALTER TABLE [dbo].[AppForm]  WITH NOCHECK ADD  CONSTRAINT [FK_AppForm_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppForm] CHECK CONSTRAINT [FK_AppForm_AppSearchView]
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppFormLayoutItem] FOREIGN KEY([FormLayoutID])
REFERENCES [dbo].[AppFormLayoutItem] ([FormLayoutItemID])
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField] CHECK CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppFormLayoutItem]
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppTransactionField] FOREIGN KEY([TransactionField])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField] CHECK CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppTransactionField]
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppTransactionUnit] FOREIGN KEY([ChildTranscationUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField] CHECK CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppTransactionUnit1] FOREIGN KEY([GrandChildTranscationUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppFormGridLayoutItemBindField] CHECK CONSTRAINT [FK_AppFormGridLayoutItemBindField_AppTransactionUnit1]
GO
ALTER TABLE [dbo].[AppFormGroupItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormGroupItem_AppForm] FOREIGN KEY([FormID])
REFERENCES [dbo].[AppForm] ([FormID])
GO
ALTER TABLE [dbo].[AppFormGroupItem] CHECK CONSTRAINT [FK_AppFormGroupItem_AppForm]
GO
ALTER TABLE [dbo].[AppFormGroupItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransFormGroupItem_AppTransFormGroup] FOREIGN KEY([FromGroupID])
REFERENCES [dbo].[AppFormGroup] ([FormGroupID])
GO
ALTER TABLE [dbo].[AppFormGroupItem] CHECK CONSTRAINT [FK_AppTransFormGroupItem_AppTransFormGroup]
GO
ALTER TABLE [dbo].[AppFormLayoutItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLayoutItem_AppForm] FOREIGN KEY([FormID])
REFERENCES [dbo].[AppForm] ([FormID])
GO
ALTER TABLE [dbo].[AppFormLayoutItem] CHECK CONSTRAINT [FK_AppFormLayoutItem_AppForm]
GO
ALTER TABLE [dbo].[AppFormLayoutItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLayoutItem_AppFormLayoutItem] FOREIGN KEY([UIGridLayoutParentID])
REFERENCES [dbo].[AppFormLayoutItem] ([FormLayoutItemID])
GO
ALTER TABLE [dbo].[AppFormLayoutItem] CHECK CONSTRAINT [FK_AppFormLayoutItem_AppFormLayoutItem]
GO
ALTER TABLE [dbo].[AppFormLayoutItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLayoutItem_AppSearchViewField] FOREIGN KEY([SearchViewFieldID])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppFormLayoutItem] CHECK CONSTRAINT [FK_AppFormLayoutItem_AppSearchViewField]
GO
ALTER TABLE [dbo].[AppFormLayoutItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLayoutItem_AppTransactionField] FOREIGN KEY([TransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppFormLayoutItem] CHECK CONSTRAINT [FK_AppFormLayoutItem_AppTransactionField]
GO
ALTER TABLE [dbo].[AppFormLayoutItem]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLayoutItem_AppTransactionUnit] FOREIGN KEY([GridTransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppFormLayoutItem] CHECK CONSTRAINT [FK_AppFormLayoutItem_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearch] FOREIGN KEY([LinkTargetSearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearch]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchField] FOREIGN KEY([TargetSearchFieldID1])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchField]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchField1] FOREIGN KEY([TargetSearchFieldID2])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchField1]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchField2] FOREIGN KEY([TargetSearchFieldID3])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchField2]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchView]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField] FOREIGN KEY([SourceViewColumnID1])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField1] FOREIGN KEY([SourceViewColumnID2])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField1]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField2] FOREIGN KEY([SourceViewColumnID3])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField2]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField3] FOREIGN KEY([RowDisplayViewColumnID])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField3]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField4] FOREIGN KEY([SourceConditionViewColumnID])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppSearchViewField4]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppTransaction] FOREIGN KEY([LinkTargetTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppTransaction]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH CHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppTransactionDataTransferSetting] FOREIGN KEY([DataTransferSettingID])
REFERENCES [dbo].[AppTransactionDataTransferSetting] ([DataTransferSettingID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppTransactionDataTransferSetting]
GO
ALTER TABLE [dbo].[AppFormLinkTarget]  WITH NOCHECK ADD  CONSTRAINT [FK_AppFormLinkTarget_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppFormLinkTarget] CHECK CONSTRAINT [FK_AppFormLinkTarget_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppIntergrationSettingParameter]  WITH CHECK ADD  CONSTRAINT [FK_AppIntergrationSettingParameter_AppIntergrationSetting] FOREIGN KEY([IntergrationSettingID])
REFERENCES [dbo].[AppIntergrationSetting] ([IntergrationSettingID])
GO
ALTER TABLE [dbo].[AppIntergrationSettingParameter] CHECK CONSTRAINT [FK_AppIntergrationSettingParameter_AppIntergrationSetting]
GO
ALTER TABLE [dbo].[AppListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppListMenu_AppDesktop] FOREIGN KEY([DesktopID])
REFERENCES [dbo].[AppDesktop] ([DesktopID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppListMenu] CHECK CONSTRAINT [FK_AppListMenu_AppDesktop]
GO
ALTER TABLE [dbo].[AppListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppListMenu_AppListMenu] FOREIGN KEY([ParentID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
GO
ALTER TABLE [dbo].[AppListMenu] CHECK CONSTRAINT [FK_AppListMenu_AppListMenu]
GO
ALTER TABLE [dbo].[AppListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppListMenu_AppProjectOrWorkFlow] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppListMenu] CHECK CONSTRAINT [FK_AppListMenu_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppListMenu_AppReport] FOREIGN KEY([ReportID])
REFERENCES [dbo].[AppReport] ([ReportID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppListMenu] CHECK CONSTRAINT [FK_AppListMenu_AppReport]
GO
ALTER TABLE [dbo].[AppListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppListMenu_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppListMenu] CHECK CONSTRAINT [FK_AppListMenu_AppSearch]
GO
ALTER TABLE [dbo].[AppListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppListMenu_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppListMenu] CHECK CONSTRAINT [FK_AppListMenu_AppTransaction]
GO
ALTER TABLE [dbo].[AppMessage]  WITH NOCHECK ADD  CONSTRAINT [FK_AppMessage_AppProjectOrWorkFlow] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppMessage] CHECK CONSTRAINT [FK_AppMessage_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppMessage]  WITH NOCHECK ADD  CONSTRAINT [FK_AppMessage_AppProjectWorkFlowTask] FOREIGN KEY([ProjectActivityID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppMessage] CHECK CONSTRAINT [FK_AppMessage_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppMessage]  WITH NOCHECK ADD  CONSTRAINT [FK_AppMessage_AppSecurityGroup] FOREIGN KEY([ProjectTeamID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppMessage] CHECK CONSTRAINT [FK_AppMessage_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppMessageDeleted]  WITH NOCHECK ADD  CONSTRAINT [FK_AppMessageDeleted_AppMessage] FOREIGN KEY([MessageID])
REFERENCES [dbo].[AppMessage] ([MessageID])
GO
ALTER TABLE [dbo].[AppMessageDeleted] CHECK CONSTRAINT [FK_AppMessageDeleted_AppMessage]
GO
ALTER TABLE [dbo].[APPMessageNotificationSetting]  WITH NOCHECK ADD  CONSTRAINT [FK_APPMessageNotificationSetting_APPMessageNotificationSetting] FOREIGN KEY([NotificationQueryContentSettingID])
REFERENCES [dbo].[APPMessageNotificationSetting] ([NotificationSettingID])
GO
ALTER TABLE [dbo].[APPMessageNotificationSetting] CHECK CONSTRAINT [FK_APPMessageNotificationSetting_APPMessageNotificationSetting]
GO
ALTER TABLE [dbo].[APPMessageNotificationSetting]  WITH NOCHECK ADD  CONSTRAINT [FK_APPMessageNotificationSetting_AppTransaction] FOREIGN KEY([TranscationID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[APPMessageNotificationSetting] CHECK CONSTRAINT [FK_APPMessageNotificationSetting_AppTransaction]
GO
ALTER TABLE [dbo].[AppMessageUserReceived]  WITH NOCHECK ADD  CONSTRAINT [FK_AppMessageUserReceived_AppMessage] FOREIGN KEY([MessageID])
REFERENCES [dbo].[AppMessage] ([MessageID])
GO
ALTER TABLE [dbo].[AppMessageUserReceived] CHECK CONSTRAINT [FK_AppMessageUserReceived_AppMessage]
GO
ALTER TABLE [dbo].[AppPorjectWorkFlowTaskTimeSheet]  WITH NOCHECK ADD  CONSTRAINT [FK_AppPorjectWorkFlowTaskTimeSheet_AppProjectWorkFlowTask] FOREIGN KEY([ProjectWorkFlowTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppPorjectWorkFlowTaskTimeSheet] CHECK CONSTRAINT [FK_AppPorjectWorkFlowTaskTimeSheet_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectOrTaskTranscation]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrTaskTranscation_AppProjectOrWorkFlow] FOREIGN KEY([ProejctID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectOrTaskTranscation] CHECK CONSTRAINT [FK_AppProjectOrTaskTranscation_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectOrTaskTranscation]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrTaskTranscation_AppProjectWorkFlowTask] FOREIGN KEY([ProjectTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectOrTaskTranscation] CHECK CONSTRAINT [FK_AppProjectOrTaskTranscation_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectOrTaskTranscation]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrTaskTranscation_AppTransaction] FOREIGN KEY([TranscationID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppProjectOrTaskTranscation] CHECK CONSTRAINT [FK_AppProjectOrTaskTranscation_AppTransaction]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppListMenu]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectOrWorkFlow] FOREIGN KEY([ParentProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectOrWorkFlow1] FOREIGN KEY([RuntimeOriginalProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectOrWorkFlow1]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectTeam] FOREIGN KEY([ProjectLeaderID])
REFERENCES [dbo].[AppProjectTeam] ([ProejctTeamID])
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectTeam]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectWorkFlowTask_ProjectSumaryTaskID] FOREIGN KEY([ProjectSumaryTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppProjectWorkFlowTask_ProjectSumaryTaskID]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppSecurityGroup] FOREIGN KEY([ProjectTeamID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectOrWorkFlow_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppProjectOrWorkFlow] CHECK CONSTRAINT [FK_AppProjectOrWorkFlow_AppTransaction]
GO
ALTER TABLE [dbo].[AppProjectPerspectiveTask]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectPerspectiveTask_AppProjectPerspectiveView] FOREIGN KEY([PerspectiveSectionID])
REFERENCES [dbo].[AppProjectPerspectiveView] ([PerspectiveSectionID])
GO
ALTER TABLE [dbo].[AppProjectPerspectiveTask] CHECK CONSTRAINT [FK_AppProjectPerspectiveTask_AppProjectPerspectiveView]
GO
ALTER TABLE [dbo].[AppProjectPerspectiveTask]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectPerspectiveTask_AppProjectWorkFlowTask] FOREIGN KEY([ProjectWorkFlowTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectPerspectiveTask] CHECK CONSTRAINT [FK_AppProjectPerspectiveTask_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectPortfolioBoard]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectPortfolioBoard_AppProjectOrWorkFlow] FOREIGN KEY([SummaryProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectPortfolioBoard] CHECK CONSTRAINT [FK_AppProjectPortfolioBoard_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectPortfolioBoard]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectPortfolioBoard_AppProjectPortfolio] FOREIGN KEY([PortfolioID])
REFERENCES [dbo].[AppProjectPortfolio] ([PortfolioID])
GO
ALTER TABLE [dbo].[AppProjectPortfolioBoard] CHECK CONSTRAINT [FK_AppProjectPortfolioBoard_AppProjectPortfolio]
GO
ALTER TABLE [dbo].[AppProjectPrivacy]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectPrivacy_AppProjectOrWorkFlow] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectPrivacy] CHECK CONSTRAINT [FK_AppProjectPrivacy_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectRolePrivilege]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectRolePrivilege_AppProjectPrivilegeLibrary] FOREIGN KEY([ProjectPrivilegeID])
REFERENCES [dbo].[AppProjectPrivilegeLibrary] ([ProjectPrivilegeID])
GO
ALTER TABLE [dbo].[AppProjectRolePrivilege] CHECK CONSTRAINT [FK_AppProjectRolePrivilege_AppProjectPrivilegeLibrary]
GO
ALTER TABLE [dbo].[AppProjectRolePrivilege]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectRolePrivilege_AppProjectRole] FOREIGN KEY([ProjectRoleID])
REFERENCES [dbo].[AppProjectRole] ([ProjectRoleID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppProjectRolePrivilege] CHECK CONSTRAINT [FK_AppProjectRolePrivilege_AppProjectRole]
GO
ALTER TABLE [dbo].[AppProjectSnapshot]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectSnapshot_AppProjectOrWorkFlow] FOREIGN KEY([ManProejctID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectSnapshot] CHECK CONSTRAINT [FK_AppProjectSnapshot_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectSnapshot]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectSnapshot_AppProjectOrWorkFlow1] FOREIGN KEY([CopyProejctID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectSnapshot] CHECK CONSTRAINT [FK_AppProjectSnapshot_AppProjectOrWorkFlow1]
GO
ALTER TABLE [dbo].[AppProjectTaskCheckList]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskCheckList_AppProjectWorkFlowTask] FOREIGN KEY([ProjectTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskCheckList] CHECK CONSTRAINT [FK_AppProjectTaskCheckList_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectTaskExpense]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskExpense_AppProjectWorkFlowTask] FOREIGN KEY([ProjectTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskExpense] CHECK CONSTRAINT [FK_AppProjectTaskExpense_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectTaskPredecessor]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskPredecessor_AppProjectWorkFlowTask] FOREIGN KEY([ProjectWorkFlowTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskPredecessor] CHECK CONSTRAINT [FK_AppProjectTaskPredecessor_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectTaskPredecessor]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskPredecessor_AppProjectWorkFlowTask1] FOREIGN KEY([PredecessorID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskPredecessor] CHECK CONSTRAINT [FK_AppProjectTaskPredecessor_AppProjectWorkFlowTask1]
GO
ALTER TABLE [dbo].[AppProjectTaskResource]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskResource_AppProjectWorkFlowTask] FOREIGN KEY([ProjectWorkFlowTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskResource] CHECK CONSTRAINT [FK_AppProjectTaskResource_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectTaskResourcePlannedHours]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskResourcePlannedHours_AppProjectTaskResource] FOREIGN KEY([TaskResourceID])
REFERENCES [dbo].[AppProjectTaskResource] ([TaskResourceID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppProjectTaskResourcePlannedHours] CHECK CONSTRAINT [FK_AppProjectTaskResourcePlannedHours_AppProjectTaskResource]
GO
ALTER TABLE [dbo].[AppProjectTaskTag]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskTag_AppBusinessScopeTag] FOREIGN KEY([ScopeTagID])
REFERENCES [dbo].[AppBusinessScopeTag] ([ScopeTagID])
GO
ALTER TABLE [dbo].[AppProjectTaskTag] CHECK CONSTRAINT [FK_AppProjectTaskTag_AppBusinessScopeTag]
GO
ALTER TABLE [dbo].[AppProjectTaskTag]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskTag_AppProjectWorkFlowTask] FOREIGN KEY([ProjectWorkFlowTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskTag] CHECK CONSTRAINT [FK_AppProjectTaskTag_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectTaskTimeLog]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTaskTimeLog_AppProjectWorkFlowTask] FOREIGN KEY([ProjectTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectTaskTimeLog] CHECK CONSTRAINT [FK_AppProjectTaskTimeLog_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectTeamMember]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTeamMember_AppProjectOrWorkFlow] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectTeamMember] CHECK CONSTRAINT [FK_AppProjectTeamMember_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectTeamMember]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTeamMember_AppProjectTeam] FOREIGN KEY([ProjectTeamID])
REFERENCES [dbo].[AppProjectTeam] ([ProejctTeamID])
GO
ALTER TABLE [dbo].[AppProjectTeamMember] CHECK CONSTRAINT [FK_AppProjectTeamMember_AppProjectTeam]
GO
ALTER TABLE [dbo].[AppProjectTeamMemberRole]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTeamMemberRole_AppProjectRole] FOREIGN KEY([ProjectRoleID])
REFERENCES [dbo].[AppProjectRole] ([ProjectRoleID])
GO
ALTER TABLE [dbo].[AppProjectTeamMemberRole] CHECK CONSTRAINT [FK_AppProjectTeamMemberRole_AppProjectRole]
GO
ALTER TABLE [dbo].[AppProjectTeamMemberRole]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTeamMemberRole_AppProjectTeamMember] FOREIGN KEY([TeamMemberID])
REFERENCES [dbo].[AppProjectTeamMember] ([TeamMemberID])
GO
ALTER TABLE [dbo].[AppProjectTeamMemberRole] CHECK CONSTRAINT [FK_AppProjectTeamMemberRole_AppProjectTeamMember]
GO
ALTER TABLE [dbo].[AppProjectTemplateResource]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectTemplateResource_AppProjectOrWorkFlow] FOREIGN KEY([ProejctID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectTemplateResource] CHECK CONSTRAINT [FK_AppProjectTemplateResource_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppDataSet] FOREIGN KEY([MessageContentQueryDataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppDataSet]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppMessage] FOREIGN KEY([MessageTemplateID])
REFERENCES [dbo].[AppMessage] ([MessageID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppMessage]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppProjectOrWorkFlow] FOREIGN KEY([NextProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppProjectWorkFlowCondition] FOREIGN KEY([WorkFlowConditionID])
REFERENCES [dbo].[AppProjectWorkFlowCondition] ([WorkFlowConditionID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppProjectWorkFlowCondition]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppProjectWorkFlowTask] FOREIGN KEY([NextWorkFlowID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransaction]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransaction_NextTransactionID] FOREIGN KEY([NextTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransaction_NextTransactionID]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionDataLoad] FOREIGN KEY([DataLoadID])
REFERENCES [dbo].[AppTransactionDataLoad] ([DataLoadID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionDataLoad]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionDataTransferSetting] FOREIGN KEY([DataTransferSettingID])
REFERENCES [dbo].[AppTransactionDataTransferSetting] ([DataTransferSettingID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionDataTransferSetting]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionField] FOREIGN KEY([UpdateActionTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionField]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionField_CommandConditionTransactionFieldID] FOREIGN KEY([CommandConditionTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionField_CommandConditionTransactionFieldID]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionField_TransactionFieldID] FOREIGN KEY([TransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_AppTransactionField_TransactionFieldID]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_CommandSearchViewID] FOREIGN KEY([CommandSearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_CommandSearchViewID]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowAction_CommandTransactionID] FOREIGN KEY([CommandTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowAction] CHECK CONSTRAINT [FK_AppProjectWorkFlowAction_CommandTransactionID]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowCondition_AppProjectOrWorkFlow] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition] CHECK CONSTRAINT [FK_AppProjectWorkFlowCondition_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowCondition_AppProjectWorkFlowTask] FOREIGN KEY([ProjectWorkFlowTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition] CHECK CONSTRAINT [FK_AppProjectWorkFlowCondition_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowCondition_AppTransactionField] FOREIGN KEY([ConditionTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition] CHECK CONSTRAINT [FK_AppProjectWorkFlowCondition_AppTransactionField]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowCondition_AppTransactionUnit] FOREIGN KEY([MonitorChildUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowCondition] CHECK CONSTRAINT [FK_AppProjectWorkFlowCondition_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowTask_AppProjectOrWorkFlow] FOREIGN KEY([ProjectID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] CHECK CONSTRAINT [FK_AppProjectWorkFlowTask_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowTask_AppProjectWorkFlowTask] FOREIGN KEY([MainTaskID])
REFERENCES [dbo].[AppProjectWorkFlowTask] ([ProjectWorkFlowTaskID])
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] CHECK CONSTRAINT [FK_AppProjectWorkFlowTask_AppProjectWorkFlowTask]
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask]  WITH NOCHECK ADD  CONSTRAINT [FK_AppProjectWorkFlowTask_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppProjectWorkFlowTask] CHECK CONSTRAINT [FK_AppProjectWorkFlowTask_AppTransaction]
GO
ALTER TABLE [dbo].[AppReport]  WITH NOCHECK ADD  CONSTRAINT [FK_AppReport_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppReport] CHECK CONSTRAINT [FK_AppReport_AppListMenu]
GO
ALTER TABLE [dbo].[AppSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearch_AppBusinessMgtScope] FOREIGN KEY([BusinessScopeID])
REFERENCES [dbo].[AppBusinessMgtScope] ([BusinessScopeID])
GO
ALTER TABLE [dbo].[AppSearch] CHECK CONSTRAINT [FK_AppSearch_AppBusinessMgtScope]
GO
ALTER TABLE [dbo].[AppSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearch_AppDataSet] FOREIGN KEY([DataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
GO
ALTER TABLE [dbo].[AppSearch] CHECK CONSTRAINT [FK_AppSearch_AppDataSet]
GO
ALTER TABLE [dbo].[AppSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearch_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppSearch] CHECK CONSTRAINT [FK_AppSearch_AppListMenu]
GO
ALTER TABLE [dbo].[AppSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearch_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppSearch] CHECK CONSTRAINT [FK_AppSearch_AppSearchView]
GO
ALTER TABLE [dbo].[AppSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearch_AppTransaction] FOREIGN KEY([FolderTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppSearch] CHECK CONSTRAINT [FK_AppSearch_AppTransaction]
GO
ALTER TABLE [dbo].[AppSearchField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchField_AppEntityInfo] FOREIGN KEY([EntityID])
REFERENCES [dbo].[AppEntityInfo] ([EntityInfoID])
GO
ALTER TABLE [dbo].[AppSearchField] CHECK CONSTRAINT [FK_AppSearchField_AppEntityInfo]
GO
ALTER TABLE [dbo].[AppSearchField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchFielD_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppSearchField] CHECK CONSTRAINT [FK_AppSearchFielD_AppSearch]
GO
ALTER TABLE [dbo].[AppSearchParameter]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchParameter_AppDataSetParameter] FOREIGN KEY([ParameterID])
REFERENCES [dbo].[AppDataSetParameter] ([ParameterID])
GO
ALTER TABLE [dbo].[AppSearchParameter] CHECK CONSTRAINT [FK_AppSearchParameter_AppDataSetParameter]
GO
ALTER TABLE [dbo].[AppSearchParameter]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchParameter_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppSearchParameter] CHECK CONSTRAINT [FK_AppSearchParameter_AppSearch]
GO
ALTER TABLE [dbo].[AppSearchSaved]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchSaved_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppSearchSaved] CHECK CONSTRAINT [FK_AppSearchSaved_AppSearch]
GO
ALTER TABLE [dbo].[AppSearchSaved]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchSaved_AppSearchView] FOREIGN KEY([DefaultSearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppSearchSaved] CHECK CONSTRAINT [FK_AppSearchSaved_AppSearchView]
GO
ALTER TABLE [dbo].[AppSearchSaved]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchSaved_AppSecurityGroup] FOREIGN KEY([GroupID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppSearchSaved] CHECK CONSTRAINT [FK_AppSearchSaved_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppSearchSavedValue]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchSavedValue_AppSearchFielD] FOREIGN KEY([SearchFieldID])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppSearchSavedValue] CHECK CONSTRAINT [FK_AppSearchSavedValue_AppSearchFielD]
GO
ALTER TABLE [dbo].[AppSearchSavedValue]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchSavedValue_AppSearchSaved] FOREIGN KEY([SearchSavedID])
REFERENCES [dbo].[AppSearchSaved] ([SearchSavedID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppSearchSavedValue] CHECK CONSTRAINT [FK_AppSearchSavedValue_AppSearchSaved]
GO
ALTER TABLE [dbo].[AppSearchView]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchView_AppDataSet] FOREIGN KEY([DataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
GO
ALTER TABLE [dbo].[AppSearchView] CHECK CONSTRAINT [FK_AppSearchView_AppDataSet]
GO
ALTER TABLE [dbo].[AppSearchView]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchView_AppSearch] FOREIGN KEY([CatalogueSearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppSearchView] CHECK CONSTRAINT [FK_AppSearchView_AppSearch]
GO
ALTER TABLE [dbo].[AppSearchView]  WITH CHECK ADD  CONSTRAINT [FK_AppSearchView_AppSearchView] FOREIGN KEY([HierachyParentViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppSearchView] CHECK CONSTRAINT [FK_AppSearchView_AppSearchView]
GO
ALTER TABLE [dbo].[AppSearchView]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchView_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppSearchView] CHECK CONSTRAINT [FK_AppSearchView_AppTransaction]
GO
ALTER TABLE [dbo].[AppSearchView]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchView_AppTransactionUnit] FOREIGN KEY([ProductDetaiViewMapUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppSearchView] CHECK CONSTRAINT [FK_AppSearchView_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppSearchView]  WITH CHECK ADD  CONSTRAINT [FK_AppSearchView_FilterSearch] FOREIGN KEY([FilterSearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppSearchView] CHECK CONSTRAINT [FK_AppSearchView_FilterSearch]
GO
ALTER TABLE [dbo].[AppSearchViewField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewField_AppEntityInfo] FOREIGN KEY([EntityID])
REFERENCES [dbo].[AppEntityInfo] ([EntityInfoID])
GO
ALTER TABLE [dbo].[AppSearchViewField] CHECK CONSTRAINT [FK_AppSearchViewField_AppEntityInfo]
GO
ALTER TABLE [dbo].[AppSearchViewField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewField_AppSearchField] FOREIGN KEY([MappingSearchFieldID])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppSearchViewField] CHECK CONSTRAINT [FK_AppSearchViewField_AppSearchField]
GO
ALTER TABLE [dbo].[AppSearchViewField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewField_AppSearchField1] FOREIGN KEY([PullCriteriaAsDefaultValueSearchFieldID])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppSearchViewField] CHECK CONSTRAINT [FK_AppSearchViewField_AppSearchField1]
GO
ALTER TABLE [dbo].[AppSearchViewField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewField_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppSearchViewField] CHECK CONSTRAINT [FK_AppSearchViewField_AppSearchView]
GO
ALTER TABLE [dbo].[AppSearchViewField]  WITH CHECK ADD  CONSTRAINT [FK_AppSearchViewField_AppSearchViewField] FOREIGN KEY([JoinToParentViewFieldID])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppSearchViewField] CHECK CONSTRAINT [FK_AppSearchViewField_AppSearchViewField]
GO
ALTER TABLE [dbo].[AppSearchViewField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewField_AppTransactionField1] FOREIGN KEY([MassUpdateTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppSearchViewField] CHECK CONSTRAINT [FK_AppSearchViewField_AppTransactionField1]
GO
ALTER TABLE [dbo].[AppSearchViewReport]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewReport_AppReport] FOREIGN KEY([ReportID])
REFERENCES [dbo].[AppReport] ([ReportID])
GO
ALTER TABLE [dbo].[AppSearchViewReport] CHECK CONSTRAINT [FK_AppSearchViewReport_AppReport]
GO
ALTER TABLE [dbo].[AppSearchViewReport]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewReport_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppSearchViewReport] CHECK CONSTRAINT [FK_AppSearchViewReport_AppSearchView]
GO
ALTER TABLE [dbo].[AppSearchViewReportParamterMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewReportParamterMapping_AppSearchViewField] FOREIGN KEY([SearchViewFieldID])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppSearchViewReportParamterMapping] CHECK CONSTRAINT [FK_AppSearchViewReportParamterMapping_AppSearchViewField]
GO
ALTER TABLE [dbo].[AppSearchViewReportParamterMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSearchViewReportParamterMapping_AppSearchViewReport] FOREIGN KEY([SearchViewReportID])
REFERENCES [dbo].[AppSearchViewReport] ([SearchViewReportID])
GO
ALTER TABLE [dbo].[AppSearchViewReportParamterMapping] CHECK CONSTRAINT [FK_AppSearchViewReportParamterMapping_AppSearchViewReport]
GO
ALTER TABLE [dbo].[AppSecurityEntityAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityEntityAction_AppRouteState] FOREIGN KEY([RouteStateID])
REFERENCES [dbo].[AppRouteState] ([RouteStateID])
GO
ALTER TABLE [dbo].[AppSecurityEntityAction] CHECK CONSTRAINT [FK_AppSecurityEntityAction_AppRouteState]
GO
ALTER TABLE [dbo].[AppSecurityGroup]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityGroup_AppComOrganization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[AppComOrganization] ([OrganizationID])
GO
ALTER TABLE [dbo].[AppSecurityGroup] CHECK CONSTRAINT [FK_AppSecurityGroup_AppComOrganization]
GO
ALTER TABLE [dbo].[AppSecurityGroupMember]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityGroupMember_AppSecurityGroup] FOREIGN KEY([GroupID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppSecurityGroupMember] CHECK CONSTRAINT [FK_AppSecurityGroupMember_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppSecurityRegDomainListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityRegDomainListMenu_AppComOrganization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[AppComOrganization] ([OrganizationID])
GO
ALTER TABLE [dbo].[AppSecurityRegDomainListMenu] CHECK CONSTRAINT [FK_AppSecurityRegDomainListMenu_AppComOrganization]
GO
ALTER TABLE [dbo].[AppSecurityRegDomainListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityRegDomainListMenu_AppListMenu] FOREIGN KEY([MenuID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
GO
ALTER TABLE [dbo].[AppSecurityRegDomainListMenu] CHECK CONSTRAINT [FK_AppSecurityRegDomainListMenu_AppListMenu]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppDeskTop] FOREIGN KEY([DesktopID])
REFERENCES [dbo].[AppDesktop] ([DesktopID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppDeskTop]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppFormLinkTarget] FOREIGN KEY([FormLinkTargetID])
REFERENCES [dbo].[AppFormLinkTarget] ([LinkTargetID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppFormLinkTarget]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppOrganization] FOREIGN KEY([OrganizationID])
REFERENCES [dbo].[AppComOrganization] ([OrganizationID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppOrganization]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppReport] FOREIGN KEY([ReportID])
REFERENCES [dbo].[AppReport] ([ReportID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppReport]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppRouteState] FOREIGN KEY([RouteStateID])
REFERENCES [dbo].[AppRouteState] ([RouteStateID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppRouteState]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppSearch]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppSearchView]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppSecurityGroup] FOREIGN KEY([GroupID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransaction]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransaction1] FOREIGN KEY([UserActionTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransaction1]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionField] FOREIGN KEY([TransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionField]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionUnit1] FOREIGN KEY([UserActionTransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionUnit1]
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionUnitLinkedSearch] FOREIGN KEY([TransactionUnitLinkedSearchId])
REFERENCES [dbo].[AppTransactionUnitLinkedSearch] ([TransactionUnitLinkedSearchId])
GO
ALTER TABLE [dbo].[AppSecuritySysObjGroupUser] CHECK CONSTRAINT [FK_AppSecuritySysObjGroupUser_AppTransactionUnitLinkedSearch]
GO
ALTER TABLE [dbo].[AppSecurityTransactionAction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityTransactionAction_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppSecurityTransactionAction] CHECK CONSTRAINT [FK_AppSecurityTransactionAction_AppTransaction]
GO
ALTER TABLE [dbo].[AppSecurityTransactionActionResource]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityTransactionActionResource_AppSecurityTransactionAction] FOREIGN KEY([TransactionActionID])
REFERENCES [dbo].[AppSecurityTransactionAction] ([TransactionActionID])
GO
ALTER TABLE [dbo].[AppSecurityTransactionActionResource] CHECK CONSTRAINT [FK_AppSecurityTransactionActionResource_AppSecurityTransactionAction]
GO
ALTER TABLE [dbo].[AppSecurityUserListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityUserListMenu_AppListMenu] FOREIGN KEY([MenuID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
GO
ALTER TABLE [dbo].[AppSecurityUserListMenu] CHECK CONSTRAINT [FK_AppSecurityUserListMenu_AppListMenu]
GO
ALTER TABLE [dbo].[AppSecurityUserListMenu]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityUserListMenu_AppSecurityGroup] FOREIGN KEY([GroupID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppSecurityUserListMenu] CHECK CONSTRAINT [FK_AppSecurityUserListMenu_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppSecurityUserRolePrevilege]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityUserRolePrevilege_AppSecurityEntityAction] FOREIGN KEY([EntityActionID])
REFERENCES [dbo].[AppSecurityEntityAction] ([EntityActionID])
GO
ALTER TABLE [dbo].[AppSecurityUserRolePrevilege] CHECK CONSTRAINT [FK_AppSecurityUserRolePrevilege_AppSecurityEntityAction]
GO
ALTER TABLE [dbo].[AppSecurityUserRolePrevilege]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSecurityUserRolePrevilege_AppSecurityGroup] FOREIGN KEY([RoleID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppSecurityUserRolePrevilege] CHECK CONSTRAINT [FK_AppSecurityUserRolePrevilege_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppSEFolder]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSEFolder_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppSEFolder] CHECK CONSTRAINT [FK_AppSEFolder_AppTransaction]
GO
ALTER TABLE [dbo].[AppSEFolderResource]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSEFolderResource_AppSecurityGroup] FOREIGN KEY([RoleID])
REFERENCES [dbo].[AppSecurityGroup] ([GroupID])
GO
ALTER TABLE [dbo].[AppSEFolderResource] CHECK CONSTRAINT [FK_AppSEFolderResource_AppSecurityGroup]
GO
ALTER TABLE [dbo].[AppSEFolderResource]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSEFolderResource_AppSEFolder] FOREIGN KEY([FolderID])
REFERENCES [dbo].[AppSEFolder] ([FolderID])
GO
ALTER TABLE [dbo].[AppSEFolderResource] CHECK CONSTRAINT [FK_AppSEFolderResource_AppSEFolder]
GO
ALTER TABLE [dbo].[AppSysLabelLanguage]  WITH NOCHECK ADD  CONSTRAINT [FK_AppSysLabelLanguage_AppListMenu] FOREIGN KEY([MenuID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
GO
ALTER TABLE [dbo].[AppSysLabelLanguage] CHECK CONSTRAINT [FK_AppSysLabelLanguage_AppListMenu]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppEntityInfo] FOREIGN KEY([LogicalDisplayEntityID])
REFERENCES [dbo].[AppEntityInfo] ([EntityInfoID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppEntityInfo]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH CHECK ADD  CONSTRAINT [FK_AppTransaction_AppExternalWebAPIConfig] FOREIGN KEY([WebApiConfigID])
REFERENCES [dbo].[AppWebApiConfig] ([WebApiConfigID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppExternalWebAPIConfig]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppForm] FOREIGN KEY([FormID])
REFERENCES [dbo].[AppForm] ([FormID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppForm]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppForm_PrintFormID] FOREIGN KEY([PrintFormID])
REFERENCES [dbo].[AppForm] ([FormID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppForm_PrintFormID]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppListMenu] FOREIGN KEY([SaasApplicationID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppListMenu]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppProjectOrWorkFlow] FOREIGN KEY([MasterWorkflowID])
REFERENCES [dbo].[AppProjectOrWorkFlow] ([ProjectID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppProjectOrWorkFlow]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppTransaction] FOREIGN KEY([FolderTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransaction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransaction_AppTransaction1] FOREIGN KEY([MasterTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransaction] CHECK CONSTRAINT [FK_AppTransaction_AppTransaction1]
GO
ALTER TABLE [dbo].[AppTransactionDataLoad]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionDataLoad_AppDataSet] FOREIGN KEY([DataSetID])
REFERENCES [dbo].[AppDataSet] ([DataSetID])
GO
ALTER TABLE [dbo].[AppTransactionDataLoad] CHECK CONSTRAINT [FK_AppTransactionDataLoad_AppDataSet]
GO
ALTER TABLE [dbo].[AppTransactionDataLoad]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionDataLoad_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionDataLoad] CHECK CONSTRAINT [FK_AppTransactionDataLoad_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionDataLoad]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionDataLoad_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionDataLoad] CHECK CONSTRAINT [FK_AppTransactionDataLoad_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionDataTransferSetting]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionDataTransferSetting_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionDataTransferSetting] CHECK CONSTRAINT [FK_AppTransactionDataTransferSetting_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppEntityInfo] FOREIGN KEY([EntityID])
REFERENCES [dbo].[AppEntityInfo] ([EntityInfoID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppEntityInfo]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppFormLayoutItem] FOREIGN KEY([HostFormLayoutItemID])
REFERENCES [dbo].[AppFormLayoutItem] ([FormLayoutItemID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppFormLayoutItem]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppProjectWorkFlowAction] FOREIGN KEY([OnChangeTriggerToCommandID])
REFERENCES [dbo].[AppProjectWorkFlowAction] ([WorkFlowActionID])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppProjectWorkFlowAction]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransaction] FOREIGN KEY([FileControlTypeFolderTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField] FOREIGN KEY([ChildUnitSubscribeParentFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField_DDLParentLevelID] FOREIGN KEY([DDLParentLevelID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField_DDLParentLevelID]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField_MasterEntityFieldlID] FOREIGN KEY([MasterEntityFieldlID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField_MasterEntityFieldlID]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField_MatrixForeignKeyFieldID] FOREIGN KEY([MatrixForeignKeyFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField_MatrixForeignKeyFieldID]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField_SiblingUnitLogicalKeyFieldID] FOREIGN KEY([SiblingUnitLogicalKeyFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField_SiblingUnitLogicalKeyFieldID]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField1] FOREIGN KEY([MatrixKeyTransactionFieldId])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField1]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionField2] FOREIGN KEY([LinkToParentPrimaryKeyFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionField2]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionFieldAggFunction] FOREIGN KEY([ParentUnitSubscribeChildAggFunctionID])
REFERENCES [dbo].[AppTransactionFieldAggFunction] ([FieldAggFunctionID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionFieldAggFunction]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionField]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionField_AppTransactionUnit1] FOREIGN KEY([DdlForeignUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionField] CHECK CONSTRAINT [FK_AppTransactionField_AppTransactionUnit1]
GO
ALTER TABLE [dbo].[AppTransactionFieldAggFunction]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionFieldAggFunction_AppTransactionField] FOREIGN KEY([TransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionFieldAggFunction] CHECK CONSTRAINT [FK_AppTransactionFieldAggFunction_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionGroupItem]  WITH CHECK ADD  CONSTRAINT [FK_AppTransactionGroupItem_AppTransactionGroup] FOREIGN KEY([TransactionGroupID])
REFERENCES [dbo].[AppTransactionGroup] ([TransactionGroupID])
GO
ALTER TABLE [dbo].[AppTransactionGroupItem] CHECK CONSTRAINT [FK_AppTransactionGroupItem_AppTransactionGroup]
GO
ALTER TABLE [dbo].[AppTransactionGroupItem]  WITH CHECK ADD  CONSTRAINT [FK_AppTransactionGroupItem_AppTransactionItem] FOREIGN KEY([TransactionItemID])
REFERENCES [dbo].[AppTransactionItem] ([AppTransactionItemID])
GO
ALTER TABLE [dbo].[AppTransactionGroupItem] CHECK CONSTRAINT [FK_AppTransactionGroupItem_AppTransactionItem]
GO
ALTER TABLE [dbo].[AppTransactionGroupSession]  WITH CHECK ADD  CONSTRAINT [FK_AppTransactionGroupSession_AppTransactionGroup] FOREIGN KEY([TransactionGroupID])
REFERENCES [dbo].[AppTransactionGroup] ([TransactionGroupID])
GO
ALTER TABLE [dbo].[AppTransactionGroupSession] CHECK CONSTRAINT [FK_AppTransactionGroupSession_AppTransactionGroup]
GO
ALTER TABLE [dbo].[AppTransactionItem]  WITH CHECK ADD  CONSTRAINT [FK_AppTransactionItem_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionItem] CHECK CONSTRAINT [FK_AppTransactionItem_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionNavigation]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionNavigation_AppSearch] FOREIGN KEY([QuickSearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppTransactionNavigation] CHECK CONSTRAINT [FK_AppTransactionNavigation_AppSearch]
GO
ALTER TABLE [dbo].[AppTransactionNavigation]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionNavigation_AppSearchView] FOREIGN KEY([FolderViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppTransactionNavigation] CHECK CONSTRAINT [FK_AppTransactionNavigation_AppSearchView]
GO
ALTER TABLE [dbo].[AppTransactionNavigation]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionNavigation_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionNavigation] CHECK CONSTRAINT [FK_AppTransactionNavigation_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionPostProcess]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionPostProcess_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionPostProcess] CHECK CONSTRAINT [FK_AppTransactionPostProcess_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionPostProcess]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionPostProcess_AppTransactionUnit] FOREIGN KEY([RootUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionPostProcess] CHECK CONSTRAINT [FK_AppTransactionPostProcess_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionSaveAsMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionSaveAsMapping_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionSaveAsMapping] CHECK CONSTRAINT [FK_AppTransactionSaveAsMapping_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionSaveAsMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionSaveAsMapping_AppTransactionDataTransferSetting] FOREIGN KEY([DataTransferSettingID])
REFERENCES [dbo].[AppTransactionDataTransferSetting] ([DataTransferSettingID])
GO
ALTER TABLE [dbo].[AppTransactionSaveAsMapping] CHECK CONSTRAINT [FK_AppTransactionSaveAsMapping_AppTransactionDataTransferSetting]
GO
ALTER TABLE [dbo].[AppTransactionSaveAsMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionSaveAsMapping_AppTransactionUnit] FOREIGN KEY([MappingUnitId])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionSaveAsMapping] CHECK CONSTRAINT [FK_AppTransactionSaveAsMapping_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnit]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnit_AppTransaction] FOREIGN KEY([TransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionUnit] CHECK CONSTRAINT [FK_AppTransactionUnit_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionUnit]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnit_AppTransactionUnit] FOREIGN KEY([ParentTransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnit] CHECK CONSTRAINT [FK_AppTransactionUnit_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitDeleteFlow]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitDeleteFlow_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnitDeleteFlow] CHECK CONSTRAINT [FK_AppTransactionUnitDeleteFlow_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitExtendFieldValue]  WITH CHECK ADD  CONSTRAINT [FK_AppTransactionUnitExtendFieldValue_AppTransactionField] FOREIGN KEY([UnitExtendFiledID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionUnitExtendFieldValue] CHECK CONSTRAINT [FK_AppTransactionUnitExtendFieldValue_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionUnitFormula]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitFormula_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnitFormula] CHECK CONSTRAINT [FK_AppTransactionUnitFormula_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitFormula]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitFormula_AppTransactionUnit1] FOREIGN KEY([ChildTransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnitFormula] CHECK CONSTRAINT [FK_AppTransactionUnitFormula_AppTransactionUnit1]
GO
ALTER TABLE [dbo].[AppTransactionUnitJoin]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitJoin_AppTransactionField] FOREIGN KEY([FromJointKeyTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionUnitJoin] CHECK CONSTRAINT [FK_AppTransactionUnitJoin_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionUnitJoin]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitJoin_AppTransactionUnit] FOREIGN KEY([TransactionUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnitJoin] CHECK CONSTRAINT [FK_AppTransactionUnitJoin_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitJoinSelectColumnMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitJoinSelectColumnMapping_AppTransactionField] FOREIGN KEY([FromJointPlaceHolderTransactionFieldID])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
GO
ALTER TABLE [dbo].[AppTransactionUnitJoinSelectColumnMapping] CHECK CONSTRAINT [FK_AppTransactionUnitJoinSelectColumnMapping_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionUnitJoinSelectColumnMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitJoinSelectColumnMapping_AppTransactionUnitJoin] FOREIGN KEY([UnitJoinID])
REFERENCES [dbo].[AppTransactionUnitJoin] ([UnitJoinID])
GO
ALTER TABLE [dbo].[AppTransactionUnitJoinSelectColumnMapping] CHECK CONSTRAINT [FK_AppTransactionUnitJoinSelectColumnMapping_AppTransactionUnitJoin]
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppSearch] FOREIGN KEY([SearchId])
REFERENCES [dbo].[AppSearch] ([SearchID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch] CHECK CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppSearch]
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppSearchSaved] FOREIGN KEY([SearchSaveID])
REFERENCES [dbo].[AppSearchSaved] ([SearchSavedID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch] CHECK CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppSearchSaved]
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppSearchView] FOREIGN KEY([SearchViewId])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch] CHECK CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppSearchView]
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppTransaction] FOREIGN KEY([TargetTransactionID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch] CHECK CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppTransaction]
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppTransactionUnit] FOREIGN KEY([TransactionUnitId])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitLinkedSearch] CHECK CONSTRAINT [FK_AppTransactionUnitLinkedSearch_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppSearchField] FOREIGN KEY([SearchFieldId])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppSearchField]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppTransactionField] FOREIGN KEY([TransactionFieldId])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppTransactionUnit] FOREIGN KEY([TargetUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppTransactionUnitLinkedSearch] FOREIGN KEY([TransactionUnitLinkedSearchId])
REFERENCES [dbo].[AppTransactionUnitLinkedSearch] ([TransactionUnitLinkedSearchId])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchFieldMapping_AppTransactionUnitLinkedSearch]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppSearchViewField] FOREIGN KEY([SearchViewFieldId])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppSearchViewField]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppTransactionField] FOREIGN KEY([TransactionFieldId])
REFERENCES [dbo].[AppTransactionField] ([TransactionFieldID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppTransactionField]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppTransactionUnit] FOREIGN KEY([TargetUnitID])
REFERENCES [dbo].[AppTransactionUnit] ([TransactionUnitID])
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppTransactionUnit]
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppTransactionUnitLinkedSearch] FOREIGN KEY([TransactionUnitLinkedSearchId])
REFERENCES [dbo].[AppTransactionUnitLinkedSearch] ([TransactionUnitLinkedSearchId])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AppTransactionUnitSearchViewFieldMapping] CHECK CONSTRAINT [FK_AppTransactionUnitSearchViewFieldMapping_AppTransactionUnitLinkedSearch]
GO
ALTER TABLE [dbo].[AppTranscationDataLoadFieldMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTranscationDataLoadFieldMapping_AppTransactionDataLoad] FOREIGN KEY([DataLoadID])
REFERENCES [dbo].[AppTransactionDataLoad] ([DataLoadID])
GO
ALTER TABLE [dbo].[AppTranscationDataLoadFieldMapping] CHECK CONSTRAINT [FK_AppTranscationDataLoadFieldMapping_AppTransactionDataLoad]
GO
ALTER TABLE [dbo].[AppTranscationReport]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTranscationReport_AppReport] FOREIGN KEY([ReportID])
REFERENCES [dbo].[AppReport] ([ReportID])
GO
ALTER TABLE [dbo].[AppTranscationReport] CHECK CONSTRAINT [FK_AppTranscationReport_AppReport]
GO
ALTER TABLE [dbo].[AppTranscationReport]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTranscationReport_AppTransaction] FOREIGN KEY([TranscationID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTranscationReport] CHECK CONSTRAINT [FK_AppTranscationReport_AppTransaction]
GO
ALTER TABLE [dbo].[AppTrascationRecycleBin]  WITH NOCHECK ADD  CONSTRAINT [FK_AppTrascationRecycleBin_AppTransaction] FOREIGN KEY([TranscationID])
REFERENCES [dbo].[AppTransaction] ([TransactionID])
GO
ALTER TABLE [dbo].[AppTrascationRecycleBin] CHECK CONSTRAINT [FK_AppTrascationRecycleBin_AppTransaction]
GO
ALTER TABLE [dbo].[AppUserSkill]  WITH NOCHECK ADD  CONSTRAINT [FK_AppUserSkill_AppUserSkillList] FOREIGN KEY([SkillItemID])
REFERENCES [dbo].[AppUserSkillList] ([SkillItemID])
GO
ALTER TABLE [dbo].[AppUserSkill] CHECK CONSTRAINT [FK_AppUserSkill_AppUserSkillList]
GO
ALTER TABLE [dbo].[AppVersionEditionModule]  WITH NOCHECK ADD  CONSTRAINT [FK_AppVersionEditionModule_AppListMenu] FOREIGN KEY([MenuID])
REFERENCES [dbo].[AppListMenu] ([MenuID])
GO
ALTER TABLE [dbo].[AppVersionEditionModule] CHECK CONSTRAINT [FK_AppVersionEditionModule_AppListMenu]
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearch] FOREIGN KEY([SearchID])
REFERENCES [dbo].[AppSearch] ([SearchID])
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping] CHECK CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearch]
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearchField] FOREIGN KEY([SearchFieldID])
REFERENCES [dbo].[AppSearchField] ([SearchFieldID])
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping] CHECK CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearchField]
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping] CHECK CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearchView]
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping]  WITH NOCHECK ADD  CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearchViewField] FOREIGN KEY([SearchViewFiledID])
REFERENCES [dbo].[AppSearchViewField] ([SearchViewFieldID])
GO
ALTER TABLE [dbo].[AppViewFiledSearchFiledMapping] CHECK CONSTRAINT [FK_AppViewFiledSearchFiledMapping_AppSearchViewField]
GO
ALTER TABLE [dbo].[AppViewLinkedSeaechOrUrl]  WITH CHECK ADD  CONSTRAINT [FK_AppViewLinkedSeaechOrUrl_AppSearchView] FOREIGN KEY([SearchViewID])
REFERENCES [dbo].[AppSearchView] ([SearchViewID])
GO
ALTER TABLE [dbo].[AppViewLinkedSeaechOrUrl] CHECK CONSTRAINT [FK_AppViewLinkedSeaechOrUrl_AppSearchView]
GO
ALTER TABLE [dbo].[AppWebApiConfig]  WITH CHECK ADD  CONSTRAINT [FK_AppExternalWebAPIConfig_AppWebApiPorvider] FOREIGN KEY([WebApiProviderID])
REFERENCES [dbo].[AppWebApiProvider] ([WebApiPorviderID])
GO
ALTER TABLE [dbo].[AppWebApiConfig] CHECK CONSTRAINT [FK_AppExternalWebAPIConfig_AppWebApiPorvider]
GO
ALTER TABLE [dbo].[AppWebApiParamsHeaderSettig]  WITH CHECK ADD  CONSTRAINT [FK_AppWebApiParamsHeaderSettig_AppWebApiConfig] FOREIGN KEY([WebApiConfigID])
REFERENCES [dbo].[AppWebApiConfig] ([WebApiConfigID])
GO
ALTER TABLE [dbo].[AppWebApiParamsHeaderSettig] CHECK CONSTRAINT [FK_AppWebApiParamsHeaderSettig_AppWebApiConfig]
GO
