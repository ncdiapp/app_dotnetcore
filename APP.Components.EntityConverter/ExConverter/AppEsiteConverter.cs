using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityConverter
{

    public static partial class AppEsiteConverter
    {

        static partial void OnCopyEntityToDtoDone(AppEsiteEntity aAppEsiteEntity, AppEsiteDto aAppEsiteDto)
        {
            if (!string.IsNullOrWhiteSpace(aAppEsiteEntity.MasteSiteCustNavigationJavaScripControl))
            {
                try
                {
                    aAppEsiteDto.EsiteAttribute = JsonConvert.DeserializeObject<EsiteAttributeDto>(aAppEsiteEntity.MasteSiteCustNavigationJavaScripControl);
                }
                catch
                {
                    aAppEsiteDto.EsiteAttribute = null;
                }

            }
            else
            {
                aAppEsiteDto.EsiteAttribute = null;
            }

            if (aAppEsiteDto.EsiteAttribute == null)
            {
                aAppEsiteDto.EsiteAttribute = new EsiteAttributeDto();
            }


            //aAppEsiteDto.CustomerInfoDataModelId = null;
            //aAppEsiteDto.CustomerInfoDbtableName = null;
            //aAppEsiteDto.CustomerInfoCustomerIdDbfieldName = null;
            //aAppEsiteDto.CustomerInfoEmailDbfieldName = null;
            //aAppEsiteDto.CustomerInfoDataTransferId = null;

            //aAppEsiteDto.SupplierInfoDataModelId = null;
            //aAppEsiteDto.SupplierInfoDbtableName = null;
            //aAppEsiteDto.SupplierInfoIdDbfieldName = null;
            //aAppEsiteDto.SupplierInfoEmailDbfieldName = null;
            //aAppEsiteDto.SupplierInfoDataTransferId = null;

        }

        static partial void OnCopyDtoToEntityDone(AppEsiteEntity aAppEsiteEntity, AppEsiteDto aAppEsiteDto)
        {
            try
            {
                aAppEsiteEntity.MasteSiteCustNavigationJavaScripControl = JsonConvert.SerializeObject(aAppEsiteDto.EsiteAttribute);
            }
            catch
            {
                aAppEsiteEntity.MasteSiteCustNavigationJavaScripControl = string.Empty;
            }
        }
    }
}

