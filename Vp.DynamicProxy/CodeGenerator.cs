using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Vp.DynamicProxy
{
    public class CodeGenerator
    {
        private readonly ILGenerator _ILGen;

        public CodeGenerator(ILGenerator ilGen)
        {
            _ILGen = ilGen;
        }

        public void NullHandler(LocalBuilder variable, Label goIfNull)
        {
            var nullConditionVariable = _ILGen.DeclareLocal(typeof(bool));
            _ILGen.Emit(OpCodes.Ldloc, variable); // load object on the stack
            _ILGen.Emit(OpCodes.Ldnull); // load null 
            _ILGen.Emit(OpCodes.Cgt_Un); // if(object != null)  
            _ILGen.Emit(OpCodes.Stloc, nullConditionVariable); // save condition result into variable
            _ILGen.Emit(OpCodes.Ldloc, nullConditionVariable); // load condition result in stack
            _ILGen.Emit(OpCodes.Brfalse_S, goIfNull);// if false (object = null) -> Goto 
        }
    }
}