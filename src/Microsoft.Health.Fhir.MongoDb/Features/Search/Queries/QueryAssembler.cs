// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.Fhir.MongoDb.Features.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static DotLiquid.Variable;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class QueryAssembler
    {
        private BsonDocument _inProcessFilter = new BsonDocument();
        private BsonArray _inProcessConditions = new BsonArray();

        public QueryAssembler()
        {
        }

        private List<BsonDocument> Filter { get; } = new List<BsonDocument>();

        public BsonDocument RenderFilters()
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<BsonDocument>();

#pragma warning disable CS0618
#pragma warning disable CS8602
            BsonArray arr = [.. Filter];

            arr.Add(new BsonDocument(FieldNameConstants.IsDeleted, false));

            BsonDocument doc = new BsonDocument("$and", arr);
            return doc;
#pragma warning restore CS8602
#pragma warning restore CS0618
        }

        public void StartNewFilter()
        {
            _inProcessFilter = new BsonDocument();
            _inProcessConditions = new BsonArray();
        }

        public void AddCondition(BsonDocument condition)
        {
            _inProcessConditions.Add(condition);
        }

        public void PushFilter()
        {
            BsonDocument matchConditions = new BsonDocument();

            foreach (BsonDocument condition in _inProcessConditions)
            {
                matchConditions.AddRange(condition);
            }

            _inProcessFilter = new BsonDocument(
                FieldNameConstants.SearchIndexes,
                new BsonDocument("$elemMatch", matchConditions));

            Filter.Add(_inProcessFilter);
        }

        public void AddFilter(BsonDocument filter)
        {
            Filter.Add(filter);
        }
    }
}
