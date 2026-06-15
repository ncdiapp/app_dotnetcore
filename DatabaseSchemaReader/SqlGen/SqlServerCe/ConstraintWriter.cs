using DatabaseSchemaMrg.DataSchema;

namespace DatabaseSchemaMrg.SqlGen.SqlServerCe
{
    class ConstraintWriter : SqlServer.ConstraintWriter
    {
        public ConstraintWriter(DatabaseTable table)
            : base(table)
        {
        }

        protected override ISqlFormatProvider SqlFormatProvider()
        {
            return new SqlServerCeFormatProvider();
        }
    }
}
