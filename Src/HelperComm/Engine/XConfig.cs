using System;
using System.ComponentModel;
using System.Text;
using NewLife.Xml;

namespace XCoder
{
    [XmlConfigFile("Config\\XCoder.config")]
    public class XConfig : XmlConfig<XConfig>
    {
        #region 属性
        /// <summary>标题</summary>        

        /// <summary>扩展数据</summary>
        [Description("扩展数据")]
        public String Extend { get; set; } = "";

        /// <summary>日志着色</summary>
        [Description("日志着色")]
        public Boolean ColorLog { get; set; } = true;
                
        #endregion

        #region 加载/保存
        public XConfig()
        {
        }

        protected override void OnLoaded()
        {            
            base.OnLoaded();
        }
        #endregion
    }
}