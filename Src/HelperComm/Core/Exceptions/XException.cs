using System;

namespace NewLife
{
    /// <summary>X组件异常</summary>
    [Serializable]
    public class XException : Exception
    {
        /// <summary>初始化</summary>
        /// <param name="message"></param>
        public XException(String message) : base(message) { }

        /// <summary>初始化</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XException(String format, params Object[] args) : base(format.F(args)) { }
    }

}