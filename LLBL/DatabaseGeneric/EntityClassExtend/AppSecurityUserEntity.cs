
using System;
using System.ComponentModel;
using System.Collections.Generic;

using System.Xml.Serialization;
using APP.LBL;
using SD.LLBLGen.Pro.ORMSupportClasses;

using APP.LBL.HelperClasses;
using System.Linq;
namespace APP.LBL.EntityClasses
{

    public partial class AppSecurityUserEntity
    {
        public readonly static string AdPasswordSaltKey = "BC365A4E-68A0-4A75-9E46-8AB18C64E796";
        public enum EmAppDomainType { SysAdmin = 1}
       // public static readonly string AppSuperuser = "AppSuperuser";

        //public string DecrypedPassword
        //{
        //    get
        //    {
        //        return EnDeCryptBLL.Decrypt(this.Password, this.LoginName);

        //    }

        //}


        //public int? CurrentCompnayId
        //{
        //    get;
        //    set;
        //}

        public bool IsInSysAdminDomain
        {
            get
            {
             
                if (this.DomainId ==(int) EmAppDomainType.SysAdmin )
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
        }

        //public bool Internal
        //{
        //    get
        //    {
        //        if (this.IsInSysAdminDomain)
        //        {
        //            return true;
        //        }
        //        else if (this.AppSecurityRegDomain.DomainType.HasValue && this.AppSecurityRegDomain.DomainType.Value == (int)EmAppDomainType.Internal)
        //        {
        //            return true;
        //        }

        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}

        //public bool IsExternalUser
        //{
        //    get
        //    {
        //        if (this.AppSecurityRegDomain.DomainType.HasValue && this.AppSecurityRegDomain.DomainType.Value == (int)EmAppDomainType.ExternalUser )
        //        {
        //            return true;
        //        }
        //        return false;
        //    }
        //}

        //public bool IsBuiltInAppSuperUser
        //{
        //    get
        //    {
        //        if (this.LoginName.Trim() == AppSuperuser)
        //            return true;
        //        else
        //            return false;
        //    }
        //}

        public int[] groupIds
        {
            get
            {
               return  this.AppSecurityGroupMember.Select(o => o.GroupId).ToArray();
               
            }
        }


       

    }

 

}


