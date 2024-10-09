// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.MongoDb.Features.Queries;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class QueryBuilderHelper
    {
        private readonly StringBuilder _queryBuilder;
        private readonly QueryParameterManager _queryParameterManager;
        private readonly QueryHelper _queryHelper;

        public QueryBuilderHelper()
        {
            _queryBuilder = new StringBuilder();
            _queryParameterManager = new QueryParameterManager();
            _queryHelper = new QueryHelper(_queryBuilder, _queryParameterManager);
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public FilterDefinition<BsonDocument> BuildSqlQuerySpec(SearchOptions searchOptions, QueryBuilderOptions queryOptions = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            EnsureArg.IsNotNull(searchOptions, nameof(searchOptions));
            EnsureArg.IsNotNull(queryOptions, nameof(queryOptions));

            var expressionQueryBuilder = new ExpressionQueryBuilder(
                _queryBuilder,
                _queryParameterManager);

            ExpressionQueryBuilderContext ctx = new ExpressionQueryBuilderContext();

            if (searchOptions.Expression != null)
            {
                _queryBuilder.Append("AND ");
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                searchOptions.Expression.AcceptVisitor(expressionQueryBuilder, ctx);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }

#pragma warning disable CS8603 // Possible null reference return.
            return ctx.Filter;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public FilterDefinition<BsonDocument> GenerateReindexSql(SearchOptions searchOptions, string searchParameterHash)
        {
            throw new NotImplementedException();
        }
    }
}
