using System;
using System.Text;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace AppWeb.Models;

public class LoginModel
{
    public string userName { get; set; }
    public string password { get; set; }
    public string Email { get; set; }
    public string UserId { get; set; }
    public bool IsRegisterCompleted { get; set; }
    public bool IsActive { get; set; }
    public Guid? GlobalGuid { get; set; }
    public string returnUrl { get; set; }
    public string RedirectToUrl { get; set; }
    public string RedirectToSubUrl { get; set; }
    public string JoinedToCompanyName { get; set; }
    public bool IsSelectingCompany { get; set; }
    public List<LookupItemDto> AvailableCompnay { get; set; }
    public string SelectedCompnayId { get; set; }

    internal static AppSecurityUserDto GetUserInfoFromRequestHeader(string authrizationHeader)
    {
        try
        {
            AppSecurityUserDto userDto = new AppSecurityUserDto();
            string encodedUsernamePassword = authrizationHeader.Substring("Basic ".Length).Trim();
            Encoding encoding = Encoding.GetEncoding("UTF-8");
            string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
            int seperatorIndex = usernamePassword.IndexOf(':');
            userDto.LoginName = usernamePassword.Substring(0, seperatorIndex);
            userDto.Password = usernamePassword.Substring(seperatorIndex + 1);
            return userDto;
        }
        catch
        {
            return null;
        }
    }
}
