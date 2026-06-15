using System.Data;
using DatabaseSchemaMrg.Conversion.KeyMaps;

namespace DatabaseSchemaMrg.Conversion
{
    class ViewColumnConverter : ColumnConverter
    {
        public ViewColumnConverter(DataTable columnsDataTable) : base(columnsDataTable)
        {
        }

        protected override ColumnsKeyMap LoadColumnsKeyMap()
        {
            var columnsKeyMap = new ColumnsKeyMap(ColumnsDataTable);
            if (ColumnsDataTable.Columns.Contains("VIEW_NAME")) columnsKeyMap.TableKey = "VIEW_NAME";
            return columnsKeyMap;
        }
    }
}
