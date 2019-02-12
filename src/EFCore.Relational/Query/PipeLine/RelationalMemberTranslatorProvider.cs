// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class RelationalMemberTranslatorProvider : IMemberTranslatorProvider
    {
        private readonly List<IMemberTranslator> _memberTranslators = new List<IMemberTranslator>();

        public RelationalMemberTranslatorProvider()
        {
        }

        public Expression Translate(MemberExpression memberExpression)
        {
            return _memberTranslators.Select(t => t.Translate(memberExpression)).FirstOrDefault(t => t != null);
        }

        protected virtual void AddTranslators(IEnumerable<IMemberTranslator> translators)
            => _memberTranslators.InsertRange(0, translators);
    }
}
