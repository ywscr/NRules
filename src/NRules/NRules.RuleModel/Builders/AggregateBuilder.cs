using System;
using System.Linq.Expressions;
using NRules.RuleModel.Aggregators;

namespace NRules.RuleModel.Builders
{
    /// <summary>
    /// Builder to compose an aggregate element.
    /// </summary>
    public class AggregateBuilder : RuleElementBuilder, IBuilder<AggregateElement>, IPatternContainerBuilder
    {
        private readonly Type _resultType;
        private IAggregatorFactory _aggregatorFactory; 
        private PatternBuilder _sourceBuilder;

        internal AggregateBuilder(Type resultType, SymbolTable scope) 
            : base(scope.New("Aggregate"))
        {
            _resultType = resultType;
        }

        /// <summary>
        /// Sets aggregator.
        /// </summary>
        /// <param name="aggregatorType">Type that implements <see cref="IAggregator"/> that aggregates facts.</param>
        public void Aggregator(Type aggregatorType)
        {
            if (!typeof(IAggregator).IsAssignableFrom(aggregatorType))
            {
                throw new InvalidOperationException(
                    "Aggregator type must implement IAggregator interface");
            }
            if (aggregatorType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new InvalidOperationException(
                    "Aggregator type must have a parameterless constructor to be used directly. Provide aggregator factory instead.");
            }
            Type factoryType = typeof(DefaultAggregatorFactory<>).MakeGenericType(aggregatorType);
            var aggregatorFactory = (IAggregatorFactory) Activator.CreateInstance(factoryType);
            AggregatorFactory(aggregatorFactory);
        }

        /// <summary>
        /// Sets aggregator factory.
        /// </summary>
        /// <param name="aggregatorFactory">Factory to create new aggregators.</param>
        public void AggregatorFactory(IAggregatorFactory aggregatorFactory)
        {
            _aggregatorFactory = aggregatorFactory;
        }

        /// <summary>
        /// Configure a collection aggregator.
        /// </summary>
        /// <param name="elementType">Type of elements to aggregate.</param>
        public void CollectionOf(Type elementType)
        {
            Type aggregateType = typeof (CollectionAggregator<>).MakeGenericType(elementType);
            Aggregator(aggregateType);
        }

        /// <summary>
        /// Configure a collection aggregate.
        /// </summary>
        /// <typeparam name="TElement">Type of elements to aggregate.</typeparam>
        public void CollectionOf<TElement>()
        {
            var aggregateFactory = new DefaultAggregatorFactory<CollectionAggregator<TElement>>();
            AggregatorFactory(aggregateFactory);
        }

        /// <summary>
        /// Configure group by aggregator.
        /// </summary>
        /// <param name="keySelector">Key selection expressions.</param>
        /// <param name="valueSelector">Value selection expression.</param>
        /// <typeparam name="TSource">Type of source elements to aggregate.</typeparam>
        /// <typeparam name="TKey">Type of grouping key.</typeparam>
        /// <typeparam name="TValue">Type of grouping value.</typeparam>
        public void GroupBy<TSource, TKey, TValue>(Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TValue>> valueSelector)
        {
            var aggregateFactory = new GroupByAggregatorFactory<TSource, TKey, TValue>(keySelector, valueSelector);
            AggregatorFactory(aggregateFactory);
        }

        /// <summary>
        /// Configure projection aggregator.
        /// </summary>
        /// <param name="selector">Projection expression.</param>
        /// <typeparam name="TSource">Type of source elements to aggregate.</typeparam>
        /// <typeparam name="TElement">Type of projection elements to produce.</typeparam>
        public void Project<TSource, TElement>(Expression<Func<TSource, TElement>> selector)
        {
            var aggregateFactory = new ProjectionAggregatorFactory<TSource, TElement>(selector);
            AggregatorFactory(aggregateFactory);
        }

        /// <summary>
        /// Creates a pattern builder that builds the source of the aggregate.
        /// </summary>
        /// <param name="type">Type of the element the pattern matches.</param>
        /// <param name="name">Pattern name (optional).</param>
        /// <returns>Pattern builder.</returns>
        public PatternBuilder Pattern(Type type, string name = null)
        {
            Declaration declaration = Scope.Declare(type, name);
            return Pattern(declaration);
        }

        /// <summary>
        /// Creates a pattern builder that builds the source of the aggregate element.
        /// </summary>
        /// <param name="declaration">Pattern declaration.</param>
        /// <returns>Pattern builder.</returns>
        public PatternBuilder Pattern(Declaration declaration)
        {
            AssertSingleSource();
            var sourceBuilder = new PatternBuilder(Scope, declaration);
            _sourceBuilder = sourceBuilder;
            return sourceBuilder;
        }

        AggregateElement IBuilder<AggregateElement>.Build()
        {
            Validate();
            IBuilder<PatternElement> sourceBuilder = _sourceBuilder;
            PatternElement sourceElement = sourceBuilder.Build();
            var aggregateElement = new AggregateElement(Scope.VisibleDeclarations, _resultType, _aggregatorFactory, sourceElement);
            return aggregateElement;
        }

        private void Validate()
        {
            if (_aggregatorFactory == null)
            {
                throw new InvalidOperationException("Aggregator factory is not provided");
            }
            if (_sourceBuilder == null)
            {
                throw new InvalidOperationException("Aggregate element source is not provided");
            }
        }

        private void AssertSingleSource()
        {
            if (_sourceBuilder != null)
            {
                throw new InvalidOperationException("Aggregate element can only have a single source");
            }
        }
    }
}