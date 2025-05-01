// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.MongoDb.Configs;
using Microsoft.Health.Fhir.MongoDb.Features.Search.Queries;
using Microsoft.Health.Fhir.MongoDb.Features.Storage;
using Microsoft.Health.Fhir.ValueSets;
using MongoDB.Bson;
using MongoDB.Driver;

// using SemVer;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Pending")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "Pending")]
    internal class FhirMongoSearchService : SearchService
    {
        private readonly MongoFhirDataStore _fhirDataStore;
        private readonly IQueryBuilder _queryBuilder;
        private readonly MongoDataStoreConfiguration _dataStoreConfiguration;
        private readonly ILogger<FhirMongoSearchService> _logger;

        public FhirMongoSearchService(
            ISearchOptionsFactory searchOptionsFactory,
            MongoFhirDataStore fhirDataStore,
            IQueryBuilder queryBuilder,
            CompartmentSearchRewriter compartmentSearchRewriter,
            SmartCompartmentSearchRewriter smartCompartmentSearchRewriter,
            ILogger<FhirMongoSearchService> logger,
            MongoDataStoreConfiguration dataStoreConfiguration)
            : base(searchOptionsFactory, fhirDataStore, logger)
        {
            EnsureArg.IsNotNull(searchOptionsFactory, nameof(searchOptionsFactory));
            EnsureArg.IsNotNull(fhirDataStore, nameof(fhirDataStore));
            EnsureArg.IsNotNull(queryBuilder, nameof(queryBuilder));
            EnsureArg.IsNotNull(compartmentSearchRewriter, nameof(compartmentSearchRewriter));
            EnsureArg.IsNotNull(smartCompartmentSearchRewriter, nameof(smartCompartmentSearchRewriter));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _fhirDataStore = fhirDataStore;
            _queryBuilder = queryBuilder;
            _logger = logger;
            _dataStoreConfiguration = dataStoreConfiguration;
        }

        public override Task<IReadOnlyList<string>> GetUsedResourceTypes(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetUsedResourceTypes");
            throw new NotImplementedException();
        }

        // this is the Search Implementation entrypoint
        private async Task<SearchResult> SearchImpl(SearchOptions searchOptions, CancellationToken cancellationToken)
        {
            var filter = _queryBuilder.BuildFilterSpec(searchOptions);

            _logger.LogDebug(filter.ToString());

            // TODOCJH:  Is this a candidate for yield ? we are getting list of documents,
            // then making a list of search entries
            // then returning.

            var documents = await _dataStoreConfiguration
                .GetCollection()
                .Find(filter)
                .ToListAsync(cancellationToken);

            List<SearchResultEntry> resultEntries = [];

            foreach (var entry in documents)
            {
                var version = 1;
                var isDeleted = false;
                var isHistory = false;
                var isRawResourceMetaSet = true;

#pragma warning disable CS8600
                string rawResource = entry[FieldNameConstants.Resource].ToString();
#pragma warning restore CS8600

                var rm = new ResourceWrapper(
                    entry[FieldNameConstants.Resource][FieldNameConstants.Id].ToString(),
                    version.ToString(CultureInfo.InvariantCulture),
                    entry[FieldNameConstants.Resource][FieldNameConstants.ResourceType].ToString(),
                    new RawResource(rawResource, FhirResourceFormat.Json, isMetaSet: isRawResourceMetaSet),
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

                resultEntries.Add(new SearchResultEntry(rm));
            }

            return new SearchResult(
                resultEntries,
                null,
                null,
                new List<Tuple<string, string>>());
        }

        public override async Task<SearchResult> SearchAsync(SearchOptions searchOptions, CancellationToken cancellationToken)
        {
            _logger.LogInformation("SearchAsync");

            SearchResult searchResult = await SearchImpl(searchOptions, cancellationToken);

            return searchResult;
        }

        protected override Task<SearchResult> SearchForReindexInternalAsync(SearchOptions searchOptions, string searchParameterHash, CancellationToken cancellationToken)
        {
            _logger.LogInformation("SearchForReindexInternalAsync");
            throw new NotImplementedException();
        }
    }
}
