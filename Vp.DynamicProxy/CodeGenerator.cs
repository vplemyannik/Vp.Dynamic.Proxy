using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Vp.DynamicProxy
{
    public class CodeGenerator
    {
        private readonly ILGenerator _ILGen;

        public CodeGenerator(ILGenerator ilGen)
        {
            _ILGen = ilGen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartIfNullBlock(LocalBuilder variable, Label goIfNull)
        {
            var nullConditionVariable = _ILGen.DeclareLocal(typeof(bool));
            _ILGen.Emit(OpCodes.Ldloc, variable); // load object on the stack
            _ILGen.Emit(OpCodes.Ldnull); // load null 
            _ILGen.Emit(OpCodes.Cgt_Un); // if(object != null)  
            _ILGen.Emit(OpCodes.Stloc, nullConditionVariable); // save condition result into variable
            _ILGen.Emit(OpCodes.Ldloc, nullConditionVariable); // load condition result in stack
            _ILGen.Emit(OpCodes.Brfalse_S, goIfNull);// if false (object = null) -> Goto 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnThis(MethodInfo method)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this
            PrepareMethodParams(method);
            _ILGen.Emit(OpCodes.Call, method); // this.get_PreAction()
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnThisWithResult(MethodInfo method, LocalBuilder resultVariable)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this
            PrepareMethodParams(method);
            _ILGen.Emit(OpCodes.Call, method); // this.{method}.Invoke()
            _ILGen.Emit(OpCodes.Stloc, resultVariable); // this.{resultVariable} = {result}
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareMethodParams(MethodInfo method)
        {
            var methodParams = method.GetParameters();
            int pidx = 1;
            foreach (var parameterInfo in methodParams)
            {
                _ILGen.Emit(OpCodes.Ldarg, pidx++);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetProperty(MethodInfo methodGetter, LocalBuilder storeVariable)
        {
            InvokeOnThisWithResult(methodGetter, storeVariable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperty(MethodInfo methodSetter, LocalBuilder variable)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this on stack
            _ILGen.Emit(OpCodes.Ldarg_1); // load proxyObject on stack
            _ILGen.Emit(OpCodes.Call, methodSetter); // ProxyObject_set(proxyObject)
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPropertyCtor(MethodInfo methodSetter, int index)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this on stack
            _ILGen.Emit(OpCodes.Ldarg, index); // load proxyObject on stack
            _ILGen.Emit(OpCodes.Call, methodSetter); // ProxyObject_set(proxyObject)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeBaseCtor(ConstructorInfo baseConstructor)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this on stack
            _ILGen.Emit(OpCodes.Call, baseConstructor); // base()
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeMethodAsVariable(LocalBuilder method)
        {
            var invokeMethod = InfoProvider.GetInvokeMethod();
            _ILGen.Emit(OpCodes.Ldloc, method); // load method on Stack
            _ILGen.Emit(OpCodes.Callvirt, invokeMethod); // method.Invoke()
        }

        public void InvokeMethodOnObject(LocalBuilder @object, MethodInfo method)
        {
            _ILGen.Emit(OpCodes.Ldloc, @object);
            PrepareMethodParams(method);
            _ILGen.EmitCall(OpCodes.Callvirt, method, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeVirtualMethod(MethodInfo method)
        {
            _ILGen.Emit(OpCodes.Callvirt, method);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkLabel(Label label)
        {
            _ILGen.MarkLabel(label);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginTry()
        {
            _ILGen.BeginExceptionBlock();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndTry()
        {
            _ILGen.Emit(OpCodes.Nop);
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginCatch()
        {
            var messageGetter = typeof(Exception).GetProperty("Message").GetGetMethod();
            var writeLine = typeof(Debug).GetMethod("Write", new[] {typeof(string)});
            
            var exceptionVariable = _ILGen.DeclareLocal(typeof(Exception));
            _ILGen.BeginCatchBlock(typeof(Exception));
            _ILGen.Emit(OpCodes.Stloc, exceptionVariable); // store exception into variable 
            _ILGen.Emit(OpCodes.Ldloc, exceptionVariable); // load exception on stack
            
            _ILGen.EmitCall(OpCodes.Callvirt, messageGetter, null);
            _ILGen.EmitCall(OpCodes.Call, writeLine, null);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndCatch()
        {
            _ILGen.EndExceptionBlock();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkEndIfBlock(Label label) => _ILGen.MarkLabel(label);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GoTo(Label label) => _ILGen.Emit(OpCodes.Leave_S, label);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Label DefineLabel() => _ILGen.DefineLabel();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LocalBuilder DeclareLocalVariable(Type type) => _ILGen.DeclareLocal(type);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReThrowException() => _ILGen.Emit(OpCodes.Rethrow);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return() => _ILGen.Emit(OpCodes.Ret);
    }
}