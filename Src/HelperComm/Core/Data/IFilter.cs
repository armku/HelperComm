using System;

namespace NewLife.Data
{
    /// <summary>数据过滤器</summary>
    public interface IFilter
    {
        /// <summary>下一个过滤器</summary>
        IFilter Next { get; }

        /// <summary>对封包执行过滤器</summary>
        /// <param name="context"></param>
        void Execute(FilterContext context);
    }

    /// <summary>过滤器上下文</summary>
    public class FilterContext
    {
        /// <summary>封包</summary>
        public virtual Packet Packet { get; set; }
    }    
}