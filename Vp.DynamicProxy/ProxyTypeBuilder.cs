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
            
            var baseConstructor = wrapperType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance, 
                null, new Type[0], null );

            var setter = wrapperType.GetProperty("ProxyObject").GetSetMethod();

            var ILGen = constructorBuilder.GetILGenerator();
            var codeGen = new CodeGenerator(ILGen);
            codeGen.InitializeBaseCtor(baseConstructor);
            codeGen.SetPropertyCtor(setter, 1);
            codeGen.Return();
        }

        public void ImplementMethod(MethodInfo proxyMethod)
        {
            var methodName = proxyMethod.Name;
            var returnType = proxyMethod.ReturnType;
            var paramsTypes = proxyMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            
            var methodBuilder = _typeBuilder.DefineMethod(
                methodName, 
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, 
                proxyMethod.CallingConvention, 
                returnType, 
                paramsTypes);


            GenerateMethod(methodBuilder, proxyMethod);
            
            _typeBuilder.DefineMethodOverride(methodBuilder, proxyMethod);
        }

        private void GenerateMethod(MethodBuilder methodBuilder, MethodInfo proxyMethod)
        {
            var ILGen = methodBuilder.GetILGenerator();
            var codeGen = new CodeGenerator(ILGen);
           
            var proxyMethodLabelStart = codeGen.DefineLabel();
            var finallyEnd = codeGen.DefineLabel();

            GenerateAdditionalMethodInvocation(codeGen, proxyMethodLabelStart, "PreAction"); // PreAction();
            GenerateProxyMethodInvocation(codeGen, proxyMethod, proxyMethodLabelStart); // ProxyMethod();
            GenerateAdditionalMethodInvocation(codeGen, finallyEnd, "PostAction");
            codeGen.MarkLabel(finallyEnd);
            codeGen.Return();
        }
        private void GenerateAdditionalMethodInvocation(CodeGenerator codeGen, Label exitLabel, string propsName)
        {
            var wrapperType = InfoProvider.GetWrapperType<TProxy>();
            var preActionGetter = wrapperType.GetProperty(propsName).GetGetMethod();
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

        private void GenerateProxyMethodInvocation(CodeGenerator codeGen, MethodInfo proxyMethod, Label proxyMethodLabelStart)
        {
            var wrapperType = InfoProvider.GetWrapperType<TProxy>();
            var getter = wrapperType.GetProperty("ProxyObject").GetGetMethod();
            
            var proxyObject = codeGen.DeclareLocalVariable(typeof(ProxyWrapper<TProxy>));
            
            codeGen.MarkLabel(proxyMethodLabelStart);
            codeGen.GetProperty(methodGetter:getter, storeVariable:proxyObject);
            codeGen.InvokeMethodOnObject(proxyObject, proxyMethod);
        }
    }
}