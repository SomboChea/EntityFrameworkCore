// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Extensions.Internal;
using Microsoft.EntityFrameworkCore.Query.PipeLine;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class RelationalSqlTranslatingExpressionVisitor : ExpressionVisitor
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly IMethodCallTranslatorProvider _methodCallTranslatorProvider;
        private readonly TypeMappingInferringExpressionVisitor _typeInference;

        private SelectExpression _selectExpression;

        public RelationalSqlTranslatingExpressionVisitor(
            IRelationalTypeMappingSource typeMappingSource, IMethodCallTranslatorProvider methodCallTranslatorProvider)
        {
            _typeInference = new TypeMappingInferringExpressionVisitor();
            _typeMappingSource = typeMappingSource;
            _methodCallTranslatorProvider = methodCallTranslatorProvider;
        }

        public SqlExpression Translate(SelectExpression selectExpression, Expression expression, bool condition)
        {
            _selectExpression = selectExpression;

            var translation = Visit(expression);

            if (!(translation is SqlExpression sqlExpression))
            {
                sqlExpression = new SqlExpression(translation, _typeMappingSource.FindMapping(translation.Type));
            }

            if (condition
                && !sqlExpression.IsCondition
                && sqlExpression.TypeMapping == _typeMappingSource.FindMapping(typeof(bool)))
            {
                sqlExpression = new SqlExpression(sqlExpression.Expression, true);
            }

            _selectExpression = null;

            return sqlExpression;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            var innerExpression = Visit(memberExpression.Expression);
            if (innerExpression is EntityShaperExpression entityShaper)
            {
                var entityType = entityShaper.EntityType;
                var property = entityType.FindProperty(memberExpression.Member.GetSimpleMemberName());

                return _selectExpression.BindProperty(entityShaper.ValueBufferExpression, property);
            }

            return memberExpression.Update(innerExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsEFPropertyMethod())
            {
                var firstArgument = Visit(methodCallExpression.Arguments[0]);

                // In certain cases EF.Property would have convert node around the source.
                if (firstArgument.NodeType == ExpressionType.Convert
                    && firstArgument.Type == typeof(object))
                {
                    firstArgument = ((UnaryExpression)firstArgument).Operand;
                }

                if (firstArgument is EntityShaperExpression entityShaper)
                {
                    var entityType = entityShaper.EntityType;
                    var property = entityType.FindProperty((string)((ConstantExpression)methodCallExpression.Arguments[1]).Value);

                    return _selectExpression.BindProperty(entityShaper.ValueBufferExpression, property);
                }
            }

            var @object = Visit(methodCallExpression.Object);
            var arguments = new Expression[methodCallExpression.Arguments.Count];
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = Visit(methodCallExpression.Arguments[i]);
            }

            var updatedMethodCallExpression = methodCallExpression.Update(@object, arguments);

            return _methodCallTranslatorProvider.Translate(updatedMethodCallExpression);
        }

        private static readonly MethodInfo _stringConcatObjectMethodInfo
            = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(object), typeof(object) });

        private static readonly MethodInfo _stringConcatStringMethodInfo
            = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });

        private SqlExpression ConvertToString(Expression expression)
        {
            if (expression is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Convert
                && unaryExpression.Type == typeof(object))
            {
                expression = unaryExpression.Operand;
            }

            var sqlExpression = expression is SqlExpression
                ? (SqlExpression)expression
                : new SqlExpression(expression, _typeMappingSource.FindMapping(expression.Type));


            if (sqlExpression.Type != typeof(string))
            {
                var stringTypeMapping = _typeMappingSource.FindMapping(typeof(string));

                sqlExpression = new SqlExpression(
                    new SqlCastExpression(sqlExpression, stringTypeMapping.ClrType, stringTypeMapping.StoreType),
                    stringTypeMapping);
            }

            return sqlExpression;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            var left = Visit(binaryExpression.Left);
            var right = Visit(binaryExpression.Right);

            if (binaryExpression.NodeType == ExpressionType.Add
                && (_stringConcatObjectMethodInfo.Equals(binaryExpression.Method)
                    || _stringConcatStringMethodInfo.Equals(binaryExpression.Method)))
            {
                left = ConvertToString(left);
                right = ConvertToString(right);

                return new SqlExpression(
                    binaryExpression.Update(left, VisitAndConvert(binaryExpression.Conversion, "VisitBinary"), right),
                    _typeMappingSource.FindMapping(typeof(string)));
            }
            else if (binaryExpression.NodeType == ExpressionType.Equal
                    || binaryExpression.NodeType == ExpressionType.NotEqual)
            {
                // Convert null comparison
                var nullComparison = TransformNullComparison(left, right, binaryExpression.NodeType);

                if (nullComparison != null)
                {
                    return nullComparison;
                }
            }

            var newExpression = binaryExpression.Update(
                left, VisitAndConvert(binaryExpression.Conversion, "VisitBinary"), right);

            return _typeInference.Visit(newExpression);
        }

        private static Expression TransformNullComparison(Expression left, Expression right, ExpressionType expressionType)
        {
            var isLeftNullConstant = left is ConstantExpression leftConstant && leftConstant.Value == null;
            var isRightNullConstant = right is ConstantExpression rightConstant && rightConstant.Value == null;

            if ((isLeftNullConstant || isRightNullConstant)
                && ((isLeftNullConstant ? right : left) is SqlExpression sqlExpression))
            {
                return new SqlExpression(
                    new IsNullExpression(sqlExpression, expressionType == ExpressionType.NotEqual),
                    true);
            }

            return null;
        }

        protected override Expression VisitExtension(Expression extensionExpression)
        {
            if (extensionExpression is EntityShaperExpression)
            {
                return extensionExpression;
            }

            return base.VisitExtension(extensionExpression);
        }

        protected override Expression VisitNew(NewExpression newExpression)
        {
            if (newExpression.Members == null
                || newExpression.Arguments.Count == 0)
            {
                return null;
            }

            var bindings = new Expression[newExpression.Arguments.Count];

            for (var i = 0; i < bindings.Length; i++)
            {
                var translation = Visit(newExpression.Arguments[i]);

                if (translation == null)
                {
                    return null;
                }

                bindings[i] = translation;
            }

            return Expression.Constant(bindings);
        }

        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            var operand = Visit(unaryExpression.Operand);

            if (operand is SqlExpression operandSql
                && unaryExpression.Type != typeof(object)
                && unaryExpression.NodeType == ExpressionType.Convert)
            {
                var typeMapping = _typeMappingSource.FindMapping(unaryExpression.Type);
                var resultType = typeMapping.ClrType;

                if (unaryExpression.Type.IsNullableType())
                {
                    resultType = resultType.MakeNullable();
                }

                return new SqlExpression(
                    new SqlCastExpression(operandSql, resultType, typeMapping.StoreType),
                    typeMapping);
            }

            return unaryExpression.Update(operand);
        }
    }
}
