using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class ProductCatalogueController : SecureBaseController
{
    // no need to control security !

    [HttpGet]
    public ObservableSet<AppSetupExDto> RetrieveAllAppSetupDtoList(bool isMasterDb)
    {
        var identity = (APP.Components.Dto.AppClientIdentity?)APP.Framework.ServerContext.Instance.CurrnetClientIdentity;
        bool isSysAdmin = identity?.CurrentLoginUserType == (int)EmAppUserType.SysAdmin;
        return isSysAdmin
            ? AppSystemSettingBL.RetrieveAllAsDto()
            : AppTenantSettingBL.RetrieveAllAsDto();
    }

    [HttpPost]
    public Dictionary<int, List<AppEshopBagItemDto>> ProcessShopBag(AppEshopBagDto appEshopBagDto)
    {
        //To do list:
        // need to check credit
        // validate address
        // split

        List<AppEshopBagItemDto> bagItemList = appEshopBagDto.EshopBagItemList;

        Dictionary<int, List<AppEshopBagItemDto>> dictTranscation = bagItemList.GroupBy(o => o.TransactionId).ToDictionary(o => o.Key.Value, o => o.ToList());

        return dictTranscation;
    }

    [HttpGet]
    public AppSearchViewExDto RetrieveOneAppSearchViewExDto(int? searchViewId)
    {
        return AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchViewId);
    }

    [HttpGet]
    public IEnumerable<StaticSearchResultRowJsonDto> RetrieveViewAllRecordResult(int? viewId)
    {
        if (viewId.HasValue)
        {
            return AppSearchBL.RetrieveViewAllRecordResult(viewId.Value);
        }

        return null;
    }

    [HttpGet]
    public ObservableSet<AppListMenuExDto> RetrieveWebPageMenus()
    {
        return AppListMenuBL.RetrieveAllAppListMenuEntityDto(true);
    }

    private static AppEshopCatalogCardDetailDto MockupAppEshopCatalogCardDetailDto(AppEshopCatalogCardDto appEshopCatalogCardDto)
    {
        AppEshopCatalogCardDetailDto av = new AppEshopCatalogCardDetailDto();

        av.DictOptionLevelLookup = new Dictionary<int, List<LookupItemDto>>();

        List<LookupItemDto> opClassGroupList = new List<LookupItemDto>();
        opClassGroupList.Add(new LookupItemDto() { Id = "1", Display = "Group A" });
        opClassGroupList.Add(new LookupItemDto() { Id = "2", Display = "Group B" });
        opClassGroupList.Add(new LookupItemDto() { Id = "3", Display = "Group C" });
        av.DictOptionLevelLookup.Add(1, opClassGroupList);

        List<LookupItemDto> opSchechBookTypeList = new List<LookupItemDto>();
        opSchechBookTypeList.Add(new LookupItemDto() { Id = "31", Display = "Regular Books" });
        opSchechBookTypeList.Add(new LookupItemDto() { Id = "32", Display = "Regular Books and Home Exercise Books" });
        av.DictOptionLevelLookup.Add(2, opSchechBookTypeList);

        av.DictOptionLable = new Dictionary<int, string>();
        av.DictOptionLable.Add(1, "Class Group");
        av.DictOptionLable.Add(2, "Skech Books");

        av.DictOptionAndDto = new Dictionary<int, AppEshopCatalogLevelOptionDto>();
        av.DictOptionAndDto.Add(1, new AppEshopCatalogLevelOptionDto() { OptionLabel = "Class Group" });
        av.DictOptionAndDto.Add(2, new AppEshopCatalogLevelOptionDto() { OptionLabel = "Skech Books" });

        av.DictMatrixKeySku = new Dictionary<string, string>();
        av.DictMatrixKeySku.Add("1_31", "sku001");
        av.DictMatrixKeySku.Add("2_31", "sku002");
        av.DictMatrixKeySku.Add("3_31", "sku003");
        av.DictMatrixKeySku.Add("1_32", "sku004");
        av.DictMatrixKeySku.Add("2_32", "sku005");
        av.DictMatrixKeySku.Add("3_32", "sku006");

        av.DictSkuPrice = new Dictionary<string, decimal>();
        av.DictSkuPrice.Add("sku001", 1010);
        av.DictSkuPrice.Add("sku002", 1020);
        av.DictSkuPrice.Add("sku003", 1030);
        av.DictSkuPrice.Add("sku004", 1040);
        av.DictSkuPrice.Add("sku005", 1050);
        av.DictSkuPrice.Add("sku006", 1060);

        av.DictSkuDescription = new Dictionary<string, Dictionary<string, string>>();

        Dictionary<string, string> aDict = new Dictionary<string, string>();
        aDict.Add("Hours", "50");
        aDict.Add("Session", "Tuesday 14:00-16:00 Teacher: Sean Zhang, Thursday 14:00-16:00 Teacher: Sean Zhang");
        av.DictSkuDescription.Add("sku001", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("Hours", "55");
        aDict.Add("Session", "Thursday 14:00-16:00 Teacher: Sean Zhang");
        av.DictSkuDescription.Add("sku002", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("Hours", "56");
        aDict.Add("Session", "Tuesday 14:00-16:00 Teacher: Sean Zhang");
        av.DictSkuDescription.Add("sku003", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("Hours", "57");
        aDict.Add("Session", "Wed 14:00-16:00 Teacher: Sean Zhang, Fri 14:00-16:00 Teacher: Sean Zhang");
        av.DictSkuDescription.Add("sku004", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("Hours", "58");
        aDict.Add("Session", "Fri 14:00-16:00 Teacher: Sean Zhang");
        av.DictSkuDescription.Add("sku005", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("Hours", "59");
        aDict.Add("Session", "Wed 14:00-16:00 Teacher: Sean Zhang");
        av.DictSkuDescription.Add("sku006", aDict);

        av.DictSkuImageUrl = new Dictionary<string, Dictionary<string, string>>();
        aDict = new Dictionary<string, string>();
        aDict.Add("FrontImage", "61");
        aDict.Add("BackImage", "62");
        av.DictSkuImageUrl.Add("sku001", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("FrontImage", "63");
        aDict.Add("BackImage", "64");
        av.DictSkuImageUrl.Add("sku002", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("FrontImage", "65");
        aDict.Add("BackImage", "66");
        av.DictSkuImageUrl.Add("sku003", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("FrontImage", "67");
        aDict.Add("BackImage", "68");
        av.DictSkuImageUrl.Add("sku004", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("FrontImage", "69");
        aDict.Add("BackImage", "70");
        av.DictSkuImageUrl.Add("sku005", aDict);

        aDict = new Dictionary<string, string>();
        aDict.Add("FrontImage", "71");
        aDict.Add("BackImage", "72");
        av.DictSkuImageUrl.Add("sku006", aDict);

        av.DictSkuPrice = new Dictionary<string, decimal>();
        av.DictSkuPrice.Add("sku001", 1010);
        av.DictSkuPrice.Add("sku002", 1020);
        av.DictSkuPrice.Add("sku003", 1030);
        av.DictSkuPrice.Add("sku004", 1040);
        av.DictSkuPrice.Add("sku005", 1050);
        av.DictSkuPrice.Add("sku006", 1060);

        return av;
    }

    private static AppEshopCatalogViewDto Test()
    {
        AppEshopCatalogViewDto av = new AppEshopCatalogViewDto();

        av.DictOptionLevel = new Dictionary<int, List<LookupItemDto>>();
        av.CardList = new List<AppEshopCatalogCardDto>();

        List<LookupItemDto> opTermList = new List<LookupItemDto>();
        opTermList.Add(new LookupItemDto() { Id = "1", Display = "Fall 2015" });
        opTermList.Add(new LookupItemDto() { Id = "2", Display = "Winter 2016" });
        opTermList.Add(new LookupItemDto() { Id = "3", Display = "Summer 2016" });
        av.DictOptionLevel.Add(1, opTermList);

        List<LookupItemDto> opCourseList = new List<LookupItemDto>();
        opCourseList.Add(new LookupItemDto() { Id = "1", Display = "Languages" });
        opCourseList.Add(new LookupItemDto() { Id = "2", Display = "Drama" });
        opCourseList.Add(new LookupItemDto() { Id = "3", Display = "Math" });
        opCourseList.Add(new LookupItemDto() { Id = "4", Display = "Art" });
        av.DictOptionLevel.Add(2, opCourseList);

        List<LookupItemDto> opCourseDetailList = new List<LookupItemDto>();
        opCourseDetailList.Add(new LookupItemDto() { Id = "2", Display = "French beginner" });
        opCourseDetailList.Add(new LookupItemDto() { Id = "14", Display = "French exit welcome class" });
        opCourseDetailList.Add(new LookupItemDto() { Id = "4", Display = "Leve2" });
        opCourseDetailList.Add(new LookupItemDto() { Id = "9", Display = "Leve3" });
        opCourseDetailList.Add(new LookupItemDto() { Id = "10", Display = "Beginner" });
        opCourseDetailList.Add(new LookupItemDto() { Id = "11", Display = "Intermead" });
        av.DictOptionLevel.Add(3, opCourseDetailList);

        AppEshopCatalogCardDto card1 = new AppEshopCatalogCardDto();
        card1.ImageUrl = "61";
        card1.Price = 133;
        card1.SingleProductSkuNo = "S0000001";
        card1.Display = new List<string>() { "Fall 2015", "Languages", "French beginner", " Tuesday 14:00-16:00 Teacher: Sean Zhang, Thursday 14:00-16:00 Teacher: Sean Zhang" };
        av.CardList.Add(card1);

        AppEshopCatalogCardDto card2 = new AppEshopCatalogCardDto();
        card2.ImageUrl = "62";
        card2.Price = 151;
        card2.SingleProductSkuNo = "S0000002";
        card2.Display = new List<string>() { "Fall 2015", "Languages", "French exit welcome class", " Sunday 14:00-15:00 Teacher: Sean Zhang" };
        av.CardList.Add(card2);

        AppEshopCatalogCardDto card3 = new AppEshopCatalogCardDto();
        card3.ImageUrl = "63";
        card3.Price = 233;
        card3.SingleProductSkuNo = "S0000003";
        card3.Display = new List<string>() { "Winter 2016", "Math", "Beginner", "Monday 10:00-11:00 Teacher: Sean Zhang, Tuesday 10:00-11:00 Teacher: Sean Zhang, Wednesday 10:00-11:00 Teacher: David Wu" };
        av.CardList.Add(card3);

        AppEshopCatalogCardDto card4 = new AppEshopCatalogCardDto();
        card4.ImageUrl = "64";
        card4.Price = 251;
        card4.SingleProductSkuNo = "S0000004";
        card4.Display = new List<string>() { "Winter 2016", "Math", "Intemedia", " Monday 10:00-11:00 Teacher: David Wu, Tuesday 10:00-11:00 Teacher: David Wu, Wednesday 10:00-11:00 Teacher: Sean Zhang" };
        av.CardList.Add(card4);
        return av;
    }

    [HttpGet]
    public AppEshopBagDto GetAppEshopBagDtoInfo()
    {
        AppEshopBagDto appEshopBagDto = new AppEshopBagDto();
        appEshopBagDto.Discount = (decimal)0.5;
        appEshopBagDto.ShippingAdress = new AppEshopShippingAdressDto();
        appEshopBagDto.ShippingAdress.City = "Montreal";

        appEshopBagDto.BillingAdress = new AppEshopShippingAdressDto();

        appEshopBagDto.EshopBagItemList = new List<AppEshopBagItemDto>();
        AppEshopBagItemDto aBagItemDto = new AppEshopBagItemDto();
        aBagItemDto.DetailId = "ABC123";
        aBagItemDto.Price = (decimal)123.00;
        aBagItemDto.DictOptionLevelSelectedValue = new Dictionary<int, object>();
        aBagItemDto.DictOptionLevelSelectedValue[1] = 1;
        aBagItemDto.DictOptionLevelSelectedValue[2] = 1;
        aBagItemDto.DictOptionLevelSelectedValue[3] = 1;
        aBagItemDto.DictOptionLevelSelectedValue[4] = 1;
        aBagItemDto.DictOptionLevelSelectedValue[5] = 1;

        aBagItemDto.Children = new List<AppEshopBagItemDto>();
        appEshopBagDto.EshopBagItemList.Add(aBagItemDto);

        AppEshopBagItemDto grancChildBagItem = new AppEshopBagItemDto();
        aBagItemDto.DetailId = "ABC123";
        aBagItemDto.Price = (decimal)123.00;
        aBagItemDto.Children.Add(grancChildBagItem);

        return appEshopBagDto;
    }

    [HttpPost]
    public AppMasterDetailDto ProcessShopBagDataTransfer(JObject appEshopBagDto)
    {
        // To do:
        //  1. need to set/get dataTransferSettingId some where
        int dataTransferSettingId = 12;

        AppMasterDetailDto newFormDataDto = AppMasterDetailFormDataLoadBL.GetNewFormDataByApiToFormDataTransfer(dataTransferSettingId, appEshopBagDto);

        if (newFormDataDto != null)
        {
            return AppMasterDetailFormDataSaveBL.SaveTransactionData(newFormDataDto).Object;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEshopBagDto> ProcessPaymentSuccessTransaction(AppEshopBagDto appEshopBagDto)
    {
        if (appEshopBagDto != null)
        {
            return AppEsiteBL.ProcessPaymentSuccessTransaction(appEshopBagDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEshopBagDto> SubmitGuestUserAddressInfoAndEmail(AppEshopBagDto appEshopBagDto)
    {
        if (appEshopBagDto != null)
        {
            return AppEsiteBL.SubmitGuestUserAddressInfoAndEmail(appEshopBagDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEshopBagDto> SubmitLoggedInUserAddressInfo(AppEshopBagDto appEshopBagDto)
    {
        if (appEshopBagDto != null)
        {
            return AppEsiteBL.SubmitLoggedInUserAddressInfo(appEshopBagDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppEshopBagDto> GetCurrentLoginUserCustomerInfo(AppEshopBagDto appEshopBagDto)
    {
        if (appEshopBagDto != null)
        {
            return AppEsiteBL.GetCurrentLoginUserCustomerInfo(appEshopBagDto);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> UpdateCurrentUserCustomerAddressInfo(AppEshopBagDto appEshopBagDto)
    {
        if (appEshopBagDto != null)
        {
            return AppEsiteBL.UpdateCurrentUserCustomerAddressInfo(appEshopBagDto);
        }

        return null;
    }

    [HttpGet]
    public List<AppEshopOrderDto> GetCurrentCustomerOrderList(int? esitId)
    {
        var toReturn = AppEsiteBL.GetCurrentCustomerOrderList(esitId);

        foreach (var orderDto in toReturn)
        {
            orderDto.OrderPlacedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(orderDto.OrderPlacedDate);
            orderDto.OrderShipDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(orderDto.OrderShipDate);
            orderDto.OrderExpectedDeliverDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(orderDto.OrderExpectedDeliverDate);
            orderDto.OrderDeliveredDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(orderDto.OrderDeliveredDate);
            orderDto.OrderCanceledDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(orderDto.OrderCanceledDate);
        }

        return toReturn;
    }
}
