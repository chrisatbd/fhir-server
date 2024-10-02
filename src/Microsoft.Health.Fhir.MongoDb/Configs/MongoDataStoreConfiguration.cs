// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Configs
{
    public class MongoDataStoreConfiguration
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string ConnectionString { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public IMongoCollection<BsonDocument> GetCollection(string resourceTypeName)
        {
            string collectionName = resourceTypeName;

            collectionName = "resource";

            var connectionString = ConnectionString;

            var client = new MongoClient(connectionString);

            var collection = client
                .GetDatabase("fhir")
                .GetCollection<BsonDocument>(collectionName.ToLowerInvariant());

            return collection;
        }
    }
}
