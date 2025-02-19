// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Configs
{
    public class MongoDataStoreConfiguration
    {
        public string? ConnectionString { get; set; }

        public IMongoCollection<BsonDocument> GetCollection(string? resourceTypeName = null)
        {
            /*
             * TODOCJH:  The idea here is that we could support different collections for different
             * resource types, but for now we are putting everything into one.  At some point we could also
             * use this as a base for partitioning, etc.
             */

            string collectionName = "resource";

            if (resourceTypeName != null)
            {
                // BUGCJH: forced on purpose, see above.
                collectionName = resourceTypeName;
                collectionName = "resource";
            }

            var connectionString = ConnectionString;

            var client = new MongoClient(connectionString);

            var collection = client
                .GetDatabase("fhir")
                .GetCollection<BsonDocument>(collectionName.ToLowerInvariant());

            return collection;
        }
    }
}
