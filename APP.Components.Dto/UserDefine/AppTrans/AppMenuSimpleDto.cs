using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Framework.Collections;

namespace APP.Components.EntityDto
{    
    public partial class AppMenuSimpleDto
    {
        [DataMember(EmitDefaultValue = false)]
        public object Id { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? ParentId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Description { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string IconName { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string RouteCode { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Link { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? Sort { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int LinkType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? EmDeviceMenuShowMode { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public Guid? GlobalGuid { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string IconName2 { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? EmAppMenuItemCategory { get; set; }





        [DataMember(EmitDefaultValue = false)]
        public List<AppMenuSimpleDto> AppListMenu_List
        {
            get;
            set;
        }




        [DataMember(EmitDefaultValue = false)]
        public string ImageUrl { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSelectedForDomainOrUser { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsImportedFromOtherDB { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsPackageInstalled { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? InstalledPackageUserDBMenuId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string MenuPath { get; set; }




        public static AppMenuSimpleDto ConvertAppListMenuExDtoToAppMenuSimpleDto(AppListMenuExDto menuExDto)
        {
            if (menuExDto != null)
            {
                AppMenuSimpleDto toReturn = new AppMenuSimpleDto();

                toReturn.AppListMenu_List = new List<AppMenuSimpleDto>();

                toReturn.Id = menuExDto.Id;
                toReturn.ParentId = menuExDto.ParentId;
                toReturn.Name = menuExDto.Name;
                toReturn.Description = menuExDto.Description;
                toReturn.IconName = menuExDto.IconName;
                toReturn.RouteCode = menuExDto.RouteCode;
                toReturn.Link = menuExDto.Link;
                toReturn.Sort = menuExDto.Sort;
                toReturn.LinkType = menuExDto.LinkType;
                toReturn.EmDeviceMenuShowMode = menuExDto.EmDeviceMenuShowMode;
                toReturn.GlobalGuid = menuExDto.GlobalGuid;
                toReturn.IconName2 = menuExDto.IconName2;
                toReturn.EmAppMenuItemCategory = menuExDto.EmAppMenuItemCategory;
                toReturn.ImageUrl = menuExDto.ImageUrl;
                toReturn.IsSelectedForDomainOrUser = menuExDto.IsSelectedForDomainOrUser;
                toReturn.IsImportedFromOtherDB = menuExDto.IsSharedbyMutipleCompany.HasValue && menuExDto.IsSharedbyMutipleCompany.Value;
                toReturn.IsPackageInstalled = menuExDto.IsPackageInstalled;
                toReturn.InstalledPackageUserDBMenuId = menuExDto.InstalledPackageUserDBMenuId;
                toReturn.MenuPath = menuExDto.MenuPath;

                if (menuExDto.AppListMenu_List != null)
                {
                    foreach (var childMenuExDto in menuExDto.AppListMenu_List)
                    {
                        var childSimpleMenuDto = ConvertAppListMenuExDtoToAppMenuSimpleDto(childMenuExDto);
                        toReturn.AppListMenu_List.Add(childSimpleMenuDto);
                    }
                }

                return toReturn;
            }

            return null;
            
        }

    }
}

