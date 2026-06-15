using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public enum EnumHttpMethod
    {
        Get =1,
        Post =2,
        Put =3,
        Delete =4
    }
    public enum AuthenticationType
    {
        None = 0,
        Basic64 = 1,
        OAuth2 = 2,
        IntegrationToken = 3,
    }
}
