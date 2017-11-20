﻿using System;

namespace NewLife.Net
{
    /// <summary>统计接口。
    /// <see cref="Increment"/>后更新<see cref="First"/>、<see cref="Last"/>、<see cref="Total"/>，
    /// 但并不会马上更新统计数据，除非<see cref="Enable"/>为true。</summary>
    /// <example>
    /// <code>
    /// private IStatistics _Statistics;
    /// /// &lt;summary&gt;统计信息，默认关闭，通过&lt;see cref="IStatistics.Enable"/&gt;打开。&lt;/summary&gt;
    /// public IStatistics Statistics { get { return _Statistics ?? (_Statistics = NetService.Resolve&lt;IStatistics&gt;()); } }
    /// </code>
    /// </example>
    public interface IStatistics
    {
        /// <summary>是否启用统计。</summary>
        Boolean Enable { get; set; }

        /// <summary>统计周期，单位秒</summary>
        Int32 Period { get; set; }

        /// <summary>首次统计时间</summary>
        DateTime First { get; }

        /// <summary>最后统计时间</summary>
        DateTime Last { get; }

        /// <summary>总数</summary>
        Int32 Total { get; }

        /// <summary>次数</summary>
        Int32 Times { get; }

        /// <summary>周期速度</summary>
        Int32 Speed { get; }

        /// <summary>周期最大值</summary>
        Int32 Max { get; }

        /// <summary>父级统计</summary>
        IStatistics Parent { get; set; }

        /// <summary>增加计数</summary>
        /// <param name="n"></param>
        void Increment(Int32 n = 1);
    }
}