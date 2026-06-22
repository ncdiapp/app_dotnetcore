

/****** Object:  Table [dbo].[Plm_Attributes]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Attributes](
	[ReferenceId] [int] NOT NULL,
	[Knit_Type_3348] [int] NULL,
	[Wales_3350] [int] NULL,
	[Bf_Wash_Swt_Weight_3355] [decimal](18, 2) NULL,
	[Aft_Wash_Swt_Weight_3357] [decimal](18, 2) NULL,
	[Weave_Type_3359] [int] NULL,
	[Warp_Yarns_p_in_3375] [nvarchar](255) NULL,
	[Weft_Yarns_p_in_3376] [nvarchar](255) NULL,
	[Cuttable_Width_3381] [decimal](18, 2) NULL,
	[Yarn_Size_3385] [int] NULL,
	[Courses_3389] [int] NULL,
	[Rib_Knit_Stitches_5115] [int] NULL,
	[Yarn_Ply_5116] [int] NULL,
	[WPI_wrap_per_inch_5117] [int] NULL,
	[Knit_Machine_Type_5118] [int] NULL,
	[Stretch_Type_5119] [int] NULL,
	[Thread_Count_Construction_5122] [nvarchar](255) NULL,
	[Yarn_Type_5123] [int] NULL,
	[Twist_type_5124] [int] NULL,
	[Denier_Size_5127] [int] NULL,
	[Pick_Count_5128] [int] NULL,
	[Pile_Attribute_5129] [int] NULL,
	[Non_Conventional_Weave_5132] [int] NULL,
	[Fill_Power_5133] [int] NULL,
	[Hand_feel_5134] [int] NULL,
	[Appearance_5135] [int] NULL,
	[Drape_5136] [int] NULL,
	[Comments_5137] [nvarchar](255) NULL,
	[Coordinator_Comments_5138] [nvarchar](255) NULL,
	[Wales_5141] [nvarchar](255) NULL,
	[Fill_Weight_5143] [decimal](18, 2) NULL,
	[Properties_5145] [int] NULL,
	[Denim_Dyes_6809] [int] NULL,
	[Fabric_Type_6811] [int] NULL,
	[Yarn_Spinning_6849] [int] NULL,
	[Gauge_6854] [int] NULL,
	[Knitting_Stitches_6867] [int] NULL,
	[Purl_Knit_Stitches_6868] [int] NULL,
	[Knit_Design_6869] [int] NULL,
	[Wash_6871] [int] NULL,
	[Denim_Base_Colors_6873] [int] NULL,
	[Weave_Measurement_6874] [int] NULL,
	[Woven_Designs_6875] [int] NULL,
	[Non_Woven_Type_6876] [int] NULL,
	[Shrinkage_Warp_6877] [nvarchar](255) NULL,
	[Shrinkage_Weft_6878] [nvarchar](255) NULL,
	[Growth_Warp_6879] [nvarchar](255) NULL,
	[Growth_Weft_6880] [nvarchar](255) NULL,
	[Denim_Fits_6901] [int] NULL,
	[Composition1_7064] [decimal](18, 1) NULL,
	[Composition2_7067] [decimal](18, 1) NULL,
	[Composition3_7070] [decimal](18, 1) NULL,
	[Composition4_7073] [decimal](18, 1) NULL,
	[Composition5_7076] [decimal](18, 1) NULL,
	[Comp1_7079] [int] NULL,
	[Weight_7141] [decimal](18, 3) NULL,
	[Width_7144] [decimal](18, 2) NULL,
	[Composition6_7314] [decimal](18, 1) NULL,
	[UOM_3356] [int] NULL,
	[UOM_3358] [int] NULL,
	[UOM_3382] [int] NULL,
	[Denim_Category_5130] [int] NULL,
	[UOM_5139] [int] NULL,
	[UOM_5140] [int] NULL,
	[UOM_5142] [int] NULL,
	[UOM_5144] [int] NULL,
	[UOM_6855] [int] NULL,
	[Finish_6872] [int] NULL,
	[Compositionfiber1_7065] [int] NULL,
	[Compositionfiber2_7068] [int] NULL,
	[Compositionfiber3_7071] [int] NULL,
	[Compositionfiber4_7074] [int] NULL,
	[Compositionfiber5_7077] [int] NULL,
	[Comp2_7080] [int] NULL,
	[Weight_Unit_7142] [int] NULL,
	[Width_Unit_7145] [int] NULL,
	[Compositionfiber6_7315] [int] NULL,
	[state_7066] [nvarchar](255) NULL,
	[state_7069] [nvarchar](255) NULL,
	[state_7072] [nvarchar](255) NULL,
	[state_7075] [nvarchar](255) NULL,
	[state_7078] [nvarchar](255) NULL,
	[Comp3_7081] [int] NULL,
	[Per_7137] [int] NULL,
	[Per_7138] [int] NULL,
	[Per_7139] [int] NULL,
	[Per_7140] [int] NULL,
	[Per_7143] [int] NULL,
	[Per_7146] [int] NULL,
	[state_7316] [nvarchar](255) NULL,
	[Comp4_7100] [int] NULL,
	[Comp5_7101] [int] NULL,
	[Comp6_7325] [int] NULL,
	[Fiber1CB_7082] [int] NULL,
	[Fiber2CB_7083] [int] NULL,
	[Fiber3CB_7084] [int] NULL,
	[Fiber4CB_7102] [int] NULL,
	[Fiber5CB_7103] [int] NULL,
	[Fiber6CB_7326] [int] NULL,
	[Fiber1IB_7085] [nvarchar](255) NULL,
	[Fiber2IB_7086] [nvarchar](255) NULL,
	[Fiber3IB_7087] [nvarchar](255) NULL,
	[Fiber4IB_7104] [nvarchar](255) NULL,
	[Fiber5IB_7105] [nvarchar](255) NULL,
	[Fiber6IB_7327] [nvarchar](255) NULL,
	[Comp_ok_7088] [bit] NULL,
	[Total_Composition_7089] [nvarchar](255) NULL,
	[CompositionDDL_7090] [int] NULL,
	[Valid_Selection_7091] [bit] NULL,
	[CompositionTXT_7092] [nvarchar](255) NULL,
	[Percent_Chk_7099] [bit] NULL,
	[comp1ok_7093] [bit] NULL,
	[comp2ok_7094] [bit] NULL,
	[comp3ok_7095] [bit] NULL,
	[comp4ok_7106] [bit] NULL,
	[comp5ok_7107] [bit] NULL,
	[comp6ok_7328] [bit] NULL,
	[Comp1_Tx_7096] [nvarchar](255) NULL,
	[Comp2_Tx_7097] [nvarchar](255) NULL,
	[Comp3_Tx_7098] [nvarchar](255) NULL,
	[Comp4_Tx_7108] [nvarchar](255) NULL,
	[Comp5_Tx_7109] [nvarchar](255) NULL,
	[Comp6_Tx_7329] [nvarchar](255) NULL,
	[Main_Fabric_Composition_7159] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Colorways]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Colorways](
	[ReferenceId] [int] NOT NULL,
	[Security_Group_3153] [int] NULL,
	[Color_Status_5266] [int] NULL,
	[Comments_5269] [nvarchar](255) NULL,
	[Created_by_7111] [int] NULL,
	[Colors_7354] [nvarchar](255) NULL,
	[Created_by_3154] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Denim_Non_Denim_Tracker_5151]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Denim_Non_Denim_Tracker_5151](
	[RowId] [int] IDENTITY(1,1) NOT NULL,
	[ReferenceId] [int] NOT NULL,
	[Sort] [int] NULL,
	[Designer_7771] [int] NULL,
	[SS_7772] [int] NULL,
	[Fabric_Type_7799] [int] NULL,
	[Fabric_Mill_7773] [int] NULL,
	[Fabric_7774] [nvarchar](255) NULL,
	[Mill_Article_7775] [nvarchar](255) NULL,
	[Final_Fabric_Content_7776] [nvarchar](255) NULL,
	[Final_Weight_7777] [int] NULL,
	[Color_Combo_7778] [nvarchar](255) NULL,
	[Original_Ref_7779] [nvarchar](255) NULL,
	[Denim_Base_Color_7800] [int] NULL,
	[Wash_Code_7801] [nvarchar](255) NULL,
	[Color_7780] [int] NULL,
	[Sample_Type_7781] [int] NULL,
	[Req_Date_7782] [datetime] NULL,
	[Mill_Send_Date_7783] [datetime] NULL,
	[Rcvd_Date_7784] [datetime] NULL,
	[Comments_Date_7785] [datetime] NULL,
	[Comments_7786] [nvarchar](255) NULL,
	[Status_7787] [int] NULL,
	[SMS_Qty_YD_or_M_7788] [nvarchar](255) NULL,
	[Delivery_Date_7789] [datetime] NULL,
	[Delivery_Status_7790] [int] NULL,
	[Send_to_7793] [int] NULL,
	[Vendor_7794] [int] NULL,
	[Wash_Leg_Send_Date_7802] [datetime] NULL,
	[Wash_Leg_Status_7803] [int] NULL,
	[Raw_Material_Status_7795] [int] NULL,
	[Weight_Unit_7796] [int] NULL,
	[Description_7797] [nvarchar](255) NULL,
	[Comments_7798] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[RowId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Fabric_Cost]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Fabric_Cost](
	[ReferenceId] [int] NOT NULL,
	[Price_By_111] [int] NULL,
	[Final_Fabric_Cost_Meter_5094] [int] NULL,
	[Rmb_Cost_Per_Yard_7054] [int] NULL,
	[Final_Fabric_Cost_Yard_5336] [int] NULL,
	[Rmb_Cost_Per_Meter_7055] [int] NULL,
	[Fabric_Price_By_7203] [int] NULL,
	[Yarn_Price_Lbs_5338] [int] NULL,
	[Rmb_Payment_Terms_7056] [int] NULL,
	[Fabric_Final_Cost_Meter_7204] [decimal](18, 2) NULL,
	[Yarn_Price_Kg_5337] [int] NULL,
	[Remarks_7061] [nvarchar](255) NULL,
	[Fabric_Final_Cost_Yard_7208] [decimal](18, 2) NULL,
	[Dye_Surcharge_7114] [nvarchar](255) NULL,
	[Currency_6828] [int] NULL,
	[Payment_Term_5339] [int] NULL,
	[MOQ_6845] [nvarchar](255) NULL,
	[MCQ_6846] [nvarchar](255) NULL,
	[Dev_Fees_7119] [nvarchar](255) NULL,
	[Lead_Time_6847] [nvarchar](255) NULL,
	[Remarks_6848] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Fabric_Header]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Fabric_Header](
	[ReferenceId] [int] NOT NULL,
	[Composition_47] [int] NULL,
	[Division_8] [int] NULL,
	[Article_22] [nvarchar](255) NULL,
	[Division_186] [int] NULL,
	[Security_Group_3094] [int] NULL,
	[Raw_Material_Status_3128] [int] NULL,
	[Fabric_Type_3130] [int] NULL,
	[Fabric_Mill_3133] [nvarchar](255) NULL,
	[Item_Type_3708] [int] NULL,
	[Supplier_Article_Number_3916] [nvarchar](255) NULL,
	[Weight_4135] [decimal](18, 2) NULL,
	[Composition1_4922] [decimal](18, 1) NULL,
	[Composition2_4924] [decimal](18, 1) NULL,
	[Composition3_4926] [decimal](18, 1) NULL,
	[Comp1_4928] [int] NULL,
	[Active_Inactive_5086] [int] NULL,
	[Comments_5098] [nvarchar](255) NULL,
	[Composition4_5099] [decimal](18, 1) NULL,
	[Composition5_5102] [decimal](18, 1) NULL,
	[Fabric_Name_5333] [int] NULL,
	[Classification_1] [int] NULL,
	[Designer_3095] [int] NULL,
	[State_3129] [int] NULL,
	[Sub_Type_3790] [int] NULL,
	[Weight_Unit_4136] [int] NULL,
	[Compositionfiber1_4923] [int] NULL,
	[Compositionfiber2_4925] [int] NULL,
	[Compositionfiber3_4927] [int] NULL,
	[Comp2_4929] [int] NULL,
	[Compositionfiber4_5100] [int] NULL,
	[Compositionfiber5_5103] [int] NULL,
	[Name_7028] [nvarchar](255) NULL,
	[Fabric_Name_txt_7359] [nvarchar](255) NULL,
	[Comp3_4930] [int] NULL,
	[state_5078] [nvarchar](255) NULL,
	[state_5079] [nvarchar](255) NULL,
	[state_5080] [nvarchar](255) NULL,
	[state_5101] [nvarchar](255) NULL,
	[state_5104] [nvarchar](255) NULL,
	[Per_7135] [int] NULL,
	[Designer_1_7202] [int] NULL,
	[Subcategory_7361] [int] NULL,
	[French_Name_7366] [nvarchar](255) NULL,
	[ProductTypeGroup_149] [int] NULL,
	[Comp4_5105] [int] NULL,
	[French_6745] [nvarchar](255) NULL,
	[Designer_2_7201] [int] NULL,
	[Description_23] [nvarchar](255) NULL,
	[Comp5_5106] [int] NULL,
	[ProductTypeGroup_txt_7352] [nvarchar](255) NULL,
	[Product_Type_2] [int] NULL,
	[Long_Description_121] [nvarchar](255) NULL,
	[Comp6_7320] [int] NULL,
	[Fiber1CB_4931] [int] NULL,
	[Product_Type_txt_7030] [nvarchar](255) NULL,
	[Product_Manager_109] [int] NULL,
	[Fiber2CB_4932] [int] NULL,
	[Fiber3CB_4933] [int] NULL,
	[Fiber4CB_5107] [int] NULL,
	[Fiber5CB_5108] [int] NULL,
	[Fiber6CB_7321] [int] NULL,
	[Fiber1IB_4934] [nvarchar](255) NULL,
	[Fiber2IB_4935] [nvarchar](255) NULL,
	[Fiber3IB_4936] [nvarchar](255) NULL,
	[Fiber4IB_5109] [nvarchar](255) NULL,
	[Fiber5IB_5110] [nvarchar](255) NULL,
	[Fiber6IB_7322] [nvarchar](255) NULL,
	[Comp_ok_4937] [bit] NULL,
	[Total_Composition_4938] [nvarchar](255) NULL,
	[CompositionDDL_4980] [int] NULL,
	[Valid_Selection_4983] [bit] NULL,
	[Percent_Chk_5081] [bit] NULL,
	[CompositionTXT_4998] [nvarchar](255) NULL,
	[comp1ok_4999] [bit] NULL,
	[comp2ok_5000] [bit] NULL,
	[comp3ok_5001] [bit] NULL,
	[comp4ok_5111] [bit] NULL,
	[comp5ok_5112] [bit] NULL,
	[comp6ok_7323] [bit] NULL,
	[Comp1_Tx_5068] [nvarchar](255) NULL,
	[Comp2_Tx_5069] [nvarchar](255) NULL,
	[Comp3_Tx_5070] [nvarchar](255) NULL,
	[Comp4_Tx_5113] [nvarchar](255) NULL,
	[Comp5_Tx_5114] [nvarchar](255) NULL,
	[Comp6_Tx_7324] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Fabric_Info]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Fabric_Info](
	[ReferenceId] [int] NOT NULL,
	[Season_3] [int] NULL,
	[Division_8] [int] NULL,
	[Size_Range_10] [int] NULL,
	[Dimension_11] [int] NULL,
	[Article_22] [nvarchar](255) NULL,
	[Created_Date_181] [datetime] NULL,
	[Division_186] [int] NULL,
	[Security_Group_3094] [int] NULL,
	[Security_Group_3098] [int] NULL,
	[Print_Solid_3127] [int] NULL,
	[Raw_Material_Status_3128] [int] NULL,
	[Fabric_Type_3130] [int] NULL,
	[Item_Type_3708] [int] NULL,
	[SupplierType_3896] [int] NULL,
	[Publish_to_ERP_3914] [bit] NULL,
	[Published_to_ERP_3915] [bit] NULL,
	[Supplier_Article_Number_3916] [nvarchar](255) NULL,
	[Publish_Failed_to_ERP_3920] [bit] NULL,
	[Width_4133] [decimal](18, 2) NULL,
	[Weight_4135] [decimal](18, 2) NULL,
	[Publish_to_ERP_Message_4184] [nvarchar](255) NULL,
	[Composition1_4922] [decimal](18, 1) NULL,
	[Composition2_4924] [decimal](18, 1) NULL,
	[Composition3_4926] [decimal](18, 1) NULL,
	[Comp1_4928] [int] NULL,
	[Original_Reference_5082] [nvarchar](255) NULL,
	[New_Carryover_5083] [int] NULL,
	[Is_this_Greige_5084] [int] NULL,
	[Greige_Type_5085] [int] NULL,
	[Active_Inactive_5086] [int] NULL,
	[Finish_1_5087] [int] NULL,
	[Finish_2_5088] [nvarchar](255) NULL,
	[Fabric_COO_5089] [int] NULL,
	[Coordinator_5090] [int] NULL,
	[Parent_Child_5092] [int] NULL,
	[RM_Risk_Commnet_5093] [int] NULL,
	[Security_Group_5097] [int] NULL,
	[Comments_5098] [nvarchar](255) NULL,
	[Composition4_5099] [decimal](18, 1) NULL,
	[Composition5_5102] [decimal](18, 1) NULL,
	[Product_Class_5232] [int] NULL,
	[Product_Class_5262] [int] NULL,
	[Fabric_Name_5333] [int] NULL,
	[Dye_Type_5334] [int] NULL,
	[Fabric_Mill_6790] [int] NULL,
	[ddl_7045] [int] NULL,
	[Vendor_7147] [int] NULL,
	[Composition6_7317] [decimal](18, 1) NULL,
	[Classification_1] [int] NULL,
	[Sketch_6] [nvarchar](255) NULL,
	[Size_Detail_Dispaly_150] [nvarchar](255) NULL,
	[Created_By_189] [nvarchar](255) NULL,
	[Designer_3095] [int] NULL,
	[Sourcing_3099] [int] NULL,
	[State_3129] [int] NULL,
	[Sub_Type_3790] [int] NULL,
	[Notes_3897] [nvarchar](255) NULL,
	[Width_Unit_4134] [int] NULL,
	[Weight_Unit_4136] [int] NULL,
	[Compositionfiber1_4923] [int] NULL,
	[Compositionfiber2_4925] [int] NULL,
	[Compositionfiber3_4927] [int] NULL,
	[Comp2_4929] [int] NULL,
	[Product_Code_5021] [nvarchar](255) NULL,
	[QC_Team_5091] [int] NULL,
	[Security_Group_5096] [int] NULL,
	[Compositionfiber4_5100] [int] NULL,
	[Compositionfiber5_5103] [int] NULL,
	[Collection_5233] [int] NULL,
	[Class_Group_5263] [int] NULL,
	[Other_Risks_5335] [nvarchar](255) NULL,
	[Garment_Factory_6887] [int] NULL,
	[Name_7028] [nvarchar](255) NULL,
	[Free_text_7115] [nvarchar](255) NULL,
	[Compositionfiber6_7318] [int] NULL,
	[Fabric_Name_txt_7359] [nvarchar](255) NULL,
	[ERP_Season_7362] [int] NULL,
	[Collection_4] [int] NULL,
	[Sample_Size_Detail_139] [int] NULL,
	[Last_Revised_Date_182] [datetime] NULL,
	[Comp3_4930] [int] NULL,
	[Size_Range_5022] [int] NULL,
	[state_5078] [nvarchar](255) NULL,
	[state_5079] [nvarchar](255) NULL,
	[state_5080] [nvarchar](255) NULL,
	[state_5101] [nvarchar](255) NULL,
	[state_5104] [nvarchar](255) NULL,
	[sketch_id_7043] [nvarchar](255) NULL,
	[Per_7135] [int] NULL,
	[Per_7136] [int] NULL,
	[Designer_1_7202] [int] NULL,
	[state_7319] [nvarchar](255) NULL,
	[Subcategory_7361] [int] NULL,
	[French_Name_7366] [nvarchar](255) NULL,
	[ProductTypeGroup_149] [int] NULL,
	[Last_Revised_By_190] [nvarchar](255) NULL,
	[DivisionBlock_5023] [int] NULL,
	[Comp4_5105] [int] NULL,
	[French_6745] [nvarchar](255) NULL,
	[Collection_txt_7125] [nvarchar](255) NULL,
	[Designer_2_7201] [int] NULL,
	[Group_5] [int] NULL,
	[Description_23] [nvarchar](255) NULL,
	[Product_Class_5024] [int] NULL,
	[Comp5_5106] [int] NULL,
	[ProductTypeGroup_txt_7352] [nvarchar](255) NULL,
	[Product_Type_2] [int] NULL,
	[Long_Description_121] [nvarchar](255) NULL,
	[Dimension_5025] [int] NULL,
	[Comp6_7320] [int] NULL,
	[Fiber1CB_4931] [int] NULL,
	[Season_5026] [int] NULL,
	[Product_Type_txt_7030] [nvarchar](255) NULL,
	[Product_Manager_109] [int] NULL,
	[Fiber2CB_4932] [int] NULL,
	[Price_Type_5027] [int] NULL,
	[Fiber3CB_4933] [int] NULL,
	[First_Cost_Currency_5028] [int] NULL,
	[Valid_Size_Selection_5029] [bit] NULL,
	[Fiber4CB_5107] [int] NULL,
	[Valid_Product_Code_Selection_5030] [bit] NULL,
	[Fiber5CB_5108] [int] NULL,
	[Valid_DivisionBlock_Selection_5031] [bit] NULL,
	[Fiber6CB_7321] [int] NULL,
	[Fiber1IB_4934] [nvarchar](255) NULL,
	[Valid_Product_Class_Selection_5032] [bit] NULL,
	[Fiber2IB_4935] [nvarchar](255) NULL,
	[Valid_Dimension_Selection_5033] [bit] NULL,
	[Fiber3IB_4936] [nvarchar](255) NULL,
	[Valid_Season_Selection_5034] [bit] NULL,
	[Valid_Price_Type_Selection_5035] [bit] NULL,
	[Fiber4IB_5109] [nvarchar](255) NULL,
	[Valid_First_Cost_Currency_Selection_5036] [bit] NULL,
	[Fiber5IB_5110] [nvarchar](255) NULL,
	[Color_5037] [int] NULL,
	[Fiber6IB_7322] [nvarchar](255) NULL,
	[Comp_ok_4937] [bit] NULL,
	[Valid_Color_Selection_5038] [bit] NULL,
	[Total_Composition_4938] [nvarchar](255) NULL,
	[Active_Count_5041] [int] NULL,
	[CompositionDDL_4980] [int] NULL,
	[DimensionColorSizeActiveBooleanSum_5042] [bit] NULL,
	[Valid_Selection_4983] [bit] NULL,
	[Percent_Chk_5081] [bit] NULL,
	[CompositionTXT_4998] [nvarchar](255) NULL,
	[comp1ok_4999] [bit] NULL,
	[comp2ok_5000] [bit] NULL,
	[comp3ok_5001] [bit] NULL,
	[comp4ok_5111] [bit] NULL,
	[comp5ok_5112] [bit] NULL,
	[comp6ok_7323] [bit] NULL,
	[Comp1_Tx_5068] [nvarchar](255) NULL,
	[Comp2_Tx_5069] [nvarchar](255) NULL,
	[Comp3_Tx_5070] [nvarchar](255) NULL,
	[Comp4_Tx_5113] [nvarchar](255) NULL,
	[Comp5_Tx_5114] [nvarchar](255) NULL,
	[Comp6_Tx_7324] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Fabric_Policy]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Fabric_Policy](
	[ReferenceId] [int] NOT NULL,
	[Policy_File_7360] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Plm_Testing_Compliance]    Script Date: 6/22/2026 8:56:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Plm_Testing_Compliance](
	[ReferenceId] [int] NOT NULL,
	[Bulk_Fabric_Status_5197] [int] NULL,
	[Quality_Standards_5204] [nvarchar](255) NULL,
	[Standard_Bodies_5208] [int] NULL,
	[Bulk_Fabric_Approved_Date_5198] [datetime] NULL,
	[QA_Status_5205] [int] NULL,
	[Standard_Claim_5209] [int] NULL,
	[Production_Date_5199] [datetime] NULL,
	[Approved_by_5206] [int] NULL,
	[Certification_Year_5210] [nvarchar](255) NULL,
	[Lot_5200] [nvarchar](255) NULL,
	[Comments_5207] [nvarchar](255) NULL,
	[Release_to_Manufacturer_5201] [nvarchar](255) NULL,
	[Lot_5202] [nvarchar](255) NULL,
	[Release_to_Manufacturer_5203] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReferenceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Plm_Attributes]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Attributes_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Attributes] CHECK CONSTRAINT [FK_Plm_Attributes_Ref]
GO

ALTER TABLE [dbo].[Plm_Colorways]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Colorways_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Colorways] CHECK CONSTRAINT [FK_Plm_Colorways_Ref]
GO

ALTER TABLE [dbo].[Plm_Denim_Non_Denim_Tracker_5151]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Denim_Non_Denim_Tracker_5151_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Denim_Non_Denim_Tracker_5151] CHECK CONSTRAINT [FK_Plm_Denim_Non_Denim_Tracker_5151_Ref]
GO

ALTER TABLE [dbo].[Plm_Fabric_Cost]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Fabric_Cost_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Fabric_Cost] CHECK CONSTRAINT [FK_Plm_Fabric_Cost_Ref]
GO

ALTER TABLE [dbo].[Plm_Fabric_Header]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Fabric_Header_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Fabric_Header] CHECK CONSTRAINT [FK_Plm_Fabric_Header_Ref]
GO

ALTER TABLE [dbo].[Plm_Fabric_Info]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Fabric_Info_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Fabric_Info] CHECK CONSTRAINT [FK_Plm_Fabric_Info_Ref]
GO

ALTER TABLE [dbo].[Plm_Fabric_Policy]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Fabric_Policy_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Fabric_Policy] CHECK CONSTRAINT [FK_Plm_Fabric_Policy_Ref]
GO

ALTER TABLE [dbo].[Plm_Testing_Compliance]  WITH CHECK ADD  CONSTRAINT [FK_Plm_Testing_Compliance_Ref] FOREIGN KEY([ReferenceId])
REFERENCES [dbo].[Plm_ReferenceBasicInfo] ([ReferenceId])
GO

ALTER TABLE [dbo].[Plm_Testing_Compliance] CHECK CONSTRAINT [FK_Plm_Testing_Compliance_Ref]
GO


