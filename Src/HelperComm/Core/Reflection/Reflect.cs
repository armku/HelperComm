using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>反射工具类</summary>
    public static class Reflect
    {
        #region 静态
        /// <summary>当前反射提供者</summary>
        public static IReflect Provider { get; set; }
        #endregion

        #region 反射获取
        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        public static MethodInfo GetMethodEx(this Type type, String name, params Type[] paramTypes)
        {
            if (name.IsNullOrEmpty()) return null;

            // 如果其中一个类型参数为空，得用别的办法
            if (paramTypes.Length > 0 && paramTypes.Any(e => e == null)) return Provider.GetMethods(type, name, paramTypes.Length).FirstOrDefault();

            return Provider.GetMethod(type, name, paramTypes);
        }

        /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
        /// <returns></returns>
        public static MethodInfo[] GetMethodsEx(this Type type, String name, Int32 paramCount = -1)
        {
            if (name.IsNullOrEmpty()) return null;

            return Provider.GetMethods(type, name, paramCount);
        }
        /// <summary>获取成员。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static MemberInfo GetMemberEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return Provider.GetMember(type, name, ignoreCase);
        }

        /// <summary>获取用于序列化的属性</summary>
        /// <remarks>过滤<seealso cref="T:XmlIgnoreAttribute"/>特性的属性和索引器</remarks>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        public static IList<PropertyInfo> GetProperties(this Type type, Boolean baseFirst)
        {
            return Provider.GetProperties(type, baseFirst);
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object CreateInstance(this Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");

            return Provider.CreateInstance(type, parameters);
        }
        /// <summary>获取目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object GetValue(this Object target, MemberInfo member)
        {
            if (member is PropertyInfo)
                return Provider.GetValue(target, member as PropertyInfo);
            else if (member is FieldInfo)
                return Provider.GetValue(target, member as FieldInfo);
            else
                throw new ArgumentOutOfRangeException("member");
        }
        /// <summary>设置目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="value">数值</param>
        [DebuggerHidden]
        public static void SetValue(this Object target, MemberInfo member, Object value)
        {
            if (member is PropertyInfo)
                Provider.SetValue(target, member as PropertyInfo, value);
            else if (member is FieldInfo)
                Provider.SetValue(target, member as FieldInfo, value);
            else
                throw new ArgumentOutOfRangeException("member");
        }
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Type GetElementTypeEx(this Type type) { return Provider.GetElementType(type); }

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static Object ChangeType(this Object value, Type conversionType) { return Provider.ChangeType(value, conversionType); }

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        public static String GetName(this Type type, Boolean isfull = false) { return Provider.GetName(type, isfull); }

        /// <summary>获取类型代码</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeCode GetTypeCode(this Type type)
        {
            return Type.GetTypeCode(type);
        }
        #endregion

        #region 插件
        /// <summary>是否能够转为指定基类</summary>
        /// <param name="type"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Boolean As(this Type type, Type baseType)
        {
            return Provider.As(type, baseType);
        }

        /// <summary>是否能够转为指定基类</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean As<T>(this Type type)
        {
            return Provider.As(type, typeof(T));
        }
        #endregion
    }
}