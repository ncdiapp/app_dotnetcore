using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        internal static bool HasPortableFormLayout(AppConfigPackTransactionDto tx)
        {
            return tx?.FormLayout?.Items != null && tx.FormLayout.Items.Count > 0;
        }

        internal static void ApplyTransactionFormLayout(int transactionId, AppConfigPackTransactionDto tx)
        {
            if (HasPortableFormLayout(tx))
            {
                ReplaceFlexFormLayout(transactionId, tx);
                return;
            }

            EnsureDefaultForm(transactionId);
            ApplyFormTabNames(transactionId, tx);
        }

        private static void ReplaceFlexFormLayout(int transactionId, AppConfigPackTransactionDto tx)
        {
            int formId = EnsureFlexFormShell(transactionId);
            var formEx = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(formId);
            if (formEx == null)
            {
                throw new InvalidOperationException(
                    $"Form shell was not found for transaction {tx.IntegrationId}.");
            }

            if (tx.FormLayout.DefaultNbColumns.HasValue)
                formEx.DefaultNbColumns = tx.FormLayout.DefaultNbColumns;
            if (!string.IsNullOrWhiteSpace(tx.FormLayout.DefaultWidth))
                formEx.DefaultWidth = tx.FormLayout.DefaultWidth.Trim();
            formEx.LayoutType = (int)EmAppFormLayoutType.Flex;

            var hostIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            formEx.AppFormLayoutItemList = new ObservableSet<AppFormLayoutItemExDto>();
            using (var conn = OpenTenantConnection())
            {
                int sort = 0;
                foreach (var node in tx.FormLayout.Items)
                {
                    if (node == null)
                        continue;
                    sort++;
                    formEx.AppFormLayoutItemList.Add(
                        BuildRuntimeLayoutItem(conn, transactionId, node, null, hostIds, sort));
                }
            }

            var saveResult = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(formEx);
            if (saveResult.ValidationResult != null && saveResult.ValidationResult.HasErrors)
            {
                throw new InvalidOperationException(
                    saveResult.ValidationResult.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to save form layout for '{tx.IntegrationId}'.");
            }

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
        }

        private static int EnsureFlexFormShell(int transactionId)
        {
            using (var conn = OpenTenantConnection())
            {
                int? formId = GetTransactionFormId(conn, transactionId);
                if (formId.HasValue)
                {
                    AppFormBL.EnsureAppFormLayoutTypeFlex(formId.Value);
                    return formId.Value;
                }
            }

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId)
                ?? AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            var formDto = AppFormBL.CreateNewTranactionForm(
                transactionId,
                (int)EmAppFormLayoutType.Flex,
                transactionExDto?.SaasApplicationId,
                false);
            int createdId = Convert.ToInt32(formDto.Id);
            AppFormBL.EnsureAppFormLayoutTypeFlex(createdId);
            return createdId;
        }

        private static AppFormLayoutItemExDto BuildRuntimeLayoutItem(
            SqlConnection conn,
            int transactionId,
            AppConfigPackFormLayoutItemDto node,
            AppFormLayoutItemExDto parent,
            HashSet<string> hostIds,
            int fallbackSort)
        {
            var item = new AppFormLayoutItemExDto();
            item.DomAttribute = new AppFormDomAttributeDto();
            item.AppFormLayoutItem_List = new ObservableSet<AppFormLayoutItemExDto>();
            item.FlowOrGridLayoutSortOrder = node.Sort ?? fallbackSort;
            item.CurrentHostId = NewPackLayoutHostId(hostIds);
            item.ParentHostId = parent?.CurrentHostId;
            item.DisplayTitle = string.IsNullOrWhiteSpace(node.DisplayName) ? null : node.DisplayName.Trim();

            string type = (node.Type ?? string.Empty).Trim();
            int? widget = node.WidgetDisplayType ?? WidgetDisplayTypeFromTypeName(type);
            bool isTab = node.IsTab == true
                || string.Equals(type, "tab", StringComparison.OrdinalIgnoreCase);
            if (isTab && !widget.HasValue)
                widget = (int)EmAppFormLayoutItemType.Section;

            string tableName = string.IsNullOrWhiteSpace(node.TableName) ? null : node.TableName.Trim();
            string columnName = string.IsNullOrWhiteSpace(node.ColumnName) ? null : node.ColumnName.Trim();
            bool isField = string.Equals(type, "field", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(columnName);
            bool isGrid = string.Equals(type, "grid", StringComparison.OrdinalIgnoreCase)
                || (widget == (int)EmAppFormLayoutItemType.Grid && string.IsNullOrWhiteSpace(columnName));
            bool isCommand = string.Equals(type, "commandButton", StringComparison.OrdinalIgnoreCase)
                || widget == (int)EmAppFormLayoutItemType.CommandActionButton;
            bool isLinkedSearch = string.Equals(type, "linkedSearch", StringComparison.OrdinalIgnoreCase)
                || widget == (int)EmAppFormLayoutItemType.LinkedSearch;

            if (isField)
            {
                if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
                {
                    throw new InvalidOperationException(
                        $"Form layout field node '{node.DisplayName ?? type}' is missing tableName/columnName.");
                }
                int? fieldId = GetTransactionFieldId(conn, transactionId, tableName, columnName);
                if (!fieldId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Form layout field '{tableName}.{columnName}' was not found.");
                }
                item.TransactionFieldId = fieldId.Value;
                if (!widget.HasValue)
                    widget = GetTransactionFieldControlType(conn, fieldId.Value) ?? (int)EmAppFormLayoutItemType.TextBox;
            }
            else if (isGrid)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new InvalidOperationException(
                        $"Form layout grid node '{node.DisplayName ?? type}' is missing tableName.");
                }
                int? unitId = GetTransactionUnitId(conn, transactionId, tableName);
                if (!unitId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Form layout grid unit '{tableName}' was not found.");
                }
                item.GridTransactionUnitId = unitId.Value;
                if (!widget.HasValue)
                    widget = (int)EmAppFormLayoutItemType.Grid;
            }
            else if (isCommand)
            {
                if (string.IsNullOrWhiteSpace(node.CommandName))
                {
                    throw new InvalidOperationException(
                        $"Form layout commandButton '{node.DisplayName ?? type}' is missing commandName.");
                }
                int? commandId = GetCommandIdByName(conn, transactionId, node.CommandName);
                if (!commandId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Form layout command '{node.CommandName}' was not found on this transaction.");
                }
                item.DomAttribute.CommandActionId = commandId.Value;
                widget = (int)EmAppFormLayoutItemType.CommandActionButton;
            }
            else if (isLinkedSearch)
            {
                if (string.IsNullOrWhiteSpace(node.SearchIntegrationId))
                {
                    throw new InvalidOperationException(
                        $"Form layout linkedSearch '{node.DisplayName ?? type}' is missing searchIntegrationId.");
                }
                int? searchId = GetSearchIdByIntegrationId(conn, node.SearchIntegrationId);
                if (!searchId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Form layout linkedSearch '{node.SearchIntegrationId}' was not found.");
                }
                item.AutoExcuteSearchId = searchId.Value;
                item.DomAttribute.LinkedSearchId = searchId.Value;
                widget = (int)EmAppFormLayoutItemType.LinkedSearch;
            }

            if (!string.IsNullOrWhiteSpace(node.EntityCode))
                item.DomAttribute.EntityId = ResolveEntityIdByCode(conn, node.EntityCode);

            item.DomAttribute.WidgetDisplayType = widget;
            item.WidgetItemType = widget;
            item.DomAttribute.DisplayName = node.DisplayName;
            item.DomAttribute.DefaultNbColumns = node.DefaultNbColumns;
            item.DomAttribute.ColSpanValue = node.ColSpan;
            item.DomAttribute.HeightValue = node.Height;
            item.DomAttribute.IsUnlimitedHeight = node.IsUnlimitedHeight == true;
            item.DomAttribute.BackgroundColor = node.BackgroundColor;
            item.DomAttribute.TextColor = node.TextColor;
            item.DomAttribute.IsHideLabel = node.IsHideLabel == true;
            item.DomAttribute.LabelWidth = node.LabelWidth;
            item.DomAttribute.EmUnitLabelPosition = node.EmUnitLabelPosition;
            item.DomAttribute.IsCollapsible = node.IsCollapsible == true;
            item.DomAttribute.IsDefaultCollapsed = node.IsDefaultCollapsed == true;
            item.DomAttribute.IsTab = isTab;
            item.DomAttribute.IsBindingToDataField = node.IsBindingToDataField
                ?? (isField || isGrid);
            item.DomAttribute.TranscationUnitLevel = node.TranscationUnitLevel
                ?? (isGrid ? 2 : (int?)null);
            item.DomAttribute.ColumnWidth = node.ColumnWidth;
            item.DomAttribute.HtmlContent = node.HtmlContent;
            item.DomAttribute.VisibleExpression = node.VisibleExpression;
            item.DomAttribute.InlineStyle = node.InlineStyle ?? string.Empty;
            item.DomAttribute.IsShowSearchCriterias = node.IsShowSearchCriterias == true;
            item.DomAttribute.IsDisplayGridAsCardList = node.IsDisplayGridAsCardList == true;
            item.DomAttribute.IsDisplayAsSlider = node.IsDisplayAsSlider == true;
            item.DomAttribute.NbDecimal = node.NbDecimal;

            int childSort = 0;
            foreach (var child in node.Children ?? Enumerable.Empty<AppConfigPackFormLayoutItemDto>())
            {
                if (child == null)
                    continue;
                childSort++;
                item.AppFormLayoutItem_List.Add(
                    BuildRuntimeLayoutItem(conn, transactionId, child, item, hostIds, childSort));
            }

            return item;
        }

        private static AppConfigPackFormLayoutDto ExportFormLayout(SqlConnection conn, int formId, int transactionId)
        {
            var formEx = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(formId);
            if (formEx?.AppFormLayoutItemList == null || formEx.AppFormLayoutItemList.Count == 0)
                return null;

            var dto = new AppConfigPackFormLayoutDto
            {
                DefaultNbColumns = formEx.DefaultNbColumns,
                DefaultWidth = string.IsNullOrWhiteSpace(formEx.DefaultWidth) ? null : formEx.DefaultWidth,
                Items = new List<AppConfigPackFormLayoutItemDto>()
            };

            foreach (var root in formEx.AppFormLayoutItemList
                .OrderBy(i => i.FlowOrGridLayoutSortOrder ?? 9999))
            {
                var node = ExportOneLayoutItem(conn, transactionId, root);
                if (node != null)
                    dto.Items.Add(node);
            }

            return dto.Items.Count == 0 ? null : dto;
        }

        private static AppConfigPackFormLayoutItemDto ExportOneLayoutItem(
            SqlConnection conn,
            int transactionId,
            AppFormLayoutItemExDto item)
        {
            if (item == null)
                return null;

            var attr = item.DomAttribute ?? new AppFormDomAttributeDto();
            int? widget = attr.WidgetDisplayType ?? item.WidgetItemType;
            bool isTab = attr.IsTab;
            bool hasField = item.TransactionFieldId.HasValue;
            bool hasGrid = item.GridTransactionUnitId.HasValue;
            bool hasCommand = widget == (int)EmAppFormLayoutItemType.CommandActionButton
                || attr.CommandActionId.HasValue;
            bool hasSearch = widget == (int)EmAppFormLayoutItemType.LinkedSearch
                || attr.LinkedSearchId.HasValue
                || item.AutoExcuteSearchId.HasValue;

            string type = TypeNameFromWidget(widget, isTab, hasField, hasGrid, hasCommand, hasSearch);
            var node = new AppConfigPackFormLayoutItemDto
            {
                Type = type,
                DisplayName = FirstNonEmpty(attr.DisplayName, item.DisplayTitle),
                Sort = item.FlowOrGridLayoutSortOrder,
                DefaultNbColumns = attr.DefaultNbColumns,
                ColSpan = attr.ColSpanValue,
                Height = attr.HeightValue,
                IsUnlimitedHeight = attr.IsUnlimitedHeight ? true : (bool?)null,
                BackgroundColor = attr.BackgroundColor,
                TextColor = attr.TextColor,
                IsHideLabel = attr.IsHideLabel ? true : (bool?)null,
                LabelWidth = attr.LabelWidth,
                EmUnitLabelPosition = attr.EmUnitLabelPosition,
                IsCollapsible = attr.IsCollapsible ? true : (bool?)null,
                IsDefaultCollapsed = attr.IsDefaultCollapsed ? true : (bool?)null,
                IsTab = isTab ? true : (bool?)null,
                IsBindingToDataField = attr.IsBindingToDataField ? true : (bool?)null,
                TranscationUnitLevel = attr.TranscationUnitLevel,
                ColumnWidth = attr.ColumnWidth,
                HtmlContent = string.IsNullOrWhiteSpace(attr.HtmlContent) ? null : attr.HtmlContent,
                VisibleExpression = string.IsNullOrWhiteSpace(attr.VisibleExpression) ? null : attr.VisibleExpression,
                InlineStyle = string.IsNullOrWhiteSpace(attr.InlineStyle) ? null : attr.InlineStyle,
                IsShowSearchCriterias = attr.IsShowSearchCriterias ? true : (bool?)null,
                IsDisplayGridAsCardList = attr.IsDisplayGridAsCardList ? true : (bool?)null,
                IsDisplayAsSlider = attr.IsDisplayAsSlider ? true : (bool?)null,
                NbDecimal = attr.NbDecimal
            };

            int? implied = WidgetDisplayTypeFromTypeName(type);
            if (type == "tab")
                implied = (int)EmAppFormLayoutItemType.Section;
            if (widget.HasValue && widget != implied)
                node.WidgetDisplayType = widget;
            else if (type == "field" && widget.HasValue)
                node.WidgetDisplayType = widget;
            else if (type == "widget" && widget.HasValue)
                node.WidgetDisplayType = widget;

            if (hasField)
            {
                var loc = ResolveFieldTableColumn(conn, item.TransactionFieldId.Value);
                node.TableName = loc.Table;
                node.ColumnName = loc.Column;
            }
            else if (hasGrid)
            {
                node.TableName = item.ForeignAppTransactionUnitExDto?.DataBaseTableName
                    ?? GetUnitTableName(conn, item.GridTransactionUnitId.Value);
            }

            if (hasCommand)
            {
                int? commandId = attr.CommandActionId;
                node.CommandName = item.BindToCommandAction?.Name
                    ?? (commandId.HasValue ? GetCommandNameById(conn, commandId.Value) : null);
            }

            int? searchId = item.AutoExcuteSearchId ?? attr.LinkedSearchId;
            if (hasSearch && searchId.HasValue)
                node.SearchIntegrationId = GetSearchIntegrationId(conn, searchId.Value);

            if (attr.EntityId.HasValue)
                node.EntityCode = GetEntityCodeById(conn, attr.EntityId);

            var children = (item.AppFormLayoutItem_List ?? new ObservableSet<AppFormLayoutItemExDto>())
                .OrderBy(c => c.FlowOrGridLayoutSortOrder ?? 9999)
                .Select(c => ExportOneLayoutItem(conn, transactionId, c))
                .Where(c => c != null)
                .ToList();
            if (children.Count > 0)
                node.Children = children;

            return node;
        }

        private static string TypeNameFromWidget(
            int? widget,
            bool isTab,
            bool hasField,
            bool hasGrid,
            bool hasCommand,
            bool hasSearch)
        {
            if (hasField)
                return "field";
            if (hasGrid)
                return "grid";
            if (hasCommand)
                return "commandButton";
            if (hasSearch)
                return "linkedSearch";
            if (isTab || widget == (int)EmAppFormLayoutItemType.Tab)
                return "tab";
            if (!widget.HasValue)
                return "widget";
            switch (widget.Value)
            {
                case (int)EmAppFormLayoutItemType.LayoutRow: return "row";
                case (int)EmAppFormLayoutItemType.Section: return "stack";
                case (int)EmAppFormLayoutItemType.Content: return "content";
                case (int)EmAppFormLayoutItemType.NewItemAddButton: return "addButton";
                case (int)EmAppFormLayoutItemType.Space: return "space";
                case (int)EmAppFormLayoutItemType.CommandActionButton: return "commandButton";
                case (int)EmAppFormLayoutItemType.TabContainer: return "tabContainer";
                case (int)EmAppFormLayoutItemType.LinkedSearch: return "linkedSearch";
                case (int)EmAppFormLayoutItemType.TableContainer: return "tableContainer";
                case (int)EmAppFormLayoutItemType.HtmlContentContainer: return "htmlContentContainer";
                case (int)EmAppFormLayoutItemType.Grid: return "grid";
                default: return "widget";
            }
        }

        private static int? WidgetDisplayTypeFromTypeName(string type)
        {
            switch ((type ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "row": return (int)EmAppFormLayoutItemType.LayoutRow;
                case "stack":
                case "section": return (int)EmAppFormLayoutItemType.Section;
                case "content": return (int)EmAppFormLayoutItemType.Content;
                case "addbutton": return (int)EmAppFormLayoutItemType.NewItemAddButton;
                case "space": return (int)EmAppFormLayoutItemType.Space;
                case "commandbutton": return (int)EmAppFormLayoutItemType.CommandActionButton;
                case "tabcontainer": return (int)EmAppFormLayoutItemType.TabContainer;
                case "tab": return (int)EmAppFormLayoutItemType.Section;
                case "linkedsearch": return (int)EmAppFormLayoutItemType.LinkedSearch;
                case "tablecontainer": return (int)EmAppFormLayoutItemType.TableContainer;
                case "htmlcontentcontainer": return (int)EmAppFormLayoutItemType.HtmlContentContainer;
                case "grid": return (int)EmAppFormLayoutItemType.Grid;
                default: return null;
            }
        }

        private static string NewPackLayoutHostId(HashSet<string> hostIds)
        {
            for (int i = 0; i < 32; i++)
            {
                string id = Guid.NewGuid().ToString("N").Substring(0, 10);
                if (hostIds.Add(id))
                    return id;
            }
            string fallback = Guid.NewGuid().ToString("N");
            hostIds.Add(fallback);
            return fallback;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return null;
        }

        private static int? GetCommandIdByName(SqlConnection conn, int transactionId, string name)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 WorkFlowActionID
FROM dbo.AppProjectWorkFlowAction
WHERE CommandTransactionID = @TxId AND Name = @Name
ORDER BY WorkFlowActionID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@Name", name.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetCommandNameById(SqlConnection conn, int commandId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Name FROM dbo.AppProjectWorkFlowAction WHERE WorkFlowActionID = @Id";
                cmd.Parameters.AddWithValue("@Id", commandId);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static string GetUnitTableName(SqlConnection conn, int unitId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DataBaseTableName FROM dbo.AppTransactionUnit WHERE TransactionUnitID = @Id";
                cmd.Parameters.AddWithValue("@Id", unitId);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static int? GetTransactionFieldControlType(SqlConnection conn, int fieldId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT ControlType FROM dbo.AppTransactionField WHERE TransactionFieldID = @Id";
                cmd.Parameters.AddWithValue("@Id", fieldId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetSearchIntegrationId(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT IntegrationId FROM dbo.AppSearch WHERE SearchID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchId);
                var val = cmd.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
        }
    }
}
