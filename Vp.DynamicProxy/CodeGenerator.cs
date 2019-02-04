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
        public void BeginIfNullBlock(LocalBuilder variable, Label goIfNull)
        {
            var nullConditionVariable = _ILGen.DeclareLocal(typeof(bool));
            _ILGen.Emit(OpCodes.Ldloc, variable); // load object on the stack
            _ILGen.Emit(OpCodes.Ldnull); // load null 
            _ILGen.Emit(OpCodes.Cgt_Un); // if(object != null)  
            _ILGen.Emit(OpCodes.Stloc, nullConditionVariable); // save condition result into variable
            _ILGen.Emit(OpCodes.Ldloc, nullConditionVariable); // load condition result on stack
            _ILGen.Emit(OpCodes.Brfalse_S, goIfNull);// if false (object = null) -> Goto 
        }

        /// <summary>
        /// Invokes method on instance 
        /// </summary>
        /// <param name="method"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnThis(MethodInfo method)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this
            PrepareMethodParams(method);
            _ILGen.Emit(OpCodes.Call, method); // this.{method}.Invoke()
        }
        /// <summary>
        /// Invokes method on instance and save result into <see cref="resultVariable"/>
        /// </summary>
        /// <param name="method"></param>
        /// <param name="resultVariable"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnThisWithResult(MethodInfo method, LocalBuilder resultVariable)
        {
            InvokeOnThis(method);
            _ILGen.Emit(OpCodes.Stloc, resultVariable); // this.{resultVariable} = {result}
        }

        /// <summary>
        /// Loads invocation params of method on stack
        /// </summary>
        /// <param name="method"></param>
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

        /// <summary>
        /// invokes <see cref="methodGetter"/> and save value into <see cref="storeVariable"/> 
        /// </summary>
        /// <param name="methodGetter"></param>
        /// <param name="storeVariable"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetProperty(MethodInfo methodGetter, LocalBuilder storeVariable)
        {
            InvokeOnThisWithResult(methodGetter, storeVariable);
        }

        /// <summary>
        /// Invokes <see cref="methodSetter"/> to save variable from parameters of ctor
        /// </summary>
        /// <code>
        ///   class MyClass
        ///   {
        ///        public int Param1 { get; }
        /// 
        ///        public MyClass(int param1)
        ///        {
        ///             Param1 = param1;
        ///        }
        ///   }
        /// 
        /// </code>
        /// <param name="methodSetter"></param>
        /// <param name="index"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPropertyCtor(MethodInfo methodSetter, int index)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this on stack
            _ILGen.Emit(OpCodes.Ldarg, index); // load param by index on the stack
            _ILGen.Emit(OpCodes.Call, methodSetter); // save param with setter
        }

        /// <summary>
        /// Invokes base ctor 
        /// </summary>
        /// <param name="baseConstructor"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeBaseCtor(ConstructorInfo baseConstructor)
        {
            _ILGen.Emit(OpCodes.Ldarg_0); // load this on stack
            _ILGen.Emit(OpCodes.Call, baseConstructor); // base()
        }

        /// <summary>
        /// when variable has type <see cref="Action"/>
        /// </summary>
        /// <param name="method"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeMethodAsVariable(LocalBuilder method)
        {
            var invokeMethod = InfoProvider.GetInvokeMethod();
            _ILGen.Emit(OpCodes.Ldloc, method); // load method on Stack
            _ILGen.Emit(OpCodes.Callvirt, invokeMethod); // method.Invoke()
        }

        /// <summary>
        /// Invokes method on object
        /// </summary>
        /// <code>
        ///  object.Method(param1, param2)
        /// </code>
        /// <param name="object"></param>
        /// <param name="method"></param>
        public void InvokeMethodOnObject(LocalBuilder @object, MethodInfo method)
        {
            _ILGen.Emit(OpCodes.Ldloc, @object); // load object on stack 
            PrepareMethodParams(method); // load method params
            _ILGen.EmitCall(OpCodes.Callvirt, method, null); // invoke
        }

        /// <summary>
        /// Mark point of code execution 
        /// </summary>
        /// <param name="label"></param>
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
            
#if DEBUG
            var messageGetter = typeof(Exception).GetProperty("Message").GetGetMethod();
            var writeLine = typeof(Debug).GetMethod("Write", new[] {typeof(string)});
#endif
            
            var exceptionVariable = _ILGen.DeclareLocal(typeof(Exception));
            _ILGen.BeginCatchBlock(typeof(Exception));
            _ILGen.Emit(OpCodes.Stloc, exceptionVariable); // store exception into variable 
            _ILGen.Emit(OpCodes.Ldloc, exceptionVariable); // load exception on stack
            
#if DEBUG
            _ILGen.EmitCall(OpCodes.Callvirt, messageGetter, null);
            _ILGen.EmitCall(OpCodes.Call, writeLine, null);   // write exception in debug log
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndCatch()
        {
            _ILGen.EndExceptionBlock();
        }
        
        /// <summary>
        /// Marks end point of if block
        /// </summary>
        /// <param name="label"></param>
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