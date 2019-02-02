using System;
using System.Reflection;

namespace Vp.DynamicProxy
{
    public static class InfoProvider
    {
        public static Type GetWrapperType<TProxy>() => typeof(ProxyWrapper<TProxy>);

        public static MethodInfo GetInvokeMethod() => typeof(Action).GetMethod("Invoke");
    }
}