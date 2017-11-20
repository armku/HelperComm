using System.Collections.Generic;
using System.Text;
//using NewLife.IO;
#if !__MOBILE__
using System.IO.Compression;
#endif

namespace System.IO
{
    /// <summary>路径操作帮助</summary>
    public static class PathHelper
    {
        #region 属性
        /// <summary>基础目录。GetFullPath依赖于此，默认为当前应用程序域基础目录</summary>
#if __CORE__
        public static String BaseDirectory { get; set; } = AppContext.BaseDirectory;
#else
        public static String BaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
#endif
        #endregion

        #region 路径操作辅助
        private static String GetPath(String path, Int32 mode)
        {
            // 处理路径分隔符，兼容Windows和Linux
            var sep = Path.DirectorySeparatorChar;
            var sep2 = sep == '/' ? '\\' : '/';
            path = path.Replace(sep2, sep);

            var dir = "";
            switch (mode)
            {
                case 1:
                    dir = BaseDirectory;
                    break;
                case 2:
#if __CORE__
                    dir = AppContext.BaseDirectory;
#else
                    dir = AppDomain.CurrentDomain.BaseDirectory;
#endif
                    break;
#if !__CORE__
                case 3:
                    dir = Environment.CurrentDirectory;
                    break;
#endif
                default:
                    break;
            }
            if (dir.IsNullOrEmpty()) return Path.GetFullPath(path);

            // 处理网络路径
            if (path.StartsWith(@"\\")) return Path.GetFullPath(path);

            // 考虑兼容Linux
            if (!NewLife.Runtime.Mono)
            {
                //if (!Path.IsPathRooted(path))
                //!!! 注意：不能直接依赖于Path.IsPathRooted判断，/和\开头的路径虽然是绝对路径，但是它们不是驱动器级别的绝对路径
                if (path[0] == sep || path[0] == sep2 || !Path.IsPathRooted(path))
                {
                    path = path.TrimStart('~');

                    path = path.TrimStart(sep);
                    path = Path.Combine(dir, path);
                }
            }
            else
            {
                if (!path.StartsWith(dir))
                {
                    // path目录存在，不用再次拼接
                    if (!Directory.Exists(path))
                    {
                        path = path.TrimStart(sep);
                        path = Path.Combine(dir, path);
                    }
                }
            }

            return Path.GetFullPath(path);
        }

        /// <summary>获取文件或目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetFullPath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            return GetPath(path, 1);
        }

        /// <summary>获取文件或目录基于应用程序域基目录的全路径，过滤相对目录</summary>
        /// <remarks>不确保目录后面一定有分隔符，是否有分隔符由原始路径末尾决定</remarks>
        /// <param name="path">文件或目录</param>
        /// <returns></returns>
        public static String GetBasePath(this String path)
        {
            if (String.IsNullOrEmpty(path)) return path;

            return GetPath(path, 2);
        }
        /// <summary>确保目录存在，若不存在则创建</summary>
        /// <remarks>
        /// 斜杠结尾的路径一定是目录，无视第二参数；
        /// 默认是文件，这样子只需要确保上一层目录存在即可，否则如果把文件当成了目录，目录的创建会导致文件无法创建。
        /// </remarks>
        /// <param name="path">文件路径或目录路径，斜杠结尾的路径一定是目录，无视第二参数</param>
        /// <param name="isfile">该路径是否是否文件路径。文件路径需要取目录部分</param>
        /// <returns></returns>
        public static String EnsureDirectory(this String path, Boolean isfile = true)
        {
            if (String.IsNullOrEmpty(path)) return path;

            path = path.GetFullPath();
            if (File.Exists(path) || Directory.Exists(path)) return path;

            var dir = path;
            // 斜杠结尾的路径一定是目录，无视第二参数
            if (dir[dir.Length - 1] == Path.DirectorySeparatorChar)
                dir = Path.GetDirectoryName(path);
            else if (isfile)
                dir = Path.GetDirectoryName(path);

            /*!!! 基础类库的用法应该有明确的用途，而不是通过某些小伎俩去让人猜测 !!!*/

            //// 如果有圆点说明可能是文件
            //var p1 = dir.LastIndexOf('.');
            //if (p1 >= 0)
            //{
            //    // 要么没有斜杠，要么圆点必须在最后一个斜杠后面
            //    var p2 = dir.LastIndexOf('\\');
            //    if (p2 < 0 || p2 < p1) dir = Path.GetDirectoryName(path);
            //}

            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            return path;
        }

        /// <summary>合并多段路径</summary>
        /// <param name="path"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static String CombinePath(this String path, params String[] ps)
        {
            if (ps == null || ps.Length < 1) return path;
            if (path == null) path = String.Empty;

            //return Path.Combine(path, path2);
            foreach (var item in ps)
            {
                if (!item.IsNullOrEmpty()) path = Path.Combine(path, item);
            }
            return path;
        }
        #endregion
    }
}