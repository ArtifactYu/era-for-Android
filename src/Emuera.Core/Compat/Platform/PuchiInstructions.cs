using MinorShift.Emuera.Runtime.Script;
using System;
using System.Globalization;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Utils;
namespace MinorShift.Emuera.GameProc.Function;
internal sealed partial class FunctionIdentifier
{
    private sealed class PuchiInstruction : AInstruction
    {
        readonly int kind;
        public PuchiInstruction(string args, int kind) { this.kind=kind; ArgBuilder=ArgumentParser.GetNormalArgumentBuilder(args,-1); flag=EXTENDED | (kind==0?METHOD_SAFE:0); }
        public override void DoInstruction(ExpressionMediator exm, InstructionLine func, ProcessState state)
        {
            var a=((ExpressionsArgument)func.Argument).ArgumentArray;
            if(kind==0){ PuchiGraphics.FontEdge=a[0].GetIntValue(exm)==1; return; }
            if(!float.TryParse(a[0].GetStrValue(exm),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)||!float.IsFinite(value)) throw new CodeEE("Invalid effect value");
            if(kind==1) exm.Console.PuchiFade(value,a[1].GetStrValue(exm),a[2].GetIntValue(exm),a[3].GetIntValue(exm));
            else exm.Console.PuchiShake(value,a[1].GetIntValue(exm),a[2].GetStrValue(exm),a[3].GetIntValue(exm));
        }
    }
}

