// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class IsNullExpression : Expression
    {
        public IsNullExpression(SqlExpression expression, bool negated = false)
        {
            Expression = expression;
            Negated = negated;
        }

        public SqlExpression Expression { get; }
        public bool Negated { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => typeof(bool);
    }
}
