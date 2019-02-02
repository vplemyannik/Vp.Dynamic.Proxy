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

            var preActionVariable = ILGen.DeclareLocal(typeof(Action));
            var nullConditionVariable = ILGen.DeclareLocal(typeof(bool));
            var exceptionVariable = ILGen.DeclareLocal(typeof(Exception));

            
            var proxyMethodLabelStart = ILGen.DefineLabel();

            GeneratePreActionInvocation(ILGen, proxyMethodLabelStart);
            ILGen.MarkLabel(proxyMethodLabelStart);
            ILGen.Emit(OpCodes.Ldarg_0); // load this
            ILGen.Emit(OpCodes.Call, getter); // this.ProxyObject_get()

            var methodParams = proxyMethod.GetParameters();
            int pidx = 1;
            foreach (var parameterInfo in methodParams)
            {
                ILGen.Emit(OpCodes.Ldarg, pidx++);
            }
            
            // TODO Implement logic for type param method
            
            ILGen.EmitCall(OpCodes.Callvirt, proxyMethod, null); // _proxyObject.{proxyMethod}.Invoke()
            ILGen.Emit(OpCodes.Ret);
            
            _typeBuilder.DefineMethodOverride(methodBuilder, proxyMethod);
        }

        public void GeneratePreActionInvocation(ILGenerator ILGen, Label exitLabel)
        {
            var wrapperType = InfoProvider.GetWrapperType<TProxy>();
            var preActionProp = wrapperType.GetProperty("PreAction");
            var preActionGetter = preActionProp.GetGetMethod();
            var invokeMethod = InfoProvider.GetInvokeMethod();
            var messageGetter = typeof(Exception).GetProperty("Message").GetGetMethod();
            var writeLine = typeof(Console).GetMethod("WriteLine", new[] {typeof(string)});
            
            var codeGen = new CodeGenerator(ILGen);
            
            // try block start
            var tryBlockLabel = ILGen.BeginExceptionBlock();
            var preActionNoLabel = ILGen.DefineLabel();
                        
            ILGen.Emit(OpCodes.Ldarg_0); // load this
            ILGen.Emit(OpCodes.Call, preActionGetter); // this.get_PreAction()
            ILGen.Emit(OpCodes.Stloc_0); // var preAction = PreAction
            ILGen.Emit(OpCodes.Ldloc_0); // load preAction on the stack
            ILGen.Emit(OpCodes.Ldnull); // load null 
            ILGen.Emit(OpCodes.Cgt_Un); // if(preAction != null)  
            ILGen.Emit(OpCodes.Stloc_1); // save condition result into variable
            ILGen.Emit(OpCodes.Ldloc_1); // load condition result in stack
            ILGen.Emit(OpCodes.Brfalse_S, preActionNoLabel);// if false (preAction = null) -> Goto preActionNoLabel
            ILGen.Emit(OpCodes.Ldarg_0); // else 
            ILGen.Emit(OpCodes.Call, preActionGetter); // load preAction on Stack
            ILGen.Emit(OpCodes.Callvirt, invokeMethod); // PreAction()
            ILGen.MarkLabel(preActionNoLabel);
            ILGen.Emit(OpCodes.Leave_S, exitLabel); // exit
            
            // start catch 
            ILGen.BeginCatchBlock(typeof(Exception));
            ILGen.Emit(OpCodes.Stloc_2); // store exception into variable 
            ILGen.Emit(OpCodes.Ldloc_2); // load exception on stack
           
            ILGen.EmitCall(OpCodes.Callvirt, messageGetter, null);
            ILGen.EmitCall(OpCodes.Call, writeLine, null);
            
             // ILGen.Emit(OpCodes.Rethrow);
             
             ILGen.EndExceptionBlock();
        }
    }
}