using System;

namespace Vp.DynamicProxy
{
    public class ProxyWrapper<T>
    {
        public T ProxyObject { get; set; }
        public Action PreAction { get; internal set; }
        public Action PostAction { get; internal set; }
    }
}