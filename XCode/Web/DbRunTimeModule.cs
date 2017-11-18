﻿using System;
using System.Web;
using NewLife.Web;
using XCode.DataAccessLayer;

namespace XCode.Web
{
    /// <summary>页面查询执行时间模块</summary>
    public class DbRunTimeModule : RunTimeModule
    {
        /// <summary>执行时间字符串</summary>
        public static String DbRunTimeFormat { get; set; } = "查询{0}次，执行{1}次，耗时{2:n0}毫秒！";

        /// <summary>初始化模块，准备拦截请求。</summary>
        protected override void OnInit()
        {
            Context.Items["DAL.QueryTimes"] = DAL.QueryTimes;
            Context.Items["DAL.ExecuteTimes"] = DAL.ExecuteTimes;
        }

        /// <summary>输出</summary>
        /// <returns></returns>
        protected override String Render()
        {
            TimeSpan ts = DateTime.Now - HttpContext.Current.Timestamp;

            Int32 StartQueryTimes = (Int32)Context.Items["DAL.QueryTimes"];
            Int32 StartExecuteTimes = (Int32)Context.Items["DAL.ExecuteTimes"];

            return String.Format(DbRunTimeFormat, DAL.QueryTimes - StartQueryTimes, DAL.ExecuteTimes - StartExecuteTimes, ts.TotalMilliseconds);
        }
    }
}