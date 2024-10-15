// -------------------------------------------------------------------------------------------------
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
    internal interface IQueryBuilder
    {
#pragma warning disable CS8625
        FilterDefinition<BsonDocument> BuildSqlQuerySpec(SearchOptions searchOptions, QueryBuilderOptions queryOptions = null);
#pragma warning restore CS8625

        FilterDefinition<BsonDocument> GenerateReindexSql(SearchOptions searchOptions, string searchParameterHash);
    }
}
