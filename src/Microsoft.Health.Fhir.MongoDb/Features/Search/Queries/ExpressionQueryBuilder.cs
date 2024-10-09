// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Fhir.Api;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Microsoft.Health.Fhir.MongoDb.Features.Queries;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
    internal sealed class ExpressionQueryBuilder : IExpressionVisitorWithInitialContext<ExpressionQueryBuilderContext, Expression>
    {
        private readonly StringBuilder _queryBuilder;
        private readonly QueryParameterManager _queryParameterManager;

        internal ExpressionQueryBuilder(
            StringBuilder queryBuilder,
            QueryParameterManager queryParameterManager)
        {
            EnsureArg.IsNotNull(queryBuilder, nameof(queryBuilder));
            EnsureArg.IsNotNull(queryParameterManager, nameof(queryParameterManager));

            _queryBuilder = queryBuilder;
            _queryParameterManager = queryParameterManager;
        }

#pragma warning disable CS8603 // Possible null reference return.
        public ExpressionQueryBuilderContext InitialContext => new ExpressionQueryBuilderContext();
#pragma warning restore CS8603 // Possible null reference return.

        public Expression VisitBinary(BinaryExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitChained(ChainedExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitCompartment(CompartmentSearchExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitIn<T>(InExpression<T> expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitInclude(IncludeExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitMissingField(MissingFieldExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitMissingSearchParameter(MissingSearchParameterExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitMultiary(MultiaryExpression expression, ExpressionQueryBuilderContext context)
        {
            MultiaryOperator op = expression.MultiaryOperation;
            IReadOnlyList<Expression> expressions = expression.Expressions;

            switch (op)
            {
                case MultiaryOperator.And:
                    break;

                case MultiaryOperator.Or:
                    break;

                default:
                    {
#pragma warning disable CA2241 // Provide correct arguments to formatting methods
                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            "UnhandledEnumValue",
                            nameof(MultiaryOperator),
                            op);
#pragma warning restore CA2241 // Provide correct arguments to formatting methods

                        Debug.Fail(message);

                        throw new InvalidOperationException(message);
                    }
            }

            for (int i = 0; i < expressions.Count; i++)
            {
                // Output each expression.
                expressions[i].AcceptVisitor(this, context);
            }
#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        public Expression VisitNotExpression(NotExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitSearchParameter(SearchParameterExpression expression, ExpressionQueryBuilderContext context)
        {
            switch (expression.Parameter.Code)
            {
                case SearchParameterNames.ResourceType:
                    context.Filter = context.Builder.Eq("resource.resourceType", "Patient");

                    // expression.Expression.AcceptVisitor(this, context.WithFieldNameOverride((n, i) => SearchValueConstants.RootResourceTypeName));

                    break;
                case SearchParameterNames.Id:
                    // expression.Expression.AcceptVisitor(this, context.WithFieldNameOverride((n, i) => KnownResourceWrapperProperties.ResourceId));

                    break;
                case SearchParameterNames.LastUpdated:
                    // For LastUpdate queries, the LastModified property on the root is
                    // more performant than the searchIndices _lastUpdated.st and _lastUpdate.et
                    // we will override the mapping for that

                    // expression.Expression.AcceptVisitor(this, context.WithFieldNameOverride((n, i) => SearchValueConstants.LastModified));

                    break;
                case SearchValueConstants.WildcardReferenceSearchParameterName:
                    // This is an internal search parameter that that matches any reference search parameter.
                    // It is used for wildcard revinclude queries
                    //                     AppendSubquery(null, context);
                    throw new NotImplementedException();
                default:
                    if (expression.Expression is NotExpression notExpression)
                    {
                        AppendSubquery(expression, context, true);
                    }
                    else
                    {
                        AppendSubquery(expression, context);
                    }

                    break;
            }

#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        private void AppendSubquery(SearchParameterExpression expression, ExpressionQueryBuilderContext context, bool negate = false)
        {
            var t = _queryParameterManager;

            if (expression.Expression != null)
            {
                context.Filter = context.Filter & context.Builder.Eq("searchIndexes.SearchParameter.Code", expression.Parameter.Code);
                expression.Expression.AcceptVisitor(this, context);
            }

            /*
                        if (expression != null)
                        {
                            expression.AcceptVisitor(this, context);
                            context.Filter = context.Filter & context.Builder.Eq("searchIndexes.SearchParameter.Code", expression.Parameter.Code);
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
                            context.Filter = context.Filter & context.Builder.Eq("searchIndexes.Value.String", (expression.Expression as StringExpression).Value);
            #pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }
            */
        }

        public Expression VisitSmartCompartment(SmartCompartmentSearchExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitSortParameter(SortExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitString(StringExpression expression, ExpressionQueryBuilderContext context)
        {
            if (expression.StringOperator == StringOperator.StartsWith)
            {
                context.Filter = context.Filter & context.Builder.Regex("searchIndexes.Value.String", "^" + expression.Value + ".*");
            }
            else if (expression.StringOperator == StringOperator.Equals)
            {
                context.Filter = context.Filter & context.Builder.Eq("searchIndexes.Value.String", expression.Value);
            }
            else if (expression.StringOperator == StringOperator.Contains)
            {
                context.Filter = context.Filter & context.Builder.Regex("searchIndexes.Value.String", expression.Value);
            }
            else
            {
                context.Filter = context.Filter & context.Builder.Eq("searchIndexes.Value.String", expression.Value);
            }

#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603

/*
            string fieldName = expression.FieldName.ToString();

            if (expression.IgnoreCase)
            {
                fieldName = SearchValueConstants.NormalizedPrefix + fieldName;
            }

            string value = expression.IgnoreCase ? expression.Value.ToUpperInvariant() : expression.Value;

            if (expression.StringOperator == StringOperator.Equals)
            {
            }
            else if (expression.StringOperator == StringOperator.LeftSideStartsWith)
            {
            }
            else
            {
            }


#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603

            */
        }

        public Expression VisitUnion(UnionExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }
    }
}
