// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class EqualsTranslator : IMethodCallTranslator
    {
        public Expression Translate(MethodCallExpression methodCallExpression)
        {
            Expression left = null;
            Expression right = null;
            if (methodCallExpression.Method.Name == nameof(object.Equals)
                && methodCallExpression.Arguments.Count == 1
                && methodCallExpression.Object != null)
            {
                left = methodCallExpression.Object;
                right = methodCallExpression.Arguments[0];
            }
            else if (methodCallExpression.Method.Name == nameof(object.Equals)
                && methodCallExpression.Arguments.Count == 2
                && methodCallExpression.Arguments[0].Type == methodCallExpression.Arguments[1].Type)
            {
                left = methodCallExpression.Arguments[0];
                right = methodCallExpression.Arguments[1];
            }

            if (left != null && right != null && left.Type == right.Type)
            {
                if (left is SqlExpression leftSql)
                {
                    if (!(right is SqlExpression))
                    {
                        right = new SqlExpression(right, leftSql.TypeMapping);
                    }
                }
                else if (right is SqlExpression rightSql)
                {
                    left = new SqlExpression(left, rightSql.TypeMapping);
                }

                return new SqlExpression(Expression.Equal(left, right), true);
            }

            return null;
        }
    }
}
