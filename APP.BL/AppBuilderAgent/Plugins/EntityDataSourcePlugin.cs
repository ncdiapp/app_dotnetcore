using System;
using System.Linq;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Tools for Step 3 — Create Entity Data Sources (dropdown list definitions used in forms).
    ///
    /// Two types:
    ///   EntityType=4  → Simple static list (fixed items like statuses, categories)
    ///   EntityType=1  → Database table entity (dynamic rows from a table)
    /// </summary>
    public class EntityDataSourcePlugin
    {
        private readonly int? _dataSourceId;

        public EntityDataSourcePlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        [AgentFunction("list_entity_data_sources",
            "List all existing Entity Data Sources (dropdown definitions) in the platform. " +
            "Call this during explore_platform or before creating entities to avoid duplicates.")]
        public string ListEntityDataSources()
        {
            try
            {
                var list = AppEntityInfoBL.RetrieveAllAppEntityInfoDto()
                    .Select(e => new
                    {
                        e.Id,
                        e.EntityCode,
                        EntityType  = e.EntityType == 4 ? "SimpleList" : e.EntityType == 1 ? "DatabaseTable" : $"Type{e.EntityType}",
                        e.TableName,
                        e.SaasApplicationId
                    })
                    .ToList();

                return JsonConvert.SerializeObject(new { Count = list.Count, Entities = list }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }

        [AgentFunction("create_entity_simple_list",
            "Step 3a — Create a Simple List Entity Data Source: a static dropdown with fixed items " +
            "(e.g. Order Status: Approved / Rejected / Canceled). " +
            "Use this for short, fixed-value lists that don't come from a database table. " +
            "Provide items as a comma-separated string: 'Item1,Item2,Item3'.")]
        public string CreateEntitySimpleList(
            [AgentParam("Name/code for this entity, e.g. 'Order Status'", isRequired: true)]
            string entityCode,
            [AgentParam("Comma-separated list of item labels in order, e.g. 'Approved,Rejected,Canceled'", isRequired: true)]
            string items,
            [AgentParam("The SaasApplicationId returned by create_app_package", isRequired: true)]
            int saasApplicationId,
            [AgentParam("Optional description of this entity")]
            string description = null)
        {
            try
            {
                var itemList = items.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                if (itemList.Count == 0)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "No items provided" });

                var dto = new AppEntityInfoExDto
                {
                    Id                = null,
                    EntityCode        = entityCode,
                    Description       = description ?? string.Empty,
                    EntityType        = 4,      // Simple list
                    DataSourceFrom    = _dataSourceId,
                    SaasApplicationId = saasApplicationId,
                    IsModified        = true
                };

                dto.AppEntitySimpleListValueList = new ObservableSet<AppEntitySimpleListValueExDto>();
                for (int i = 0; i < itemList.Count; i++)
                {
                    dto.AppEntitySimpleListValueList.Add(new AppEntitySimpleListValueExDto
                    {
                        Sort        = i + 1,
                        InternalKey = i + 1,
                        Code        = itemList[i],
                        Description = string.Empty
                    });
                }

                var result = AppEntityInfoBL.SaveOneAppEntityInfoDto(dto);
                bool success = result?.ValidationResult?.HasErrors == false;

                if (!success)
                {
                    var errors = result?.ValidationResult?.Items?
                        .Where(x => x.ItemType == APP.Framework.Validation.ValidationItemType.Error).Select(x => x.Message).ToList();
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error     = errors != null && errors.Count > 0 ? string.Join("; ", errors) : "Save failed"
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess      = true,
                    EntityId       = result.Object?.Id,
                    EntityCode     = entityCode,
                    ItemCount      = itemList.Count,
                    Items          = itemList
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        [AgentFunction("create_entity_from_table",
            "Step 3b — Create a Database Table Entity Data Source: a dynamic dropdown backed by rows " +
            "from an existing database table (e.g. a Customer table where CustomerId is the value " +
            "and CustomerName is the display label). " +
            "The table must already exist in the database before calling this.")]
        public string CreateEntityFromTable(
            [AgentParam("Name/code for this entity, e.g. 'Customer List'", isRequired: true)]
            string entityCode,
            [AgentParam("Database table name without schema, e.g. 'AppCustomer'", isRequired: true)]
            string tableName,
            [AgentParam("Schema owner of the table, e.g. 'dbo'", isRequired: true)]
            string schemaOwner,
            [AgentParam("Column name to use as the selected value (primary key), e.g. 'CustomerId'", isRequired: true)]
            string identityField,
            [AgentParam("Primary display column shown to user, e.g. 'CustomerName'", isRequired: true)]
            string displayField1,
            [AgentParam("The SaasApplicationId returned by create_app_package", isRequired: true)]
            int saasApplicationId,
            [AgentParam("Optional secondary display column, e.g. 'Email'")]
            string displayField2 = null,
            [AgentParam("Optional tertiary display column")]
            string displayField3 = null,
            [AgentParam("Optional description of this entity")]
            string description = null)
        {
            try
            {
                var dto = new AppEntityInfoExDto
                {
                    Id                = null,
                    EntityCode        = entityCode,
                    Description       = description ?? string.Empty,
                    EntityType        = 1,      // Database table entity
                    DataSourceFrom    = _dataSourceId,
                    SaasApplicationId = saasApplicationId,
                    TableName         = tableName,
                    SchemaOwner       = schemaOwner,
                    IdentityField     = identityField,
                    DisplayFiled1     = displayField1,
                    DisplayFiled2     = displayField2 ?? string.Empty,
                    DisplayFiled3     = displayField3 ?? string.Empty,
                    IsModified        = true
                };

                var result = AppEntityInfoBL.SaveOneAppEntityInfoDto(dto);
                bool success = result?.ValidationResult?.HasErrors == false;

                if (!success)
                {
                    var errors = result?.ValidationResult?.Items?
                        .Where(x => x.ItemType == APP.Framework.Validation.ValidationItemType.Error).Select(x => x.Message).ToList();
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error     = errors != null && errors.Count > 0 ? string.Join("; ", errors) : "Save failed"
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess     = true,
                    EntityId      = result.Object?.Id,
                    EntityCode    = entityCode,
                    TableName     = tableName,
                    IdentityField = identityField,
                    DisplayField1 = displayField1,
                    DisplayField2 = displayField2,
                    DisplayField3 = displayField3
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }
    }
}
