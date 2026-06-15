using System;
using System.Collections;
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
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class SearchCriteriaDto : ObservableObject
    {
        public static readonly string CriteriaOperatorProperty = ObjectInfoHelper.GetName<SearchCriteriaDto, CriteriaOperatorDto>(o => o.CriteriaOperator); 

        public SearchCriteriaDto()
        {
            Values = new ObservableCollection<object>();
        }

        // for builtIn table search

        [DataMember(EmitDefaultValue = false)]
        public string SysTableFiledPath
        {
            get;
            set;
        }


		[DataMember(EmitDefaultValue = false)]
		public bool ?  IsSkipSearch
		{
			get;
			set;
		}


		[DataMember(EmitDefaultValue = false)]
		public bool? IsUseAsCandarNavigator
		{
			get;
			set;
		}

        [DataMember(EmitDefaultValue = false)]
        public bool? IsAllowMultipleSelect
        {
            get;
            set;
        }


        

        [DataMember(EmitDefaultValue = false)]
        public int? OperationId
        {
            get;
            set;
        }

        [DataMember]
        public int? ControlType
        {
            get;
            set;
        }


        [DataMember]
        public EmAppCriteriaType CriteriaType
        {
            get;
            set;
        }

        [DataMember]
        public int? CriteriaSubType
        {
            get;
            set;
        }


        [DataMember]
        public int RowIndex
        {
            get;
            set;
        }

        [DataMember]
        public int ColumnIndex
        {
            get;
            set;
        }

        [DataMember]
        public string Display
        {
            get;
            set;
        }

        [DataMember]
        public CriteriaOperatorDto CriteriaOperator
        {
            get
            {
                return GetValue<CriteriaOperatorDto>(CriteriaOperatorProperty);
            }
            set
            {
                SetValue(CriteriaOperatorProperty, value);
            }
        }    

        [DataMember]
        public ObservableCollection<object> Values
        {
            get;
            set;
        }

        [DataMember]
        public IEnumerable<LookupItemDto> ItemsSource
        {
            get;
            set;
        }

        [DataMember]
        public IEnumerable<CriteriaOperatorDto> SupportedOperators
        {
            get;
            set;
        }

        [DataMember]
        public IEnumerable<int> DependentCriteriaIds
        {
            get;
            set;
        }

        [DataMember]
        public int SearcDCUID
        {
            get;
            set;
        }

        [DataMember]
        public bool IsReadOnly
        {
            get;
            set;
        }

		[DataMember]
		public bool IsVisible
		{
			get;
			set;
		}

       


        [DataMember]
        public string OriginalDefaultValue
        {
            get;
            set;
        }


        [DataMember]
        public bool? IsChangedAutoExecute
        {
            get;
            set;
        }

        [DataMember]
        public string StartValueEntityField
        {
            get;
            set;
        }

        [DataMember]
        public string EndValueEntityField
        {
            get;
            set;
        }

        [DataMember]
        public string StartValueDataSetField
        {
            get;
            set;
        }


		[DataMember]
		public int? EmInternalCodeRegistration
		{
			get;
			set;
		}

		

		[DataMember]
        public string EndValueDataSetField
        {
            get;
            set;
        }

        // only for Product Grid Search
        [DataMember]
        public bool IsAutoPopulate
        {
            get;
            set;
        }

        // need to filter search result view
        public int? GridColumnId
        {
            get;
            set;
        }

        // need to filter search result view
        public int? GridColumnSubitemId
        {
            get;
            set;
        }

        // need to filter search result view
        public object DCUColumnSearchValue
        {
            get;
            set;
        }

        // need to filter search result view
        public string GridColumnCondition
        {
            get;
            set;
        }

        public bool IsMultipleValues
        {
            get
            {
                return Values.Count > 1;
            }
        }

        public object Value
        {
            get
            {
                return Values.FirstOrDefault();
            }
            set
            {
                if (Values.Count > 0)
                {
                    Values[0] = value;
                }
                else
                {
                    Values.Add(value);
                }
                base.OnPropertyChanged("Value");
            }
        }




        public string ValuesDisplay
        {
            get
            {
                StringBuilder display = new StringBuilder();

                foreach (object value in Values)
                {
                    if (value != null)
                    {
                        if (ItemsSource == null)
                        {
                            display.Append(value.ToString() + ";");
                        }
                        else
                        {
                            var lookupitem = ItemsSource.FirstOrDefault(o => object.Equals(o.Id, value));
                            if (lookupitem != null)
                            {
                                display.Append(lookupitem.Display + ";");
                            }
                            else
                            {
                                display.Append(value.ToString() + ";");
                            }
                        }
                    }
                }

                return display.ToString();
            }
        }

        public bool IsUIEnabled
        {
            get { return GetValue(() => IsUIEnabled); }
            set { SetValue(() => IsUIEnabled, value); }
        }               

        public void NotifyValuesChanged()
        {
            OnPropertyChanged("IsMultipleValues");
            OnPropertyChanged("Value");
            OnPropertyChanged("ValuesDisplay");
        }
    }
}