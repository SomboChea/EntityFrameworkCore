// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Relational.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerStringLengthTranslator : IMemberTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public SqlServerStringLengthTranslator(IRelationalTypeMappingSource typeMappingSource)
        {
            _typeMappingSource = typeMappingSource;
        }

        public Expression Translate(MemberExpression memberExpression)
        {
            if (memberExpression.Expression is SqlExpression sql
                && sql.Type == typeof(string)
                && memberExpression.Member.Name == nameof(string.Length))
            {
                var intTypeMapping = _typeMappingSource.FindMapping(typeof(int));

                return new SqlExpression(
                    new SqlCastExpression(
                        new SqlExpression(
                            new SqlFunctionExpression(
                                null,
                                "LEN",
                                null,
                                new[] { sql },
                                memberExpression.Type),
                            intTypeMapping),
                        memberExpression.Type,
                        intTypeMapping.StoreType),
                    intTypeMapping);
            }

            return null;
        }
    }
}
