using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class AppApplicationImportSettingDto
    {
        public AppApplicationImportSettingDto()
        {
            DictUserTableNameAndIsNeedToImport = new Dictionary<string, bool>();

            SystemConfigTableNameList = new List<string>() {
                "AppListMenu"
                ,"AppReport"
                ,"AppEntityInfo"
                ,"AppDataSet"
                ,"AppDataSetParameter"
                ,"AppSearch"
                ,"AppSearchField"
                ,"AppSearchParameter"
                ,"AppSearchSaved"
                ,"AppSearchSavedValue"
                ,"AppSearchView"
                ,"AppSearchViewField"
                ,"AppViewLinkedSeaechOrUrl"
                ,"AppTransaction"
                ,"AppTransactionUnit"
                ,"AppTransactionField"
                ,"AppTransactionFieldAggFunction"
                ,"AppTransactionUnitDeleteFlow"
                ,"AppTransactionUnitFormula"
                ,"AppConditionalAction"
                ,"AppFormLinkTarget"
                ,"AppTransactionUnitLinkedSearch"
                ,"AppTransactionUnitSearchFieldMapping"
                ,"AppTransactionUnitSearchViewFieldMapping"
                ,"AppTransactionDataTransferSetting"
                ,"AppTransactionSaveAsMapping"
                ,"AppTransactionNavigation"
                ,"AppTransactionDataLoad"
                ,"AppTranscationDataLoadFieldMapping"
                ,"AppTranscationReport"
                ,"AppMessage"
                ,"AppProjectOrWorkFlow"
                ,"AppProjectTaskPredecessor"
                ,"AppProjectWorkFlowAction"
                ,"AppProjectWorkFlowCondition"
                ,"AppProjectWorkFlowTask"
                ,"AppForm"
                ,"AppFormLayoutItem"
                ,"AppIntergrationSetting"
                ,"AppIntergrationSettingParameter"
                ,"AppWebAPIDataExchangeSetting"
                ,"AppWinScheduleSetting"
                ,"AppApplicationAssetsItem"
                ,"AppEsite"
                ,"AppEsiteCatalogue"
                ,"AppESiteNavMenu"
                ,"AppESitePages"
                ,"AppDesktop"
            };

            DictSystemTableOrgIdAndNewId = SystemConfigTableNameList.ToDictionary(o => o, o => new Dictionary<int, int>());
        }

        public int? ApplicationId { get; set; }

        public int? OrgApplicationId { get; set; }

        public Dictionary<string, bool> DictUserTableNameAndIsNeedToImport { get; set; }

        public List<string> SystemConfigTableNameList { get; set; }

        public Dictionary<string, Dictionary<int, int>> DictSystemTableOrgIdAndNewId { get; set; }


        public static List<string> UninstallTableNamesInOrder
        {
            get
            {
                List<string> tableNames = new List<string>() {
                    "AppIntergrationSettingParameter"
                    ,"AppIntergrationSetting"
                    ,"AppWebAPIDataExchangeSetting"
                    ,"AppWinScheduleSetting"
                    ,"AppApplicationAssetsItem"
                    ,"AppEsiteCatalogue"
                    ,"AppESiteNavMenu"
                    ,"AppESitePages"
                    ,"AppEsite"
                    ,"AppTransactionUnitSearchFieldMapping"
                    ,"AppTransactionUnitSearchViewFieldMapping"
                    ,"AppTransactionUnitLinkedSearch"
                    ,"AppFormLinkTarget"
                    ,"AppViewLinkedSeaechOrUrl"
                    ,"AppTransactionNavigation"
                    ,"AppTranscationReport"
                    ,"AppReport"
                    ,"AppProjectWorkFlowAction"
                    ,"AppSearchField"
                    ,"AppSearch"
                    ,"AppSearchViewField"
                    ,"AppSearchView"
                    ,"AppTransactionSaveAsMapping"
                    ,"AppTransactionDataTransferSetting"
                    ,"AppMessage"
                    ,"AppTranscationDataLoadFieldMapping"
                    ,"AppTransactionDataLoad"
                    ,"AppDataSetParameter"
                    ,"AppDataSet"
                    ,"AppTransactionUnitFormula"
                    ,"AppTransactionFieldAggFunction"
                    ,"AppConditionalAction"
                    ,"AppFormLayoutItem"
                    ,"AppForm"
                    ,"AppTransactionField"
                    ,"AppTransactionUnit"
                    ,"AppTransaction"
                    ,"AppEntityInfo"
                    ,"AppDesktop"
                    ,"AppListMenu"
                    
                };

                return tableNames;
            }
        }

        public static List<string> TablesWithGlobalGuid
        {
            get
            {
                List<string> tableNames = new List<string>() {
                    "AppListMenu"
                    ,"AppEntityInfo"
                    ,"AppMessage"
                    ,"AppDesktop"
                    ,"AppReport"
                    ,"AppEsite"
                    //,"AppSearch"
                    //,"AppSearchView"                    
                    //,"AppDataSet"
                    //,"AppTransaction"
                    
                    
                };

                return tableNames;
            }
        }

    }

}
