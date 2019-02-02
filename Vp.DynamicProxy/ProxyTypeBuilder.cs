using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Vp.DynamicProxy
{
    /// <inheritdoc cref="IProxyTypeBuilder{TProxy}"/>
    internal class ProxyTypeBuilder<TProxy> : IProxyTypeBuilder<TProxy> 
        where TProxy : class 
    {
        private readonly TypeBuilder _typeBuilder;

        public ProxyTypeBuilder(TypeBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder ?? throw new ArgumentNullException(nameof(typeBuilder));
        }

        public void ImplementConstructor()
        {
            var constructorBuilder = _typeBuilder.DefineConstructor(
                MethodAttributes.Public, 
                CallingConventions.HasThis,
                new []{typeof(TProxy)});
            
            var wrapperType = InfoProvider.GetWrapperType<TProxy>();
            
            var constructor = wrapperType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance, 
                null, new Type[0], null );

            var setter = wrapperType.GetProperty("ProxyObject").GetSetMethod();
            
            var ILGen = constructorBuilder.GetILGenerator();
            ILGen.Emit(OpCodes.Ldarg_0); // load this on stack
            ILGen.Emit(OpCodes.Call, constructor); // base()
            ILGen.Emit(OpCodes.Ldarg_0); // load this on stack again
            ILGen.Emit(OpCodes.Ldarg_1); // load proxyObject on stack
            ILGen.Emit(OpCodes.Call, setter); // ProxyObject_set(proxyObject)
            ILGen.Emit(OpCodes.Ret);
        }

        public void ImplementMethod(MethodInfo proxyMethod)
        {
            var methodName = proxyMethod.Name;
            var returnType = proxyMethod.ReturnType;
            var attributes = proxyMethod.Attributes;
            var paramsTypes = proxyMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            
            var methodBuilder = _typeBuilder.DefineMethod(
                methodName, 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, 
                proxyMethod.CallingConvention, 
                returnType, 
                paramsTypes);
            
            var wrapperType = InfoProvider.GetWrapperType<TProxy>();
            var getter = wrapperType.GetProperty("ProxyObject").GetGetMethod();
            var ILGen = methodBuilder.GetILGenerator();
            var codeGen = new CodeGenerator(ILGen);
           
            var proxyObject = codeGen.DeclareLocalVariable(typeof(ProxyWrapper<TProxy>));
            var proxyMethodLabelStart = codeGen.DefineLabel();

            GeneratePreActionInvocation(codeGen, proxyMethodLabelStart);
            codeGen.MarkLabel(proxyMethodLabelStart);
            codeGen.GetProperty(methodGetter:getter, storeVariable:proxyObject);
            codeGen.InvokeMethodOnObject(proxyObject, proxyMethod);
            codeGen.Return();
            
            _typeBuilder.DefineMethodOverride(methodBuilder, proxyMethod);
        }

        private void GeneratePreActionInvocation(CodeGenerator codeGen, Label exitLabel)
        {
            var wrapperType = InfoProvider.GetWrapperType<TProxy>();
            var preActionGetter = wrapperType.GetProperty("PreAction").GetGetMethod();
            var preActionVariable = codeGen.DeclareLocalVariable(typeof(Action));
            
            // try block start
            codeGen.BeginTry();
                var preActionNoLabel = codeGen.DefineLabel();
                codeGen.GetProperty(methodGetter: preActionGetter, storeVariable: preActionVariable);
                codeGen.StartIfNullBlock(variable: preActionVariable, goIfNull: preActionNoLabel);
                    codeGen.InvokeMethodAsVariable(preActionVariable);
                codeGen.MarkEndIfBlock(preActionNoLabel);
                codeGen.GoTo(exitLabel); // exit
            
            // start catch 
            codeGen.EndTry();
            codeGen.BeginCatch();
                // codeGen.ReThrowException();
            codeGen.EndCatch();
        }
    }
}