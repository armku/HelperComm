﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NewLife;
using NewLife.Model;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using XTemplate.Templating;

namespace XCoder
{
    /// <summary>代码生成引擎</summary>
    public class Engine
    {
        #region 属性
        public const String TemplatePath = "Template";

        private static Dictionary<String, String> _Templates;
        /// <summary>模版</summary>
        public static Dictionary<String, String> Templates { get { return _Templates ?? (_Templates = Source.GetTemplates()); } }

        private static List<String> _FileTemplates;
        /// <summary>文件模版</summary>
        public static List<String> FileTemplates
        {
            get
            {
                if (_FileTemplates == null)
                {
                    var list = new List<String>();

                    var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplatePath);
                    if (Directory.Exists(dir))
                    {
                        var ds = Directory.GetDirectories(dir);
                        if (ds != null && ds.Length > 0)
                        {
                            foreach (var item in ds)
                            {
                                var di = new DirectoryInfo(item);
                                list.Add(di.Name);
                            }
                        }
                    }
                    _FileTemplates = list.OrderBy(e => e).ToList();
                }
                return _FileTemplates;
            }
        }

        public Engine(ModelConfig config)
        {
            Config = config;
        }

        private ModelConfig _Config;
        /// <summary>配置</summary>
        public ModelConfig Config { get { return _Config; } set { _Config = value; } }

        //private String _LastTableKey;
        //private List<IDataTable> _LastTables;
        private List<IDataTable> _Tables;
        /// <summary>所有表</summary>
        public List<IDataTable> Tables
        {
            get
            {
                //if (!Config.NeedFix) return _Tables;

                //// 不同的前缀、大小写选项，得到的表集合是不一样的。这里用字典来缓存
                //var key = String.Format("{0}_{1}_{2}_{3}_{4}", Config.AutoCutPrefix, Config.AutoCutTableName, Config.AutoFixWord, Config.Prefix, Config.UseID);
                ////return _cache.GetItem(key, k => FixTable(_Tables));
                //if (_LastTableKey != key)
                //{
                //    _LastTables = FixTable(_Tables);
                //    _LastTableKey = key;
                //}
                //return _LastTables;
                return _Tables;
            }
            set { _Tables = value; }
        }

        //private static ITranslate _Translate;
        ///// <summary>翻译接口</summary>
        //static ITranslate Translate { get { return _Translate ?? (_Translate = new NnhyServiceTranslate()); } }
        #endregion

        #region 构造
        static Engine()
        {
            Template.BaseClassName = typeof(XCoderBase).FullName;
        }
        #endregion

        #region 生成
        /// <summary>生成代码，参数由Config传入</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public String[] Render(String tableName)
        {
            var tables = Tables;
            if (tables == null || tables.Count < 1) return null;

            //var table = tables.Find(e => e.Name.EqualIC(tableName) || e.TableName.EqualIC(tableName));
            var table = tables.Find(e => tableName.EqualIgnoreCase(e.Name, e.TableName));
            if (table == null) return null;

            return Render(table);
        }

        public String[] Render(IDataTable table)
        {
            // 检查表格完整性
            foreach (var dc in table.Columns)
            {
                if (dc.DataType == null) throw new ArgumentException("{0}.DataType数据类型错误".F(dc.Name), dc.Name);
            }

            var data = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            //data["Config"] = Config;
            data["Tables"] = Tables;
            data["Table"] = table;

            #region 配置
            // 复制表属性到配置
            var cfg = new ModelConfig();
            foreach (var pi in cfg.GetType().GetProperties(true))
            {
                if (table.Properties.ContainsKey(pi.Name))
                    cfg.SetValue(pi, table.Properties[pi.Name]);
                else
                    cfg.SetValue(pi, Config.GetValue(pi));
            }

            #region 命名空间处理
            var NameSpace = cfg.NameSpace;
            var reg = new Regex(@"\$\((\w+)\)", RegexOptions.Compiled);
            NameSpace = reg.Replace(NameSpace, math =>
            {
                var key = math.Groups[1].Value;
                if (String.IsNullOrEmpty(key)) return null;

                var pix = typeof(IDataTable).GetPropertyEx(key);
                if (pix != null)
                    return (String)table.GetValue(pix);
                else
                    return table.Properties[key];
            });
            NewLife.Log.XTrace.WriteLine("NameSpace" + Config.NameSpace + "@" + NameSpace);
            cfg.NameSpace = NameSpace;
            #endregion

            data["Config"] = cfg;
            #endregion

            #region 模版预处理
            // 声明模版引擎
            //Template tt = new Template();
            Template.Debug = Config.Debug;
            var templates = new Dictionary<String, String>();
            // 每一个模版的编码，用于作为输出文件的编码
            var encs = new List<Encoding>();

            var tempName = Config.TemplateName;
            var tempKind = "";
            var p = tempName.IndexOf(']');
            if (p >= 0)
            {
                tempKind = tempName.Substring(0, p + 1);
                tempName = tempName.Substring(p + 1);
            }
            if (tempKind == "[内置]")
            {
                // 系统模版
                foreach (var item in Templates)
                {
                    var key = item.Key;
                    var name = key.Substring(0, key.IndexOf("."));
                    if (name != tempName) continue;

                    var content = item.Value;

                    // 添加文件头
                    if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && key.EndsWithIgnoreCase(".cs"))
                        content = Config.HeadTemplate + content;

                    templates.Add(key.Substring(name.Length + 1), content);
                    encs.Add(Encoding.UTF8);
                }
            }
            else
            {
                // 文件模版
                //var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TemplatePath);
                //dir = Path.Combine(dir, tempName);
                var dir = TemplatePath.GetFullPath().CombinePath(tempName);
                if (!Directory.Exists(dir)) throw new XException("找不到模版目录{0}，没有可用模版！", dir);

                var ss = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
                if (ss != null && ss.Length > 0)
                {
                    foreach (var item in ss)
                    {
                        if (item.EndsWithIgnoreCase("scc")) continue;

                        var content = File.ReadAllText(item);

                        var name = item.Substring(dir.Length);
                        if (name.StartsWith(@"\")) name = name.Substring(1);

                        // 添加文件头
                        if (Config.UseHeadTemplate && !String.IsNullOrEmpty(Config.HeadTemplate) && name.EndsWithIgnoreCase(".cs"))
                            content = Config.HeadTemplate + content;

                        templates.Add(name, content);
                        encs.Add(GetEncoding(item));
                    }
                }
            }
            if (templates.Count < 1) throw new Exception("没有可用模版！");

            var tt = Template.Create(templates);
            if (tempName.StartsWith("*")) tempName = tempName.Substring(1);
            tt.AssemblyName = tempName;
            #endregion

            #region 输出目录预处理
            var outpath = Config.OutputPath;
            // 使用正则替换处理 命名空间处已经定义
            //var reg = new Regex(@"\$\((\w+)\)", RegexOptions.Compiled);
            outpath = reg.Replace(outpath, math =>
            {
                var key = math.Groups[1].Value;
                if (String.IsNullOrEmpty(key)) return null;

                var pix = typeof(IDataTable).GetPropertyEx(key);
                if (pix != null)
                    return (String)table.GetValue(pix);
                else
                    return table.Properties[key];
            });
            #endregion

            #region 编译生成
            // 编译模版。这里至少要处理，只有经过了处理，才知道模版项是不是被包含的
            tt.Compile();

            var rs = new List<String>();
            var i = -1;
            foreach (var item in tt.Templates)
            {
                i++;
                if (item.Included) continue;

                // 计算输出文件名
                var fileName = Path.GetFileName(item.Name);
                var fname = Config.UseCNFileName ? table.DisplayName : table.Name;
                fname = fname.Replace("/", "_").Replace("\\", "_").Replace("?", null);
                // 如果中文名无效，采用英文名
                if (String.IsNullOrEmpty(Path.GetFileNameWithoutExtension(fname)) || fname[0] == '.') fname = table.Name;
                fileName = fileName.Replace("类名", fname).Replace("中文名", fname).Replace("连接名", Config.EntityConnName);

                fileName = Path.Combine(outpath, fileName);

                // 如果不覆盖，并且目标文件已存在，则跳过
                if (!Config.Override && File.Exists(fileName)) continue;

                var content = tt.Render(item.Name, data);

                var dir = Path.GetDirectoryName(fileName);
                if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                //File.WriteAllText(fileName, content, Encoding.UTF8);
                // 将文件保存为utf-8无bom格式
                //File.WriteAllText(fileName, content, new UTF8Encoding(false));

                // aspx页面如果不是UTF8编码，很有可能出现页面中文乱码，CMX生成的页面文件出现该情况
                // 使用模版文件本身的文件编码来作为输出文件的编码，默认UTF8
                File.WriteAllText(fileName, content, encs[i]);

                rs.Add(content);
            }
            return rs.ToArray();
            #endregion
        }
        #endregion

        #region 辅助
        /// <summary>获取文件编码</summary>
        /// <param name="file"></param>
        /// <returns></returns>
        static Encoding GetEncoding(String file)
        {
            if (String.IsNullOrEmpty(file) || !File.Exists(file)) return Encoding.UTF8;

            using (var reader = new StreamReader(file, Encoding.UTF8, true))
            {
                return reader.CurrentEncoding;
            }
        }
        #endregion
    }
}