using System;

namespace Vp.DynamicProxy
{
    internal interface IProxyTypeGenerator
    {
        Type GenerateProxyType<TProxy>() where TProxy: class;
    }
}