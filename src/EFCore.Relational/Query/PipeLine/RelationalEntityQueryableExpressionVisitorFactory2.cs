// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.PipeLine;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class RelationalEntityQueryableExpressionVisitorsFactory : EntityQueryableExpressionVisitorsFactory
    {
        private readonly IModel _model;

        public RelationalEntityQueryableExpressionVisitorsFactory(IModel model)
        {
            _model = model;
        }

        public override EntityQueryableExpressionVisitors Create(QueryCompilationContext2 queryCompilationContext)
        {
            return new RelationalEntityQueryableExpressionVisitors(_model);
        }
    }

    public class RelationalEntityQueryableExpressionVisitors : EntityQueryableExpressionVisitors
    {
        private readonly IModel _model;

        public RelationalEntityQueryableExpressionVisitors(IModel model)
        {
            _model = model;
        }

        public override IEnumerable<ExpressionVisitor> GetVisitors()
        {
            yield return new RelationalEntityQueryableExpressionVisitor2(_model);
        }
    }
}
