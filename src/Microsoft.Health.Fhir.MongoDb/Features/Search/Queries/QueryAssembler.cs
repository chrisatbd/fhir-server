// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Microsoft.Health.Fhir.MongoDb.Features.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class QueryAssembler
    {
        private BsonDocument _inProcessFilter = new BsonDocument();
        private BsonArray _inProcessConditions = new BsonArray();

        private Stack<Tuple<MultiaryOperator, BsonArray>> _stack = new();

        private MultiaryOperator? _root = null;

        private int _notCounter = 0;

        public QueryAssembler()
        {
        }

        private List<BsonDocument> Filters { get; } = new List<BsonDocument>();

        public BsonDocument RenderFilters()
        {
            // var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();

#pragma warning disable CS0618
#pragma warning disable CS8602
            BsonArray arr = [.. Filters];

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

        public void PushMultiaryOperator(MultiaryOperator op)
        {
            if (_root == null)
            {
                _root = op;
                return;
            }

            var stackty = new Tuple<MultiaryOperator, BsonArray>(op, new BsonArray());
            _stack.Push(stackty);
        }

        public void PopMultiaryOperator()
        {
            // if stack is 0 we are at the bottom, and only the root remains

            if (_stack.Count > 0)
            {
                BsonDocument matchConditions = new BsonDocument();

                Tuple<MultiaryOperator, BsonArray> arr = _stack.Pop();

                if (arr.Item1 == MultiaryOperator.Or)
                {
                    matchConditions.Add(new BsonElement("$or", arr.Item2));
                }
                else
                {
                    matchConditions.Add(new BsonElement("$and", arr.Item2));
                }

                _inProcessConditions.Add(matchConditions);
            }
        }

        public void AddCondition(BsonDocument condition)
        {
            if (_notCounter > 0)
            {
                condition = new BsonDocument(
                    condition.Names.First(),
                    new BsonDocument("$not", condition.Values.First()));
            }

            if (_stack.Count > 0)
            {
                _stack.Peek().Item2.Add(condition);
            }
            else
            {
                _inProcessConditions.Add(condition);
            }
        }

        public void PushNegation()
        {
            _notCounter++;
        }

        public void PopNegation()
        {
            _notCounter--;
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

            Filters.Add(_inProcessFilter);
        }

        public void AddFilter(BsonDocument filter)
        {
            Filters.Add(filter);
        }
    }
}
