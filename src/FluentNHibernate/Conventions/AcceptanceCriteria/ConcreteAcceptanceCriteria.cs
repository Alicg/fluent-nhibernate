using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Utils;
using FluentNHibernate.Utils.Reflection;

namespace FluentNHibernate.Conventions.AcceptanceCriteria
{
    public class ConcreteAcceptanceCriteria<TInspector> : IAcceptanceCriteria<TInspector>
        where TInspector : IInspector
    {
        private readonly List<IExpectation> expectations = new List<IExpectation>();

        public IAcceptanceCriteria<TInspector> SameAs<T>() where T : IConventionAcceptance<TInspector>, new()
        {
            var otherCriteria = new T();

            otherCriteria.Accept(this);

            return this;
        }

        public IAcceptanceCriteria<TInspector> OppositeOf<T>() where T : IConventionAcceptance<TInspector>, new()
        {
            var invertedCriteria = new InverterAcceptanceCriteria<TInspector>();
            var otherCriteria = new T();

            otherCriteria.Accept(invertedCriteria);

            foreach (var expectation in invertedCriteria.Expectations)
                expectations.Add(expectation);

            return this;
        }

        public virtual IAcceptanceCriteria<TInspector> Expect(Expression<Func<TInspector, object>> propertyExpression, IAcceptanceCriterion value)
        {
            var expectation = CreateExpectation(propertyExpression, value);
            expectations.Add(expectation);
            return this;
        }

        public IAcceptanceCriteria<TInspector> Expect(Expression<Func<TInspector, string>> propertyExpression, IAcceptanceCriterion value)
        {
            var property = ReflectionHelper.GetProperty(propertyExpression);
            var castedExpression = ExpressionBuilder.Create<TInspector>(property);
            var expectation = CreateExpectation(castedExpression, value);

            expectations.Add(expectation);
            return this;
        }

        public IAcceptanceCriteria<TInspector> Expect(Expression<Func<TInspector, bool>> evaluation)
        {
            var expectation = CreateEvalExpectation(evaluation);

            expectations.Add(expectation);
            return this;
        }

        public IAcceptanceCriteria<TInspector> Expect<TCollectionItem>(Expression<Func<TInspector, IEnumerable<TCollectionItem>>> property, ICollectionAcceptanceCriterion<TCollectionItem> value)
            where TCollectionItem : IInspector
        {
            var expectation = CreateCollectionExpectation(property, value);

            expectations.Add(expectation);
            return this;
        }

        public IAcceptanceCriteria<TInspector> Any(params Action<IAcceptanceCriteria<TInspector>>[] criteriaBuilders)
        {
            var tempCriteria = new ConcreteAcceptanceCriteria<TInspector>();
            foreach (var builder in criteriaBuilders)
                builder(tempCriteria);

            expectations.Add(new AnyExpectation<TInspector>(tempCriteria.Expectations));
            return this;
        }

        public IAcceptanceCriteria<TInspector> Either(Action<IAcceptanceCriteria<TInspector>> criteriaBuilderA, Action<IAcceptanceCriteria<TInspector>> criteriaBuilderB)
        {
            return Any(criteriaBuilderA, criteriaBuilderB);
        }

        public IEnumerable<IExpectation> Expectations
        {
            get { return expectations; }
        }

        public bool Matches(IInspector inspector)
        {
            return Expectations.All(x => x.Matches(inspector));
        }

        protected virtual IExpectation CreateExpectation(Expression<Func<TInspector, object>> expression, IAcceptanceCriterion value)
        {
            return new Expectation<TInspector>(expression, value);
        }

        protected virtual IExpectation CreateEvalExpectation(Expression<Func<TInspector, bool>> expression)
        {
            return new EvalExpectation<TInspector>(expression);
        }

        protected virtual IExpectation CreateCollectionExpectation<TCollectionItem>(Expression<Func<TInspector, IEnumerable<TCollectionItem>>> property, ICollectionAcceptanceCriterion<TCollectionItem> value)
            where TCollectionItem : IInspector
        {
            return new CollectionExpectation<TInspector, TCollectionItem>(property, value);
        }
    }
}