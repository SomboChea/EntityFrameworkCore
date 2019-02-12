// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Relational.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerDateTimeMemberTranslator : IMemberTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public SqlServerDateTimeMemberTranslator(IRelationalTypeMappingSource typeMappingSource)
        {
            _typeMappingSource = typeMappingSource;
        }

        public Expression Translate(MemberExpression memberExpression)
        {
            var declaringType = memberExpression.Member.DeclaringType;
            if (declaringType == typeof(DateTime))
            {
                var memberName = memberExpression.Member.Name;

                switch (memberName)
                {
                    case nameof(DateTime.Now):
                        return new SqlExpression(
                            new SqlFunctionExpression(
                                null,
                                "GETDATE",
                                null,
                                null,
                                memberExpression.Type),
                            _typeMappingSource.FindMapping(memberExpression.Type));
                }
            }

            return null;
        }
    }
}
