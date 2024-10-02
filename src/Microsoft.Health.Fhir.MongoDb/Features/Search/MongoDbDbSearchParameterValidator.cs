// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "WTF")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "WTF Part 2")]

    internal class MongoDbDbSearchParameterValidator : IDataStoreSearchParameterValidator
    {
        // Currently Cosmos DB has not additional validation steps to perform specific to the
        // data store for validation of a SearchParameter
        public bool ValidateSearchParameter(SearchParameterInfo searchParameter, out string errorMessage)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            errorMessage = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return true;
        }
    }
}
