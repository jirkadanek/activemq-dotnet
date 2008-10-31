/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;

namespace Apache.NMS.WCF
{
	// ItemDequeuedCallback is called as an item is dequeued from the InputQueue.  The 
	// InputQueue lock is not held during the callback.  However, the user code is
	// not notified of the item being available until the callback returns.  If you
	// are not sure if the callback blocks for a long time, then first call 
	// IOThreadScheduler.ScheduleCallback to get to a "safe" thread.
	internal delegate void ItemDequeuedCallback();

	/// <summary>
	/// Handles asynchronous interactions between producers and consumers. 
	/// Producers can dispatch available data to the input queue, 
	/// where it is dispatched to a waiting consumer or stored until a
	/// consumer becomes available. Consumers can synchronously or asynchronously
	/// request data from the queue, which is returned when data becomes
	/// available.
	/// </summary>
	/// <typeparam name="T">The concrete type of the consumer objects that are waiting for data.</typeparam>
	internal class InputQueue<T> : IDisposable where T : class
	{
		//Stores items that are waiting to be accessed.
		ItemQueue itemQueue;

		//Each IQueueReader represents some consumer that is waiting for
		//items to appear in the queue. The readerQueue stores them
		//in an ordered list so consumers get serviced in a FIFO manner.
		Queue<IQueueReader> readerQueue;

		//Each IQueueWaiter represents some waiter that is waiting for
		//items to appear in the queue.  When any item appears, all
		//waiters are signaled.
		List<IQueueWaiter> waiterList;

		static WaitCallback onInvokeDequeuedCallback;
		static WaitCallback onDispatchCallback;
		static WaitCallback completeOutstandingReadersCallback;
		static WaitCallback completeWaitersFalseCallback;
		static WaitCallback completeWaitersTrueCallback;

		//Represents the current state of the InputQueue.
		//as it transitions through its lifecycle.
		QueueState _queueState;
		enum QueueState
		{
			Open,
			Shutdown,
			Closed
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;"/> class.
		/// </summary>
		public InputQueue()
		{
			itemQueue = new ItemQueue();
			readerQueue = new Queue<IQueueReader>();
			waiterList = new List<IQueueWaiter>();
			_queueState = QueueState.Open;
		}

		public int PendingCount
		{
			get
			{
				lock (ThisLock)
				{
					return itemQueue.ItemCount;
				}
			}
		}

		object ThisLock
		{
			get { return itemQueue; }
		}

		public IAsyncResult BeginDequeue(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Item item = default(Item);

			lock (ThisLock)
			{
				if (_queueState == QueueState.Open)
				{
					if (itemQueue.HasAvailableItem)
					{
						item = itemQueue.DequeueAvailableItem();
					}
					else
					{
						AsyncQueueReader reader = new AsyncQueueReader(this, timeout, callback, state);
						readerQueue.Enqueue(reader);
						return reader;
					}
				}
				else if (_queueState == QueueState.Shutdown)
				{
					if (itemQueue.HasAvailableItem)
					{
						item = itemQueue.DequeueAvailableItem();
					}
					else if (itemQueue.HasAnyItem)
					{
						AsyncQueueReader reader = new AsyncQueueReader(this, timeout, callback, state);
						readerQueue.Enqueue(reader);
						return reader;
					}
				}
			}

			InvokeDequeuedCallback(item.DequeuedCallback);
			return new TypedCompletedAsyncResult<T>(item.GetValue(), callback, state);
		}

		public IAsyncResult BeginWaitForItem(TimeSpan timeout, AsyncCallback callback, object state)
		{
			lock (ThisLock)
			{
				if (_queueState == QueueState.Open)
				{
					if (!itemQueue.HasAvailableItem)
					{
						AsyncQueueWaiter waiter = new AsyncQueueWaiter(timeout, callback, state);
						waiterList.Add(waiter);
						return waiter;
					}
				}
				else if (_queueState == QueueState.Shutdown)
				{
					if (!itemQueue.HasAvailableItem && itemQueue.HasAnyItem)
					{
						AsyncQueueWaiter waiter = new AsyncQueueWaiter(timeout, callback, state);
						waiterList.Add(waiter);
						return waiter;
					}
				}
			}

			return new TypedCompletedAsyncResult<bool>(true, callback, state);
		}

		static void CompleteOutstandingReadersCallback(object state)
		{
			IQueueReader[] outstandingReaders = (IQueueReader[])state;

			for (int i = 0; i < outstandingReaders.Length; i++)
			{
				outstandingReaders[i].Set(default(Item));
			}
		}

		static void CompleteWaitersFalseCallback(object state)
		{
			CompleteWaiters(false, (IQueueWaiter[])state);
		}

		static void CompleteWaitersTrueCallback(object state)
		{
			CompleteWaiters(true, (IQueueWaiter[])state);
		}

		static void CompleteWaiters(bool itemAvailable, IQueueWaiter[] waiters)
		{
			for (int i = 0; i < waiters.Length; i++)
			{
				waiters[i].Set(itemAvailable);
			}
		}

		static void CompleteWaitersLater(bool itemAvailable, IQueueWaiter[] waiters)
		{
			if (itemAvailable)
			{
				if (completeWaitersTrueCallback == null)
				{
					completeWaitersTrueCallback = CompleteWaitersTrueCallback;
				}

				ThreadPool.QueueUserWorkItem(completeWaitersTrueCallback, waiters);
			}
			else
			{
				if (completeWaitersFalseCallback == null)
				{
					completeWaitersFalseCallback = CompleteWaitersFalseCallback;
				}

				ThreadPool.QueueUserWorkItem(completeWaitersFalseCallback, waiters);
			}
		}

		void GetWaiters(out IQueueWaiter[] waiters)
		{
			if (waiterList.Count > 0)
			{
				waiters = waiterList.ToArray();
				waiterList.Clear();
			}
			else
			{
				waiters = null;
			}
		}

		public void Close()
		{
			((IDisposable)this).Dispose();
		}

		public void Shutdown()
		{
			IQueueReader[] outstandingReaders = null;
			lock (ThisLock)
			{
				if (_queueState == QueueState.Shutdown)
				{
					return;
				}

				if (_queueState == QueueState.Closed)
				{
					return;
				}

				_queueState = QueueState.Shutdown;

				if (readerQueue.Count > 0 && itemQueue.ItemCount == 0)
				{
					outstandingReaders = new IQueueReader[readerQueue.Count];
					readerQueue.CopyTo(outstandingReaders, 0);
					readerQueue.Clear();
				}
			}

			if (outstandingReaders != null)
			{
				for (int i = 0; i < outstandingReaders.Length; i++)
				{
					outstandingReaders[i].Set(new Item((Exception)null, null));
				}
			}
		}

		public T Dequeue(TimeSpan timeout)
		{
			T value;

			if (!Dequeue(timeout, out value))
			{
				throw new TimeoutException(string.Format("Dequeue timed out in {0}.", timeout));
			}

			return value;
		}

		public bool Dequeue(TimeSpan timeout, out T value)
		{
			WaitQueueReader reader = null;
			Item item = new Item();

			lock (ThisLock)
			{
				if (_queueState == QueueState.Open)
				{
					if (itemQueue.HasAvailableItem)
					{
						item = itemQueue.DequeueAvailableItem();
					}
					else
					{
						reader = new WaitQueueReader(this);
						readerQueue.Enqueue(reader);
					}
				}
				else if (_queueState == QueueState.Shutdown)
				{
					if (itemQueue.HasAvailableItem)
					{
						item = itemQueue.DequeueAvailableItem();
					}
					else if (itemQueue.HasAnyItem)
					{
						reader = new WaitQueueReader(this);
						readerQueue.Enqueue(reader);
					}
					else
					{
						value = default(T);
						return true;
					}
				}
				else // queueState == QueueState.Closed
				{
					value = default(T);
					return true;
				}
			}

			if (reader != null)
			{
				return reader.Wait(timeout, out value);
			}

			InvokeDequeuedCallback(item.DequeuedCallback);
			value = item.GetValue();
			return true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				bool dispose = false;

				lock (ThisLock)
				{
					if (_queueState != QueueState.Closed)
					{
						_queueState = QueueState.Closed;
						dispose = true;
					}
				}

				if (dispose)
				{
					while (readerQueue.Count > 0)
					{
						IQueueReader reader = readerQueue.Dequeue();
						reader.Set(default(Item));
					}

					while (itemQueue.HasAnyItem)
					{
						Item item = itemQueue.DequeueAnyItem();
						item.Dispose();
						InvokeDequeuedCallback(item.DequeuedCallback);
					}
				}
			}
		}

		public void Dispatch()
		{
			IQueueReader reader = null;
			Item item = new Item();
			IQueueReader[] outstandingReaders = null;
			IQueueWaiter[] waiters = null;
			bool itemAvailable = true;

			lock (ThisLock)
			{
				itemAvailable = !((_queueState == QueueState.Closed) || (_queueState == QueueState.Shutdown));
				GetWaiters(out waiters);

				if (_queueState != QueueState.Closed)
				{
					itemQueue.MakePendingItemAvailable();

					if (readerQueue.Count > 0)
					{
						item = itemQueue.DequeueAvailableItem();
						reader = readerQueue.Dequeue();

						if (_queueState == QueueState.Shutdown && readerQueue.Count > 0 && itemQueue.ItemCount == 0)
						{
							outstandingReaders = new IQueueReader[readerQueue.Count];
							readerQueue.CopyTo(outstandingReaders, 0);
							readerQueue.Clear();

							itemAvailable = false;
						}
					}
				}
			}

			if (outstandingReaders != null)
			{
				if (completeOutstandingReadersCallback == null)
					completeOutstandingReadersCallback = CompleteOutstandingReadersCallback;

				ThreadPool.QueueUserWorkItem(completeOutstandingReadersCallback, outstandingReaders);
			}

			if (waiters != null)
			{
				CompleteWaitersLater(itemAvailable, waiters);
			}

			if (reader != null)
			{
				InvokeDequeuedCallback(item.DequeuedCallback);
				reader.Set(item);
			}
		}

		//Ends an asynchronous Dequeue operation.
		public T EndDequeue(IAsyncResult result)
		{
			T value;

			if (!EndDequeue(result, out value))
			{
				throw new TimeoutException("Asynchronous Dequeue operation timed out.");
			}

			return value;
		}

		public bool EndDequeue(IAsyncResult result, out T value)
		{
			TypedCompletedAsyncResult<T> typedResult = result as TypedCompletedAsyncResult<T>;

			if (typedResult != null)
			{
				value = TypedCompletedAsyncResult<T>.End(result);
				return true;
			}

			return AsyncQueueReader.End(result, out value);
		}

		public bool EndWaitForItem(IAsyncResult result)
		{
			TypedCompletedAsyncResult<bool> typedResult = result as TypedCompletedAsyncResult<bool>;
			if (typedResult != null)
			{
				return TypedCompletedAsyncResult<bool>.End(result);
			}

			return AsyncQueueWaiter.End(result);
		}

		public void EnqueueAndDispatch(T item)
		{
			EnqueueAndDispatch(item, null);
		}

		public void EnqueueAndDispatch(T item, ItemDequeuedCallback dequeuedCallback)
		{
			EnqueueAndDispatch(item, dequeuedCallback, true);
		}

		public void EnqueueAndDispatch(Exception exception, ItemDequeuedCallback dequeuedCallback, bool canDispatchOnThisThread)
		{
			Debug.Assert(exception != null, "exception parameter should not be null");
			EnqueueAndDispatch(new Item(exception, dequeuedCallback), canDispatchOnThisThread);
		}

		public void EnqueueAndDispatch(T item, ItemDequeuedCallback dequeuedCallback, bool canDispatchOnThisThread)
		{
			Debug.Assert(item != null, "item parameter should not be null");
			EnqueueAndDispatch(new Item(item, dequeuedCallback), canDispatchOnThisThread);
		}

		void EnqueueAndDispatch(Item item, bool canDispatchOnThisThread)
		{
			bool disposeItem = false;
			IQueueReader reader = null;
			bool dispatchLater = false;
			IQueueWaiter[] waiters = null;
			bool itemAvailable = true;

			lock (ThisLock)
			{
				itemAvailable = !((_queueState == QueueState.Closed) || (_queueState == QueueState.Shutdown));
				GetWaiters(out waiters);

				if (_queueState == QueueState.Open)
				{
					if (canDispatchOnThisThread)
					{
						if (readerQueue.Count == 0)
						{
							itemQueue.EnqueueAvailableItem(item);
						}
						else
						{
							reader = readerQueue.Dequeue();
						}
					}
					else
					{
						if (readerQueue.Count == 0)
						{
							itemQueue.EnqueueAvailableItem(item);
						}
						else
						{
							itemQueue.EnqueuePendingItem(item);
							dispatchLater = true;
						}
					}
				}
				else // queueState == QueueState.Closed || queueState == QueueState.Shutdown
				{
					disposeItem = true;
				}
			}

			if (waiters != null)
			{
				if (canDispatchOnThisThread)
				{
					CompleteWaiters(itemAvailable, waiters);
				}
				else
				{
					CompleteWaitersLater(itemAvailable, waiters);
				}
			}

			if (reader != null)
			{
				InvokeDequeuedCallback(item.DequeuedCallback);
				reader.Set(item);
			}

			if (dispatchLater)
			{
				if (onDispatchCallback == null)
				{
					onDispatchCallback = OnDispatchCallback;
				}

				ThreadPool.QueueUserWorkItem(onDispatchCallback, this);
			}
			else if (disposeItem)
			{
				InvokeDequeuedCallback(item.DequeuedCallback);
				item.Dispose();
			}
		}

		public bool EnqueueWithoutDispatch(T item, ItemDequeuedCallback dequeuedCallback)
		{
			Debug.Assert(item != null, "EnqueueWithoutDispatch: item parameter should not be null");
			return EnqueueWithoutDispatch(new Item(item, dequeuedCallback));
		}

		public bool EnqueueWithoutDispatch(Exception exception, ItemDequeuedCallback dequeuedCallback)
		{
			Debug.Assert(exception != null, "EnqueueWithoutDispatch: exception parameter should not be null");
			return EnqueueWithoutDispatch(new Item(exception, dequeuedCallback));
		}

		// This does not block, however, Dispatch() must be called later if this function
		// returns true.
		bool EnqueueWithoutDispatch(Item item)
		{
			lock (ThisLock)
			{
				// Open
				if (_queueState != QueueState.Closed && _queueState != QueueState.Shutdown)
				{
					if (readerQueue.Count == 0)
					{
						itemQueue.EnqueueAvailableItem(item);
						return false;
					}
					itemQueue.EnqueuePendingItem(item);
					return true;
				}
			}

			item.Dispose();
			InvokeDequeuedCallbackLater(item.DequeuedCallback);
			return false;
		}

		static void OnDispatchCallback(object state)
		{
			((InputQueue<T>)state).Dispatch();
		}

		static void InvokeDequeuedCallbackLater(ItemDequeuedCallback dequeuedCallback)
		{
			if (dequeuedCallback != null)
			{
				if (onInvokeDequeuedCallback == null)
				{
					onInvokeDequeuedCallback = OnInvokeDequeuedCallback;
				}

				ThreadPool.QueueUserWorkItem(onInvokeDequeuedCallback, dequeuedCallback);
			}
		}

		static void InvokeDequeuedCallback(ItemDequeuedCallback dequeuedCallback)
		{
			if (dequeuedCallback != null)
			{
				dequeuedCallback();
			}
		}

		static void OnInvokeDequeuedCallback(object state)
		{
			ItemDequeuedCallback dequeuedCallback = (ItemDequeuedCallback)state;
			dequeuedCallback();
		}

		bool RemoveReader(IQueueReader reader)
		{
			lock (ThisLock)
			{
				if (_queueState == QueueState.Open || _queueState == QueueState.Shutdown)
				{
					bool removed = false;

					for (int i = readerQueue.Count; i > 0; i--)
					{
						IQueueReader temp = readerQueue.Dequeue();
						if (ReferenceEquals(temp, reader))
						{
							removed = true;
						}
						else
						{
							readerQueue.Enqueue(temp);
						}
					}

					return removed;
				}
			}

			return false;
		}

		public bool WaitForItem(TimeSpan timeout)
		{
			WaitQueueWaiter waiter = null;
			bool itemAvailable = false;

			lock (ThisLock)
			{
				if (_queueState == QueueState.Open)
				{
					if (itemQueue.HasAvailableItem)
					{
						itemAvailable = true;
					}
					else
					{
						waiter = new WaitQueueWaiter();
						waiterList.Add(waiter);
					}
				}
				else if (_queueState == QueueState.Shutdown)
				{
					if (itemQueue.HasAvailableItem)
					{
						itemAvailable = true;
					}
					else if (itemQueue.HasAnyItem)
					{
						waiter = new WaitQueueWaiter();
						waiterList.Add(waiter);
					}
					else
					{
						return false;
					}
				}
				else // queueState == QueueState.Closed
				{
					return true;
				}
			}

			return waiter != null ? waiter.Wait(timeout) : itemAvailable;
		}

		interface IQueueReader
		{
			void Set(Item item);
		}

		interface IQueueWaiter
		{
			void Set(bool itemAvailable);
		}

		class WaitQueueReader : IQueueReader
		{
			Exception _exception;
			InputQueue<T> _inputQueue;
			T _item;
			ManualResetEvent _waitEvent;
			object _thisLock = new object();

			public WaitQueueReader(InputQueue<T> inputQueue)
			{
				_inputQueue = inputQueue;
				_waitEvent = new ManualResetEvent(false);
			}

			object ThisLock
			{
				get
				{
					return _thisLock;
				}
			}

			public void Set(Item item)
			{
				lock (ThisLock)
				{
					Debug.Assert(_item == null, "InputQueue.WaitQueueReader.Set: (this.item == null)");
					Debug.Assert(_exception == null, "InputQueue.WaitQueueReader.Set: (this.exception == null)");

					_exception = item.Exception;
					_item = item.Value;
					_waitEvent.Set();
				}
			}

			public bool Wait(TimeSpan timeout, out T value)
			{
				bool isSafeToClose = false;
				try
				{
					if (timeout == TimeSpan.MaxValue)
					{
						_waitEvent.WaitOne();
					}
					else if (!_waitEvent.WaitOne(timeout, false))
					{
						if (_inputQueue.RemoveReader(this))
						{
							value = default(T);
							isSafeToClose = true;
							return false;
						}
						else
						{
							_waitEvent.WaitOne();
						}
					}

					isSafeToClose = true;
				}
				finally
				{
					if (isSafeToClose)
					{
						_waitEvent.Close();
					}
				}

				value = _item;
				return true;
			}
		}

		class AsyncQueueReader : AsyncResult, IQueueReader
		{
			static TimerCallback timerCallback = new TimerCallback(AsyncQueueReader.TimerCallback);

			bool _expired;
			InputQueue<T> _inputQueue;
			T _item;
			Timer _timer;

			public AsyncQueueReader(InputQueue<T> inputQueue, TimeSpan timeout, AsyncCallback callback, object state)
				: base(callback, state)
			{
				_inputQueue = inputQueue;
				if (timeout != TimeSpan.MaxValue)
				{
					_timer = new Timer(timerCallback, this, timeout, TimeSpan.FromMilliseconds(-1));
				}
			}

			public static bool End(IAsyncResult result, out T value)
			{
				AsyncQueueReader readerResult = AsyncResult.End<AsyncQueueReader>(result);

				if (readerResult._expired)
				{
					value = default(T);
					return false;
				}

				value = readerResult._item;
				return true;
			}

			static void TimerCallback(object state)
			{
				AsyncQueueReader thisPtr = (AsyncQueueReader)state;
				if (thisPtr._inputQueue.RemoveReader(thisPtr))
				{
					thisPtr._expired = true;
					thisPtr.Complete(false);
				}
			}

			public void Set(Item item)
			{
				_item = item.Value;
				if (_timer != null)
				{
					_timer.Change(-1, -1);
				}
				Complete(false, item.Exception);
			}
		}

		internal struct Item
		{
			private T _value;
			private Exception _exception;
			ItemDequeuedCallback _dequeuedCallback;

			/// <summary>
			/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;.Item"/> class.
			/// </summary>
			/// <param name="value">The value.</param>
			/// <param name="dequeuedCallback">The dequeued callback.</param>
			public Item(T value, ItemDequeuedCallback dequeuedCallback) : this(value, null, dequeuedCallback)
			{
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;.Item"/> class.
			/// </summary>
			/// <param name="exception">The exception.</param>
			/// <param name="dequeuedCallback">The dequeued callback.</param>
			public Item(Exception exception, ItemDequeuedCallback dequeuedCallback) : this(null, exception, dequeuedCallback)
			{
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;.Item"/> class.
			/// </summary>
			/// <param name="value">The value.</param>
			/// <param name="exception">The exception.</param>
			/// <param name="dequeuedCallback">The dequeued callback.</param>
			internal Item(T value, Exception exception, ItemDequeuedCallback dequeuedCallback) 
			{
				_value = value;
				_exception = exception;
				_dequeuedCallback = dequeuedCallback;
			}

			/// <summary>
			/// Gets the exception.
			/// </summary>
			/// <value>The exception.</value>
			public Exception Exception
			{
				get { return _exception; }
			}

			/// <summary>
			/// Gets the value.
			/// </summary>
			/// <value>The value.</value>
			public T Value
			{
				get { return _value; }
			}

			/// <summary>
			/// Gets the dequeued callback.
			/// </summary>
			/// <value>The dequeued callback.</value>
			public ItemDequeuedCallback DequeuedCallback
			{
				get { return _dequeuedCallback; }
			}

			/// <summary>
			/// Releases unmanaged and - optionally - managed resources
			/// </summary>
			public void Dispose()
			{
				if (_value != null)
				{
					if (_value is IDisposable)
					{
						((IDisposable)_value).Dispose();
					}
					else if (_value is ICommunicationObject)
					{
						((ICommunicationObject)_value).Abort();
					}
				}
			}

			/// <summary>
			/// Gets the value.
			/// </summary>
			/// <returns></returns>
			public T GetValue()
			{
				if (_exception != null)
				{
					throw _exception;
				}

				return _value;
			}
		}

		internal class WaitQueueWaiter : IQueueWaiter
		{
			bool _itemAvailable;
			ManualResetEvent _waitEvent;
			object _thisLock = new object();

			/// <summary>
			/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;.WaitQueueWaiter"/> class.
			/// </summary>
			public WaitQueueWaiter()
			{
				_waitEvent = new ManualResetEvent(false);
			}

			/// <summary>
			/// Gets the this lock.
			/// </summary>
			/// <value>The this lock.</value>
			object ThisLock
			{
				get
				{
					return _thisLock;
				}
			}

			/// <summary>
			/// Sets the specified item available.
			/// </summary>
			/// <param name="itemAvailable">if set to <see langword="true"/> [item available].</param>
			public void Set(bool itemAvailable)
			{
				lock (ThisLock)
				{
					_itemAvailable = itemAvailable;
					_waitEvent.Set();
				}
			}

			/// <summary>
			/// Waits the specified timeout.
			/// </summary>
			/// <param name="timeout">The timeout.</param>
			/// <returns></returns>
			public bool Wait(TimeSpan timeout)
			{
				if (timeout == TimeSpan.MaxValue)
				{
					_waitEvent.WaitOne();
				}
				else if (!_waitEvent.WaitOne(timeout, false))
				{
					return false;
				}

				return _itemAvailable;
			}
		}

		internal class AsyncQueueWaiter : AsyncResult, IQueueWaiter
		{
			static TimerCallback timerCallback = new TimerCallback(AsyncQueueWaiter.TimerCallback);
			Timer _timer;
			bool _itemAvailable;
			object _thisLock = new object();

			/// <summary>
			/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;.AsyncQueueWaiter"/> class.
			/// </summary>
			/// <param name="timeout">The timeout.</param>
			/// <param name="callback">The callback.</param>
			/// <param name="state">The state.</param>
			public AsyncQueueWaiter(TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				if (timeout != TimeSpan.MaxValue)
				{
					_timer = new Timer(timerCallback, this, timeout, TimeSpan.FromMilliseconds(-1));
				}
			}

			/// <summary>
			/// Gets the this lock.
			/// </summary>
			/// <value>The this lock.</value>
			object ThisLock
			{
				get
				{
					return _thisLock;
				}
			}

			/// <summary>
			/// Ends the specified result.
			/// </summary>
			/// <param name="result">The result.</param>
			/// <returns></returns>
			public static bool End(IAsyncResult result)
			{
				AsyncQueueWaiter waiterResult = AsyncResult.End<AsyncQueueWaiter>(result);
				return waiterResult._itemAvailable;
			}

			/// <summary>
			/// Callback that is invoked when the timer completes.
			/// </summary>
			/// <param name="state">The state.</param>
			public static void TimerCallback(object state)
			{
				AsyncQueueWaiter thisPtr = (AsyncQueueWaiter)state;
				thisPtr.Complete(false);
			}

			/// <summary>
			/// Sets the specified item available.
			/// </summary>
			/// <param name="itemAvailable">if set to <see langword="true"/> [item available].</param>
			public void Set(bool itemAvailable)
			{
				bool timely;

				lock (ThisLock)
				{
					timely = (_timer == null) || _timer.Change(-1, -1);
					_itemAvailable = itemAvailable;
				}

				if (timely)
				{
					Complete(false);
				}
			}
		}

		internal class ItemQueue
		{
			Item[] _items;
			int _head;
			int _pendingCount;
			int _totalCount;

			/// <summary>
			/// Initializes a new instance of the <see cref="InputQueue&lt;T&gt;.ItemQueue"/> class.
			/// </summary>
			public ItemQueue()
			{
				_items = new Item[1];
			}

			/// <summary>
			/// Dequeues the available item.
			/// </summary>
			/// <returns></returns>
			public Item DequeueAvailableItem()
			{
				if (_totalCount == _pendingCount)
				{
					throw new Exception("Internal Error - ItemQueue does not contain any available items");
				}
				return DequeueItemCore();
			}

			/// <summary>
			/// Dequeues any item.
			/// </summary>
			/// <returns></returns>
			public Item DequeueAnyItem()
			{
				if (_pendingCount == _totalCount)
				{
					_pendingCount--;
				}
				return DequeueItemCore();
			}

			/// <summary>
			/// Enqueues the item core.
			/// </summary>
			/// <param name="item">The item.</param>
			void EnqueueItemCore(Item item)
			{
				if (_totalCount == _items.Length)
				{
					Item[] newItems = new Item[_items.Length * 2];
					for (int i = 0; i < _totalCount; i++)
					{
						newItems[i] = _items[(_head + i) % _items.Length];
					}
					_head = 0;
					_items = newItems;
				}
				int tail = (_head + _totalCount) % _items.Length;
				_items[tail] = item;
				_totalCount++;
			}

			/// <summary>
			/// Dequeues the item core.
			/// </summary>
			/// <returns></returns>
			Item DequeueItemCore()
			{
				if (_totalCount == 0)
				{
					throw new Exception("Internal Error - ItemQueue does not contain any items");
				}
				Item item = _items[_head];
				_items[_head] = new Item();
				_totalCount--;
				_head = (_head + 1) % _items.Length;
				return item;
			}

			/// <summary>
			/// Enqueues the pending item.
			/// </summary>
			/// <param name="item">The item.</param>
			public void EnqueuePendingItem(Item item)
			{
				EnqueueItemCore(item);
				_pendingCount++;
			}

			/// <summary>
			/// Enqueues the available item.
			/// </summary>
			/// <param name="item">The item.</param>
			public void EnqueueAvailableItem(Item item)
			{
				EnqueueItemCore(item);
			}

			/// <summary>
			/// Makes the pending item available.
			/// </summary>
			public void MakePendingItemAvailable()
			{
				if (_pendingCount == 0)
				{
					throw new Exception("Internal Error - ItemQueue does not contain any pending items");
				}
				_pendingCount--;
			}

			/// <summary>
			/// Gets a value indicating whether this instance has available items.
			/// </summary>
			/// <value>
			/// 	<see langword="true"/> if this instance has available item; otherwise, <see langword="false"/>.
			/// </value>
			public bool HasAvailableItem
			{
				get { return _totalCount > _pendingCount; }
			}

			/// <summary>
			/// Gets a value indicating whether this instance has any item.
			/// </summary>
			/// <value>
			/// 	<see langword="true"/> if this instance has any item; otherwise, <see langword="false"/>.
			/// </value>
			public bool HasAnyItem
			{
				get { return _totalCount > 0; }
			}

			public int ItemCount
			{
				get { return _totalCount; }
			}
		}
	}
}