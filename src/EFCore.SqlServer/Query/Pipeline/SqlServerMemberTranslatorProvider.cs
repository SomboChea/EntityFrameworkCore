// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Relational.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerMemberTranslatorProvider : RelationalMemberTranslatorProvider
    {
        public SqlServerMemberTranslatorProvider(IRelationalTypeMappingSource relationalTypeMappingSource)
        {
            AddTranslators(
                new[] {
                    new SqlServerDateTimeMemberTranslator(relationalTypeMappingSource)
                });
        }
    }
}
