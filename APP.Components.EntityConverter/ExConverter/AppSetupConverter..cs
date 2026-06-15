using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.LBL.EntityClasses;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.EntityDto;


namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppSetupEntity and  AppSetupDto
    /// </summary>
    public static partial class AppSetupConverter
    {
        public readonly static string  AdSetupValueSaltKey = "BC365A4E-68A0-4A75-9E46-8AB18C64E796";

        static partial void OnCopyEntityToDtoDone(AppSetupEntity aAppSetupEntity, AppSetupDto aAppSetupDto)
        {
         

            if (!string.IsNullOrWhiteSpace(aAppSetupEntity.SetupValue ))
            {
                try
                {
                    aAppSetupDto.SetupValue = EnDeCrypt.Decrypt(aAppSetupEntity.SetupValue, AdSetupValueSaltKey);
                }
                catch
                {
                    aAppSetupDto.SetupValue = string.Empty;
                }
                
            }
            else
            {
                aAppSetupDto.SetupValue = string.Empty;
            }

          
        }

        static partial void OnCopyDtoToEntityDone(AppSetupEntity aAppSetupEntity, AppSetupDto aAppSetupDto)
        {
          //  aAppSetupEntity.SetupValue = EnDeCrypt.Encrypt(aAppSetupDto.SetupValue, AdSetupValueSaltKey);

            if (!string.IsNullOrWhiteSpace(aAppSetupDto.SetupValue))
            {
                try
                {
                    aAppSetupEntity.SetupValue = EnDeCrypt.Encrypt(aAppSetupDto.SetupValue, AdSetupValueSaltKey);
                }
                catch
                {
                    aAppSetupEntity.SetupValue = string.Empty;
                }
                
            }
            else
            {
                aAppSetupEntity.SetupValue = string.Empty;
            }

      
        }
    }
}