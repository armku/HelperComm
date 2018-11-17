﻿using NewLife.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Collections
{
    /// <summary>字典缓存。当指定键的缓存项不存在时，调用委托获取值，并写入缓存。</summary>
    /// <remarks>常用匿名函数或者Lambda表达式作为委托。</remarks>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class DictionaryCache<TKey, TValue> : IDisposable
    {
        #region 属性
        /// <summary>过期时间。单位是秒，默认0秒，表示永不过期</summary>
        public Int32 Expire { get; set; }

        /// <summary>过期清理时间，缓存项过期后达到这个时间时，将被移除缓存。单位是秒，默认0秒，表示不清理过期项</summary>
        public Int32 ClearPeriod { get; set; }

        /// <summary>异步更新。默认false</summary>
        public Boolean Asynchronous { get; set; }

        /// <summary>是否缓存默认值，有时候委托返回默认值不希望被缓存，而是下一次尽快进行再次计算。默认true</summary>
        public Boolean CacheDefault { get; set; } = true;

        private Dictionary<TKey, CacheItem> Items;
        #endregion

        #region 构造
        /// <summary>实例化一个字典缓存</summary>
        public DictionaryCache()
        {
            Items = new Dictionary<TKey, CacheItem>();
        }

        /// <summary>实例化一个字典缓存</summary>
        /// <param name="comparer"></param>
        public DictionaryCache(IEqualityComparer<TKey> comparer)
        {
            Items = new Dictionary<TKey, CacheItem>(comparer);
        }
        
        /// <summary>销毁字典，关闭</summary>
        public void Dispose()
        {
            lock (Items)
            {
                Items.Clear();
            }
            StopTimer();
        }
        #endregion

        #region 缓存项
        /// <summary>缓存项</summary>
        class CacheItem
        {
            /// <summary>数值</summary>
            public TValue Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired { get { return ExpiredTime <= TimerX.Now; } }

            public CacheItem(TValue value, Int32 seconds)
            {
                Value = value;
                if (seconds > 0) ExpiredTime = TimerX.Now.AddSeconds(seconds);
            }
        }
        #endregion

        #region 核心取值方法
        
        /// <summary>扩展获取数据项，当数据项不存在时，通过调用委托获取数据项。线程安全。</summary>
        /// <param name="key">键</param>
        /// <param name="func">获取值的委托，该委托以键作为参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public virtual TValue GetItem(TKey key, Func<TKey, TValue> func)
        {
            var exp = Expire;
            var items = Items;
            if (items.TryGetValue(key, out var item) && (exp <= 0 || !item.Expired)) return item.Value;

            // 提前计算，避免因为不同的Key错误锁定了主键
            var value = default(TValue);

            lock (items)
            {
                if (items.TryGetValue(key, out item) && (exp <= 0 || !item.Expired)) return item.Value;

                // 对于缓存命中，仅是缓存过期的项，如果采用异步，则马上修改缓存时间，让后面的来访者直接采用已过期的缓存项
                if (exp > 0 && Asynchronous)
                {
                    if (item != null)
                    {
                        item.ExpiredTime = TimerX.Now.AddSeconds(exp);
                        // 异步更新缓存
                        if (func != null) Task.Run(() => { item.Value = func(key); });

                        return item.Value;
                    }
                }

                if (func == null)
                {
                    if (CacheDefault) items[key] = new CacheItem(value, exp);
                }
                else
                {
                    value = func(key);

                    if (CacheDefault || !Object.Equals(value, default(TValue))) items[key] = new CacheItem(value, exp);
                }
                StartTimer();

                return value;
            }
        }
        #endregion
        
        #region 清理过期缓存
        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        void StartTimer()
        {
            var period = ClearPeriod;
            // 缓存数大于0才启动定时器
            if (period <= 0 || Items.Count < 1) return;

            if (clearTimer == null) clearTimer = new TimerX(RemoveNotAlive, null, period * 1000, period * 1000);
        }

        void StopTimer()
        {
            clearTimer = null;
        }

        /// <summary>移除过期的缓存项</summary>
        void RemoveNotAlive(Object state)
        {
            var expriod = ClearPeriod;
            if (expriod <= 0) return;

            var dic = Items;
            if (dic.Count < 1)
            {
                // 缓存数小于0时关闭定时器
                StopTimer();
                return;
            }
            lock (dic)
            {
                if (dic.Count < 1)
                {
                    StopTimer();
                    return;
                }

                // 这里先计算，性能很重要
                var now = TimerX.Now;
                //var exp = now.AddSeconds(-1 * expriod);
                foreach (var item in dic.ToArray())
                {
                    var t = item.Value.ExpiredTime;
                    if (t < now)
                    {
                        dic.Remove(item.Key);
                    }
                }
            }
        }
        #endregion
    }
}