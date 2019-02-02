using System.Reflection;

namespace Vp.DynamicProxy
{
    /// <summary>
    /// Represents a class which builds type at runtime
    /// that implements <see cref="TProxy"/>
    /// </summary>
    /// <typeparam name="TProxy"></typeparam>
    public interface IProxyTypeBuilder<TProxy>
    {
        /// <summary>
        /// Defines and implements ctor 
        /// </summary>
        /// <code>
        ///  public RanTime_TypeWrapper(TProxy proxyObject)
        ///  {
        ///     ProxyObject = proxyObject;
        ///  }
        /// </code>
        void ImplementConstructor();
        /// <summary>
        /// Implements virtual method of <see cref="TProxy"/> 
        /// </summary>
        /// <param name="proxyMethod"></param>
        void ImplementMethod(MethodInfo proxyMethod);
    }
}