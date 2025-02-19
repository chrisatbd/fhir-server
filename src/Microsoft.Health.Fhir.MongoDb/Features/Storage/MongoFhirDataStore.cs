﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Transform;
using Azure.Core;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Persistence.Orchestration;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.MongoDb.Configs;
using Microsoft.Health.Fhir.MongoDb.Extensions;
using Microsoft.Health.Fhir.MongoDb.Features.Search;
using Microsoft.Health.Fhir.ValueSets;
using Microsoft.Identity.Client;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using SemVer;
using static System.Net.Mime.MediaTypeNames;
using static DotLiquid.Variable;

namespace Microsoft.Health.Fhir.MongoDb.Features.Storage
{
    public sealed class MongoFhirDataStore : IFhirDataStore, IProvideCapability
    {
        // private const string InitialVersion = "1";

        private readonly ILogger<MongoFhirDataStore> _logger;
        private readonly RequestContextAccessor<IFhirRequestContext> _requestContextAccessor;
        private readonly IBundleOrchestrator _bundleOrchestrator;
        private readonly MongoDataStoreConfiguration _dataStoreConfiguration;
        private readonly CoreFeatureConfiguration _coreFeatures;

        public MongoFhirDataStore(
            ILogger<MongoFhirDataStore> logger,
            RequestContextAccessor<IFhirRequestContext> requestContextAccessor,
            IBundleOrchestrator bundleOrchestrator,
            IOptions<CoreFeatureConfiguration> coreFeatures,
            MongoDataStoreConfiguration dataStoreConfiguration)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(coreFeatures, nameof(coreFeatures));
            EnsureArg.IsNotNull(bundleOrchestrator, nameof(bundleOrchestrator));
            EnsureArg.IsNotNull(requestContextAccessor, nameof(requestContextAccessor));
            EnsureArg.IsNotNull(dataStoreConfiguration, nameof(dataStoreConfiguration));

            _logger = logger;
            _requestContextAccessor = requestContextAccessor;
            _bundleOrchestrator = bundleOrchestrator;
            _dataStoreConfiguration = dataStoreConfiguration;
            _coreFeatures = coreFeatures.Value;
        }

        /// <summary>
        /// Builds the compatability statement for our FHIR server
        /// </summary>
        /// <param name="builder"></param>
        public void Build(ICapabilityStatementBuilder builder)
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            _logger.LogInformation("MongoFhirDataStore. Building Capability Statement.");

            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                builder = builder.PopulateDefaultResourceInteractions();
                _logger.LogInformation("MongoFhirDataStore. 'Default Resource Interactions' built. Elapsed: {ElapsedTime}. Memory: {MemoryInUse}.", watch.Elapsed, GC.GetTotalMemory(forceFullCollection: false));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MongoFhirDataStore. 'Default Resource Interactions' failed. Elapsed: {ElapsedTime}. Memory: {MemoryInUse}.", watch.Elapsed, GC.GetTotalMemory(forceFullCollection: false));
                throw;
            }

            // NOTECJH:  For batch and transaction intersecting
            // the implementation between sql and cosmos

            if (_coreFeatures.SupportsBatch)
            {
                try
                {
                    watch = Stopwatch.StartNew();
                    builder.AddGlobalInteraction(SystemRestfulInteraction.Batch);
                    _logger.LogInformation("MongoFhirDataStore. 'Global Interaction' built. Elapsed: {ElapsedTime}. Memory: {MemoryInUse}.", watch.Elapsed, GC.GetTotalMemory(forceFullCollection: false));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "MongoFhirDataStore. 'Global Interaction' failed. Elapsed: {ElapsedTime}. Memory: {MemoryInUse}.", watch.Elapsed, GC.GetTotalMemory(forceFullCollection: false));
                    throw;
                }
            }

            if (_coreFeatures.SupportsTransaction)
            {
                try
                {
                    watch = Stopwatch.StartNew();
                    builder.AddGlobalInteraction(SystemRestfulInteraction.Transaction);
                    _logger.LogInformation("MongoFhirDataStore. 'Global Interaction' built. Elapsed: {ElapsedTime}. Memory: {MemoryInUse}.", watch.Elapsed, GC.GetTotalMemory(forceFullCollection: false));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "MongoFhirDataStore. 'Global Interaction' failed. Elapsed: {ElapsedTime}. Memory: {MemoryInUse}.", watch.Elapsed, GC.GetTotalMemory(forceFullCollection: false));
                    throw;
                }
            }
        }

        public Task BulkUpdateSearchParameterIndicesAsync(IReadOnlyCollection<ResourceWrapper> resources, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        // Gets a list of of FHIR Resources by keys
        public async Task<IReadOnlyList<ResourceWrapper>> GetAsync(IReadOnlyList<ResourceKey> keys, CancellationToken cancellationToken)
        {
            List<ResourceWrapper> listData = new List<ResourceWrapper>();

            foreach (var item in keys)
            {
                ResourceWrapper rw = await GetAsync(item, cancellationToken);
                if (rw != null)
                {
                    listData.Add(rw);
                }
            }

            return listData.AsReadOnly();
        }

        // Gets a FHIR resource
        public async Task<ResourceWrapper> GetAsync(ResourceKey key, CancellationToken cancellationToken)
        {
            var resourceType = key.ResourceType;
            var resourceId = key.Id;
            var version = 1;
            var isDeleted = false;
            var isHistory = false;
            var isRawResourceMetaSet = true;

            /*
            TODO:  Need to review these
            var searchParamHash = reader.Read(VLatest.Resource.SearchParamHash, 8);
            var requestMethod = readRequestMethod ? reader.Read(VLatest.Resource.RequestMethod, 9) : null;
            var resourceSurrogateId = 5108658606258160320;
            var rawResourceBytes = reader.GetSqlBytes(6).Value;
            var resourceTypeId = 103;
            */

            // set up the filters
            // TODOCJH:  Should we also filter on resource type
            // TODOCJH:  Add checks for isdeleted, version , etc.
            var filter = Builders<BsonDocument>.Filter.Eq("resource.id", resourceId);

            var document = await _dataStoreConfiguration
                .GetCollection()
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == null)
            {
#pragma warning disable CS8603
                return null;
#pragma warning restore CS8603
            }

            return new ResourceWrapper(
                resourceId,
                version.ToString(CultureInfo.InvariantCulture),
                key.ResourceType,
                new RawResource(document[FieldNameConstants.Resource].ToString(), FhirResourceFormat.Json, isMetaSet: isRawResourceMetaSet),
                null,
                DateTimeOffset.Now,
                isDeleted,
                searchIndices: null,
                compartmentIndices: null,
                lastModifiedClaims: null,
                searchParameterHash: null)
            {
                IsHistory = isHistory,
            };
        }

        public Task<int?> GetProvisionedDataStoreCapacityAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// HardDeleteAsync
        /// </summary>
        /// <param name="key">resource key to delete</param>
        /// <param name="keepCurrentVersion">not implemented</param>
        /// <param name="cancellationToken">the async cancellation token</param>
        /// <returns>DeleteResult</returns>
        public async Task HardDeleteAsync(ResourceKey key, bool keepCurrentVersion, CancellationToken cancellationToken)
        {
            var resourceType = key.ResourceType;
            var resourceId = key.Id;

            var filter = Builders<BsonDocument>.Filter.Eq($"{FieldNameConstants.Resource}.{FieldNameConstants.Id}", resourceId);

            var deleteResults = await _dataStoreConfiguration
                .GetCollection()
                .DeleteOneAsync(filter, cancellationToken);
        }

        public async Task<IDictionary<DataStoreOperationIdentifier, DataStoreOperationOutcome>> MergeAsync(IReadOnlyList<ResourceWrapperOperation> resources, CancellationToken cancellationToken)
        {
            return await MergeAsync(resources, MergeOptions.Default, cancellationToken);
        }

        public async Task<IDictionary<DataStoreOperationIdentifier, DataStoreOperationOutcome>> MergeAsync(IReadOnlyList<ResourceWrapperOperation> resources, MergeOptions mergeOptions, CancellationToken cancellationToken)
        {
            var retries = 0;
            var results = await MergeInternalAsync(resources, false, false, mergeOptions.EnlistInTransaction, retries == 0, cancellationToken); // TODO: Pass correct retries value once we start supporting retries
            return results;
        }

        // does the actual work of merging or creating new if the old one does not exist
        internal async Task<IDictionary<DataStoreOperationIdentifier, DataStoreOperationOutcome>> MergeInternalAsync(IReadOnlyList<ResourceWrapperOperation> resources, bool keepLastUpdated, bool keepAllDeleted, bool enlistInTransaction, bool useReplicasForReads, CancellationToken cancellationToken)
        {
            var results = new Dictionary<DataStoreOperationIdentifier, DataStoreOperationOutcome>();
            if (resources == null || resources.Count == 0)
            {
                return results;
            }

            var existingResources = (await GetAsync(resources.Select(r => r.Wrapper.ToResourceKey(true)).Distinct().ToList(), cancellationToken)).ToDictionary(r => r.ToResourceKey(true), r => r);

            foreach (var resourceExt in resources) // if list contains more that one version per resource it must be sorted by id and last updated DESC.
            {
                ResourceWrapper resource = resourceExt.Wrapper;
                DataStoreOperationIdentifier identifier = resourceExt.GetIdentifier();

                existingResources.TryGetValue(resource.ToResourceKey(true), out var existingResource);

                if (existingResource == null)
                {
                    string text = resourceExt.Wrapper.RawResource.Data;

                    var doc = new JObject
                    {
                        { FieldNameConstants.Resource, JToken.Parse(text) },
                    };

                    var document = doc.ToBsonDocument();
                    document.Add(FieldNameConstants.IsDeleted, false);
                    document.AddRange(new BsonDocument(FieldNameConstants.SearchIndexes, GetSearchIndexes(resourceExt.Wrapper.SearchIndices)));

                    await _dataStoreConfiguration
                        .GetCollection()
                        .InsertOneAsync(document, new InsertOneOptions(), cancellationToken);

                    results.Add(
                        identifier,
                        new DataStoreOperationOutcome(new UpsertOutcome(resourceExt.Wrapper, SaveOutcomeType.Created)));
                }
                else
                {
                    // TODOCJH: Implement Update
                    // ok, we are updating an existing resource
                    // for now we are just going to flush and fill the resource and search indexes

                    string text = resourceExt.Wrapper.RawResource.Data;

                    var filter = Builders<BsonDocument>.Filter.Eq($"{FieldNameConstants.Resource}.{FieldNameConstants.Id}", resource.ResourceId);

                    var update = Builders<BsonDocument>.Update
                        .Set(FieldNameConstants.Resource, JObject.Parse(text).ToBsonDocument())
                        .Set(FieldNameConstants.SearchIndexes, GetSearchIndexes(resourceExt.Wrapper.SearchIndices))
                        .Set(FieldNameConstants.IsDeleted, resourceExt.Wrapper.IsDeleted);

                    var updateResult = await _dataStoreConfiguration
                        .GetCollection()
                        .UpdateOneAsync(filter, update, null, cancellationToken);

                    results.Add(identifier, new DataStoreOperationOutcome(new UpsertOutcome(resourceExt.Wrapper, SaveOutcomeType.Updated)));
                }
            }

            return results;
        }

#pragma warning disable CA1822
        private BsonArray GetSearchIndexes(IReadOnlyCollection<SearchIndexEntry> searchIndices)
        {
#pragma warning restore CA1822

            BsonArray indexes = new BsonArray();

            foreach (SearchIndexEntry entry in searchIndices)
            {
                indexes.Add(SearchIndexEntryBsonDocumentGenerator.Generate(entry));
            }

            return indexes;
        }

        /// <summary>
        /// we can land here on a 'POST' a 'PUT' and 'DELETE'
        /// </summary>
        /// <param name="resource">Resource to upsert</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>UpsertOutcome</returns>
        /// <exception cref="NotImplementedException">bundle operation not supported</exception>        
        public async Task<UpsertOutcome> UpsertAsync(ResourceWrapperOperation resource, CancellationToken cancellationToken)
        {
            bool isBundleOperation = _bundleOrchestrator.IsEnabled && resource.BundleResourceContext != null;

            if (isBundleOperation)
            {
                throw new NotImplementedException();
            }

            _logger.LogInformation(resource.Wrapper.ResourceId);

            var mergeOutcome = await MergeAsync(new[] { resource }, cancellationToken);

            DataStoreOperationOutcome dataStoreOperationOutcome = mergeOutcome.First().Value;

            if (dataStoreOperationOutcome.IsOperationSuccessful)
            {
                return dataStoreOperationOutcome.UpsertOutcome;
            }
            else
            {
                throw dataStoreOperationOutcome.Exception;
            }
        }

        // UpdateSearchParameterIndicesAsync
        public Task<ResourceWrapper> UpdateSearchParameterIndicesAsync(ResourceWrapper resourceWrapper, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task HardDeleteAsync(ResourceKey key, bool keepCurrentVersion, bool allowPartialSuccess, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
