// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search
{
    internal sealed class SearchIndexEntryBsonDocumentGenerator : ISearchValueVisitor
    {
        private SearchIndexEntry? Entry { get; set; }

        public BsonDocument Generate(SearchIndexEntry entry)
        {
            EnsureArg.IsNotNull(entry, nameof(entry));
            Entry = entry;

            string sp = Newtonsoft.Json.JsonConvert.SerializeObject(entry.SearchParameter);
            string val = Newtonsoft.Json.JsonConvert.SerializeObject(entry.Value);

            BsonDocument valueDocument = BsonSerializer.Deserialize<BsonDocument>(val);

            if (entry.SearchParameter.Type == ValueSets.SearchParamType.Date)
            {
                var dtsv = (DateTimeSearchValue)entry.Value;

                // The Cosmos implementation does not have IsValidAsCompositeComponent, IsMin, or IsMax
                valueDocument =
                [
                    new BsonElement(SearchValueConstants.DateTimeStartName, new BsonDateTime(dtsv.Start.DateTime)),
                    new BsonElement(SearchValueConstants.DateTimeEndName, new BsonDateTime(dtsv.End.DateTime)),
                    new BsonElement("IsValidAsCompositeComponent", new BsonBoolean(dtsv.IsValidAsCompositeComponent)),
                    new BsonElement("IsMin", new BsonBoolean(dtsv.IsMin)),
                    new BsonElement("IsMax", new BsonBoolean(dtsv.IsMax)),
                ];
            }

            if (entry.SearchParameter.Type == ValueSets.SearchParamType.Quantity)
            {
                var quantity = (QuantitySearchValue)entry.Value;

                // The Cosmos implementation does not have IsValidAsCompositeComponent, IsMin, or IsMax
                valueDocument = new BsonDocument();
                if (quantity.System != null)
                {
                    valueDocument.Add(SearchValueConstants.SystemName, quantity.System);
                }

                if (quantity.Code != null)
                {
                    valueDocument.Add(SearchValueConstants.CodeName, quantity.Code);
                }

                if (quantity.Low == quantity.High)
                {
                    valueDocument.Add(SearchValueConstants.QuantityName, quantity.Low);
                }

                valueDocument.Add(SearchValueConstants.LowQuantityName, quantity.Low);
                valueDocument.Add(SearchValueConstants.HighQuantityName, quantity.High);
            }

            BsonDocument searchParams = BsonSerializer.Deserialize<BsonDocument>(sp);

            // TODOCJH:  Just some simple 'trimming' for now
            searchParams.Remove("Description");
            searchParams.Remove("TargetResourceTypes");
            searchParams.Remove("BaseResourceTypes");

            var ret = new BsonDocument
            {
                { "SearchParameter", searchParams },
                { "Value", valueDocument },
            };

            return ret;
        }

        public void Visit(CompositeSearchValue composite)
        {
            throw new NotImplementedException();
        }

        public void Visit(DateTimeSearchValue dateTime)
        {
            throw new NotImplementedException();
        }

        public void Visit(NumberSearchValue number)
        {
            throw new NotImplementedException();
        }

        public void Visit(QuantitySearchValue quantity)
        {
            throw new NotImplementedException();
        }

        public void Visit(ReferenceSearchValue reference)
        {
            throw new NotImplementedException();
        }

        public void Visit(StringSearchValue s)
        {
            throw new NotImplementedException();
        }

        public void Visit(TokenSearchValue token)
        {
            throw new NotImplementedException();
        }

        public void Visit(UriSearchValue uri)
        {
            throw new NotImplementedException();
        }
    }
}
