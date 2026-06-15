using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// DTO class for the entity 'AppTransactionField'.
    /// </summary>


    public partial class AppTransactionFieldExDto
    {


        // no need to tranfer to UI
        public bool IsChangedNeedToCascading
        {
            get;
            set;
        }

        public bool IsHasTrigerCondition
        {
            get;
            set;
        }

        // no need to tranfer to UI
        public bool IsUsedCascadingDataSource
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSiblingField
        {
            get;
            set;
        }


        public static string GetRootorSiblingBindField(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string modelBindField = string.Empty;

            if (aAppTransactionFieldExDto.IsSiblingField)
            {
                modelBindField = "dataModel.currentFormData.DictSiblingOneToOneFields['" + aAppTransactionFieldExDto.TransactionUnitId + "']['" + aAppTransactionFieldExDto.DataBaseFieldName + "']"; ;
            }
            else
            {
                modelBindField = "dataModel.currentFormData.DictOneToOneFields['" + aAppTransactionFieldExDto.DataBaseFieldName + "']";
            }

            return modelBindField;

        }

        public static string GetChildUnitRowEditFieldBinding(AppTransactionFieldExDto transField)
        {
            return "dataModel.currentEditChildGridRow.DictOneToOneFields." + transField.DataBaseFieldName;
        }

        public static string GetNeedToHideBindField(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string modelBindField = "dataModel.currentFormData.DictOneToOneConditionHideFields['" + aAppTransactionFieldExDto.Id + "']";
            return modelBindField;
        }

        public static string GetLockingBindField(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string modelBindField = "dataModel.currentFormData.IsLockTransaction || dataModel.currentFormData.DictOneToOneLockFields['" + aAppTransactionFieldExDto.Id + "']"
                + " || dataModel.currentFormData.DictLockUnitIds['" + aAppTransactionFieldExDto.TransactionUnitId.ToString() + "']";
            return modelBindField;
        }

        public static string GetTransfieldIsRequiredSymbol(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string toReturn = "*";

            if (aAppTransactionFieldExDto.IsAllowEmpty.HasValue && aAppTransactionFieldExDto.IsAllowEmpty.Value
                || aAppTransactionFieldExDto.IsReadonly.HasValue && aAppTransactionFieldExDto.IsReadonly.Value
                || aAppTransactionFieldExDto.IsFormLayoutReadOnly.HasValue && aAppTransactionFieldExDto.IsFormLayoutReadOnly.Value)
            {
                toReturn = string.Empty;
            }

            return toReturn;
        }


        public static string GetPrimaryKeyReadonlyBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            if (aAppTransactionFieldExDto.IsPrimaryKey && !(aAppTransactionFieldExDto.IsReadonly.HasValue && aAppTransactionFieldExDto.IsReadonly.Value))
            {
                return "controllerModel.formRequestMode == 'Edit'";
            }

            return "false";
        }

        //public static string GetGridFieldReadonlyBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        //{
        //    bool isFieldReadonly = aAppTransactionFieldExDto.IsReadonly.HasValue && aAppTransactionFieldExDto.IsReadonly.Value;

        //    if (isFieldReadonly)
        //    {
        //        return "true";
        //    }
        //    else
        //    {
        //        if (aAppTransactionFieldExDto.IsPrimaryKey)
        //        {
        //            return "$item.isNewRow? 'false':'true'";
        //        }

        //        return "false";
        //    }
        //}      





        public static string GetFileDisplayBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            // dataModel.currentFormData.DictDocumentIdFileCode[ (dataModel.currentFormData.DictOneToOneFields['@aAppTransactionFieldExDto.DataBaseFieldName']).toString() ]
            string fieldBiding = GetRootorSiblingBindField(aAppTransactionFieldExDto);
            string toReturn = "dataModel.currentFormData.DictDocumentIdFileCode[(" + fieldBiding + ").toString()]";
            return toReturn;
        }

        public static string GetChildUnitRowEditFieldFileDisplayBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {            
            string fieldBiding = GetChildUnitRowEditFieldBinding(aAppTransactionFieldExDto);
            string toReturn = "dataModel.currentFormData.DictDocumentIdFileCode[(" + fieldBiding + ").toString()]";
            return toReturn;
        }


        public static string GetFolderDisplayBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            // dataModel.currentFormData.DictDocumentIdFileCode[ (dataModel.currentFormData.DictOneToOneFields['@aAppTransactionFieldExDto.DataBaseFieldName']).toString() ]
            string fieldBiding = GetRootorSiblingBindField(aAppTransactionFieldExDto);
            string toReturn = "dataModel.currentFormStructure.DictFolderIdFolderDisplay[(" + fieldBiding + ").toString()]";
            return toReturn;
        }

        public static string GetFolderPathDisplayBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            // dataModel.currentFormData.DictDocumentIdFileCode[ (dataModel.currentFormData.DictOneToOneFields['@aAppTransactionFieldExDto.DataBaseFieldName']).toString() ]
            string fieldBiding = GetRootorSiblingBindField(aAppTransactionFieldExDto);
            string toReturn = "dataModel.currentFormData.DictFolderIdAndPath[(" + fieldBiding + ").toString()]";
            return toReturn;
        }

        public static string GetFieldValidationCssClass(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            //string reReturn = "{invalidField: dataModel.dictRootFieldIsInvalid['" + aAppTransactionFieldExDto.Id + "']}";
            //return reReturn;
            string binding = string.Empty;

            if (aAppTransactionFieldExDto.IsSiblingField)
            {
                binding = "{invalidField: dataModel.validationResultDto.DictSiblingOneToOneFields['" + aAppTransactionFieldExDto.TransactionUnitId + "']['" + aAppTransactionFieldExDto.DataBaseFieldName + "']}";
            }
            else
            {
                binding = "{invalidField: dataModel.validationResultDto.DictOneToOneFields['" + aAppTransactionFieldExDto.DataBaseFieldName + "']}";
            }

            return binding.Trim();
        }

        public static string GetFieldUiValidationFunction(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string functionExpression = string.Empty;

            string isNeedValidate = (aAppTransactionFieldExDto.NeedValidator.HasValue && aAppTransactionFieldExDto.NeedValidator.Value) ? "true" : "false";
            string fieldId = aAppTransactionFieldExDto.Id.ToString();
            string sibUnitId = string.Empty;
            string fieldDbName = aAppTransactionFieldExDto.DataBaseFieldName;
            string fieldDisplayName = aAppTransactionFieldExDto.DisplayName;
            string isRequired = (aAppTransactionFieldExDto.IsAllowEmpty.HasValue && !aAppTransactionFieldExDto.IsAllowEmpty.Value) ? "true" : "false";
            int minCharLength = 0;
            int maxCharLength = 0;
            string regExp = string.Empty;
            string bindField = AppTransactionFieldExDto.GetRootorSiblingBindField(aAppTransactionFieldExDto);

            if (aAppTransactionFieldExDto.IsSiblingField)
            {
                sibUnitId = aAppTransactionFieldExDto.TransactionUnitId.ToString();
            }

            if (aAppTransactionFieldExDto.ControlType == (int)EmAppControlType.TextBox
                || aAppTransactionFieldExDto.ControlType == (int)EmAppControlType.Memo
                || aAppTransactionFieldExDto.ControlType == (int)EmAppControlType.Numeric)
            {
                minCharLength = 0;
                maxCharLength = aAppTransactionFieldExDto.MaxCharLegnth.HasValue ? aAppTransactionFieldExDto.MaxCharLegnth.Value : 0;
            }

            functionExpression = string.Format("uiValidateOneRootField({0}, '{1}', '{2}', '{3}', '{4}', {5}, {6}, {7}, {8}, '{9}')",
                isNeedValidate, sibUnitId, fieldId, fieldDbName, fieldDisplayName, bindField, isRequired, minCharLength, maxCharLength, regExp);

            return functionExpression.Trim();
        }

        public static string GetFieldErrorMessageBinding(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string binding = string.Empty;

            if (aAppTransactionFieldExDto.IsSiblingField)
            {
                binding = "dataModel.validationResultDto.DictSiblingOneToOneFieldNameAndErrorMessage['" + aAppTransactionFieldExDto.TransactionUnitId + "']['" + aAppTransactionFieldExDto.DataBaseFieldName + "']";
            }
            else
            {
                binding = "dataModel.validationResultDto.DictOneToOneFieldNameAndErrorMessage['" + aAppTransactionFieldExDto.DataBaseFieldName + "']";
            }

            return binding.Trim();
        }


        public string LabelDisplayBinding
        {
            get
            {
                var fieldExDto = this;

                string binding = fieldExDto.DisplayName;

                if (fieldExDto.MasterEntityFieldlId.HasValue && !string.IsNullOrWhiteSpace(fieldExDto.InnerEntityLabelSubscribeFiled))
                {
                    binding = "{{(dataModel.currentFormData.DictCascadingFieldIdAndLabel['" + fieldExDto.Id + "']) || '" + fieldExDto.DisplayName + "'}}";
                }

                return binding;
            }            
        }


        public static string GetFieldItemSource(AppTransactionFieldExDto aAppTransactionFieldExDto)
        {
            string reReturn = "dataModel.dictFieldEntityDataSource['" + aAppTransactionFieldExDto.Id + "']";
            return reReturn;
        }

        public static string GetChildUnitRowEditFieldItemSource(AppTransactionFieldExDto transField)
        {
            string reReturn = "dataModel.dictCurrentEditChildRowCascadingFiledIdAndCV['" + transField.Id.ToString() + "'] || dataModel.dictFieldEntityDataMap['" + transField.Id.ToString() + "'].collectionView";
            return reReturn;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldExDto> CascadngChildren
        {
            get;
            set;
        }

        // [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionFieldExDto> InnerEntityChildren
        {
            get;
            set;
        }


        // key:  transaction dbfileName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> InputParentTransFiledNameSqlParaMapping
        {
            get;
            set;
        }

        // key: child UnitID, key: transaction dbfileName
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> InputChildTransFiledNameSqlParaNameMapping
        {
            get;
            set;
        }


        //key child unitId
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> InputChildDataSetSqlParaNameMapping
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> OutputTransactionFieldAndSpResultFieldMapping
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsUsedRetrieveDataMappingDataSource
        {
            get;
            set;
        }

        public string FormulaDisplayName
        {
            get;
            set;
        }

        public bool? IsFormLayoutVisible
        {
            get;
            set;
        }


        private bool? _isFormLayoutReadOnly = null;

        [DataMember(EmitDefaultValue = false)]
        public bool? IsFormLayoutReadOnly
        {
            get
            {
                if (!_isFormLayoutReadOnly.HasValue)
                {
                    _isFormLayoutReadOnly = IsReadonly;
                }

                return _isFormLayoutReadOnly;
            }
            set
            {
                _isFormLayoutReadOnly = value;
            }
        }

        public string LabelDisplayName
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public object PrintValue
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public string DataContextPrefix
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public AppFormDomAttributeDto DomAttribute
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string StyleLayoutInfo
        {
            get;
            set;
        }

        public void ConvertDataRetrieveMappingStringToDict()
        {
            string storProc = CascadingRelationTable;
            string inputParatmers = CascadingRelationTableParentKeyField;
            string output = CascadingRelationTableChildKeyField;

            string[] inputSet = inputParatmers.Split(";".ToCharArray());

            // no child set

            InputChildTransFiledNameSqlParaNameMapping = new Dictionary<string, Dictionary<string, string>>();
            InputParentTransFiledNameSqlParaMapping = new Dictionary<string, string>();
            InputChildDataSetSqlParaNameMapping = new Dictionary<string, string>();
            if (inputSet.Length == 1)
            {
                if (ControlType == (int)EmAppControlType.RetrieveData)
                {
                    string rootInput = inputSet[0];
                    InputParentTransFiledNameSqlParaMapping = AppTransactionFieldExDto.SplitInputStringAsDictTransFieldSqlParameterMapping(rootInput);
                }


            }// for two set 
            else if (inputSet.Length >= 2)
            {
                string rootInput = inputSet[0];
                InputParentTransFiledNameSqlParaMapping = AppTransactionFieldExDto.SplitInputStringAsDictTransFieldSqlParameterMapping(rootInput);



                for (int i = 1; i < inputSet.Length; i++)
                {

                    string childSet = inputSet[i];

                    if (childSet.IndexOf("=") != -1)
                    {

                        string[] childUnitStrings = childSet.Split("=".ToCharArray());

                        string unitIdParakey = childUnitStrings[0];

                        //1234->@CourseCalendar=column1:ChildTransFiled1 | column2: ChildTransFiled2 | | column3: ChildTransFiled3;

                        string unitpara = childUnitStrings[1];

                        //222>@CourseCalendar
                        Dictionary<string, string> childTransFieSql = AppTransactionFieldExDto.SplitInputStringAsDictTransFieldSqlParameterMapping(unitpara);

                        InputChildTransFiledNameSqlParaNameMapping.Add(unitIdParakey, childTransFieSql);


                    }

                }


            }

            if (!output.IsEmpty())
            {

                OutputTransactionFieldAndSpResultFieldMapping = AppTransactionFieldExDto.SplitInputStringAsDictTransFieldSqlParameterMapping(output);

            }

        }


        public void ConvertBackDataRetrieveMappingDictToString()
        {
            //  string storProc = CascadingRelationTable;
            if (ControlType == (int)EmAppControlType.RetrieveData)
            {

                string inputParent = ConmbinePairString(InputParentTransFiledNameSqlParaMapping);

                string inputChildPara = string.Empty;
                ////pararentCscading: StartDate:@startDate|EndDate:@endDate;222>@CourseCalendar=WeekDayId:WeekDayId|StartTime:StartTime|EndTime:EndTime;
                foreach (string unitISqlName in InputChildTransFiledNameSqlParaNameMapping.Keys)
                {

                    //222>@CourseCalendar as key
                    inputChildPara = inputChildPara + ";" + unitISqlName + "=";

                    Dictionary<string, string> transFiledcolumn = InputChildTransFiledNameSqlParaNameMapping[unitISqlName];

                    inputChildPara = inputChildPara + ConmbinePairString(transFiledcolumn) + ";";
                }

                if (inputChildPara != string.Empty)
                {
                    // remove last index ";"
                    inputChildPara = inputChildPara.Substring(0, inputChildPara.Length - 1);
                }

                this.CascadingRelationTableParentKeyField = inputParent + inputChildPara;
            }

            CascadingRelationTableChildKeyField = ConmbinePairString(this.OutputTransactionFieldAndSpResultFieldMapping);

        }

        private static string ConmbinePairString(Dictionary<string, string> transFiledcolumn)
        {
            string inpurPara = string.Empty;

            foreach (string filedName in transFiledcolumn.Keys)
            {
                inpurPara = inpurPara + filedName + ":" + transFiledcolumn[filedName] + "|";
            }

            // remove last index of "|"
            if (inpurPara != string.Empty)
            {
                inpurPara = inpurPara.Substring(0, inpurPara.Length - 1);

            }
            return inpurPara;
        }

        private string ConmbineOutputPairString(string inpurPara)
        {
            foreach (string filedName in InputParentTransFiledNameSqlParaMapping.Keys)
            {
                inpurPara = inpurPara + filedName + ":" + InputParentTransFiledNameSqlParaMapping[filedName] + "|";
            }

            // remove last index of "|"
            if (inpurPara != string.Empty)
            {
                inpurPara = inpurPara.Substring(0, inpurPara.Length - 1);

            }
            return inpurPara;
        }

        public static Dictionary<string, string> SplitInputStringAsDictTransFieldSqlParameterMapping(string childKey)
        {
            Dictionary<string, string> dictDataSetTransasFieldMapping = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(childKey))
            {

                string[] listString = childKey.Split("|".ToArray());
                foreach (string dataSetTransFieldString in listString)
                {
                    string[] dataSetTransField = dataSetTransFieldString.Split(":".ToArray());
                    dictDataSetTransasFieldMapping.Add(dataSetTransField[0], dataSetTransField[1]);



                }

            }
            return dictDataSetTransasFieldMapping;
        }


        public static List<KeyValuePair<string, string>> SplitInputStringAsKeyValuePairList(string childKey)
        {
            return SplitInputStringAsDictTransFieldSqlParameterMapping(childKey).ToList();
        }


        [DataMember(EmitDefaultValue = false)]
        public TransactionFieldChangeSettingDto FieldChangeSetting
        {
            get;
            set;
        }
    }



    public partial class TransactionFieldChangeSettingDto
    {
        public bool IsChagneFromDDLToOtherType { get; set; }

        public bool IsChangeFromOtherTypeToDDL { get; set; }

        public int? OrgEntityId { get; set; }

        public int? NewEntityId { get; set; }

        public string MappingToEntityColumnName { get; set; }




    }
}

