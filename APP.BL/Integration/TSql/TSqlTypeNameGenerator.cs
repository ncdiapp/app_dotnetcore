using System;
using System.Collections.Generic;
using System.Text;
using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace ExchangeBL
{
    public class TSqlTypeNameGenerator : DefaultTypeNameGenerator
    {
        protected override string Generate(JsonSchema schema, string typeNameHint)
        {
            string returnTypeNameHint = base.Generate(schema, typeNameHint);

            if (returnTypeNameHint.Equals(typeNameHint, StringComparison.InvariantCultureIgnoreCase))
            {
                returnTypeNameHint = typeNameHint;
            }

            return returnTypeNameHint;
        }
    }
}
