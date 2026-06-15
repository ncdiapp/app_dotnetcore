using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Communication;

namespace APP.Components.Dto
{
   // [DataContract(Namespace = ContractNamespaces.Dto)]
    public struct AppClientIdentity : IClientIdentity
    {
        public const string Key = "{5814BAD3-1467-48CD-93EE-905559E3C21A}";


        // will  drop ExternalTokenKeyUserId  
        // only  ExternalApplicationSessionKey
        public const string ExternalTokenKeyUserId = "FB48E966-2086-4FCE-86B8-722B784F1B71";
        public const string ExternalApplicationSessionKey = "B8A48121-78E8-4F0B-9816-8F4856C81A83";
        public const string ExternalApplicationTimeZoneKey = "B7952205-9D45-4D43-A5D6-CC7406AF64E5";
     
       // [DataMember(EmitDefaultValue = false)]
        public object SessionId
        {
            get;
            set;
        }

      //  [DataMember(EmitDefaultValue = false)]
        public object UserId
        {
            get;
            set;
        }


        ////  [DataMember(EmitDefaultValue = false)]
        //public object RuningTimeBusinessAccountId
        //{
        //    get;
        //    set;
        //}

        // [DataMember(EmitDefaultValue = false)]
        public   object LanguageId
        {
            get;
            set;
        }

       // [DataMember(EmitDefaultValue = false)]
        public string TimeZoneKey
        {
            get;
            set;
        }



		//[DataMember(EmitDefaultValue = false)]
		public object CurrentWorkingCompanyId
		{
			get;
			set;
		}


        public int? CurrentPartnerId
        {
            get;
            set;
        }

        public int DataSourceId
		{
			get;
			set;
		}

		public string CurrentUserDbConnectionString
        {
            get;
            set;
        }

		public string CurrentUserDataBaseName
		{
			get;
			set;
		}


        public int CurrentLoginUserType
        {
            get;
            set;
        }

        public bool? IsCallingFromBrowser
        {
            get;
            set;
        }



        public bool IsValid()
        {
            return UserId != null;
        }

        public bool IsExternalApp
        {
            get;
            set;
        }


		public string Serialize()
		{
			using (MemoryStream stream = new MemoryStream())
			{
				DataContractSerializer serializer = new DataContractSerializer(typeof(AppClientIdentity));
				serializer.WriteObject(stream, this);
				stream.Seek(0, SeekOrigin.Begin);
				return stream.ToStringValue();
			}
		}

		public static AppClientIdentity Deserialize(string identity)
		{
			ArgumentValidator.IsNotNullOrEmpty("identity", identity);

			using (MemoryStream stream = new MemoryStream(identity.ToByteArray()))
			{
				DataContractSerializer serializer = new DataContractSerializer(typeof(AppClientIdentity));
				return (AppClientIdentity)(serializer.ReadObject(stream));
			}
		}
	}
}