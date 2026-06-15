using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using APP.Framework;
using APP.Framework.Communication;

using APP.Components.Dto;

using System.Collections;
#if NETFRAMEWORK
using System.Web;
#else
using Microsoft.AspNetCore.Http;
#endif
using System.Runtime.Serialization;

namespace APP.Framework
{
    public class ServerContext
    {
        private static readonly ServerContext _instance = new ServerContext()
        {
            CompanySettings = new CompanySettingDto()
        };

#if !NETFRAMEWORK
        // Wired up in Program.cs after builder.Build():
        //   ServerContext.SetHttpContextAccessor(app.Services.GetRequiredService<IHttpContextAccessor>());
        private static IHttpContextAccessor? _httpContextAccessor;
        public static void SetHttpContextAccessor(IHttpContextAccessor accessor) =>
            _httpContextAccessor = accessor;
#endif

        public static readonly string InternalApplcationSessionTokenValue = "41E146D8-25E9-4367-8428-E21E6DBD2171";
        public static readonly string ExternalApplcationSessionTokenValue = "35A5C58F-4C6F-45DD-85CF-1694D9265B15";

        public static readonly string AccessTokenName = "AccessToken";
        //	public static readonly Dictionary<string, TblSketchDto> DictExternalByteFileGuIdSketchDto = new Dictionary<string, TblSketchDto>();


        private static readonly Dictionary<string, string> _DictTimeZoneList = TimeZoneHelper.GetTimeZoneList();

        public const string CurrentUserSessionIdToken = "CurrentUserSessionId";

        public string ApplicationDomain
        {
            get; set;
        }

        static ServerContext()
        {
            // _DictTimeZoneList = TimeZoneHelper.GetTimeZoneList();
        }

        private ServerContext()
        {
        }

        public static Dictionary<string, string> DictTimeZoneList
        {
            get
            {
                return _DictTimeZoneList;
            }
        }

        public static ServerContext Instance
        {
            get
            {
                return _instance;
            }
        }

        public TimeSpan CloseTimeout { get; set; }

        public TimeSpan OpenTimeout { get; set; }

        public TimeSpan ReceiveTimeout { get; set; }

        public TimeSpan SendTimeout { get; set; }


        public IClientIdentityProvider<APP.Components.Dto.AppClientIdentity> HttpIdentityProvider { get; private set; }
        public void InitializeHttpClient(IClientIdentityProvider<APP.Components.Dto.AppClientIdentity> httpIdentityProvider)
        {
            ArgumentValidator.IsNotNull("httpIdentityProvider", httpIdentityProvider);
            if (HttpIdentityProvider == null)
            {
                HttpIdentityProvider = httpIdentityProvider;
            }

        }

        public IClientIdentityProvider<APP.Components.Dto.AppClientIdentity> WindowsIdentityProvider { get; private set; }

        public void InitializeWindowsClient(IClientIdentityProvider<APP.Components.Dto.AppClientIdentity> windowsIdentityProvider)
        {
            ArgumentValidator.IsNotNull("windowsIdentityProvider", windowsIdentityProvider);
            if (WindowsIdentityProvider == null)
            {
                WindowsIdentityProvider = windowsIdentityProvider;
            }

        }


        public object CurrentUid
        {
            get
            {


                if (CurrnetClientIdentity != null)
                {
                    return CurrnetClientIdentity.UserId;
                }

                //#if DEBUG
                // return 1;
                //#endif            

                return null;
            }
        }

        public object CurrentCompanyId
        {
            get
            {


                if (CurrnetClientIdentity != null)
                {
                    return CurrnetClientIdentity.CurrentWorkingCompanyId;
                }

                //#if DEBUG
                // return 1;
                //#endif            

                return null;
            }
        }



        public int CurrentLoginUserType
        {
            get
            {



                return CurrnetClientIdentity.CurrentLoginUserType;

            }
        }


        public IClientIdentity CurrnetClientIdentity
        {
            get
            {
#if NETFRAMEWORK
                bool hasHttpContext = HttpContext.Current != null;
#else
                bool hasHttpContext = _httpContextAccessor?.HttpContext != null;
#endif
                if (hasHttpContext && ServerContext.Instance.HttpIdentityProvider != null)
                {
                    return ServerContext.Instance.HttpIdentityProvider.ProvideIdentity();
                }
                else if (ServerContext.Instance.WindowsIdentityProvider != null)
                {
                    return ServerContext.Instance.WindowsIdentityProvider.ProvideIdentity();
                }

                return null;
            }
        }

        //??? must call secuirty control method in order to s
        public object CurrentSessionId
        {
            get
            {


                if (CurrnetClientIdentity != null)
                {
                    return CurrnetClientIdentity.SessionId;
                }

                return null;
            }
        }

        public string CurrentUserTimeZoneKey
        {
            get
            {
                if (CurrnetClientIdentity != null)
                {
                    return ((APP.Components.Dto.AppClientIdentity)CurrnetClientIdentity).TimeZoneKey;
                }


                return null;
            }
        }

        public string CurrentUserDbConnectionString
        {
            get
            {
                if (CurrnetClientIdentity != null)
                    return ((APP.Components.Dto.AppClientIdentity)CurrnetClientIdentity).CurrentUserDbConnectionString;
                return null;
            }
        }

        public string CurrentUserDataBaseName
        {
            get
            {
                if (CurrnetClientIdentity != null)
                    return ((APP.Components.Dto.AppClientIdentity)CurrnetClientIdentity).CurrentUserDataBaseName;
                return null;
            }
        }


        public int DataSourceId
        {
            get
            {
                if (CurrnetClientIdentity != null)
                {
                    return ((APP.Components.Dto.AppClientIdentity)CurrnetClientIdentity).DataSourceId;
                }

                // not exsit
                return -1;
            }
        }








        public bool? IsCallingFromBrowser
        {
            get
            {
                if (CurrnetClientIdentity != null)
                {
                    return ((APP.Components.Dto.AppClientIdentity)CurrnetClientIdentity).IsCallingFromBrowser;
                }


                return null;
            }
        }


        public CompanySettingDto CompanySettings
        {
            get;
            set;
        }
    }


    public class WindowClientIdentityProvider : IClientIdentityProvider<AppClientIdentity>
    {
        // ConcurrentDictionary: safe for concurrent async reads+writes under .NET 10's async pipeline.
        public static readonly IDictionary<object, AppClientIdentity?> WinContext = new ConcurrentDictionary<object, AppClientIdentity?>();
        private const string IdentityKey = "{CB93D768-5EB5-492B-87D1-E3D265CA4FDB}";

        // only for interface 
        public AppClientIdentity? ProvideIdentity()
        {
            return WinContext[IdentityKey];
        }

        //public AppClientIdentity? ProvideIdentity( object sessionId)
        //{
        //    return WinContext[sessionId];
        //}

        public void RegisterIdentity(AppClientIdentity? identity)
        {
            if (identity.HasValue)
            {
                if (WinContext.ContainsKey(IdentityKey))
                {
                    WinContext.Remove(IdentityKey);
                }

                WinContext[IdentityKey] = identity;
            }

        }

        public string Name
        {
            get { return AppClientIdentity.Key; }
        }

        public string Namespace
        {
            get { return "http://visual-2000.com/plms/"; }
        }

        public string SerializeIdentity(AppClientIdentity identity)
        {
            return identity.Serialize();
        }

        public AppClientIdentity DeserializeIdentity(string identity)
        {
            return AppClientIdentity.Deserialize(identity);
        }
    }



    public class CompanySettingDto
    {
        public bool IsHostCompany =>
            CompanyId.HasValue && CompanyId.Value == 1; // 1 = reserved host company ID (AppSystemConstants.HostCompanyId)


        public int? CompanyId
        {
            get;
            set;
        }

        public string CompanyName
        {
            get;
            set;
        }

        public bool IsEnableClientSelfRegistration
        {
            get;
            set;
        }


        public bool IsEnableSupplierSelfRegistration
        {
            get;
            set;
        }


        public bool IsEnableClientAgentSelfRegistration
        {
            get;
            set;
        }


        public bool IsEnableSupplierAgentSelfRegistration
        {
            get;
            set;
        }


        public string ClientLabelName
        {
            get;
            set;
        }


        public string SupplierLabelName
        {
            get;
            set;
        }


        public string ClientAgentLabelName
        {
            get;
            set;
        }


        public string SupplierAgentLabelName
        {
            get;
            set;
        }



        //public int? LogoImageId
        //{
        //    get;
        //    set;
        //}



        //public List<int> LoginPageBackgroundImageIdList
        //{
        //    get;
        //    set;
        //}

    }
}