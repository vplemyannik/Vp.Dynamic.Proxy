using System;

namespace Vp.DynamicProxy
{
    public class DynamicProxyBuilder<T> where T: class
    {
        private readonly IProxyTypeGenerator _proxyTypeGenerator;
        private readonly ProxyWrapper<T> _proxyWrapper;

        public DynamicProxyBuilder()
        {
            _proxyWrapper = new ProxyWrapper<T>();
            _proxyTypeGenerator = new ProxyTypeGenerator();
        }
        
        public DynamicProxyBuilder<T> AddPreAction(Action preAction)
        {
            _proxyWrapper.PreAction = preAction;
            return this;
        }

        public DynamicProxyBuilder<T> AddPostAction(Action postAction)
        {
            _proxyWrapper.PostAction = postAction;
            return this;
        }
        
        public T Build(T proxyObject)
        {
            var newType = _proxyTypeGenerator.GenerateProxyType<T>();
            var wrapper = Activator.CreateInstance(newType, proxyObject) as ProxyWrapper<T>;
            wrapper.PreAction = _proxyWrapper.PreAction;
            wrapper.PostAction = _proxyWrapper.PostAction;
            return wrapper as T ;
        }
    }
}