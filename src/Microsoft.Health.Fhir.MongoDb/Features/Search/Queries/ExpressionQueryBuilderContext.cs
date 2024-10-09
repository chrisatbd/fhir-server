// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class ExpressionQueryBuilderContext
    {
        public ExpressionQueryBuilderContext()
        {
        }

        public FilterDefinitionBuilder<BsonDocument> Builder { get; private set; } = Builders<BsonDocument>.Filter;

        public FilterDefinition<BsonDocument>? Filter { get; set; }
    }
}
