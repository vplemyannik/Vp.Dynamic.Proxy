using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace Vp.DynamicProxy
{
    internal class ProxyTypeGenerator : IProxyTypeGenerator
    {
        public Type GenerateProxyType<TProxy>() where TProxy: class
        {
            try
            {
                var assemblyBuilder = DefineRuntimeAssembly();
                var moduleBuilder = DefineAssemblyModule(assemblyBuilder);
                var typeBuilder = DefineRuntimeProxyType<TProxy>(moduleBuilder);
                var newType = BuildType<TProxy>(typeBuilder);
                return newType;
            }
            catch (TypeLoadException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

        }
        
        Type BuildType<TProxy>(TypeBuilder typeBuilder) where TProxy: class
        {
         
            var proxyType = typeof(TProxy);
            
            IProxyTypeBuilder<TProxy> proxyTypeBuilder = new ProxyTypeBuilder<TProxy>(typeBuilder);
            
            proxyTypeBuilder.ImplementConstructor();
            
            var proxyMethods = proxyType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var proxyMethod in proxyMethods)
            {
                proxyTypeBuilder.ImplementMethod(proxyMethod);
            }

            return typeBuilder.CreateType();
        }

        TypeBuilder DefineRuntimeProxyType<TProxy>(ModuleBuilder moduleBuilder)
        {
            var proxyType = typeof(TProxy);
            var wrapperType = typeof(ProxyWrapper<TProxy>);
            var typeName = $"Vp.{wrapperType.Name}";
            
            var typeBuilder = moduleBuilder
                .DefineType(typeName, 
                    TypeAttributes.Class | TypeAttributes.Public, 
                    wrapperType, new []{ proxyType });
            
            return typeBuilder;
        }

        ModuleBuilder DefineAssemblyModule(AssemblyBuilder assemblyBuilder)
        {
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(
                $"Vp.Module.{assemblyBuilder.GetName()}.dll");
            return moduleBuilder;
        }

        AssemblyBuilder DefineRuntimeAssembly()
        {
            var name = $"Vp.Assembly.{Guid.NewGuid().ToString().Replace("-", "")}";
            var assemblyName = new AssemblyName(name);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            return assemblyBuilder;
        }
    }
}