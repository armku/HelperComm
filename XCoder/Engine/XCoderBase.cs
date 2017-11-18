﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XCode.DataAccessLayer;
using XTemplate.Templating;

namespace XCoder
{
    /// <summary>代码生成器模版基类</summary>
    public class XCoderBase : TemplateBase
    {
        #region 属性
        private IDataTable _Table;
        /// <summary>表架构</summary>
        public virtual IDataTable Table { get { return _Table; } set { _Table = value; } }

        private ModelConfig _Config;
        /// <summary>配置</summary>
        public ModelConfig Config { get { return _Config; } set { _Config = value; } }

        private List<IDataTable> _Tables;
        /// <summary>表集合</summary>
        public List<IDataTable> Tables { get { return _Tables; } set { _Tables = value; } }
        #endregion

        #region 扩展属性
        /// <summary>文件版本</summary>
        public static String Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
        #endregion

        #region 重载
        /// <summary>初始化</summary>
        public override void Initialize()
        {
            base.Initialize();

            if (Data.ContainsKey("Table")) Table = (IDataTable)Data["Table"];
            if (Data.ContainsKey("Config")) Config = (ModelConfig)Data["Config"];
            if (Data.ContainsKey("Tables")) Tables = (List<IDataTable>)Data["Tables"];
        }
        #endregion

        #region 辅助方法
        /// <summary>根据指定名称查找表</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IDataTable FindTable(String tableName)
        {
            //return Tables.Find(t => t.TableName.EqualIC(tableName) || t.Name.EqualIC(tableName));
            return Tables.Find(t => tableName.EqualIgnoreCase(t.TableName, t.Name));
        }

        /// <summary>判断是否存在指定列</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public Boolean HasColumn(String name)
        {
            return Table.Columns.Any(dc => dc.Name.EqualIgnoreCase(name));
        }
        #endregion
    }
}