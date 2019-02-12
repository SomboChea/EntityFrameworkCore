// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class TypeMappingInferringExpressionVisitor : ExpressionVisitor
    {
        private RelationalTypeMapping _currentTypeMapping;
        private bool _condition;

        public TypeMappingInferringExpressionVisitor()
        {
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            var parentTypeMapping = _currentTypeMapping;
            var parentCondition = _condition;

            _currentTypeMapping = null;
            _condition = false;

            var condition = false;
            RelationalTypeMapping aggregateTypeMapping = null;


            var left = binaryExpression.Left;
            var right = binaryExpression.Right;
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    {
                        if (left is SqlExpression leftSql)
                        {
                            _currentTypeMapping = leftSql.TypeMapping;

                            if (!(right is SqlExpression))
                            {
                                right = Visit(right);
                            }
                        }
                        else if (right is SqlExpression rightSql)
                        {
                            _currentTypeMapping = rightSql.TypeMapping;

                            left = Visit(left);
                        }

                        condition = true;

                        break;
                    }

                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    {
                        _condition = true;

                        if (!(left is SqlExpression))
                        {
                            left = Visit(left);
                        }

                        if (!(right is SqlExpression))
                        {
                            right = Visit(right);
                        }

                        Debug.Assert(((SqlExpression)left).IsCondition);
                        Debug.Assert(((SqlExpression)right).IsCondition);

                        condition = true;

                        break;
                    }
            }

            _currentTypeMapping = parentTypeMapping;
            _condition = parentCondition;

            var updatedBinaryExpression = binaryExpression.Update(left, binaryExpression.Conversion, right);

            return left is SqlExpression && right is SqlExpression
                ? condition
                    ? new SqlExpression(updatedBinaryExpression)
                    : new SqlExpression(updatedBinaryExpression, aggregateTypeMapping)
                : (Expression)updatedBinaryExpression;
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            if (_currentTypeMapping != null)
            {
                return new SqlExpression(constantExpression, _currentTypeMapping);
            }

            if (_condition)
            {
                return new SqlExpression(constantExpression);
            }

            return constantExpression;
        }

        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            var operand = Visit(unaryExpression.Operand);

            if (unaryExpression.NodeType == ExpressionType.Convert
                && unaryExpression.Type == typeof(object))
            {
                return operand;
            }

            return unaryExpression.Update(operand);
        }

        protected override Expression VisitParameter(ParameterExpression parameterExpression)
        {
            if (_currentTypeMapping != null)
            {
                return new SqlExpression(parameterExpression, _currentTypeMapping);
            }

            if (_condition)
            {
                return new SqlExpression(parameterExpression);
            }

            return parameterExpression;
        }
    }
}
