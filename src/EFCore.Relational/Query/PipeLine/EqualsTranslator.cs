// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
                right = UnwrapObjectConvert(methodCallExpression.Arguments[0]);
            }
            else if (methodCallExpression.Method.Name == nameof(object.Equals)
                && methodCallExpression.Arguments.Count == 2
                && methodCallExpression.Arguments[0].Type == methodCallExpression.Arguments[1].Type)
            {
                left = UnwrapObjectConvert(methodCallExpression.Arguments[0]);
                right = UnwrapObjectConvert(methodCallExpression.Arguments[1]);
            }

            if (left != null && right != null)
            {
                if (left.Type.UnwrapNullableType() == right.Type.UnwrapNullableType())
                {
                    if (left is SqlExpression leftSql)
                    {
                        if (!(right is SqlExpression))
                        {
                            right = new SqlExpression(right, leftSql.TypeMapping).ChangeTypeNullablility(left.Type.IsNullableType());
                        }
                    }
                    else if (right is SqlExpression rightSql)
                    {
                        left = new SqlExpression(left, rightSql.TypeMapping).ChangeTypeNullablility(right.Type.IsNullableType());
                    }

                    return new SqlExpression(Expression.Equal(left, right));
                }
                else
                {
                    return Expression.Constant(false);
                }
            }

            return null;
        }

        private static Expression UnwrapObjectConvert(Expression expression)
        {
            return expression is UnaryExpression unary
                && expression.Type == typeof(object)
                && expression.NodeType == ExpressionType.Convert
                ? unary.Operand
                : expression;
        }
    }
}
