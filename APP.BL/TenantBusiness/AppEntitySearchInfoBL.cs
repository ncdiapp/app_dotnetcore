using System.Collections.Generic;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Components.EntityConverter;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using System;
using System.Data;

using APP.Framework;
namespace App.BL
{
    public static class AppEntitySearchInfoBL
    {

        public static List<AppEntitySearchInfoDto> GetCurrnetUserAddress(EmAppEntitySearchInfoCode emAppEntitySearchInfoCode)
        {
            List<AppEntitySearchInfoDto> toRetrun = new List<AppEntitySearchInfoDto>();

            AppSearchViewEntity appSearchViewEntity = AppSearchViewConfigBL.RetrieveOneAppSearchViewEntityWithEntityCode(emAppEntitySearchInfoCode.ToString ());

            object searchId = appSearchViewEntity.CatalogueSearchId;
            var appSearchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(searchId) ;
            SearchDto aSearchDto = AppSearchConfigBL.GetSearchDtoSetupDefaultSearchCretia(appSearchEntity);

            DataTable dataTableResult = AppStaticDataSetSearchBL.RetriveMasterDataSetDataTable(aSearchDto, appSearchViewEntity);

            var pkfieldEntity = appSearchViewEntity.AppSearchViewField.FirstOrDefault(o => o.IsGroupBy.HasValue && o.IsGroupBy.Value);

            var filedList = appSearchViewEntity.AppSearchViewField.OrderBy(o => o.Sort).ToList();
             filedList.Remove(pkfieldEntity); 


            IEnumerable<DataRow> dataTableRows = dataTableResult.AsEnumerable();

            foreach (DataRow row in dataTableRows)
            {
                AppEntitySearchInfoDto aAppEntitySearchInfoDto = new AppEntitySearchInfoDto();
                aAppEntitySearchInfoDto.TransactionId = appSearchViewEntity.TransactionId;
                aAppEntitySearchInfoDto.PrimayKeyValue = row[pkfieldEntity.SysTableFiledPath];

                aAppEntitySearchInfoDto.DictDisplayFiledValue = new Dictionary<string, object>();
                foreach (var displayField in filedList)
                {
                    aAppEntitySearchInfoDto.DictDisplayFiledValue.Add(displayField.DisplayText, row[displayField.SysTableFiledPath ]);
                
                }


                toRetrun.Add(aAppEntitySearchInfoDto);
 
            }

            return toRetrun;

        
        }


       
    }
}