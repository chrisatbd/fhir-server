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
    internal sealed class ExpressionQueryBuilder : IExpressionVisitorWithInitialContext<ExpressionQueryBuilderContext, object>
    {
        private readonly QueryParameterManager _queryParameterManager;
        private readonly QueryAssembler _queryAssembler;

#pragma warning disable CA1823
        private static readonly Dictionary<FieldName, string> FieldNameMapping = new Dictionary<FieldName, string>()
#pragma warning restore CA1823
        {
            { FieldName.DateTimeEnd, SearchValueConstants.DateTimeEndName },
            { FieldName.DateTimeStart, SearchValueConstants.DateTimeStartName },
            { FieldName.Number, SearchValueConstants.NumberName },
            { FieldName.ParamName, SearchValueConstants.ParamName },
            { FieldName.Quantity, SearchValueConstants.QuantityName },
            { FieldName.QuantityCode, SearchValueConstants.CodeName },
            { FieldName.QuantitySystem, SearchValueConstants.SystemName },
            { FieldName.ReferenceBaseUri, SearchValueConstants.ReferenceBaseUriName },
            { FieldName.ReferenceResourceId, SearchValueConstants.ReferenceResourceIdName },
            { FieldName.ReferenceResourceType, SearchValueConstants.ReferenceResourceTypeName },
            { FieldName.String, SearchValueConstants.StringName },
            { FieldName.TokenCode, SearchValueConstants.CodeName },
            { FieldName.TokenSystem, SearchValueConstants.SystemName },
            { FieldName.TokenText, SearchValueConstants.TextName },
            { FieldName.Uri, SearchValueConstants.UriName },
        };

        internal ExpressionQueryBuilder(
            QueryParameterManager queryParameterManager)
        {
            EnsureArg.IsNotNull(queryParameterManager, nameof(queryParameterManager));

            _queryParameterManager = queryParameterManager;
            _queryAssembler = new QueryAssembler();
        }

#pragma warning disable CS8603
        public ExpressionQueryBuilderContext InitialContext => new ExpressionQueryBuilderContext();
#pragma warning restore CS8603

        public BsonDocument GetFilters()
        {
#pragma warning disable CS8603
            Console.WriteLine(_queryAssembler.RenderFilters().ToString());
            return _queryAssembler.RenderFilters();
#pragma warning restore CS8603
        }

        public object VisitBinary(BinaryExpression expression, ExpressionQueryBuilderContext context)
        {
            string fieldName = GetFieldName(expression);

            string field = $"searchIndexes.Value.{fieldName}";

            if (expression.BinaryOperator == BinaryOperator.Equal)
            {
                throw new NotImplementedException();
            }

            if (expression.BinaryOperator == BinaryOperator.NotEqual)
            {
                throw new NotImplementedException();
            }

            if (expression.BinaryOperator == BinaryOperator.GreaterThan)
            {
                throw new NotImplementedException();

                // _queryAssembler.Filter = _queryAssembler.Filter &
                 //   _queryAssembler.Builder.Gt(field, expression.Value);
            }

            if (expression.BinaryOperator == BinaryOperator.GreaterThanOrEqual)
            {
                throw new NotImplementedException();

                // _queryAssembler.Filter = _queryAssembler.Filter &
                //    _queryAssembler.Builder.Gte(field, expression.Value);
            }

            if (expression.BinaryOperator == BinaryOperator.LessThan)
            {
                throw new NotImplementedException();
            }

            if (expression.BinaryOperator == BinaryOperator.LessThanOrEqual)
            {
                throw new NotImplementedException();
            }

#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        public object VisitChained(ChainedExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitCompartment(CompartmentSearchExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitIn<T>(InExpression<T> expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitInclude(IncludeExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitMissingField(MissingFieldExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitMissingSearchParameter(MissingSearchParameterExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitMultiary(MultiaryExpression expression, ExpressionQueryBuilderContext context)
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
#pragma warning disable CA2241
                        string message = string.Format(
                            CultureInfo.InvariantCulture,
                            "UnhandledEnumValue",
                            nameof(MultiaryOperator),
                            op);
#pragma warning restore CA2241

                        Debug.Fail(message);

                        throw new InvalidOperationException(message);
                    }
            }

            for (int i = 0; i < expressions.Count; i++)
            {
                expressions[i].AcceptVisitor(this, context);
            }

#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        public object VisitNotExpression(NotExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitSearchParameter(SearchParameterExpression expression, ExpressionQueryBuilderContext context)
        {
            switch (expression.Parameter.Code)
            {
                case SearchParameterNames.ResourceType:

                    _queryAssembler
                        .Filter
                        .Add(new BsonDocument("resource.resourceType", ((StringExpression)expression.Expression).Value));

                    // _queryAssembler.Filter = _queryAssembler
                     //   .Builder
                      //  .Eq("resource.resourceType", ((StringExpression)expression.Expression).Value);

                    // new BsonDocument("resource.resourceType", ((StringExpression)expression.Expression).Value)

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
                    // This is an internal search parameter that matches any reference search parameter.
                    // It is used for wildcard revinclude queries
                    throw new NotImplementedException();
                default:
                    // TODOCJH:  Review NotExpression notExpression expressions
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

        // AppendSubquery
        private void AppendSubquery(SearchParameterExpression expression, ExpressionQueryBuilderContext context, bool negate = false)
        {
            _queryAssembler.StartNewFilter();

            if (expression.Expression != null)
            {
                _queryAssembler.AddCondition(new BsonDocument("SearchParameter.Code", expression.Parameter.Code));
                expression.Expression.AcceptVisitor(this, context);
            }

            _queryAssembler.PushNewFilter();
        }

        public object VisitSmartCompartment(SmartCompartmentSearchExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public object VisitSortParameter(SortExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }

        private static string GetFieldName(IFieldExpression field)
        {
            // field.FieldName;
            string fieldName = field.FieldName.ToString();

            // if (FieldNameMapping.TryGetValue(field.FieldName, out string value))
            // {
              //  fieldName = value;
            // }

            // string fieldName = field.FieldName.ToString();

            if (fieldName == "TokenCode")
            {
                fieldName = "Code";
            }

            if (fieldName == "TokenSystem")
            {
                fieldName = "System";
            }

            if (fieldName == "ReferenceResourceType")
            {
                fieldName = "ResourceType";
            }

            if (fieldName == "ReferenceResourceId")
            {
                fieldName = "ResourceId";
            }

            if (fieldName == "DateTimeStart")
            {
                fieldName = "Start";
            }

            if (fieldName == "DateTimeEnd")
            {
                fieldName = "StartEnd";
            }

            return fieldName;
        }

        public object VisitString(StringExpression expression, ExpressionQueryBuilderContext context)
        {
            string fieldName = GetFieldName(expression);

            string field = $"Value.{fieldName}";

            if (expression.StringOperator == StringOperator.StartsWith)
            {
                // _queryAssembler.Filter = _queryAssembler.Filter &
                //   _queryAssembler.Builder.Regex(field, "^" + expression.Value + ".*");

                _queryAssembler.AddCondition(new BsonDocument(field, new BsonRegularExpression("^" + expression.Value + ".*")));
            }
            else if (expression.StringOperator == StringOperator.Equals)
            {
                _queryAssembler.AddCondition(new BsonDocument(field, expression.Value));

               // _queryAssembler.Filter = _queryAssembler.Filter &
                 //   _queryAssembler.Builder.Eq(field, expression.Value);
            }
            else if (expression.StringOperator == StringOperator.Contains)
            {
               // _queryAssembler.Filter = _queryAssembler.Filter &
                 //   _queryAssembler.Builder.Regex(field, expression.Value);
            }
            else
            {
               // _queryAssembler.Filter = _queryAssembler.Filter &
                 //   _queryAssembler.Builder.Eq(field, expression.Value);
            }

#pragma warning disable CS8603
            return null;
#pragma warning restore CS8603
        }

        public object VisitUnion(UnionExpression expression, ExpressionQueryBuilderContext context)
        {
            throw new NotImplementedException();
        }
    }
}
