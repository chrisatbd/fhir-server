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
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class ExpressionQueryBuilderContext
    {
        public QueryAssembler Assembler { get; set; } = new QueryAssembler();

#pragma warning disable SA1201
        public ExpressionQueryBuilderContext()
        {
        }
#pragma warning restore SA1201

        public BsonDocument GetFilters()
        {
#pragma warning disable CS8603
            return Assembler.RenderFilters();
#pragma warning restore CS8603
        }
    }
}
