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
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static DotLiquid.Variable;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class QueryAssembler
    {
        public QueryAssembler()
        {
        }

        public FilterDefinitionBuilder<BsonDocument> Builder { get; private set; } = Builders<BsonDocument>.Filter;

        public FilterDefinition<BsonDocument>? Filter { get; set; }

        public BsonDocument RenderFilters()
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<BsonDocument>();

#pragma warning disable CS0618
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            BsonDocument doc = Filter.Render(documentSerializer, serializerRegistry);
            return doc;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS0618
        }
    }
}
