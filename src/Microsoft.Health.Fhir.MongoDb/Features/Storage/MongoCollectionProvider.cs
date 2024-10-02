// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Fhir.MongoDb.Features.Storage
{
    public class MongoCollectionProvider : IHostedService, IRequireInitializationOnFirstRequest, IDisposable
    {
        private readonly ILogger<MongoCollectionProvider> _logger;
        private readonly IMediator _mediator;

        public MongoCollectionProvider(
            ILogger<MongoCollectionProvider> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // _initializationOperation.Dispose();
                // _client.Dispose();
                // _container = null;
            }
        }

        public Task EnsureInitialized()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
