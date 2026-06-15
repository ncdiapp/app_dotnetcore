using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;


namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class FormLayoutDto
    {


        // master detail form Id
        [DataMember]
        public int? FormID
        {
            get;
            set;
        }

        [DataMember]
        public List<string> IsChangedNeedToCascadingFiedIds
        {
            get;
            set;
        }


        [DataMember]
        public List<string> IsUsedCascadingDataSourceFiedIds
        {
            get;
            set;
        }


        //[DataMember]
        //public List<string> IsUsedCascadingDataSourceUnitIds
        //{
        //    get;
        //    set;
        //}


        //key:FieldId, value: true false, must used FieldId as key !!
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneLockFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneConditionHideFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsLockTransaction
        {
            get;
            set;
        }

        //key:UnitId, value: true false
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictLockUnitIds
        {
            get;
            set;
        }



        //key:DB FieldName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
        {
            get;
            set;
        }


      
        //key:FiledID : for BL server side usage
        [IgnoreDataMember]
        public Dictionary<int, object> DictRootAndSiblingFieldValue
        {
            get;
            set;
        }
        // optional
        private Dictionary<string, Dictionary<string, object>> _DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>();

        //Key: sibling Tanscation unit ID toString()
        // value for one 
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, object>> DictSiblingOneToOneFields
        {
            get
            {
                return _DictSiblingOneToOneFields;
            }
            set
            {
                _DictSiblingOneToOneFields = value;

            }
        }


        //Key: Tanscation unit ID toString()
        //aChildTransactionUnitExDto.Id.ToString()
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<AppChildDataDto>> DictOneToManyFields
        {
            get;
            set;
        }


		//Key1: Tanscation unit ID toString()
		//Key2: MasterFieldlId
		//Value lookup Item
		[DataMember(EmitDefaultValue = false)]
		public Dictionary<string, Dictionary<String, List<LookupItemDto>>> DictForeignUnitMasterFieldlIdLookupItem
		{
			get;
			set;
		}




		//Key: Tanscation unit ID toString()
		//aChildTransactionUnitExDto.Id.ToString()
		[DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<AppChildDataDto>> EditCloneDictOneToManyFields
        {
            get;
            set;
        }



        [DataMember]
        public bool IsDirty
        {
            get;
            set;
        }


        [DataMember]
        public bool IsNew
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public object RootPrimaryKeyValue
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public object FormTitleDisplay
        {
            get;
            set;
        }

        public static List<Dictionary<string, object>> ConvertOneDataTableToClientDataRowDict(DataTable dt, Dictionary<string, int> dictFieldNameAndDataType)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (DataRow dr in dt.Rows)
            {
                row = ConvertOneDataRowToClientDictRow(dr, dictFieldNameAndDataType);
                rows.Add(row);
            }

            return rows;

        }

        // Key: TranscationFiledId.ToString()
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<String, List<LookupItemDto>> DictCascadingFiledDataSource
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public Dictionary<String, List<LookupItemDto>> DictAutoCompleteFieldDataSource
        //{
        //    get;
        //    set;
        //}


        public static Dictionary<string, object> ConvertOneDataRowToClientDictRow(DataRow dr, Dictionary<string, int> dictFieldNameAndDataType)
        {
            DataTable dt = dr.Table;

            Dictionary<string, object> row;
            row = new Dictionary<string, object>();
            foreach (DataColumn col in dt.Columns)
            {
                object value = dr[col];
                if (value == DBNull.Value)
                {
                    value = null;
                }

                if (value != null)
                {
                    if (dictFieldNameAndDataType.ContainsKey(col.ColumnName))
                    {
                        int dataType = dictFieldNameAndDataType[col.ColumnName];
                        if ( dataType == (int)EmAppDataType.DateTime)
                        {
                            DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(value);

                            if (dateTime.HasValue)
                            {
                                value = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);
                            }


                        }
                    }
                    else if (col.DataType != null && (col.DataType.Name == "DateTime" ))
                    {
                        DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(value);

                        if (dateTime.HasValue)
                        {
                            value = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);
                        }
                    }                   
                }

                row.Add(col.ColumnName, value);
            }


            return row;
        }



        [DataMember]
        public int TransactionId
        {
            get;
            set;
        }



        [DataMember]
        public int? CurrentCascadingFieldId
        {
            get;
            set;
        }

        [DataMember]
        public int? CurrentCascadingUnitId
        {
            get;
            set;
        }







        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictDocumentIdFileCode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictFolderIdAndPath
        {
            get;
            set;
        }



        [DataMember]
        public object RootUnitId
        {
            get;
            set;
        }


        [DataMember]
        public int? ExternalMethodRegId
        {
            get;
            set;
        }


     
      




        [DataMember(EmitDefaultValue = false)]
        public AppMasterDetailValidationDto ValidationResultDto
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<string>> DictFormulaTypeAndWarningMessage
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, int> DictTransFieldIdAndWarningHighlightStyleId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CurrentUserId
        {
            get;
            set;
        }

		[DataMember(EmitDefaultValue = false)]
		public int? TransactionCommandId
		{
			get;
			set;
		}



        [DataMember]
        public List<int> ValidationLimittedTransFieldIdList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public CalendarRepeatSettingDto CalendarRepeatSetting
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String CalendarRepeatToken
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CalendarRepeatSettingApplyToRange
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsIgnoreCalendarRepeat
        {
            get;
            set;
        }

        //key:FiledID : Key FiledId, KeyValuePair<OrgVale, currentValue>
        [IgnoreDataMember]
        public Dictionary<int, KeyValuePair<object, object>> DictRootAndSiblingChangedField
        {
            get;
            set;
        }

        // only copy value frin ClientinputMasterdeail dto

        private HashSet<int> _ChildChangedUnitIds = new HashSet<int>();
        [IgnoreDataMember]
        public HashSet<int> ChildChangedUnitIds
        {
            get
            {
                return _ChildChangedUnitIds;
            }

        }


        //key:ChildunitID : Key FiledId, KeyValuePair<OrgVale, currentValue>


        //key:unitID : Key UnitId, KeyValuePair<OrgVale, currentValue>
        [IgnoreDataMember]
        public Dictionary<int, Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>>> DictChildUnitIdChangedCollection
        {
            get;
            set;
        }

    
        private HashSet<int> _SourceChildChangedUnitIds = new HashSet<int>();

        [IgnoreDataMember]
        public HashSet<int> SourceChildChangedUnitIds
        {
            get
            {
                if(! DictChildUnitIdChangedCollection.IsEmpty())
                {
                    if(_SourceChildChangedUnitIds.IsEmpty())
                    {
                        foreach (int unitId in DictChildUnitIdChangedCollection.Keys)
                        {
                            var childCollectionChange = DictChildUnitIdChangedCollection[unitId];
                            if (IsChildChangedCollection(childCollectionChange))
                            {
                                _SourceChildChangedUnitIds.Add(unitId);
                            }
                        }
                    }
                    
                }
               
                return _SourceChildChangedUnitIds;
            }
        }

       

        private bool IsChildChangedCollection(Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>>  childChangedCollection)
        {
           
                if (childChangedCollection != null)
                {
                    return (
                              (!childChangedCollection.Item1.IsEmpty())
                            || (!childChangedCollection.Item2.IsEmpty())
                            || (!childChangedCollection.Item3.IsEmpty())
                        );

                }

                return false;
            

        }

    }


}
