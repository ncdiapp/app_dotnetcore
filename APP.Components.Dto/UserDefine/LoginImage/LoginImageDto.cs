using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APP.Framework;
using System.Runtime.Serialization;

namespace APP.Components.Dto.UserDefine.LoginImage
{

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class LoginImageDto : EditableObject
    {
        public static readonly string PictureProperty = ObjectInfoHelper.GetName<LoginImageDto, Byte[]>(o => o.Picture);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<LoginImageDto, string>(o => o.Name);

        [DataMember(EmitDefaultValue = false)]
        public string Name
        {
            get { return GetValue(() => Name); }
            set { SetValue(() => Name, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public Byte[] Picture
        {
            get { return GetValue(() => Picture); }
            set { SetValue(() => Picture, value); }
        }
    }
}
