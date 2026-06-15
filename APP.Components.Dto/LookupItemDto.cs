using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;

namespace APP.Components.Dto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class LookupItemDto : ObservableObject, IIdentifiable
    {
        public static readonly string IdProperty = ObjectInfoHelper.GetName<LookupItemDto, object>(o => o.Id);
        public static readonly string DisplayProperty = ObjectInfoHelper.GetName<LookupItemDto, string>(o => o.Display);
        public static readonly string IsCheckedProperty = ObjectInfoHelper.GetName<LookupItemDto, bool>(o => o.IsChecked);

        

        [DataMember(EmitDefaultValue = false)]
        public object Id
        {
            get
            {
                return GetValue<object>(IdProperty);
            }
            set
            {
                SetValue(IdProperty, value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get
            {
                return GetValue<string>(DisplayProperty);
            }
            set
            {
                SetValue(DisplayProperty, value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictDependentFieldValue
        {
            get;
            set;
        }


	

		public override string ToString()
        {
            return this.Display;
        }

        public string FirstField
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(Display))
                {
                    string[] tokens = Display.Split(new Char[] { '|' });

                    if (tokens.Length == 1)
                    {
                        return Display.Trim();
                    }
                    else
                    {
                        return tokens[0].Trim();
                    }
                }

                return string.Empty;
            }
        }

        public string LastField
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(Display))
                {
                    string[] tokens = Display.Split(new Char[] { '|' });

                    if (tokens.Length == 2)
                    {
                        return tokens[1].Trim();
                    }
                }

                return string.Empty;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string ColorCode
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string UserDefinedValue1
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UserDefinedValue2
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsChecked
        {
            get
            {
                return GetValue<bool>(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
            }
        }



        // for WebApi method call
        //    aLookupItemDto.ItemType = 1; // Request Paramter parameter
        // aLookupItemDto.ItemType = 2  Respone Parameter
        [DataMember(EmitDefaultValue = false)]
        public int ItemType
        {
            get;
            set;
        }



        //public override bool Equals(object obj)
        //{
        //    if (obj == null || GetType() != obj.GetType())
        //    {
        //        return false;
        //    }

        //    return Value == ((EntityInfoDto)obj).Value;
        //}

        //public override int GetHashCode()
        //{
        //    return base.GetHashCode() ^ Value;
        //}

        //public bool IsActive
        //{
        //    get;
        //    set;
        //}
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class SimpleLookupItemDto : ObservableObject, IIdentifiable
    {


        [DataMember(EmitDefaultValue = false)]
        public object Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }

     
    }
}