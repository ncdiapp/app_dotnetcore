using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace APP.Framework.Excel
{
    [DataContract(Namespace = FrameworkContractNamespaces.Excel)]
    public class RowDto : ObservableObject
    {
        public RowDto()
        {
            Keys = new List<int>();
            Values = new List<object>();
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<int> Keys
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<object> Values
        {
            get;
            set;
        }

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

                if (Values[Keys.IndexOf(index)] != value)
                {
                    Values[Keys.IndexOf(index)] = value;
                }
            }
        }

        public override string ToString()
        {
            return Display;
        }
    }
}
