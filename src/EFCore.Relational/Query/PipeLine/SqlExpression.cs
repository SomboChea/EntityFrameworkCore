// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class SqlExpression : Expression
    {
        public SqlExpression(Expression expression)
            : this(expression, typeof(bool), null)
        {
        }

        public SqlExpression(Expression expression, RelationalTypeMapping typeMapping)
            : this(expression, expression.Type, typeMapping)
        {
        }

        private SqlExpression(Expression expression, Type type, RelationalTypeMapping typeMapping)
        {
            Expression = expression;
            Type = type;
            TypeMapping = typeMapping;
            IsCondition = typeMapping == null;
        }

        public SqlExpression ChangeTypeNullablility(bool makeNullable)
        {
            var type = Type.IsNullableType()
                ? (makeNullable ? Type : Type.UnwrapNullableType())
                : (makeNullable ? Type.MakeNullable() : Type);

            return new SqlExpression(Expression, type, TypeMapping);
        }

        public RelationalTypeMapping TypeMapping { get; }

        public Expression Expression { get; }
        public bool IsCondition { get; }
        public override Type Type { get; }
        public override ExpressionType NodeType => ExpressionType.Extension;
    }
}
