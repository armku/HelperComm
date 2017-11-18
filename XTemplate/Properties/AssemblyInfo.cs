﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("新生命模版引擎")]
[assembly: AssemblyDescription("编译型模版引擎")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("XTemplate")]
[assembly: AssemblyCompany("新生命开发团队")]
[assembly: AssemblyCopyright("©2002-2017 新生命开发团队 http://www.NewLifeX.com")]
[assembly: AssemblyTrademark("四叶草")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("7285c38e-a761-4a76-96e4-c9f89867ab14")]

// 程序集的版本信息由下面四个值组成:
//
//      主版本
//      次版本 
//      内部版本号
//      修订号
//
// 可以指定所有这些值，也可以使用“内部版本号”和“修订号”的默认值，
// 方法是按如下所示使用“*”:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.0.*")]
[assembly: AssemblyFileVersion("2.0.2016.0728")]

/*
 * v2.0.2016.0728   模版运行出错时，准确提示错误所在模版位置
 * 
 * v1.9.2013.0901   增加模版运行时调试功能，原有编译时调试
 * 
 * v1.8.2012.0113   修正类名处理的BUG
 * 
 * v1.8.2011.1124   调整模版的编译，不再输出临时文件，改为输出模版生成的代码
 * 
 * v1.8.2011.1120   增加模版执行错误异常TemplateExecutionException，所有Render产生的异常都经过其包装
 *                  修正处理指令的一个错误
 * 
 * v1.7.2011.0904   include指令包含其它模版文件时，如果没有全路径，则采用当前模版的相对路径
 * 
 * v1.6.2011.0831   改善编译错误提示，增加提示错误行及上下行共三行源代码
 * 
 * v1.5.2011.0603   增加var指令，允许指定创建封装自Data的模版变量：<#@var name="pname" type="String"#>，然后<#=pname#>等同于<#=Data["pname"]#>
 *                  template指令，增加name属性，允许指定模版名称
 * 
 * v1.4.2011.0316   Template中原来的静态方法Process改为ProcessFile，另外根据需要增加常用的静态方法ProcessContent
 *                  核心类Template改为私有构造，统一由带缓存的静态Create创建实例，避免分析及编译模版带来的性能损耗
 * 
 * v1.3.2011.0314   修正放置XTemp目录不正确的错误
 * 
 * v1.3.2010.1014   取消模版优化处理，保证模版的输出原汁原味，模版编辑人员更加容易理解
 * 
 * v1.2.2010.1009   重整XTemplate结构，批量处理模版，编译到同一个程序集去。
 * 
 * v1.1.2010.0927   使用C#作为模版语言，支持自定义模版基类，支持模版包含，支持类成员定义，支持编译语法检查
 * 
 * v1.0.2010.0609   创建
 *
**/
