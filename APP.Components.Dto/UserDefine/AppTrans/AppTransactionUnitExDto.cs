using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionUnitExDto
    {
		partial void OnInitialized()
		{
			Children = new List<AppTransactionUnitExDto>();
			PrimaryKeyDbfieldList = new List<string>();

			



		}

		public int Level { get; set; }

		//public string LinkToParentPrimaryKeyDbfield
		//{
		//	get;
		//	set;
		//}

		// Key: current table foreign key DB file; value: ParentPrimaryKeyDb filed
		public Dictionary<string, string> DictLinkToParentKeyDbfield
		{
			get;
			set;
		}
	

		private Dictionary<string, int> _DictDbFieldSysToken = null;

		[IgnoreDataMember]
		public Dictionary<string, int> DictDbFieldSysToken
		{
			get
			{
				if (_DictDbFieldSysToken == null)
				{
					_DictDbFieldSysToken = this.AppTransactionFieldList.OrderBy(o => o.SortOrder).Where(o => o.MappingEmSystemTokenField.HasValue)
						.ToDictionary(o =>o.DataBaseFieldName , o => o.MappingEmSystemTokenField.Value);
				}

				return _DictDbFieldSysToken;
			}
		}

        private Dictionary<string, int> _DictExtendDbField_Id = null;

        [IgnoreDataMember]
        public Dictionary<string, int> DictExtendDbField_Id
        {
            get
            {
                if (_DictExtendDbField_Id == null)
                {
                    _DictExtendDbField_Id = this.AppTransactionFieldList
                        .Where(o => o.IsStoreToExtendTable.HasValue && o.IsStoreToExtendTable.Value==true)
                        .ToDictionary(o => o.DataBaseFieldName, o => (int) o.Id);
                }

                return _DictExtendDbField_Id;
            }
        }


        private Dictionary<int, string> _DictExtenId_DbField = null;

        [IgnoreDataMember]
        public Dictionary<int, string> DictExtenId_DbField
        {
            get
            {
                if (_DictExtenId_DbField == null)
                {
                    _DictExtenId_DbField = new Dictionary<int, string>();
                    foreach ( var  pair in DictExtendDbField_Id)
                    {
                        _DictExtenId_DbField.Add(pair.Value, pair.Key);
                    }
                }

                return _DictExtenId_DbField;
            }
        }

        public List<string> PrimaryKeyDbfieldList
		{
			get;
			set;
		}

		public bool IsMatrixOrPivot
		{
			get
			{
				return (EmGridViewDisplayType.HasValue && EmGridViewDisplayType.Value == (int)EmAppTransactionGridDisplayType.PivotEditGrid)
					 || (IsMatrixUnit.HasValue && IsMatrixUnit.Value);

			}
		}

		[DataMember(EmitDefaultValue = false)]
		public bool? IsFormLayoutVisible
		{
			get;
			set;
		}



	
		public string  DataContextPrefix
		{
			get;
			set;
		}


        public string ParentDataItemPrefix
        {
            get;
            set;
        }

        public bool IsEditOnPopup
        {
            get;
            set;
        }

       

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionUnitExDto> Children
        {
            get;
            set;
        }

        public List<AppTransactionFieldExDto> RootLevelTriggerFieldList
        {
            get;
            set;
        }

		

		private Dictionary<int, AppTransactionFieldExDto> _DicFieldIdFieldExdto = null;

        public Dictionary<int, AppTransactionFieldExDto> DicFieldIdFieldExdto
        {
            get
            {
                if (_DicFieldIdFieldExdto == null)
                {
                    _DicFieldIdFieldExdto = this.AppTransactionFieldList.OrderBy(o=>o.SortOrder).Where(o=>o.Id != null).ToDictionary(o => (int)o.Id, o => o);
                }

                return _DicFieldIdFieldExdto;
            }
        }

        Dictionary<string, int> _DictDbFileNameFieldId = null;
        public Dictionary<string, int> DictDbFileNameFieldId
        {
            get
            {
                if (_DictDbFileNameFieldId == null)
                {
                    _DictDbFileNameFieldId = AppTransactionFieldList.OrderBy(o => o.SortOrder).Where(o => o.Id != null).ToDictionary(o => o.DataBaseFieldName, o => (int)o.Id);
                }

                return _DictDbFileNameFieldId;
            }
        }

               


        Dictionary<int, string> _DictFieldIdDbFileName = null;
        public Dictionary<int, string> DictFieldIdDbFileName
        {
            get
            {
                if (_DictFieldIdDbFileName == null)
                {
                    _DictFieldIdDbFileName = AppTransactionFieldList.OrderBy(o => o.SortOrder).Where(o => o.Id != null).ToDictionary(o => (int)o.Id, o => o.DataBaseFieldName);
                }

                return _DictFieldIdDbFileName;
            }
        }

        static readonly List<string> SystemTraTokeFiled = new List<string>();

        //public Dictionary<string, int> DictDbFieldSysToken
        //
//AppCreatedByID int Checked
//AppCreatedDate datetime    Checked
//AppModifiedDate datetime Checked
//AppModifiedByID int Checked
//AppCreatedByCompanyID int Checked
        List<string> _ListExcludeSystemTokenDbFileName = null;
        public List<string> ListExcludeSystemTokenDbFileName
        {
            get
            {
                if (_ListExcludeSystemTokenDbFileName == null)
                {
                    _ListExcludeSystemTokenDbFileName = 
                        AppTransactionFieldList.Where( o => ! (DictDbFieldSysToken.ContainsKey(o.DataBaseFieldName)))
                        .Select (o=>o.DataBaseFieldName).ToList ();
                }

                return _ListExcludeSystemTokenDbFileName;
            }
        }

        Dictionary<int, int> _DictTransactionFieldIdControlType = null;
        public Dictionary<int, int> DictTransactionFieldIdControlType
        {
            get
            {
                if (_DictTransactionFieldIdControlType == null)
                {
                    _DictTransactionFieldIdControlType = AppTransactionFieldList.OrderBy(o => o.SortOrder).Where(o => o.Id != null).ToDictionary(o => (int)o.Id, o => o.ControlType); ;
                }

                return _DictTransactionFieldIdControlType;
            }
        }


        Dictionary<string, int> _DictTransFieldDBFiledNameControlType = null;
        public Dictionary<string, int> DictTransFieldDBFiledNameControlType
        {
            get
            {
                if (_DictTransFieldDBFiledNameControlType == null)
                {
                    _DictTransFieldDBFiledNameControlType = AppTransactionFieldList.OrderBy(o => o.SortOrder).ToDictionary(o => o.DataBaseFieldName, o => o.ControlType);
                }

                return _DictTransFieldDBFiledNameControlType;
            }
        }

        Dictionary<string, object> _DictTransactionFieldNameFiledID = null;
        public Dictionary<string, object> DictTransactionFieldNameFielID
        {
            get
            {
                if (_DictTransactionFieldNameFiledID == null)
                {
                    _DictTransactionFieldNameFiledID = AppTransactionFieldList.OrderBy(o => o.SortOrder).Where(o => o.Id != null).ToDictionary(o => o.DataBaseFieldName, o => o.Id ); ;
                }

                return _DictTransactionFieldNameFiledID;
            }
        }

        List<string> _GroupByTransactionFieldList = null;
        public List<string> GroupByTransactionFieldList
        {
            get
            {
                if (_GroupByTransactionFieldList == null)
                {
                    _GroupByTransactionFieldList = AppTransactionFieldList.Where(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value && o.GroupByLevel.HasValue).OrderBy(o => o.GroupByLevel.Value).Select(o => o.DataBaseFieldName).ToList();
                }

                return _GroupByTransactionFieldList;
            }
        }

        private Dictionary<int, List<AppTransactionFieldExDto>> _DictInnerEntityChildFieldExdto;
        public Dictionary<int, List<AppTransactionFieldExDto>> DictInnerEntityChildFieldExdto
        {
            get
            {
                if (_DictInnerEntityChildFieldExdto == null)
                {
                    _DictInnerEntityChildFieldExdto = new Dictionary<int, List<AppTransactionFieldExDto>>();

                    var masterEntityFiledIds = this.AppTransactionFieldList.Where(o => o.MasterEntityFieldlId.HasValue).Select(o => o.MasterEntityFieldlId.Value).Distinct();
                    foreach (int masterFieldId in masterEntityFiledIds)
                    {
                        _DictInnerEntityChildFieldExdto.Add(masterFieldId, this.AppTransactionFieldList.Where(o => o.MasterEntityFieldlId == masterFieldId).ToList());
                    }

                }

                return _DictInnerEntityChildFieldExdto;

            }
        }


        private Dictionary<int, int> _DictChildUnitSubscribeFieldIdParentFieldId;
        public Dictionary<int, int> DictChildUnitSubscribeFieldIdParentFieldId
        {
            get
            {
                if (_DictChildUnitSubscribeFieldIdParentFieldId == null)
                {
                    _DictChildUnitSubscribeFieldIdParentFieldId = this.AppTransactionFieldList.Where(o => o.ChildUnitSubscribeParentFieldId.HasValue).ToDictionary(o => (int)o.Id, o => o.ChildUnitSubscribeParentFieldId.Value);

                }

                return _DictChildUnitSubscribeFieldIdParentFieldId;

            }
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> IsUsedRetrieveDataMappingDataSourceFiedIds
        {
            get;
            set;
        }


        [IgnoreDataMember]
        public List<int> RestrictedTransactionUnitUserActionList
        {
            get;
            set;
        }
        

		[IgnoreDataMember]
		public int?  DataSourceFrom
		{
			get;
			set;
		}


        [IgnoreDataMember]
        public bool IsEditableUnit
        {
           get
            {
                if (this.IsUsedForLoadingAvailableSource.HasValue && this.IsUsedForLoadingAvailableSource.Value  )
                {
                    return false;
                }

               if (this.IsReadOnly.HasValue && this.IsReadOnly.Value)
                {
                    return false;
                }

                if (this.IsVirtualUnit)
                {
                    return false;
                }

                return true;
            }
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsVirtualUnit
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.DataBaseTableName) && string.IsNullOrWhiteSpace(this.DataSourceQuery))
                {
                    return true;
                }

                return false;
            }
            set
            { 
            
            }
        }




        [IgnoreDataMember]
        public AppTransactionUnitExDto AvailableSourceUnitExDto
        {
            get;
            set;
        }
             

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Expression
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string SortingColumnName
        {
            get;
            set;
        }
    }
    


}