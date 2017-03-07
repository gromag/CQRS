﻿#region Copyright
// // -----------------------------------------------------------------------
// // <copyright company="cdmdotnet Limited">
// // 	Copyright cdmdotnet Limited. All rights reserved.
// // </copyright>
// // -----------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using cdmdotnet.Logging;
using Cqrs.Entities;
using Cqrs.Events;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Cqrs.Azure.BlobStorage
{
	public abstract class TableStorageStore<TData>
		: StorageStore<TData, CloudTable>
	{
		protected TableQuery<TData> Collection { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TableStorageStore{TData}"/> class using the specified container.
		/// </summary>
		protected TableStorageStore(ILogger logger)
			: base(logger)
		{
		}

		#region Overrides of StorageStore<TData,CloudTable>

		protected override void Initialise(IStorageStoreConnectionStringFactory tableStorageDataStoreConnectionStringFactory)
		{
			base.Initialise(tableStorageDataStoreConnectionStringFactory);
			Collection = new TableQuery<TData>();
		}

		#endregion

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public override IEnumerator<TData> GetEnumerator()
		{
			return Collection.GetEnumerator();
		}

		#endregion

		#region Implementation of IQueryable

		/// <summary>
		/// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable"/>.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.Linq.Expressions.Expression"/> that is associated with this instance of <see cref="T:System.Linq.IQueryable"/>.
		/// </returns>
		public override Expression Expression
		{
			get
			{
				return Collection.Expression;
			}
		}

		/// <summary>
		/// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="T:System.Linq.IQueryable"/> is executed.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Type"/> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.
		/// </returns>
		public override Type ElementType
		{
			get
			{
				return Collection.ElementType;
			}
		}

		/// <summary>
		/// Gets the query provider that is associated with this data source.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.Linq.IQueryProvider"/> that is associated with this data source.
		/// </returns>
		public override IQueryProvider Provider
		{
			get
			{
				return Collection.Provider;
			}
		}

		#endregion

		protected virtual void AsyncSaveData<TSaveData, TResult>(TSaveData data, Func<TSaveData, CloudTable, TResult> function, Func<TSaveData, string> customFilenameFunction = null)
		{
			IList<Task> persistTasks = new List<Task>();
			foreach (Tuple<CloudStorageAccount, CloudTable> tuple in WritableCollection)
			{
				TSaveData taskData = data;
				CloudTable table = tuple.Item2;
				Task task = Task.Factory.StartNewSafely
				(
					() =>
					{
						AzureStorageRetryPolicy.ExecuteAction(() => function(taskData, table));
					}
				);
				persistTasks.Add(task);
			}

			bool anyFailed = Task.Factory.ContinueWhenAll(persistTasks.ToArray(), tasks =>
			{
				return tasks.Any(task => task.IsFaulted);
			}).Result;
			if (anyFailed)
				throw new AggregateException("Persisting data to table storage failed. Check the logs for more details.");
		}

		protected abstract TableEntity CreateTableEntity(TData data);

		#region Implementation of IDataStore<TData>

		public override void Add(TData data)
		{
			AsyncSaveData
			(
				data,
				(taskData, table) =>
				{
					try
					{
						// Create the TableOperation object that inserts the customer entity.
						TableEntity tableEntity = CreateTableEntity(data);
						TableOperation insertOperation = TableOperation.Insert(tableEntity);

						// Execute the insert operation.
						return table.Execute(insertOperation);
					}
					catch (Exception exception)
					{
						Logger.LogError("There was an issue persisting data to table storage.", exception: exception);
						throw;
					}
				}
			);
		}

		public override void Add(IEnumerable<TData> data)
		{
			AsyncSaveData
			(
				data,
				(taskData, table) =>
				{
					try
					{
						// Create the batch operation.
						TableBatchOperation batchOperation = new TableBatchOperation();

						foreach (TData item in data)
						{
							// Create the TableOperation object that inserts the customer entity.
							TableEntity tableEntity = CreateTableEntity(item);
							batchOperation.Insert(tableEntity);
						}

						// Execute the insert operation.
						return table.ExecuteBatch(batchOperation);
					}
					catch (Exception exception)
					{
						Logger.LogError("There was an issue persisting data to table storage.", exception: exception);
						throw;
					}
				}
			);
		}

		public override void Destroy(TData data)
		{
			AsyncSaveData
			(
				data,
				(taskData, table) =>
				{
					try
					{
						// Create a retrieve operation that takes a customer entity.
						TableOperation retrieveOperation = GetUpdatableTableEntity(data);

						// Execute the operation.
						TableResult retrievedResult = table.Execute(retrieveOperation);
						TableEntity tableEntity = (TableEntity)retrievedResult.Result;
						var eventTableEntity = tableEntity as IEventDataTableEntity<TData>;
						if (eventTableEntity != null)
							eventTableEntity.EventData = data;
						else
							((IEntityTableEntity<TData>)tableEntity).Entity = data;

						TableOperation deleteOperation = TableOperation.Delete(tableEntity);

						// Execute the delete operation.
						return table.Execute(deleteOperation);
					}
					catch (Exception exception)
					{
						Logger.LogError("There was an issue deleting data from table storage.", exception: exception);
						throw;
					}
				}
			);
		}

		public override void RemoveAll()
		{
			foreach (Tuple<CloudStorageAccount, CloudTable> tuple in WritableCollection)
				tuple.Item2.DeleteIfExists();
		}

		public override void Update(TData data)
		{
			AsyncSaveData
			(
				data,
				(taskData, table) =>
				{
					try
					{
						// Create a retrieve operation that takes a customer entity.
						TableOperation retrieveOperation = GetUpdatableTableEntity(data);

						// Execute the operation.
						TableResult retrievedResult = table.Execute(retrieveOperation);
						TableEntity tableEntity = (TableEntity)retrievedResult.Result;
						var eventTableEntity = tableEntity as IEventDataTableEntity<TData>;
						if (eventTableEntity != null)
							eventTableEntity.EventData = data;
						else
							((IEntityTableEntity<TData>) tableEntity).Entity = data;

						TableOperation updateOperation = TableOperation.Replace(tableEntity);

						// Execute the update operation.
						return table.Execute(updateOperation);
					}
					catch (Exception exception)
					{
						Logger.LogError("There was an issue updating data in table storage.", exception: exception);
						throw;
					}
				}
			);
		}

		#endregion

		protected abstract TableOperation GetUpdatableTableEntity(TData data);

		/// <summary>
		/// Creates a <see cref="CloudTable"/> with the specified name <paramref name="sourceName"/> if it doesn't already exist.
		/// </summary>
		/// <param name="storageAccount">The storage account to create the <see cref="CloudTable"/> is</param>
		/// <param name="sourceName">The name of the <see cref="CloudTable"/>.</param>
		/// <param name="isPublic">Whether or not this <see cref="CloudTable"/> is publicly accessible.</param>
		protected override CloudTable CreateSource(CloudStorageAccount storageAccount, string sourceName, bool isPublic = true)
		{
			// Create the table client.
			CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

			// Retrieve a reference to the table.
			CloudTable table = tableClient.GetTableReference(GetSafeSourceName(sourceName));

			// Create the table if it doesn't exist.
			table.CreateIfNotExists();

			return table;
		}

		public interface IEntityTableEntity<TEntity>
		{
			TEntity Entity { get; set; }
		}

		public class EntityTableEntity<TEntity>
			: TableEntity
			, IEntityTableEntity<TEntity>
			where TEntity : IEntity
		{
			public EntityTableEntity(TEntity entity)
			{
				PartitionKey = entity.GetType().FullName;
				RowKey = entity.Rsn.ToString("N");
			}

			public EntityTableEntity()
			{
			}

			public TEntity Entity { get; set; }
		}

		public interface IEventDataTableEntity<TEventData>
		{
			TEventData EventData { get; set; }
		}

		public class EventDataTableEntity<TEventData>
			: TableEntity
			, IEventDataTableEntity<TEventData>
			where TEventData : EventData
		{
			public EventDataTableEntity(TEventData eventData, bool isCorrelationIdTableStorageStore = false)
			{
				PartitionKey = isCorrelationIdTableStorageStore ? eventData.CorrelationId.ToString("N") : eventData.AggregateId;
				RowKey = eventData.EventId.ToString("N");
			}

			public EventDataTableEntity()
			{
			}

			public TEventData EventData { get; set; }
		}
	}
}