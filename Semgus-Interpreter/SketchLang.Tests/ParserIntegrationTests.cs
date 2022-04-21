using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semgus.MiniParser;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Sprache;
using System.Linq;

namespace Semgus.SketchLang.Tests {
    [TestClass]
    public class ParserIntegrationTests {
        static SketchSyntaxParser SketchParser = SketchSyntaxParser.Instance;
        const string FRAG1 = @"

harness void main (int Out_0_s0_v0, int Out_0_s1_v0) {
    // Assemble structs
    Out_0 Out_0_s0 = new Out_0(v0 = Out_0_s0_v0);
    return;
}
";


        const string SOME_MAIN = @"

harness void main (int Out_0_s0_v0, int Out_0_s1_v0, int Out_0_s2_v0, int Out_0_alt_v0, int In_0_s0_v0, int In_0_s0_v1, int In_0_s1_v0, int In_0_s1_v1, int In_0_s2_v0, int In_0_s2_v1, int In_0_alt_v0, int In_0_alt_v1, bit Out_1_s0_v0, bit Out_1_s1_v0, bit Out_1_s2_v0, bit Out_1_alt_v0) {
    // Assemble structs
    Out_0 Out_0_s0 = new Out_0(v0 = Out_0_s0_v0);
    Out_0 Out_0_s1 = new Out_0(v0 = Out_0_s1_v0);
    Out_0 Out_0_s2 = new Out_0(v0 = Out_0_s2_v0);
    Out_0 Out_0_alt = new Out_0(v0 = Out_0_alt_v0);
    In_0 In_0_s0 = new In_0(v0 = In_0_s0_v0, v1 = In_0_s0_v1);
    In_0 In_0_s1 = new In_0(v0 = In_0_s1_v0, v1 = In_0_s1_v1);
    In_0 In_0_s2 = new In_0(v0 = In_0_s2_v0, v1 = In_0_s2_v1);
    In_0 In_0_alt = new In_0(v0 = In_0_alt_v0, v1 = In_0_alt_v1);
    Out_1 Out_1_s0 = new Out_1(v0 = Out_1_s0_v0);
    Out_1 Out_1_s1 = new Out_1(v0 = Out_1_s1_v0);
    Out_1 Out_1_s2 = new Out_1(v0 = Out_1_s2_v0);
    Out_1 Out_1_alt = new Out_1(v0 = Out_1_alt_v0);
    
    
    // Check partial equality properties
    
    // Out_0: reflexivity and antisymmetry
    assert((compare_Out_0(Out_0_s0, Out_0_s1) && compare_Out_0(Out_0_s1, Out_0_s0)) == eq_Out_0(Out_0_s0, Out_0_s1));
    // Out_0: transitivity
    assert(!(compare_Out_0(Out_0_s0, Out_0_s1)) || !(compare_Out_0(Out_0_s1, Out_0_s2)) || compare_Out_0(Out_0_s0, Out_0_s2));
    
    // In_0: reflexivity and antisymmetry
    assert((compare_In_0(In_0_s0, In_0_s1) && compare_In_0(In_0_s1, In_0_s0)) == eq_In_0(In_0_s0, In_0_s1));
    // In_0: transitivity
    assert(!(compare_In_0(In_0_s0, In_0_s1)) || !(compare_In_0(In_0_s1, In_0_s2)) || compare_In_0(In_0_s0, In_0_s2));
    
    // Out_1: reflexivity and antisymmetry
    assert((compare_Out_1(Out_1_s0, Out_1_s1) && compare_Out_1(Out_1_s1, Out_1_s0)) == eq_Out_1(Out_1_s0, Out_1_s1));
    // Out_1: transitivity
    assert(!(compare_Out_1(Out_1_s0, Out_1_s1)) || !(compare_Out_1(Out_1_s1, Out_1_s2)) || compare_Out_1(Out_1_s0, Out_1_s2));
    
    
    // Monotonicity
    int n_mono = 0;
    
    // Monotonicity of lang_f0 ($x)
    int mono_lang_f0_0 = ?? /*#MONO $x_0*/;
    if(mono_lang_f0_0 == 0) {
        assert(!(compare_In_0(In_0_s0, In_0_alt)) || compare_Out_0(lang_f0(In_0_s0), lang_f0(In_0_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f0_0 == 1) {
        assert(!(compare_In_0(In_0_s0, In_0_alt)) || compare_Out_0(lang_f0(In_0_alt), lang_f0(In_0_s0)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f1 ($y)
    int mono_lang_f1_0 = ?? /*#MONO $y_0*/;
    if(mono_lang_f1_0 == 0) {
        assert(!(compare_In_0(In_0_s0, In_0_alt)) || compare_Out_0(lang_f1(In_0_s0), lang_f1(In_0_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f1_0 == 1) {
        assert(!(compare_In_0(In_0_s0, In_0_alt)) || compare_Out_0(lang_f1(In_0_alt), lang_f1(In_0_s0)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f2 (($+ E E))
    int mono_lang_f2_0 = ?? /*#MONO ($+ E E)_0*/;
    if(mono_lang_f2_0 == 0) {
        assert(!(compare_Out_0(Out_0_s0, Out_0_alt)) || compare_Out_0(lang_f2(Out_0_s0, Out_0_s1), lang_f2(Out_0_alt, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f2_0 == 1) {
        assert(!(compare_Out_0(Out_0_s0, Out_0_alt)) || compare_Out_0(lang_f2(Out_0_alt, Out_0_s1), lang_f2(Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    int mono_lang_f2_1 = ?? /*#MONO ($+ E E)_1*/;
    if(mono_lang_f2_1 == 0) {
        assert(!(compare_Out_0(Out_0_s1, Out_0_alt)) || compare_Out_0(lang_f2(Out_0_s0, Out_0_s1), lang_f2(Out_0_s0, Out_0_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f2_1 == 1) {
        assert(!(compare_Out_0(Out_0_s1, Out_0_alt)) || compare_Out_0(lang_f2(Out_0_s0, Out_0_alt), lang_f2(Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f3 (($ite B E E))
    int mono_lang_f3_0 = ?? /*#MONO ($ite B E E)_0*/;
    if(mono_lang_f3_0 == 0) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_0(lang_f3(Out_1_s0, Out_0_s0, Out_0_s1), lang_f3(Out_1_alt, Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f3_0 == 1) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_0(lang_f3(Out_1_alt, Out_0_s0, Out_0_s1), lang_f3(Out_1_s0, Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    int mono_lang_f3_1 = ?? /*#MONO ($ite B E E)_1*/;
    if(mono_lang_f3_1 == 0) {
        assert(!(compare_Out_0(Out_0_s0, Out_0_alt)) || compare_Out_0(lang_f3(Out_1_s0, Out_0_s0, Out_0_s1), lang_f3(Out_1_s0, Out_0_alt, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f3_1 == 1) {
        assert(!(compare_Out_0(Out_0_s0, Out_0_alt)) || compare_Out_0(lang_f3(Out_1_s0, Out_0_alt, Out_0_s1), lang_f3(Out_1_s0, Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    int mono_lang_f3_2 = ?? /*#MONO ($ite B E E)_2*/;
    if(mono_lang_f3_2 == 0) {
        assert(!(compare_Out_0(Out_0_s1, Out_0_alt)) || compare_Out_0(lang_f3(Out_1_s0, Out_0_s0, Out_0_s1), lang_f3(Out_1_s0, Out_0_s0, Out_0_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f3_2 == 1) {
        assert(!(compare_Out_0(Out_0_s1, Out_0_alt)) || compare_Out_0(lang_f3(Out_1_s0, Out_0_s0, Out_0_alt), lang_f3(Out_1_s0, Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f4 (($not B))
    int mono_lang_f4_0 = ?? /*#MONO ($not B)_0*/;
    if(mono_lang_f4_0 == 0) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_1(lang_f4(Out_1_s0), lang_f4(Out_1_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f4_0 == 1) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_1(lang_f4(Out_1_alt), lang_f4(Out_1_s0)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f5 (($and B B))
    int mono_lang_f5_0 = ?? /*#MONO ($and B B)_0*/;
    if(mono_lang_f5_0 == 0) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_1(lang_f5(Out_1_s0, Out_1_s1), lang_f5(Out_1_alt, Out_1_s1)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f5_0 == 1) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_1(lang_f5(Out_1_alt, Out_1_s1), lang_f5(Out_1_s0, Out_1_s1)));
        n_mono = n_mono + 1;
    }
    int mono_lang_f5_1 = ?? /*#MONO ($and B B)_1*/;
    if(mono_lang_f5_1 == 0) {
        assert(!(compare_Out_1(Out_1_s1, Out_1_alt)) || compare_Out_1(lang_f5(Out_1_s0, Out_1_s1), lang_f5(Out_1_s0, Out_1_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f5_1 == 1) {
        assert(!(compare_Out_1(Out_1_s1, Out_1_alt)) || compare_Out_1(lang_f5(Out_1_s0, Out_1_alt), lang_f5(Out_1_s0, Out_1_s1)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f6 (($or B B))
    int mono_lang_f6_0 = ?? /*#MONO ($or B B)_0*/;
    if(mono_lang_f6_0 == 0) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_1(lang_f6(Out_1_s0, Out_1_s1), lang_f6(Out_1_alt, Out_1_s1)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f6_0 == 1) {
        assert(!(compare_Out_1(Out_1_s0, Out_1_alt)) || compare_Out_1(lang_f6(Out_1_alt, Out_1_s1), lang_f6(Out_1_s0, Out_1_s1)));
        n_mono = n_mono + 1;
    }
    int mono_lang_f6_1 = ?? /*#MONO ($or B B)_1*/;
    if(mono_lang_f6_1 == 0) {
        assert(!(compare_Out_1(Out_1_s1, Out_1_alt)) || compare_Out_1(lang_f6(Out_1_s0, Out_1_s1), lang_f6(Out_1_s0, Out_1_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f6_1 == 1) {
        assert(!(compare_Out_1(Out_1_s1, Out_1_alt)) || compare_Out_1(lang_f6(Out_1_s0, Out_1_alt), lang_f6(Out_1_s0, Out_1_s1)));
        n_mono = n_mono + 1;
    }
    
    // Monotonicity of lang_f7 (($< E E))
    int mono_lang_f7_0 = ?? /*#MONO ($< E E)_0*/;
    if(mono_lang_f7_0 == 0) {
        assert(!(compare_Out_0(Out_0_s0, Out_0_alt)) || compare_Out_1(lang_f7(Out_0_s0, Out_0_s1), lang_f7(Out_0_alt, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f7_0 == 1) {
        assert(!(compare_Out_0(Out_0_s0, Out_0_alt)) || compare_Out_1(lang_f7(Out_0_alt, Out_0_s1), lang_f7(Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    int mono_lang_f7_1 = ?? /*#MONO ($< E E)_1*/;
    if(mono_lang_f7_1 == 0) {
        assert(!(compare_Out_0(Out_0_s1, Out_0_alt)) || compare_Out_1(lang_f7(Out_0_s0, Out_0_s1), lang_f7(Out_0_s0, Out_0_alt)));
        n_mono = n_mono + 1;
    }
    else if(mono_lang_f7_1 == 1) {
        assert(!(compare_Out_0(Out_0_s1, Out_0_alt)) || compare_Out_1(lang_f7(Out_0_s0, Out_0_alt), lang_f7(Out_0_s0, Out_0_s1)));
        n_mono = n_mono + 1;
    }
    minimize(14 - n_mono);
}
";

        const string GENERATED_WRAPPER = @"

void non_eq_Out_0__Wrapper ()  implements non_eq_Out_0__WrapperNospec/*ord-max..exp.sl.sk:141*/
{
  non_eq_Out_0();
}
/*ord-max..exp.sl.sk:141*/

";

        const string GENERATED_MAIN = @"

void _main (int Out_0_s0_v0, int Out_0_s1_v0, int Out_0_s2_v0, int Out_0_alt_v0, int In_0_s0_v0, int In_0_s0_v1, int In_0_s1_v0, int In_0_s1_v1, int In_0_s2_v0, int In_0_s2_v1, int In_0_alt_v0, int In_0_alt_v1, bit Out_1_s0_v0, bit Out_1_s1_v0, bit Out_1_s2_v0, bit Out_1_alt_v0)/*ord-max..exp.sl.sk:162*/
{
  Out_0@ANONYMOUS Out_0_s0;
  Out_0_s0 = new Out_0(v0=Out_0_s0_v0);
  Out_0@ANONYMOUS Out_0_s1;
  Out_0_s1 = new Out_0(v0=Out_0_s1_v0);
  Out_0@ANONYMOUS Out_0_s2;
  Out_0_s2 = new Out_0(v0=Out_0_s2_v0);
  Out_0@ANONYMOUS Out_0_alt;
  Out_0_alt = new Out_0(v0=Out_0_alt_v0);
  In_0@ANONYMOUS In_0_s0;
  In_0_s0 = new In_0(v0=In_0_s0_v0, v1=In_0_s0_v1);
  In_0@ANONYMOUS In_0_s1;
  In_0_s1 = new In_0(v0=In_0_s1_v0, v1=In_0_s1_v1);
  In_0@ANONYMOUS In_0_s2;
  In_0_s2 = new In_0(v0=In_0_s2_v0, v1=In_0_s2_v1);
  In_0@ANONYMOUS In_0_alt;
  In_0_alt = new In_0(v0=In_0_alt_v0, v1=In_0_alt_v1);
  Out_1@ANONYMOUS Out_1_s0;
  Out_1_s0 = new Out_1(v0=Out_1_s0_v0);
  Out_1@ANONYMOUS Out_1_s1;
  Out_1_s1 = new Out_1(v0=Out_1_s1_v0);
  Out_1@ANONYMOUS Out_1_s2;
  Out_1_s2 = new Out_1(v0=Out_1_s2_v0);
  Out_1@ANONYMOUS Out_1_alt;
  Out_1_alt = new Out_1(v0=Out_1_alt_v0);
  bit _pac_sc_s0_s2 = 0;
  compare_Out_0(Out_0_s0, Out_0_s1, _pac_sc_s0_s2);
  bit _pac_sc_s0;
  _pac_sc_s0 = _pac_sc_s0_s2;
  if(_pac_sc_s0_s2)/*ord-max..exp.sl.sk:181*/
  {
    bit _pac_sc_s0_s4 = 0;
    compare_Out_0(Out_0_s1, Out_0_s0, _pac_sc_s0_s4);
    _pac_sc_s0 = _pac_sc_s0_s4;
  }
  bit _out_s6 = 0;
  eq_Out_0(Out_0_s0, Out_0_s1, _out_s6);
  assert (_pac_sc_s0 == _out_s6); //Assert at ord-max..exp.sl.sk:181 (0)
  bit _pac_sc_s8_s10 = 0;
  compare_Out_0(Out_0_s0, Out_0_s1, _pac_sc_s8_s10);
  bit _pac_sc_s8;
  _pac_sc_s8 = !(_pac_sc_s8_s10);
  if(!(_pac_sc_s8))/*ord-max..exp.sl.sk:183*/
  {
    bit _pac_sc_s8_s12 = 0;
    compare_Out_0(Out_0_s1, Out_0_s2, _pac_sc_s8_s12);
    _pac_sc_s8 = !(_pac_sc_s8_s12);
  }
  bit _pac_sc_s7 = _pac_sc_s8;
  if(!(_pac_sc_s8))/*ord-max..exp.sl.sk:183*/
  {
    bit _pac_sc_s7_s14 = 0;
    compare_Out_0(Out_0_s0, Out_0_s2, _pac_sc_s7_s14);
    _pac_sc_s7 = _pac_sc_s7_s14;
  }
  assert (_pac_sc_s7); //Assert at ord-max..exp.sl.sk:183 (0)
  bit _pac_sc_s15_s17 = 0;
  compare_In_0(In_0_s0, In_0_s1, _pac_sc_s15_s17);
  bit _pac_sc_s15;
  _pac_sc_s15 = _pac_sc_s15_s17;
  if(_pac_sc_s15_s17)/*ord-max..exp.sl.sk:186*/
  {
    bit _pac_sc_s15_s19 = 0;
    compare_In_0(In_0_s1, In_0_s0, _pac_sc_s15_s19);
    _pac_sc_s15 = _pac_sc_s15_s19;
  }
  bit _out_s21 = 0;
  eq_In_0(In_0_s0, In_0_s1, _out_s21);
  assert (_pac_sc_s15 == _out_s21); //Assert at ord-max..exp.sl.sk:186 (0)
  bit _pac_sc_s23_s25 = 0;
  compare_In_0(In_0_s0, In_0_s1, _pac_sc_s23_s25);
  bit _pac_sc_s23;
  _pac_sc_s23 = !(_pac_sc_s23_s25);
  if(!(_pac_sc_s23))/*ord-max..exp.sl.sk:188*/
  {
    bit _pac_sc_s23_s27 = 0;
    compare_In_0(In_0_s1, In_0_s2, _pac_sc_s23_s27);
    _pac_sc_s23 = !(_pac_sc_s23_s27);
  }
  bit _pac_sc_s22 = _pac_sc_s23;
  if(!(_pac_sc_s23))/*ord-max..exp.sl.sk:188*/
  {
    bit _pac_sc_s22_s29 = 0;
    compare_In_0(In_0_s0, In_0_s2, _pac_sc_s22_s29);
    _pac_sc_s22 = _pac_sc_s22_s29;
  }
  assert (_pac_sc_s22); //Assert at ord-max..exp.sl.sk:188 (0)
  bit _pac_sc_s30_s32 = 0;
  compare_Out_1(Out_1_s0, Out_1_s1, _pac_sc_s30_s32);
  bit _pac_sc_s30;
  _pac_sc_s30 = _pac_sc_s30_s32;
  if(_pac_sc_s30_s32)/*ord-max..exp.sl.sk:191*/
  {
    bit _pac_sc_s30_s34 = 0;
    compare_Out_1(Out_1_s1, Out_1_s0, _pac_sc_s30_s34);
    _pac_sc_s30 = _pac_sc_s30_s34;
  }
  bit _out_s36 = 0;
  eq_Out_1(Out_1_s0, Out_1_s1, _out_s36);
  assert (_pac_sc_s30 == _out_s36); //Assert at ord-max..exp.sl.sk:191 (0)
  bit _pac_sc_s38_s40 = 0;
  compare_Out_1(Out_1_s0, Out_1_s1, _pac_sc_s38_s40);
  bit _pac_sc_s38;
  _pac_sc_s38 = !(_pac_sc_s38_s40);
  if(!(_pac_sc_s38))/*ord-max..exp.sl.sk:193*/
  {
    bit _pac_sc_s38_s42 = 0;
    compare_Out_1(Out_1_s1, Out_1_s2, _pac_sc_s38_s42);
    _pac_sc_s38 = !(_pac_sc_s38_s42);
  }
  bit _pac_sc_s37 = _pac_sc_s38;
  if(!(_pac_sc_s38))/*ord-max..exp.sl.sk:193*/
  {
    bit _pac_sc_s37_s44 = 0;
    compare_Out_1(Out_1_s0, Out_1_s2, _pac_sc_s37_s44);
    _pac_sc_s37 = _pac_sc_s37_s44;
  }
  assert (_pac_sc_s37); //Assert at ord-max..exp.sl.sk:193 (0)
  bit _pac_sc_s54_s56 = 0;
  compare_In_0(In_0_s0, In_0_alt, _pac_sc_s54_s56);
  bit _pac_sc_s54;
  _pac_sc_s54 = !(_pac_sc_s54_s56);
  if(!(_pac_sc_s54))/*ord-max..exp.sl.sk:206*/
  {
    Out_0@ANONYMOUS _pac_sc_s54_s58 = null;
    lang_f0(In_0_alt, _pac_sc_s54_s58);
    Out_0@ANONYMOUS _pac_sc_s54_s60 = null;
    lang_f0(In_0_s0, _pac_sc_s54_s60);
    bit _pac_sc_s54_s62 = 0;
    compare_Out_0(_pac_sc_s54_s58, _pac_sc_s54_s60, _pac_sc_s54_s62)//{};
    _pac_sc_s54 = _pac_sc_s54_s62;
  }
  assert (_pac_sc_s54); //Assert at ord-max..exp.sl.sk:206 (0)
  bit _pac_sc_s72_s74 = 0;
  compare_In_0(In_0_s0, In_0_alt, _pac_sc_s72_s74);
  bit _pac_sc_s72;
  _pac_sc_s72 = !(_pac_sc_s72_s74);
  if(!(_pac_sc_s72))/*ord-max..exp.sl.sk:217*/
  {
    Out_0@ANONYMOUS _pac_sc_s72_s76 = null;
    lang_f1(In_0_alt, _pac_sc_s72_s76);
    Out_0@ANONYMOUS _pac_sc_s72_s78 = null;
    lang_f1(In_0_s0, _pac_sc_s72_s78);
    bit _pac_sc_s72_s80 = 0;
    compare_Out_0(_pac_sc_s72_s76, _pac_sc_s72_s78, _pac_sc_s72_s80)//{};
    _pac_sc_s72 = _pac_sc_s72_s80;
  }
  assert (_pac_sc_s72); //Assert at ord-max..exp.sl.sk:217 (0)
  bit _pac_sc_s81_s83 = 0;
  compare_Out_0(Out_0_s0, Out_0_alt, _pac_sc_s81_s83);
  bit _pac_sc_s81;
  _pac_sc_s81 = !(_pac_sc_s81_s83);
  if(!(_pac_sc_s81))/*ord-max..exp.sl.sk:224*/
  {
    Out_0@ANONYMOUS _pac_sc_s81_s85 = null;
    lang_f2(Out_0_s0, Out_0_s1, _pac_sc_s81_s85);
    Out_0@ANONYMOUS _pac_sc_s81_s87 = null;
    lang_f2(Out_0_alt, Out_0_s1, _pac_sc_s81_s87);
    bit _pac_sc_s81_s89 = 0;
    compare_Out_0(_pac_sc_s81_s85, _pac_sc_s81_s87, _pac_sc_s81_s89)//{};
    _pac_sc_s81 = _pac_sc_s81_s89;
  }
  assert (_pac_sc_s81); //Assert at ord-max..exp.sl.sk:224 (0)
  bit _pac_sc_s99_s101 = 0;
  compare_Out_0(Out_0_s1, Out_0_alt, _pac_sc_s99_s101);
  bit _pac_sc_s99;
  _pac_sc_s99 = !(_pac_sc_s99_s101);
  if(!(_pac_sc_s99))/*ord-max..exp.sl.sk:233*/
  {
    Out_0@ANONYMOUS _pac_sc_s99_s103 = null;
    lang_f2(Out_0_s0, Out_0_s1, _pac_sc_s99_s103);
    Out_0@ANONYMOUS _pac_sc_s99_s105 = null;
    lang_f2(Out_0_s0, Out_0_alt, _pac_sc_s99_s105);
    bit _pac_sc_s99_s107 = 0;
    compare_Out_0(_pac_sc_s99_s103, _pac_sc_s99_s105, _pac_sc_s99_s107)//{};
    _pac_sc_s99 = _pac_sc_s99_s107;
  }
  assert (_pac_sc_s99); //Assert at ord-max..exp.sl.sk:233 (0)
  bit _pac_sc_s135_s137 = 0;
  compare_Out_0(Out_0_s0, Out_0_alt, _pac_sc_s135_s137);
  bit _pac_sc_s135;
  _pac_sc_s135 = !(_pac_sc_s135_s137);
  if(!(_pac_sc_s135))/*ord-max..exp.sl.sk:253*/
  {
    Out_0@ANONYMOUS _pac_sc_s135_s139 = null;
    lang_f3(Out_1_s0, Out_0_s0, Out_0_s1, _pac_sc_s135_s139);
    Out_0@ANONYMOUS _pac_sc_s135_s141 = null;
    lang_f3(Out_1_s0, Out_0_alt, Out_0_s1, _pac_sc_s135_s141);
    bit _pac_sc_s135_s143 = 0;
    compare_Out_0(_pac_sc_s135_s139, _pac_sc_s135_s141, _pac_sc_s135_s143)//{};
    _pac_sc_s135 = _pac_sc_s135_s143;
  }
  assert (_pac_sc_s135); //Assert at ord-max..exp.sl.sk:253 (0)
  bit _pac_sc_s153_s155 = 0;
  compare_Out_0(Out_0_s1, Out_0_alt, _pac_sc_s153_s155);
  bit _pac_sc_s153;
  _pac_sc_s153 = !(_pac_sc_s153_s155);
  if(!(_pac_sc_s153))/*ord-max..exp.sl.sk:262*/
  {
    Out_0@ANONYMOUS _pac_sc_s153_s157 = null;
    lang_f3(Out_1_s0, Out_0_s0, Out_0_s1, _pac_sc_s153_s157);
    Out_0@ANONYMOUS _pac_sc_s153_s159 = null;
    lang_f3(Out_1_s0, Out_0_s0, Out_0_alt, _pac_sc_s153_s159);
    bit _pac_sc_s153_s161 = 0;
    compare_Out_0(_pac_sc_s153_s157, _pac_sc_s153_s159, _pac_sc_s153_s161)//{};
    _pac_sc_s153 = _pac_sc_s153_s161;
  }
  assert (_pac_sc_s153); //Assert at ord-max..exp.sl.sk:262 (0)
  bit _pac_sc_s180_s182 = 0;
  compare_Out_1(Out_1_s0, Out_1_alt, _pac_sc_s180_s182);
  bit _pac_sc_s180;
  _pac_sc_s180 = !(_pac_sc_s180_s182);
  if(!(_pac_sc_s180))/*ord-max..exp.sl.sk:277*/
  {
    Out_1@ANONYMOUS _pac_sc_s180_s184 = null;
    lang_f4(Out_1_alt, _pac_sc_s180_s184);
    Out_1@ANONYMOUS _pac_sc_s180_s186 = null;
    lang_f4(Out_1_s0, _pac_sc_s180_s186);
    bit _pac_sc_s180_s188 = 0;
    compare_Out_1(_pac_sc_s180_s184, _pac_sc_s180_s186, _pac_sc_s180_s188)//{};
    _pac_sc_s180 = _pac_sc_s180_s188;
  }
  assert (_pac_sc_s180); //Assert at ord-max..exp.sl.sk:277 (0)
  bit _pac_sc_s189_s191 = 0;
  compare_Out_1(Out_1_s0, Out_1_alt, _pac_sc_s189_s191);
  bit _pac_sc_s189;
  _pac_sc_s189 = !(_pac_sc_s189_s191);
  if(!(_pac_sc_s189))/*ord-max..exp.sl.sk:284*/
  {
    Out_1@ANONYMOUS _pac_sc_s189_s193 = null;
    lang_f5(Out_1_s0, Out_1_s1, _pac_sc_s189_s193);
    Out_1@ANONYMOUS _pac_sc_s189_s195 = null;
    lang_f5(Out_1_alt, Out_1_s1, _pac_sc_s189_s195);
    bit _pac_sc_s189_s197 = 0;
    compare_Out_1(_pac_sc_s189_s193, _pac_sc_s189_s195, _pac_sc_s189_s197)//{};
    _pac_sc_s189 = _pac_sc_s189_s197;
  }
  assert (_pac_sc_s189); //Assert at ord-max..exp.sl.sk:284 (0)
  bit _pac_sc_s207_s209 = 0;
  compare_Out_1(Out_1_s1, Out_1_alt, _pac_sc_s207_s209);
  bit _pac_sc_s207;
  _pac_sc_s207 = !(_pac_sc_s207_s209);
  if(!(_pac_sc_s207))/*ord-max..exp.sl.sk:293*/
  {
    Out_1@ANONYMOUS _pac_sc_s207_s211 = null;
    lang_f5(Out_1_s0, Out_1_s1, _pac_sc_s207_s211);
    Out_1@ANONYMOUS _pac_sc_s207_s213 = null;
    lang_f5(Out_1_s0, Out_1_alt, _pac_sc_s207_s213);
    bit _pac_sc_s207_s215 = 0;
    compare_Out_1(_pac_sc_s207_s211, _pac_sc_s207_s213, _pac_sc_s207_s215)//{};
    _pac_sc_s207 = _pac_sc_s207_s215;
  }
  assert (_pac_sc_s207); //Assert at ord-max..exp.sl.sk:293 (0)
  bit _pac_sc_s225_s227 = 0;
  compare_Out_1(Out_1_s0, Out_1_alt, _pac_sc_s225_s227);
  bit _pac_sc_s225;
  _pac_sc_s225 = !(_pac_sc_s225_s227);
  if(!(_pac_sc_s225))/*ord-max..exp.sl.sk:304*/
  {
    Out_1@ANONYMOUS _pac_sc_s225_s229 = null;
    lang_f6(Out_1_s0, Out_1_s1, _pac_sc_s225_s229);
    Out_1@ANONYMOUS _pac_sc_s225_s231 = null;
    lang_f6(Out_1_alt, Out_1_s1, _pac_sc_s225_s231);
    bit _pac_sc_s225_s233 = 0;
    compare_Out_1(_pac_sc_s225_s229, _pac_sc_s225_s231, _pac_sc_s225_s233)//{};
    _pac_sc_s225 = _pac_sc_s225_s233;
  }
  assert (_pac_sc_s225); //Assert at ord-max..exp.sl.sk:304 (0)
  bit _pac_sc_s243_s245 = 0;
  compare_Out_1(Out_1_s1, Out_1_alt, _pac_sc_s243_s245);
  bit _pac_sc_s243;
  _pac_sc_s243 = !(_pac_sc_s243_s245);
  if(!(_pac_sc_s243))/*ord-max..exp.sl.sk:313*/
  {
    Out_1@ANONYMOUS _pac_sc_s243_s247 = null;
    lang_f6(Out_1_s0, Out_1_s1, _pac_sc_s243_s247);
    Out_1@ANONYMOUS _pac_sc_s243_s249 = null;
    lang_f6(Out_1_s0, Out_1_alt, _pac_sc_s243_s249);
    bit _pac_sc_s243_s251 = 0;
    compare_Out_1(_pac_sc_s243_s247, _pac_sc_s243_s249, _pac_sc_s243_s251)//{};
    _pac_sc_s243 = _pac_sc_s243_s251;
  }
  assert (_pac_sc_s243); //Assert at ord-max..exp.sl.sk:313 (0)
  bit _pac_sc_s270_s272 = 0;
  compare_Out_0(Out_0_s0, Out_0_alt, _pac_sc_s270_s272);
  bit _pac_sc_s270;
  _pac_sc_s270 = !(_pac_sc_s270_s272);
  if(!(_pac_sc_s270))/*ord-max..exp.sl.sk:328*/
  {
    Out_1@ANONYMOUS _pac_sc_s270_s274 = null;
    lang_f7(Out_0_alt, Out_0_s1, _pac_sc_s270_s274);
    Out_1@ANONYMOUS _pac_sc_s270_s276 = null;
    lang_f7(Out_0_s0, Out_0_s1, _pac_sc_s270_s276);
    bit _pac_sc_s270_s278 = 0;
    compare_Out_1(_pac_sc_s270_s274, _pac_sc_s270_s276, _pac_sc_s270_s278)//{};
    _pac_sc_s270 = _pac_sc_s270_s278;
  }
  assert (_pac_sc_s270); //Assert at ord-max..exp.sl.sk:328 (0)
  bit _pac_sc_s279_s281 = 0;
  compare_Out_0(Out_0_s1, Out_0_alt, _pac_sc_s279_s281);
  bit _pac_sc_s279;
  _pac_sc_s279 = !(_pac_sc_s279_s281);
  if(!(_pac_sc_s279))/*ord-max..exp.sl.sk:333*/
  {
    Out_1@ANONYMOUS _pac_sc_s279_s283 = null;
    lang_f7(Out_0_s0, Out_0_s1, _pac_sc_s279_s283);
    Out_1@ANONYMOUS _pac_sc_s279_s285 = null;
    lang_f7(Out_0_s0, Out_0_alt, _pac_sc_s279_s285);
    bit _pac_sc_s279_s287 = 0;
    compare_Out_1(_pac_sc_s279_s283, _pac_sc_s279_s285, _pac_sc_s279_s287)//{};
    _pac_sc_s279 = _pac_sc_s279_s287;
  }
  assert (_pac_sc_s279); //Assert at ord-max..exp.sl.sk:333 (0)
}
";

        const string GENERATED_FRAGMENT1 = @"
{
  bit _pac_sc_s54;
  _pac_sc_s54 = !(_pac_sc_s54_s56);
  if(!(_pac_sc_s54))/*ord-max..exp.sl.sk:206*/
  {
    Out_0@ANONYMOUS _pac_sc_s54_s58 = null;
    lang_f0(In_0_alt, _pac_sc_s54_s58);
    Out_0@ANONYMOUS _pac_sc_s54_s60 = null;
    lang_f0(In_0_s0, _pac_sc_s54_s60);
    bit _pac_sc_s54_s62 = 0;
    compare_Out_0(_pac_sc_s54_s58, _pac_sc_s54_s60, _pac_sc_s54_s62)//{};
    _pac_sc_s54 = _pac_sc_s54_s62;
  }
  assert (_pac_sc_s54); //Assert at ord-max..exp.sl.sk:206 (0)
}
";
        const string GENERATED_FRAGMENT2 = @"
void partial() {
  _pac_sc_s38 = !(_pac_sc_s38_s40);
  if(!(_pac_sc_s38))/*ord-max..exp.sl.sk:193*/
  {
    bit _pac_sc_s38_s42 = 0;
    compare_Out_1(Out_1_s1, Out_1_s2, _pac_sc_s38_s42);
    _pac_sc_s38 = !(_pac_sc_s38_s42);
  }
  bit _pac_sc_s37 = _pac_sc_s38;
  if(!(_pac_sc_s38))/*ord-max..exp.sl.sk:193*/
  {
    bit _pac_sc_s37_s44 = 0;
    compare_Out_1(Out_1_s0, Out_1_s2, _pac_sc_s37_s44);
    _pac_sc_s37 = _pac_sc_s37_s44;
  }
  assert (_pac_sc_s37); //Assert at ord-max..exp.sl.sk:193 (0)
  bit _pac_sc_s54_s56 = 0;
  compare_In_0(In_0_s0, In_0_alt, _pac_sc_s54_s56);
  bit _pac_sc_s54;
  _pac_sc_s54 = !(_pac_sc_s54_s56);
  if(!(_pac_sc_s54))/*ord-max..exp.sl.sk:206*/
  {
    Out_0@ANONYMOUS _pac_sc_s54_s58 = null;
    lang_f0(In_0_alt, _pac_sc_s54_s58);
    Out_0@ANONYMOUS _pac_sc_s54_s60 = null;
    lang_f0(In_0_s0, _pac_sc_s54_s60);
    bit _pac_sc_s54_s62 = 0;
    compare_Out_0(_pac_sc_s54_s58, _pac_sc_s54_s60, _pac_sc_s54_s62)//{};
    _pac_sc_s54 = _pac_sc_s54_s62;
  }
  assert (_pac_sc_s54); //Assert at ord-max..exp.sl.sk:206 (0)
  bit _pac_sc_s72_s74 = 0;
  compare_In_0(In_0_s0, In_0_alt, _pac_sc_s72_s74);
}
";

        [TestMethod]
        public void CanParseMain() {
            var main = SketchParser.FunctionDefinition.Parse(SOME_MAIN);
            Assert.IsTrue(main != null);
            Assert.AreEqual(6, main.Body.Count(s => s is AssertStatement));
            Assert.AreEqual(14, main.Body.Count(s => s is IfStatement));
            Assert.AreEqual(1, main.Body.Count(s => s is MinimizeStatement));
        }
        [TestMethod]
        public void CanParseFrag1() {
            var main = SketchParser.FunctionDefinition.Parse(FRAG1);
            Assert.IsTrue(main != null);
            Assert.AreEqual(2, main.Signature.Args.Count);
            Assert.AreEqual(2, main.Body.Count);
        }
        [TestMethod]
        public void CanParseFrag2() {
            var main = SketchParser.FunctionDefinition.Parse(FRAG1);
            Assert.IsTrue(main != null);
            Assert.AreEqual(2, main.Signature.Args.Count);
            Assert.AreEqual(2, main.Body.Count);
        }
        [TestMethod]
        public void GeneratedWrapper() {
            var main = SketchParser.FunctionDefinition.Parse(GENERATED_WRAPPER);
            Assert.IsTrue(main != null);
            Assert.IsNotNull(main.Signature.ImplementsId);
        }
        [TestMethod]
        public void GeneratedMain() {
            var main = SketchParser.FunctionDefinition.Parse(GENERATED_MAIN);
            Assert.IsTrue(main != null);
            Assert.IsTrue(main.Body.Count > 100);
        }
        //[TestMethod]
        //public void GeneratedFragment1() {
        //    var ll = SketchParser.ProceduralBlock.Parse(GENERATED_FRAGMENT1).ToList();
        //    Assert.AreEqual(4, ll.Count);
        //    Assert.IsTrue(ll[0] is WeakVariableDeclaration);

        //    Assert.AreEqual(
        //        new VariableRef(new("_pac_sc_s54")).Assign(
        //            UnaryOp.Not.Of(
        //                new VariableRef(new("_pac_sc_s54_s56"))
        //            )
        //        ),
        //        ll[1]
        //    );

        //    Assert.IsTrue(ll[2] is IfStatement);
        //    Assert.AreEqual(7, ((IfStatement)ll[2]).BodyLhs.Count);
        //}

        [TestMethod]
        public void FileWithHeaderFooter() {

            const string a = @"
SKETCH version 1.7.6
Benchmark = /mnt/c/Users/Wiley/home/uw/semgus/monotonicity-synthesis/sketch3/ord-max2-exp.sl.sk
/* BEGIN PACKAGE ANONYMOUS*/
void non_eq_Out_1__WrapperNospec ()/*ord-max..exp.sl.sk:155*/
{ }
/* END PACKAGE ANONYMOUS*/
[SKETCH] DONE
Total time = 6615
";
            (var head, var body, var foot) = SketchSyntaxParser.StripHeaders(a).Unwrap();

            Assert.AreEqual(a.Substring(0, 10), head.Substring(0, 10));
            Assert.IsTrue(body.Substring(0,10).TrimStart().StartsWith("void"));

            Assert.IsTrue(SketchParser.FileContent.ParseMany(body).ToList().Count > 0);


        }
    }
}