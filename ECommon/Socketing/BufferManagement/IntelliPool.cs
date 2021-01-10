using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ECommon.Socketing.BufferManagement
{
    struct PoolItemState
    {
        public byte Generation { get; set; }
    }

    public class IntelliPool<T> : IntelliPoolBase<T>
    {
        private ConcurrentDictionary<T, PoolItemState> _bufferDict = new ConcurrentDictionary<T, PoolItemState>();
        private ConcurrentDictionary<T, T> _removedItemDict;

        public IntelliPool(int initialCount, IPoolItemCreator<T> itemCreator, Action<T> itemCleaner = null, Action<T> itemPreGet = null)
            : base(initialCount, itemCreator, itemCleaner, itemPreGet)
        {

        }

        public override bool Shrink()
        {
            var generation = CurrentGeneration;

            if (!base.Shrink())
                return false;

            var toBeRemoved = new List<T>(TotalCount / 2);

            foreach (var item in _bufferDict)
            {
                if (item.Value.Generation == generation)
                {
                    toBeRemoved.Add(item.Key);
                }
            }

            if (_removedItemDict == null)
                _removedItemDict = new ConcurrentDictionary<T, T>();

            foreach (var item in toBeRemoved)
            {
                PoolItemState state;
                if (_bufferDict.TryRemove(item, out state))
                    _removedItemDict.TryAdd(item, item);
            }

            return true;
        }

        protected override bool CanReturn(T item)
        {
            return _bufferDict.ContainsKey(item);
        }

        protected override void RegisterNewItem(T item)
        {
            PoolItemState state = new PoolItemState();
            state.Generation = CurrentGeneration;
            _bufferDict.TryAdd(item, state);
        }

        protected override bool TryRemove(T item)
        {
            if (_removedItemDict == null || _removedItemDict.Count == 0)
                return false;

            T removedItem;
            return _removedItemDict.TryRemove(item, out removedItem);
        }
    }

    public abstract class IntelliPoolBase<T> : IPool<T>
    {
        private ConcurrentStack<T> _store;
        private IPoolItemCreator<T> _itemCreator;
        private byte _currentGeneration = 0;
        private int _nextExpandThreshold;
        private int _totalCount;
        private int _availableCount;
        private int _inExpanding = 0;
        private Action<T> _itemCleaner;
        private Action<T> _itemPreGet;

        protected byte CurrentGeneration
        {
            get { return _currentGeneration; }
        }

        public int TotalCount {
            get { return _totalCount; }
        }

        public int AvailableCount
        {
            get { return _availableCount; }
        }

        public IntelliPoolBase(int initialCount,IPoolItemCreator<T> itemCreator, Action<T> itemCleaner = null, Action<T> itemPreGet = null)
        {
            _itemCreator = itemCreator;
            _itemCleaner = itemCleaner;
            _itemPreGet = itemPreGet;

            var list = new List<T>(initialCount);

            foreach (var item in itemCreator.Create(initialCount))
            {
                RegisterNewItem(item);
                list.Add(item);
            }

            _store = new ConcurrentStack<T>(list);

            _totalCount = initialCount;
            _availableCount = _totalCount;
            UpdateNextExpandThreshold();
        }

        protected abstract void RegisterNewItem(T item);

        public T Get()
        {
            T item;

            if(_store.TryPop(out item))
            {
                //Interlocked.Increment 方法：让++成为原子操作；Interlocked.Decrement 方法让--成为原子操作。
                //什么叫原子操作呢。就是不会被别人打断，因为C#中的一个语句，编译成机器代码后会变成多个语句。
                //在多线程环境中，线程切换有可能会发生在这多个语句中间。使用Interlocked.Increment,Interlocked.Decrement 可以避免被打断, 保证线程安全。
                Interlocked.Decrement(ref _availableCount);

                var itemPreGet = _itemPreGet;

                if (itemPreGet != null)
                    itemPreGet(item);

                return item;
            }

            if (_inExpanding == 1)
            {
                var spinWait = new SpinWait();

                while (true)
                {
                    spinWait.SpinOnce();

                    if (_store.TryPop(out item))
                    {
                        Interlocked.Decrement(ref _availableCount);

                        var itemPreGet = _itemPreGet;

                        if (itemPreGet != null)
                            itemPreGet(item);

                        return item;
                    }

                    if (_inExpanding != 1)
                        return Get();
                }
            }
            else
            {
                TryExpand();
                return Get();
            }
        }

        bool TryExpand()
        {
            if (Interlocked.CompareExchange(ref _inExpanding, 1, 0) != 0)
                return false;

            Expand();
            _inExpanding = 0;
            return true;
        }

        void Expand()
        {
            var totalCount = _totalCount;

            foreach (var item in _itemCreator.Create(totalCount))
            {
                _store.Push(item);
                Interlocked.Increment(ref _availableCount);
                RegisterNewItem(item);
            }

            _currentGeneration++;

            _totalCount += totalCount;
            UpdateNextExpandThreshold();
        }

        public void Return(T item)
        {
            var itemCleaner = _itemCleaner;
            if (itemCleaner != null)
                itemCleaner(item);

            if (CanReturn(item))
            {
                _store.Push(item);
                Interlocked.Increment(ref _availableCount);
                return;
            }

            if (TryRemove(item))
                Interlocked.Decrement(ref _totalCount);
        }

        protected abstract bool CanReturn(T item);

        protected abstract bool TryRemove(T item);

        public virtual bool Shrink()
        {
            var generation = _currentGeneration;
            if (generation == 0)
                return false;

            var shrinThreshold = _totalCount * 3 / 4;

            if (_availableCount <= shrinThreshold)
                return false;

            _currentGeneration = (byte)(generation - 1);
            return true;
        }

        private void UpdateNextExpandThreshold()
        {
            _nextExpandThreshold = _totalCount / 5; //if only 20% buffer left, we can expand the buffer count
        }
    }
}
