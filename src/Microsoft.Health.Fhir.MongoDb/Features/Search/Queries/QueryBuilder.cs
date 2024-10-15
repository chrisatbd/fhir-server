﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.Core.Features.Search;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Pending")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "Pending")]
    internal class QueryBuilder : IQueryBuilder
    {
#pragma warning disable CS8625
        public FilterDefinition<BsonDocument> BuildSqlQuerySpec(SearchOptions searchOptions, QueryBuilderOptions queryOptions = null)
#pragma warning restore CS8625
        {
            return new QueryBuilderHelper().BuildSqlQuerySpec(searchOptions, queryOptions ?? new QueryBuilderOptions());
        }

        public FilterDefinition<BsonDocument> GenerateReindexSql(SearchOptions searchOptions, string searchParameterHash)
        {
            throw new NotImplementedException();
        }
    }
}
