﻿using System;
using System.Threading;
using NewLife.Security;

namespace NewLife.Agent
{
    /// <summary>代理服务例子。自定义服务程序可参照该类实现。</summary>
    public class AgentService : AgentServiceBase<AgentService>
    {
        #region 属性
        #endregion

        #region 构造函数
        /// <summary>实例化一个代理服务</summary>
        public AgentService()
        {
            // 一般在构造函数里面指定服务名
            ServiceName = "XAgent";

            ThreadCount = 2;
            DisplayName = "新生命服务代理";
            Description = "用于承载各种服务的服务代理！";
        }
        #endregion

        #region 核心
        /// <summary>核心工作方法。调度线程会定期调用该方法</summary>
        /// <param name="index">线程序号</param>
        /// <returns>是否立即开始下一步工作。某些任务能达到满负荷，线程可以不做等待</returns>
        public override Boolean Work(Int32 index)
        {
            // XAgent将开启ThreadCount个线程，0<index<ThreadCount，本函数即为每个任务线程的主函数，间隔Interval循环调用
            var ms = Rand.Next(3000, 20000);
            WriteLog("任务{0}，当前时间：{1} 睡眠：{2:n0}ms", index, DateTime.Now, ms);
            Thread.Sleep(ms);

            return false;
        }
        #endregion
    }

    /// <summary>计划任务例子</summary>
    public class AgentJob : JobBase
    {
        /// <summary>执行任务</summary>
        /// <param name="context"></param>
        public override void Execute(JobContext context)
        {
            var ms = Rand.Next(3000, 20000);
            WriteLog("任务{0}，当前时间：{1} 睡眠：{2:n0}ms", context, DateTime.Now, ms);
            Thread.Sleep(ms);
        }
    }
}