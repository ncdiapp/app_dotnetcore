using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;
using System.ComponentModel;

namespace APP.Components.EntityDto
{
   
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class   EditResultRowDto : EditableObject, IList
    {
        public EditResultRowDto()
        {
            Keys = new List<int>();
            Values = new List<object>();
            ColumnIdAndCellHeight = new Dictionary<int, double>();
        }

        [DataMember(EmitDefaultValue = false)]
        public int ReferenceId
        {
            get { return GetValue(() => ReferenceId); }
            set { SetValue(() => ReferenceId, value); }
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string Display
        //{
        //    get { return GetValue(() => Display); }
        //    set { SetValue(() => Display, value); }
        //}

        [DataMember(EmitDefaultValue = false)]
        public List<int> Keys
        {
            get { return GetValue(() => Keys); }
            set { SetValue(() => Keys, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public List<object> Values
        {
            get { return GetValue(() => Values); }
            set { SetValue(() => Values, value); }
        }

        public void Add(int key, object value)
        {
            Keys.Add(key);
            Values.Add(value);

            //ColumnIdAndCellHeight.Add(key, 25.0);
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

          [IgnoreDataMember]
        public bool IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

          [IgnoreDataMember]
        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        [IgnoreDataMember]
        public object this[int index]
        {
            get
            {
                if (Keys.IndexOf(index) != -1)
                {
                    return Values[Keys.IndexOf(index)];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var valueIndex = Keys.IndexOf(index);
                if (valueIndex >= 0
                    && Values.Count > valueIndex
                    && Values[valueIndex] != value)
                {
                    Values[valueIndex] = value;

                    CurrentChangedColumnId = index;

                    RowCellValueChanged = true;

#if SILVERLIGHT
          
                   
                    //The binding system is looking for a property named "Item[]",  "Item[" + key + "]"
                     OnPropertyChanged(new PropertyChangedEventArgs("Item[" + index + "]"));
                    
#endif

                }
            }
        }

        public bool RowCellValueChanged { get; set; }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

          [IgnoreDataMember]
        public int Count
        {
            get { return int.MaxValue; }
        }

          [IgnoreDataMember]
        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

          [IgnoreDataMember]
        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        //public override string ToString()
        //{
        //    return Display;
        //}

        private IntValueCast _intValue;

        public IntValueCast IntValue
        {
            get
            {
                if (_intValue == null)
                {
                    _intValue = new IntValueCast(this);
                }

                return _intValue;
            }
        }

        public class IntValueCast
        {
            public IntValueCast(EditResultRowDto row)
            {
                Row = row;
            }

            private EditResultRowDto Row
            {
                get;
                set;
            }

            public int? this[int index]
            {
                get
                {
                    if (Row.Keys.IndexOf(index) != -1)
                    {
                        //modify for combobox filter(string converter to int throw error)
                        //return (int?)Row.Values[Row.Keys.IndexOf(index)];

                        if (Row.Values[Row.Keys.IndexOf(index)] == null)
                        {
                            return null;
                        }

                        int result;
                        if (int.TryParse(Row.Values[Row.Keys.IndexOf(index)].ToString(), out result))
                        {
                            return result;
                        }
                        else
                        {
                            return null;
                        }
                        //modify end
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public bool IsSelected
        {
            get { return GetValue(() => IsSelected); }
            set { SetValue(() => IsSelected, value); }
        }

        public int CurrentChangedColumnId
        {
            get { return GetValue(() => CurrentChangedColumnId); }
            set { SetValue(() => CurrentChangedColumnId, value); }
        }

        // only for print:key:column id,value,height of cell
        public Dictionary<int, double> ColumnIdAndCellHeight
        {
            get { return GetValue(() => ColumnIdAndCellHeight); }
            set { SetValue(() => ColumnIdAndCellHeight, value); }
        }

        public double GetMaxHeightOfAllCells()
        {
            double defaultHeith = 25;

            var maxHeight = ColumnIdAndCellHeight.Values.Max();

            if (maxHeight > defaultHeith)
            {
                return maxHeight;
            }

            return defaultHeith;
        }

        public string ValueSortComboDisplay { get; set; }

    }
}