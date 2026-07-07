-- ============================================================
-- SP_StyleHeader
-- Transaction 2256 (Style Header) report data source
-- Generated from AppTransactionField / AppEntityInfo metadata
-- Tenant DB: TenantDB_PLM26
-- ERP lookup DB: SourceERP (AppDataSourceRegister id 1071)
-- Generator: AppReact/ImportDoc/CreateTransactionStoredProcedure/_gen_SP_FromTransaction.ps1
-- ============================================================
-- Report Engine contract: RS0 = header (1 row), tokens {{header.ColumnAlias}}
-- Parameters must match AppReportTemplateService.FetchData
-- ============================================================
IF OBJECT_ID('dbo.SP_StyleHeader', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_StyleHeader;
GO

CREATE PROCEDURE dbo.SP_StyleHeader
    @MainReferenceId    INT,
    @MasterReferenceId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: header (exactly 1 row for {{header.*}} tokens)
    SELECT
        u1.[ReferenceId] AS [ReferenceId],
        u1.[ReferenceCode] AS [ReferenceCode],
        u1.[MasterReferenceId] AS [MasterReferenceId],
        u1.[FolderId] AS [FolderId],
        u2.[ReferenceId] AS [PlmStyleHeader_ReferenceId],
        [Classification_1].[Name] AS [ProductClass],
        [Product_Type_2_2].[Name] AS [ProductType],
        [Season_3_3].[Description] AS [Season3],
        [Collection_4_4].[Description] AS [Collection4],
        [Group_5].[Description] AS [Group],
        u2.[Sketch] AS [Sketch],
        [Division_8_6].[DivisionName] AS [Division8],
        [Size_Range_10_7].[SizeRunCode] AS [SizeRange10],
        [Dimension_11_8].[DimensionCode] AS [DimensionInseam],
        u2.[Article] AS [Style],
        u2.[Description_23] AS [Details],
        [Product_Manager_9].[LoginName] AS [ProductManager],
        u2.[Long_Description] AS [Description],
        [Sample_Size_Detail_10].[SizeName] AS [SampleSizeNeeded],
        [ProductTypeGroup_11].[GroupCode] AS [TypeGroup],
        u2.[Size_Detail_Dispaly] AS [SizeDetailDispaly],
        [Division_186_12].[DivisionName] AS [Division186],
        u2.[Created_By] AS [CreatedBy],
        u2.[Last_Revised_By] AS [LastRevisedBy],
        [Style_Status_13].Code AS [StyleStatus],
        u2.[State] AS [State],
        u2.[Sample_Status] AS [SampleStatus],
        [CB_Fit_1_Status_14].Code AS [CBFit1Status],
        u2.[IN_Fit_1_State] AS [INFit1State],
        [CB_Fit_2_Status_15].Code AS [CBFit2Status],
        u2.[IN_Fit_2_State] AS [INFit2State],
        [CB_Fit_3_Status_16].Code AS [CBFit3Status],
        u2.[IN_Fit_3_State] AS [INFit3State],
        [CB_Fit_4_Status_17].Code AS [CBFit4Status],
        u2.[IN_Fit_4_State] AS [INFit4State],
        [CB_PP_1_Status_18].Code AS [CBPP1Status],
        u2.[IN_PP_1_State] AS [INPP1State],
        [CB_PP_2_Status_19].Code AS [CBPP2Status],
        u2.[IN_PP_2_State] AS [INPP2State],
        [CB_PP_3_Status_20].Code AS [CBPP3Status],
        u2.[IN_PP_3_State] AS [INPP3State],
        [CB_TOP_1_Status_21].Code AS [CBTOP1Status],
        u2.[IN_TOP_1_State] AS [INTOP1State],
        [CB_TOP_2_Status_22].Code AS [CBTOP2Status],
        u2.[IN_TOP_2_State] AS [INTOP2State],
        CASE WHEN u2.[CHK_Fit_1_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_Fit_1_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_Fit_1_Latest] AS NVARCHAR(MAX)) END AS [CHKFit1Latest],
        CASE WHEN u2.[CHK_Fit_2_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_Fit_2_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_Fit_2_Latest] AS NVARCHAR(MAX)) END AS [CHKFit2Latest],
        CASE WHEN u2.[CHK_Fit_3_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_Fit_3_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_Fit_3_Latest] AS NVARCHAR(MAX)) END AS [CHKFit3Latest],
        CASE WHEN u2.[CHK_Fit_4_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_Fit_4_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_Fit_4_Latest] AS NVARCHAR(MAX)) END AS [CHKFit4Latest],
        CASE WHEN u2.[CHK_PP_1_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_PP_1_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_PP_1_Latest] AS NVARCHAR(MAX)) END AS [CHKPP1Latest],
        CASE WHEN u2.[CHK_PP_2_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_PP_2_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_PP_2_Latest] AS NVARCHAR(MAX)) END AS [CHKPP2Latest],
        CASE WHEN u2.[CHK_PP_3_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_PP_3_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_PP_3_Latest] AS NVARCHAR(MAX)) END AS [CHKPP3Latest],
        CASE WHEN u2.[CHK_TOP_1_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_TOP_1_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_TOP_1_Latest] AS NVARCHAR(MAX)) END AS [CHKTOP1Latest],
        CASE WHEN u2.[CHK_TOP_2_Latest] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[CHK_TOP_2_Latest] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[CHK_TOP_2_Latest] AS NVARCHAR(MAX)) END AS [CHKTOP2Latest],
        u2.[fit1status_IB] AS [fit1statusIB],
        u2.[fit2status_IB] AS [fit2statusIB],
        u2.[fit3status_IB] AS [fit3statusIB],
        u2.[fit4status_IB] AS [fit4statusIB],
        u2.[pp1status_IB] AS [pp1statusIB],
        u2.[pp2status_IB] AS [pp2statusIB],
        u2.[pp3status_IB] AS [pp3statusIB],
        u2.[top1status_IB] AS [top1statusIB],
        u2.[top2status_IB] AS [top2statusIB],
        [CB_Fit_1_Type_23].Code AS [CBFit1Type],
        u2.[fit1type_IB] AS [fit1typeIB],
        [CB_Fit_2_Type_24].Code AS [CBFit2Type],
        u2.[fit2type_IB] AS [fit2typeIB],
        [CB_Fit_3_Type_25].Code AS [CBFit3Type],
        u2.[fit3type_IB] AS [fit3typeIB],
        [CB_Fit_4_Type_26].Code AS [CBFit4Type],
        u2.[fit4type_IB] AS [fit4typeIB],
        [CB_PP_1_Type_27].Code AS [CBPP1Type],
        u2.[pp1type_IB] AS [pp1typeIB],
        [CB_PP_2_Type_28].Code AS [CBPP2Type],
        u2.[pp2type_IB] AS [pp2typeIB],
        [CB_PP_3_Type_29].Code AS [CBPP3Type],
        u2.[pp3type_IB] AS [pp3typeIB],
        [CB_TOP_1_Type_30].Code AS [CBTOP1Type],
        u2.[top1type_IB] AS [top1typeIB],
        [CB_TOP_2_Type_31].Code AS [CBTOP2Type],
        u2.[top2type_IB] AS [top2typeIB],
        u2.[Calc_Fit_Status] AS [CalcFitStatus],
        [Print_Solid_32].Code AS [PrintSolid],
        u2.[Supplier_Cost] AS [SupplierCost],
        [Currency_33].[CurrencyCode] AS [Currency],
        u2.[Needed_for_Calc] AS [NeededforCalc],
        [Item_Type_34].[TechPackTypeName] AS [ItemType],
        CASE WHEN u2.[Publish_to_ERP] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Publish_to_ERP] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Publish_to_ERP] AS NVARCHAR(MAX)) END AS [PublishtoERP],
        CASE WHEN u2.[Published_to_ERP] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Published_to_ERP] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Published_to_ERP] AS NVARCHAR(MAX)) END AS [PublishedtoERP],
        CASE WHEN u2.[Publish_Failed_to_ERP] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Publish_Failed_to_ERP] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Publish_Failed_to_ERP] AS NVARCHAR(MAX)) END AS [PublishFailedtoERP],
        u2.[Product_Code] AS [ProductCode],
        [Size_Range_5022_35].[SizeRunCode] AS [SizeRange5022],
        [DivisionBlock_36].[DivisionName] AS [DivisionBlock],
        [Product_Class_5024_37].[Name] AS [ProductClass5024],
        [Dimension_5025_38].[DimensionCode] AS [Dimension5025],
        [Season_5026_39].[Description] AS [Season5026],
        u2.[Price_Type] AS [PriceType],
        [First_Cost_Currency_40].[CurrencyCode] AS [FirstCostCurrency],
        CASE WHEN u2.[Valid_Size_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Size_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Size_Selection] AS NVARCHAR(MAX)) END AS [ValidSizeSelection],
        CASE WHEN u2.[Valid_Product_Code_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Product_Code_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Product_Code_Selection] AS NVARCHAR(MAX)) END AS [ValidProductCodeSelection],
        CASE WHEN u2.[Valid_DivisionBlock_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_DivisionBlock_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_DivisionBlock_Selection] AS NVARCHAR(MAX)) END AS [ValidDivisionBlockSelection],
        CASE WHEN u2.[Valid_Product_Class_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Product_Class_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Product_Class_Selection] AS NVARCHAR(MAX)) END AS [ValidProductClassSelection],
        CASE WHEN u2.[Valid_Dimension_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Dimension_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Dimension_Selection] AS NVARCHAR(MAX)) END AS [ValidDimensionSelection],
        CASE WHEN u2.[Valid_Season_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Season_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Season_Selection] AS NVARCHAR(MAX)) END AS [ValidSeasonSelection],
        CASE WHEN u2.[Valid_Price_Type_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Price_Type_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Price_Type_Selection] AS NVARCHAR(MAX)) END AS [ValidPriceTypeSelection],
        CASE WHEN u2.[Valid_First_Cost_Currency_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_First_Cost_Currency_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_First_Cost_Currency_Selection] AS NVARCHAR(MAX)) END AS [ValidFirstCostCurrencySelection],
        u2.[Color] AS [Color],
        CASE WHEN u2.[Valid_Color_Selection] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[Valid_Color_Selection] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[Valid_Color_Selection] AS NVARCHAR(MAX)) END AS [ValidColorSelection],
        u2.[Active_Count] AS [ActiveCount],
        CASE WHEN u2.[DimensionColorSizeActiveBooleanSum] IN ('1','Y','True','true') THEN 'Yes' WHEN u2.[DimensionColorSizeActiveBooleanSum] IN ('0','N','False','false') THEN 'No' ELSE CAST(u2.[DimensionColorSizeActiveBooleanSum] AS NVARCHAR(MAX)) END AS [DimensionColorSizeActiveBooleanSum],
        u2.[Original_Reference] AS [OriginalReference],
        [New_Carryover_41].Code AS [NewCarryover],
        [Parent__Child_42].Code AS [ParentChild],
        u2.[Ref_Style_from_Past_SS] AS [RefStylefromPastSS],
        u2.[Version_Code] AS [VersionCode],
        [Fit_Type_5239_43].Code AS [FitType5239],
        [Directional_Fabric_44].Code AS [DirectionalFabric],
        [Treatment_1_45].Code AS [Treatment1],
        [Treatment_2_46].Code AS [Treatment2],
        u2.[Treatment_Comments] AS [TreatmentComments],
        u2.[BW_File] AS [File],
        u2.[Original_Image] AS [OriginalImage],
        [Product_Class_5262_47].[Name] AS [ProductClass5262],
        [Class_Group_48].[GroupCode] AS [ClassGroup],
        u2.[French] AS [French],
        [Inseam_49].Code AS [Length],
        [Manufacturer_COO_50].[Name] AS [ManufacturerCOO],
        [Garment_Factory_51].Code AS [GarmentFactory],
        u2.[Name] AS [Name],
        u2.[Gender_txt_7029] AS [Gendertxt7029],
        u2.[Product_Type_txt_7030] AS [ProductTypetxt7030],
        u2.[Fit_Type_txt] AS [FitTypetxt],
        u2.[CC_7032] AS [CC7032],
        u2.[Gender_7033] AS [Gender7033],
        u2.[Product_Type_7034] AS [ProductType7034],
        u2.[Details_7035] AS [Details7035],
        u2.[Fit_Type_7036] AS [FitType7036],
        u2.[sketch_id] AS [sketchid],
        [ddl_52].[Description] AS [ddl],
        u2.[Free_text] AS [Freetext],
        [Special_Customer_53].Code AS [SpecialCustomer],
        u2.[Inseam_txt] AS [Inseamtxt],
        u2.[Length] AS [PlmStyleHeader_Length],
        u2.[Collection_7124] AS [Collection7124],
        u2.[Collection_txt] AS [Collectiontxt],
        [Selling_Period_54].[Description] AS [SellingPeriod],
        [Private_Label_55].Code AS [PrivateLabel],
        [Vendor_56].Code AS [Vendor],
        u2.[Fabric_Code] AS [FabricCode],
        u2.[_of_Characters] AS [ofCharacters],
        u2.[Description_7162] AS [Description7162],
        u2.[Char_Count_Check] AS [CharCountCheck],
        [Gender_7171_57].[SubjectName1] AS [Gender7171],
        u2.[CC_7192] AS [CC7192],
        u2.[Gender_txt_7193] AS [Gendertxt7193],
        u2.[Product_Type_txt_7194] AS [ProductTypetxt7194],
        u2.[Length_txt] AS [Lengthtxt],
        u2.[Details_7198] AS [Details7198],
        u2.[ProductTypeGroup_txt] AS [ProductTypeGrouptxt],
        [Subcategory_58].Code AS [Subcategory],
        [ERP_Season_59].[Description] AS [ERPSeason],
        u2.[French_Name] AS [FrenchName],
        u2.[Sets_details_fabric] AS [Setsdetailsfabric]
    FROM dbo.[Plm_ReferenceBasicInfo] AS [u1]
    INNER JOIN dbo.[Plm_Style_Header] AS [u2] ON [u2].[ReferenceId] = [u1].[ReferenceId]
    LEFT JOIN [SourceERP].dbo.[tblProductClass] AS [Classification_1] ON [Classification_1].[ProductClassID] = u2.[Classification]
    LEFT JOIN [SourceERP].dbo.[tblProductType] AS [Product_Type_2_2] ON [Product_Type_2_2].[ProductType_Id] = u2.[Product_Type_2]
    LEFT JOIN [SourceERP].dbo.[tblSellingPeriod] AS [Season_3_3] ON [Season_3_3].[SellingPeriod_Id] = u2.[Season_3]
    LEFT JOIN [SourceERP].dbo.[tblCollection] AS [Collection_4_4] ON [Collection_4_4].[Collection_Id] = u2.[Collection_4]
    LEFT JOIN [SourceERP].dbo.[tblGroup] AS [Group_5] ON [Group_5].[Group_Id] = u2.[Group]
    LEFT JOIN [SourceERP].dbo.[tblCompanyDivision] AS [Division_8_6] ON [Division_8_6].[CieDivisionID] = u2.[Division_8]
    LEFT JOIN [SourceERP].dbo.[tblSizeRun] AS [Size_Range_10_7] ON [Size_Range_10_7].[SizeRunId] = u2.[Size_Range_10]
    LEFT JOIN [SourceERP].dbo.[tblDimension] AS [Dimension_11_8] ON [Dimension_11_8].[DimensionID] = u2.[Dimension_11]
    LEFT JOIN [TenantDB_PLM26].dbo.[Plm_pdmsecuritywebuser] AS [Product_Manager_9] ON [Product_Manager_9].[UserID] = u2.[Product_Manager]
    LEFT JOIN [SourceERP].dbo.[tblSizeRunRotate] AS [Sample_Size_Detail_10] ON [Sample_Size_Detail_10].[SizeRunRotateID] = u2.[Sample_Size_Detail]
    LEFT JOIN [SourceERP].dbo.[tblProductClassGroup] AS [ProductTypeGroup_11] ON [ProductTypeGroup_11].[ProductClassGroupID] = u2.[ProductTypeGroup]
    LEFT JOIN [SourceERP].dbo.[tblCompanyDivision] AS [Division_186_12] ON [Division_186_12].[CieDivisionID] = u2.[Division_186]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Style_Status_13] ON [Style_Status_13].EntityInfoID = 4649 AND [Style_Status_13].InternalKey = u2.[Style_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_1_Status_14] ON [CB_Fit_1_Status_14].EntityInfoID = 4756 AND [CB_Fit_1_Status_14].InternalKey = u2.[CB_Fit_1_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_2_Status_15] ON [CB_Fit_2_Status_15].EntityInfoID = 4756 AND [CB_Fit_2_Status_15].InternalKey = u2.[CB_Fit_2_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_3_Status_16] ON [CB_Fit_3_Status_16].EntityInfoID = 4756 AND [CB_Fit_3_Status_16].InternalKey = u2.[CB_Fit_3_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_4_Status_17] ON [CB_Fit_4_Status_17].EntityInfoID = 4756 AND [CB_Fit_4_Status_17].InternalKey = u2.[CB_Fit_4_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_PP_1_Status_18] ON [CB_PP_1_Status_18].EntityInfoID = 4756 AND [CB_PP_1_Status_18].InternalKey = u2.[CB_PP_1_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_PP_2_Status_19] ON [CB_PP_2_Status_19].EntityInfoID = 4756 AND [CB_PP_2_Status_19].InternalKey = u2.[CB_PP_2_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_PP_3_Status_20] ON [CB_PP_3_Status_20].EntityInfoID = 4756 AND [CB_PP_3_Status_20].InternalKey = u2.[CB_PP_3_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_TOP_1_Status_21] ON [CB_TOP_1_Status_21].EntityInfoID = 4756 AND [CB_TOP_1_Status_21].InternalKey = u2.[CB_TOP_1_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_TOP_2_Status_22] ON [CB_TOP_2_Status_22].EntityInfoID = 4756 AND [CB_TOP_2_Status_22].InternalKey = u2.[CB_TOP_2_Status]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_1_Type_23] ON [CB_Fit_1_Type_23].EntityInfoID = 4757 AND [CB_Fit_1_Type_23].InternalKey = u2.[CB_Fit_1_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_2_Type_24] ON [CB_Fit_2_Type_24].EntityInfoID = 4757 AND [CB_Fit_2_Type_24].InternalKey = u2.[CB_Fit_2_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_3_Type_25] ON [CB_Fit_3_Type_25].EntityInfoID = 4757 AND [CB_Fit_3_Type_25].InternalKey = u2.[CB_Fit_3_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_Fit_4_Type_26] ON [CB_Fit_4_Type_26].EntityInfoID = 4757 AND [CB_Fit_4_Type_26].InternalKey = u2.[CB_Fit_4_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_PP_1_Type_27] ON [CB_PP_1_Type_27].EntityInfoID = 4757 AND [CB_PP_1_Type_27].InternalKey = u2.[CB_PP_1_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_PP_2_Type_28] ON [CB_PP_2_Type_28].EntityInfoID = 4757 AND [CB_PP_2_Type_28].InternalKey = u2.[CB_PP_2_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_PP_3_Type_29] ON [CB_PP_3_Type_29].EntityInfoID = 4757 AND [CB_PP_3_Type_29].InternalKey = u2.[CB_PP_3_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_TOP_1_Type_30] ON [CB_TOP_1_Type_30].EntityInfoID = 4757 AND [CB_TOP_1_Type_30].InternalKey = u2.[CB_TOP_1_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [CB_TOP_2_Type_31] ON [CB_TOP_2_Type_31].EntityInfoID = 4757 AND [CB_TOP_2_Type_31].InternalKey = u2.[CB_TOP_2_Type]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Print_Solid_32] ON [Print_Solid_32].EntityInfoID = 4734 AND [Print_Solid_32].InternalKey = u2.[Print_Solid]
    LEFT JOIN [SourceERP].dbo.[tblCurrency] AS [Currency_33] ON [Currency_33].[CurrencyID] = u2.[Currency]
    LEFT JOIN [TenantDB_PLM26].dbo.[Plm_pdmTechPackType] AS [Item_Type_34] ON [Item_Type_34].[TechPackTypeID] = u2.[Item_Type]
    LEFT JOIN [SourceERP].dbo.[tblSizeRun] AS [Size_Range_5022_35] ON [Size_Range_5022_35].[SizeRunId] = u2.[Size_Range_5022]
    LEFT JOIN [SourceERP].dbo.[tblCompanyDivision] AS [DivisionBlock_36] ON [DivisionBlock_36].[CieDivisionID] = u2.[DivisionBlock]
    LEFT JOIN [SourceERP].dbo.[tblProductClass] AS [Product_Class_5024_37] ON [Product_Class_5024_37].[ProductClassID] = u2.[Product_Class_5024]
    LEFT JOIN [SourceERP].dbo.[tblDimension] AS [Dimension_5025_38] ON [Dimension_5025_38].[DimensionID] = u2.[Dimension_5025]
    LEFT JOIN [SourceERP].dbo.[tblSellingPeriod] AS [Season_5026_39] ON [Season_5026_39].[SellingPeriod_Id] = u2.[Season_5026]
    LEFT JOIN [SourceERP].dbo.[tblCurrency] AS [First_Cost_Currency_40] ON [First_Cost_Currency_40].[CurrencyID] = u2.[First_Cost_Currency]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [New_Carryover_41] ON [New_Carryover_41].EntityInfoID = 4700 AND [New_Carryover_41].InternalKey = u2.[New_Carryover]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Parent__Child_42] ON [Parent__Child_42].EntityInfoID = 4713 AND [Parent__Child_42].InternalKey = u2.[Parent__Child]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Fit_Type_5239_43] ON [Fit_Type_5239_43].EntityInfoID = 4650 AND [Fit_Type_5239_43].InternalKey = u2.[Fit_Type_5239]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Directional_Fabric_44] ON [Directional_Fabric_44].EntityInfoID = 4629 AND [Directional_Fabric_44].InternalKey = u2.[Directional_Fabric]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Treatment_1_45] ON [Treatment_1_45].EntityInfoID = 4789 AND [Treatment_1_45].InternalKey = u2.[Treatment_1]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Treatment_2_46] ON [Treatment_2_46].EntityInfoID = 4789 AND [Treatment_2_46].InternalKey = u2.[Treatment_2]
    LEFT JOIN [SourceERP].dbo.[tblProductClass] AS [Product_Class_5262_47] ON [Product_Class_5262_47].[ProductClassID] = u2.[Product_Class_5262]
    LEFT JOIN [SourceERP].dbo.[tblProductClassGroup] AS [Class_Group_48] ON [Class_Group_48].[ProductClassGroupID] = u2.[Class_Group]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Inseam_49] ON [Inseam_49].EntityInfoID = 4675 AND [Inseam_49].InternalKey = u2.[Inseam]
    LEFT JOIN [SourceERP].dbo.[tblCountry] AS [Manufacturer_COO_50] ON [Manufacturer_COO_50].[Country_Id] = u2.[Manufacturer_COO]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Garment_Factory_51] ON [Garment_Factory_51].EntityInfoID = 4655 AND [Garment_Factory_51].InternalKey = u2.[Garment_Factory]
    LEFT JOIN [TenantDB_PLM26].dbo.[Plm_tblSketch] AS [ddl_52] ON [ddl_52].[SketchID] = u2.[ddl]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Special_Customer_53] ON [Special_Customer_53].EntityInfoID = 4768 AND [Special_Customer_53].InternalKey = u2.[Special_Customer]
    LEFT JOIN [SourceERP].dbo.[tblSellingPeriod] AS [Selling_Period_54] ON [Selling_Period_54].[SellingPeriod_Id] = u2.[Selling_Period]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Private_Label_55] ON [Private_Label_55].EntityInfoID = 4736 AND [Private_Label_55].InternalKey = u2.[Private_Label]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Vendor_56] ON [Vendor_56].EntityInfoID = 4722 AND [Vendor_56].InternalKey = u2.[Vendor]
    LEFT JOIN [SourceERP].dbo.[View_tblGender] AS [Gender_7171_57] ON [Gender_7171_57].[KeyValue] = u2.[Gender_7171]
    LEFT JOIN dbo.AppEntitySimpleListValue AS [Subcategory_58] ON [Subcategory_58].EntityInfoID = 4738 AND [Subcategory_58].InternalKey = u2.[Subcategory]
    LEFT JOIN [SourceERP].dbo.[tblSellingPeriod] AS [ERP_Season_59] ON [ERP_Season_59].[SellingPeriod_Id] = u2.[ERP_Season]
    WHERE [u1].[ReferenceId] = @MainReferenceId;
END
GO
