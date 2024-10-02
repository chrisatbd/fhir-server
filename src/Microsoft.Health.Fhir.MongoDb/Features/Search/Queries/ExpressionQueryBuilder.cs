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

namespace Microsoft.Health.Fhir.MongoDb.Features.Search.Queries
{
#pragma warning disable SA1649 // File name should match first type name
    internal struct ExpressionQueryBuilderContext
#pragma warning restore SA1649 // File name should match first type name
    {
        public ExpressionQueryBuilderContext(string instanceVariableName, Func<FieldName, int?, string> fieldNameOverride)
        {
            InstanceVariableName = instanceVariableName;
            FieldNameOverride = fieldNameOverride;
        }

        public string InstanceVariableName { get; }

        public Func<FieldName, int?, string> FieldNameOverride { get; }

        public ExpressionQueryBuilderContext WithInstanceVariableName(string instanceVariableName)
        {
            return new ExpressionQueryBuilderContext(instanceVariableName: instanceVariableName, fieldNameOverride: FieldNameOverride);
        }

        public ExpressionQueryBuilderContext WithFieldNameOverride(Func<FieldName, int?, string> fieldNameOverride)
        {
            return new ExpressionQueryBuilderContext(instanceVariableName: InstanceVariableName, fieldNameOverride: fieldNameOverride);
        }
    }

    internal sealed class ExpressionQueryBuilder : IExpressionVisitorWithInitialContext<object, Expression>
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
        public object InitialContext => null;
#pragma warning restore CS8603 // Possible null reference return.

        public Expression VisitBinary(BinaryExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitChained(ChainedExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitCompartment(CompartmentSearchExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitIn<T>(InExpression<T> expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitInclude(IncludeExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitMissingField(MissingFieldExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitMissingSearchParameter(MissingSearchParameterExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitMultiary(MultiaryExpression expression, object context)
        {
            MultiaryOperator op = expression.MultiaryOperation;
            IReadOnlyList<Expression> expressions = expression.Expressions;
            string operation;

            switch (op)
            {
                case MultiaryOperator.And:
                    operation = "AND";
                    break;

                case MultiaryOperator.Or:
                    operation = "OR";
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

                if (i != expressions.Count - 1)
                {
                    if (!char.IsWhiteSpace(_queryBuilder[_queryBuilder.Length - 1]))
                    {
                        _queryBuilder.Append(' ');
                    }

                    _queryBuilder.Append(operation).Append(' ');
                }
            }
#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        public Expression VisitNotExpression(NotExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitSearchParameter(SearchParameterExpression expression, object context)
        {
            switch (expression.Parameter.Code)
            {
                case SearchParameterNames.ResourceType:
                    // We do not currently support specifying the system for the _type parameter value.
                    // We would need to add it to the document, but for now it seems pretty unlikely that it will
                    // be specified when searching.

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
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    AppendSubquery(parameterName: null, expression.Expression, context);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                    break;
                default:
                    if (expression.Expression is NotExpression notExpression)
                    {
                        AppendSubquery(expression.Parameter.Code, notExpression.Expression, context, true);
                    }
                    else
                    {
                        AppendSubquery(expression.Parameter.Code, expression.Expression, context);
                    }

                    break;
            }

#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        private void AppendSubquery(string parameterName, Expression expression, object context, bool negate = false)
        {
            if (negate)
            {
                _queryBuilder.Append("NOT ");
            }

            _queryBuilder.Append("EXISTS (SELECT VALUE ")
                .Append(SearchValueConstants.SearchIndexAliasName)
                .Append(" FROM ")
                .Append(SearchValueConstants.SearchIndexAliasName)
                .Append(" IN ")
#pragma warning disable CA1834 // Consider using 'StringBuilder.Append(char)' when applicable
                .Append(SearchValueConstants.RootAliasName)
#pragma warning restore CA1834 // Consider using 'StringBuilder.Append(char)' when applicable
                .Append('.')
                .Append(KnownResourceWrapperProperties.SearchIndices)
                .Append(" WHERE ");

            // context = context.WithInstanceVariableName(SearchValueConstants.SearchIndexAliasName);

            if (parameterName != null)
            {
                // VisitBinary(GetMappedValue(FieldNameMapping, FieldName.ParamName), BinaryOperator.Equal, parameterName, context);
            }

            if (expression != null)
            {
                if (parameterName != null)
                {
                    _queryBuilder.Append(" AND ");
                }

                expression.AcceptVisitor(this, context);
            }

            _queryBuilder.Append(')');
        }

        public Expression VisitSmartCompartment(SmartCompartmentSearchExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitSortParameter(SortExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitString(StringExpression expression, object context)
        {
            throw new NotImplementedException();
        }

        public Expression VisitUnion(UnionExpression expression, object context)
        {
            throw new NotImplementedException();
        }
    }
}
