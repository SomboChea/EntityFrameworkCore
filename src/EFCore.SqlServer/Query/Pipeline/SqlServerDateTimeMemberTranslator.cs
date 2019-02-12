// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Relational.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerDateTimeMemberTranslator : IMemberTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private static readonly Dictionary<string, string> _datePartMapping
            = new Dictionary<string, string>
            {
                { nameof(DateTime.Year), "year" },
                { nameof(DateTime.Month), "month" },
                { nameof(DateTime.DayOfYear), "dayofyear" },
                { nameof(DateTime.Day), "day" },
                { nameof(DateTime.Hour), "hour" },
                { nameof(DateTime.Minute), "minute" },
                { nameof(DateTime.Second), "second" },
                { nameof(DateTime.Millisecond), "millisecond" }
            };

        public SqlServerDateTimeMemberTranslator(IRelationalTypeMappingSource typeMappingSource)
        {
            _typeMappingSource = typeMappingSource;
        }

        public Expression Translate(MemberExpression memberExpression)
        {
            var declaringType = memberExpression.Member.DeclaringType;

            if (declaringType == typeof(DateTime)
                || declaringType == typeof(DateTimeOffset))
            {
                var memberName = memberExpression.Member.Name;

                if (_datePartMapping.TryGetValue(memberName, out var datePart))
                {
                    return new SqlExpression(
                        new SqlFunctionExpression(
                            null,
                            "DATEPART",
                            null,
                            new[]{
                            new SqlExpression(
                                new SqlFragmentExpression(datePart, typeof(object)),
                                _typeMappingSource.FindMapping(typeof(string))),
                            memberExpression.Expression
                            },
                            memberExpression.Type),
                        _typeMappingSource.FindMapping(memberExpression.Type));
                }

                switch (memberName)
                {
                    case nameof(DateTime.Now):
                        return new SqlExpression(
                            new SqlFunctionExpression(
                                null,
                                declaringType == typeof(DateTime) ? "GETDATE" : "SYSDATETIMEOFFSET",
                                null,
                                null,
                                memberExpression.Type),
                            _typeMappingSource.FindMapping(memberExpression.Type));

                    case nameof(DateTime.UtcNow):
                        var serverTranslation = new SqlExpression(
                            new SqlFunctionExpression(
                                null,
                                declaringType == typeof(DateTime) ? "GETUTCDATE" : "SYSUTCDATETIME",
                                null,
                                null,
                                memberExpression.Type),
                            _typeMappingSource.FindMapping(memberExpression.Type));

                        var dateTimeOffsetTypeMapping = _typeMappingSource.FindMapping(typeof(DateTimeOffset));

                        return declaringType == typeof(DateTime)
                            ? serverTranslation
                            : new SqlExpression(
                                new SqlCastExpression(
                                    serverTranslation,
                                    serverTranslation.Type,
                                    dateTimeOffsetTypeMapping.StoreType),
                                dateTimeOffsetTypeMapping);

                    case nameof(DateTime.Date):
                        return new SqlExpression(
                            new SqlFunctionExpression(
                                null,
                                "CONVERT",
                                null,
                                new[]{
                                    new SqlExpression(
                                        new SqlFragmentExpression("date", typeof(object)),
                                        _typeMappingSource.FindMapping(typeof(string))),
                                    memberExpression.Expression
                                },
                                memberExpression.Type),
                            _typeMappingSource.FindMapping(memberExpression.Type));

                    case nameof(DateTime.Today):
                        return new SqlExpression(
                            new SqlFunctionExpression(
                                null,
                                "CONVERT",
                                null,
                                new[]{
                                    new SqlExpression(
                                        new SqlFragmentExpression("date", typeof(object)),
                                        _typeMappingSource.FindMapping(typeof(string))),
                                    new SqlExpression(
                                        new SqlFunctionExpression(
                                            null,
                                            "GETDATE",
                                            null,
                                            null,
                                            memberExpression.Type),
                                        _typeMappingSource.FindMapping(memberExpression.Type))
                                },
                                memberExpression.Type),
                            _typeMappingSource.FindMapping(memberExpression.Type));

                    case nameof(DateTime.TimeOfDay)
                    when memberExpression.Expression is SqlExpression sqlExpression:
                        var timeSpanTypeMapping = _typeMappingSource.FindMapping(memberExpression.Type);

                        return new SqlExpression(
                            new SqlCastExpression(
                                sqlExpression,
                                memberExpression.Type,
                                timeSpanTypeMapping.StoreType),
                            timeSpanTypeMapping);
                }
            }

            return null;
        }
    }
}
