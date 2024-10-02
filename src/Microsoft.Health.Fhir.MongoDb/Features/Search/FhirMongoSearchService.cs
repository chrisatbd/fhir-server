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
using SemVer;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Pending")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "Pending")]
    internal class FhirMongoSearchService : SearchService
    {
        // private static readonly SearchParameterInfo _wildcardReferenceSearchParameter = new(SearchValueConstants.WildcardReferenceSearchParameterName, SearchValueConstants.WildcardReferenceSearchParameterName);
        // private readonly Lazy<IReadOnlyCollection<IExpressionVisitorWithInitialContext<object, Expression>>> _expressionRewriters;

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

            /*
            _expressionRewriters = new Lazy<IReadOnlyCollection<IExpressionVisitorWithInitialContext<object, Expression>>>(() =>
                new IExpressionVisitorWithInitialContext<object, Expression>[]
                {
                    compartmentSearchRewriter,
                  smartCompartmentSearchRewriter,
                    DateTimeEqualityRewriter.Instance,
                });
            */
        }

        public override Task<IReadOnlyList<string>> GetUsedResourceTypes(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetUsedResourceTypes");
            throw new NotImplementedException();
        }

        public override async Task<SearchResult> SearchAsync(SearchOptions searchOptions, CancellationToken cancellationToken)
        {
            _logger.LogInformation("SearchAsync");

            // we're going to mutate searchOptions, so clone it first so the caller of this method does not see the changes.
            searchOptions = searchOptions.Clone();

            if (searchOptions.Expression != null)
            {
                // Apply Mongo specific expression rewriters
                // searchOptions.Expression = _expressionRewriters.Value
                //    .Aggregate(searchOptions.Expression, (e, rewriter) => e.AcceptVisitor(rewriter));
            }

            var filter = _queryBuilder.BuildSqlQuerySpec(searchOptions);

            var document = await _dataStoreConfiguration.GetCollection("patient")
                .Find(filter)
                .ToListAsync(cancellationToken);

            List<SearchResultEntry> resultEntries = new List<SearchResultEntry>();

            foreach (var entry in document)
            {
                var version = 1;
                var isDeleted = false;
                var isHistory = false;
                var isRawResourceMetaSet = true;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string rawResource = entry["resource"].ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                var rm = new ResourceWrapper(
                    entry["resource"]["id"].ToString(),
                    version.ToString(CultureInfo.InvariantCulture),
                    entry["resource"]["resourceType"].ToString(),
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

                SearchResultEntry sre = new SearchResultEntry(rm);

                resultEntries.Add(sre);
            }

            return new SearchResult(
                resultEntries,
                null,
                null,
                new System.Collections.Generic.List<Tuple<string, string>>());
        }

        protected override Task<SearchResult> SearchForReindexInternalAsync(SearchOptions searchOptions, string searchParameterHash, CancellationToken cancellationToken)
        {
            _logger.LogInformation("SearchForReindexInternalAsync");
            throw new NotImplementedException();
        }
    }
}
