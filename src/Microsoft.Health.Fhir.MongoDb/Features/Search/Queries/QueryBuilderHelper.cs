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
        private readonly QueryParameterManager _queryParameterManager;

        public QueryBuilderHelper()
        {
            _queryParameterManager = new QueryParameterManager();
        }

#pragma warning disable CS8625
        public FilterDefinition<BsonDocument> BuildSqlQuerySpec(SearchOptions searchOptions, QueryBuilderOptions queryOptions = null)
#pragma warning restore CS8625
        {
            EnsureArg.IsNotNull(searchOptions, nameof(searchOptions));
            EnsureArg.IsNotNull(queryOptions, nameof(queryOptions));

            var expressionQueryBuilder = new ExpressionQueryBuilder(_queryParameterManager);

            ExpressionQueryBuilderContext ctx = new ExpressionQueryBuilderContext();

            if (searchOptions.Expression != null)
            {
#pragma warning disable CS8620
                searchOptions.Expression.AcceptVisitor(expressionQueryBuilder, ctx);
#pragma warning restore CS8620
            }

            return expressionQueryBuilder.GetFilters();
        }

        public FilterDefinition<BsonDocument> GenerateReindexSql(SearchOptions searchOptions, string searchParameterHash)
        {
            throw new NotImplementedException();
        }
    }
}
