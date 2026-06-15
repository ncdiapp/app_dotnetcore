using APP.LBL.EntityClasses;
using APP.Framework;

using APP.Components.EntityDto;

namespace APP.Components.EntityConverter
{
	/// <summary>
	/// Convert Properties between  AppSecurityUserEntity and  AppSecurityUserDto
	/// </summary>
	public static partial class AppSecurityUserConverter
    {
        public readonly static string  AdPasswordSaltKey = "BC365A4E-68A0-4A75-9E46-8AB18C64E796";

        static partial void OnCopyEntityToDtoDone(AppSecurityUserEntity aAppSecurityUserEntity, AppSecurityUserDto aAppSecurityUserDto)
        {
			//  aAppSecurityUserDto.Password = aAppSecurityUserEntity.DecrypedPassword;

			aAppSecurityUserDto.Password = "password";



			if (!string.IsNullOrWhiteSpace(aAppSecurityUserEntity.Adpassword))
            {
                try
                {
                    aAppSecurityUserDto.Adpassword = EnDeCrypt.Decrypt(aAppSecurityUserEntity.Adpassword, AdPasswordSaltKey);
                }
                catch
                {
                    aAppSecurityUserDto.Adpassword = string.Empty;
                }
                
            }
            else
            {
                aAppSecurityUserDto.Adpassword = string.Empty;
            }


          //  aAppSecurityUserDto.Adpassword = aAppSecurityUserEntity.DecrypedADPassword;
          


          
        }

        static partial void OnCopyDtoToEntityDone(AppSecurityUserEntity aAppSecurityUserEntity, AppSecurityUserDto aAppSecurityUserDto)
        {
           // aAppSecurityUserEntity.Password = EnDeCrypt.Encrypt(aAppSecurityUserDto.Password, aAppSecurityUserDto.LoginName);

            if (!string.IsNullOrWhiteSpace(aAppSecurityUserDto.Adpassword))
            {
                try
                {
                    aAppSecurityUserEntity.Adpassword = EnDeCrypt.Encrypt(aAppSecurityUserDto.Adpassword, AppSecurityUserConverter.AdPasswordSaltKey);
                }
                catch
                {
                    aAppSecurityUserEntity.Adpassword = string.Empty;
                }
                
            }
            else
            {
                aAppSecurityUserEntity.Adpassword = string.Empty;
            }

           
            
        }
    }
}