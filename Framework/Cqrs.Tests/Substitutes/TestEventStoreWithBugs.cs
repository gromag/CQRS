using System;
using System.Collections.Generic;
using System.Linq;
using Cqrs.Events;
using Cqrs.Authentication;
using Cqrs.Domain;

namespace Cqrs.Tests.Substitutes
{
	public class TestEventStoreWithBugs
		: IEventStore<ISingleSignOnToken>
	{
		public void Save(Type aggregateRootType, IEvent<ISingleSignOnToken> @event)
		{
		}

		public IEnumerable<IEvent<ISingleSignOnToken>> Get<T>(Guid aggregateId, bool useLastEventOnly = false, int fromVersion = -1)
		{
			return Get(typeof (T), aggregateId, useLastEventOnly, fromVersion);
		}

		public IEnumerable<IEvent<ISingleSignOnToken>> Get(Type aggregateType, Guid aggregateId, bool useLastEventOnly = false, int fromVersion = -1)
		{
			if (aggregateId == Guid.Empty)
			{
				return new List<IEvent<ISingleSignOnToken>>();
			}

			return new List<IEvent<ISingleSignOnToken>>
			{
				new TestAggregateDidSomething {Id = aggregateId, Version = 3},
				new TestAggregateDidSomething {Id = aggregateId, Version = 4},
				new TestAggregateDidSomethingElse {Id = aggregateId, Version = 2}
			}
			.Where(x => x.Version > fromVersion);
		}

		/// <summary>
		/// Gets a collection of <see cref="IEvent{TAuthenticationToken}"/> for the <see cref="IAggregateRoot{TAuthenticationToken}"/> of type <paramref name="aggregateRootType"/> with the ID matching the provided <paramref name="aggregateId"/> up to and including the provided <paramref name="version"/>.
		/// </summary>
		/// <param name="aggregateRootType"> <see cref="Type"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/> the <see cref="IEvent{TAuthenticationToken}"/> was raised in.</param>
		/// <param name="aggregateId">The <see cref="IAggregateRoot{TAuthenticationToken}.Id"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/>.</param>
		/// <param name="version">Load events up-to and including from this version</param>
		public IEnumerable<IEvent<ISingleSignOnToken>> GetToVersion(Type aggregateRootType, Guid aggregateId, int version)
		{
			return Get(aggregateRootType, aggregateId);
		}

		/// <summary>
		/// Gets a collection of <see cref="IEvent{TAuthenticationToken}"/> for the <typeparamref name="T">aggregate root</typeparamref> with the ID matching the provided <paramref name="aggregateId"/> up to and including the provided <paramref name="version"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="Type"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/> the <see cref="IEvent{TAuthenticationToken}"/> was raised in.</typeparam>
		/// <param name="aggregateId">The <see cref="IAggregateRoot{TAuthenticationToken}.Id"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/>.</param>
		/// <param name="version">Load events up-to and including from this version</param>
		public IEnumerable<IEvent<ISingleSignOnToken>> GetToVersion<T>(Guid aggregateId, int version)
		{
			return GetToVersion(typeof(T), aggregateId, version);
		}

		/// <summary>
		/// Gets a collection of <see cref="IEvent{TAuthenticationToken}"/> for the <see cref="IAggregateRoot{TAuthenticationToken}"/> of type <paramref name="aggregateRootType"/> with the ID matching the provided <paramref name="aggregateId"/> up to and including the provided <paramref name="versionedDate"/>.
		/// </summary>
		/// <param name="aggregateRootType"> <see cref="Type"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/> the <see cref="IEvent{TAuthenticationToken}"/> was raised in.</param>
		/// <param name="aggregateId">The <see cref="IAggregateRoot{TAuthenticationToken}.Id"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/>.</param>
		/// <param name="versionedDate">Load events up-to and including from this <see cref="DateTime"/></param>
		public IEnumerable<IEvent<ISingleSignOnToken>> GetToDate(Type aggregateRootType, Guid aggregateId, DateTime versionedDate)
		{
			return Get(aggregateRootType, aggregateId);
		}

		/// <summary>
		/// Gets a collection of <see cref="IEvent{TAuthenticationToken}"/> for the <typeparamref name="T">aggregate root</typeparamref> with the ID matching the provided <paramref name="aggregateId"/> up to and including the provided <paramref name="versionedDate"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="Type"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/> the <see cref="IEvent{TAuthenticationToken}"/> was raised in.</typeparam>
		/// <param name="aggregateId">The <see cref="IAggregateRoot{TAuthenticationToken}.Id"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/>.</param>
		/// <param name="versionedDate">Load events up-to and including from this <see cref="DateTime"/></param>
		public IEnumerable<IEvent<ISingleSignOnToken>> GetToDate<T>(Guid aggregateId, DateTime versionedDate)
		{
			return GetToDate(typeof(T), aggregateId, versionedDate);
		}

		/// <summary>
		/// Gets a collection of <see cref="IEvent{TAuthenticationToken}"/> for the <see cref="IAggregateRoot{TAuthenticationToken}"/> of type <paramref name="aggregateRootType"/> with the ID matching the provided <paramref name="aggregateId"/> from and including the provided <paramref name="fromVersionedDate"/> up to and including the provided <paramref name="toVersionedDate"/>.
		/// </summary>
		/// <param name="aggregateRootType"> <see cref="Type"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/> the <see cref="IEvent{TAuthenticationToken}"/> was raised in.</param>
		/// <param name="aggregateId">The <see cref="IAggregateRoot{TAuthenticationToken}.Id"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/>.</param>
		/// <param name="fromVersionedDate">Load events from and including from this <see cref="DateTime"/></param>
		/// <param name="toVersionedDate">Load events up-to and including from this <see cref="DateTime"/></param>
		public IEnumerable<IEvent<ISingleSignOnToken>> GetBetweenDates(Type aggregateRootType, Guid aggregateId, DateTime fromVersionedDate, DateTime toVersionedDate)
		{
			return Get(aggregateRootType, aggregateId);
		}

		/// <summary>
		/// Gets a collection of <see cref="IEvent{TAuthenticationToken}"/> for the <typeparamref name="T">aggregate root</typeparamref> with the ID matching the provided <paramref name="aggregateId"/> from and including the provided <paramref name="fromVersionedDate"/> up to and including the provided <paramref name="toVersionedDate"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="Type"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/> the <see cref="IEvent{TAuthenticationToken}"/> was raised in.</typeparam>
		/// <param name="aggregateId">The <see cref="IAggregateRoot{TAuthenticationToken}.Id"/> of the <see cref="IAggregateRoot{TAuthenticationToken}"/>.</param>
		/// <param name="fromVersionedDate">Load events from and including from this <see cref="DateTime"/></param>
		/// <param name="toVersionedDate">Load events up-to and including from this <see cref="DateTime"/></param>
		public IEnumerable<IEvent<ISingleSignOnToken>> GetBetweenDates<T>(Guid aggregateId, DateTime fromVersionedDate, DateTime toVersionedDate)
		{
			return GetBetweenDates(typeof(T), aggregateId, fromVersionedDate, toVersionedDate);
		}

		public IEnumerable<EventData> Get(Guid correlationId)
		{
			return Enumerable.Empty<EventData>();
		}

		public void Save<T>(IEvent<ISingleSignOnToken> eventDescriptor)
		{
		}
	}
}