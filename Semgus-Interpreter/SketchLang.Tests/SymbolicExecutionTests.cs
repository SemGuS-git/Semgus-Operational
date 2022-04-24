using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semgus.OrderSynthesis.SketchSyntax;
using Semgus.OrderSynthesis.SketchSyntax.Parsing;
using Semgus.OrderSynthesis.SketchSyntax.Sugar;
using Semgus.OrderSynthesis.SketchSyntax.SymbolicEvaluation;
using System.Collections.Generic;
using Semgus.MiniParser;

namespace Semgus.SketchLang.Tests {
    [TestClass]
    public class SymbolicExecutionTests {
        static SketchSyntaxParser SketchParser = SketchSyntaxParser.Instance;

        static FunctionDefinition new_compare_In_0;
        static FunctionDefinition compare_In_0;
        static IExpression new_compare_In_0_normalized;
        static IExpression compare_In_0_normalized;

        static SymbolicExecutionTests() {
            new_compare_In_0 = SketchParser.FunctionDefinition.Parse(@"

void new_compare_In_0 (In_0 a, In_0 b, ref bit _out)/*manual_outer.sk:58*/
{
  bit leq_s178 = 0;
  compare_In_0(a, b, leq_s178);
  bit _pac_sc_s179 = leq_s178;
  if(!(leq_s178))/*manual_outer.sk:62*/
  {
    int a_0 = a.v0;
    int b_0 = b.v0;
    bit _pac_sc_s182_s184 = a_0 <= b_0;
    bit _pac_sc_s182;
    _pac_sc_s182 = _pac_sc_s182_s184;
    if(_pac_sc_s182_s184)/*manual_outer.sk:69*/
    {
      int a_1 = a.v1;
      int b_1 = b.v1;
      _pac_sc_s182 = a_1 <= b_1;
    }
    _pac_sc_s179 = _pac_sc_s182;
  }
  _out = _pac_sc_s179;
  return;
}
"
            );
            compare_In_0 = SketchParser.FunctionDefinition.Parse(@"

void compare_In_0 (In_0 a, In_0 b, ref bit _out)/*manual_outer.sk:6*/
{
  _out = (((a.v0) == (b.v0)) && ((a.v1) == (b.v1))) || (((a.v0) < (b.v0)) && ((a.v1) < (b.v1)));
  return;
}
/*manual_outer.sk:16*/


"
            );

            new_compare_In_0_normalized = SketchParser.Expression.Parse(@"a.v0 <= b.v0 && a.v1 <= b.v1 || a.v0 == b.v0 && a.v1 == b.v1 || a.v0 < b.v0 && a.v1 < b.v1");

        }


        [TestMethod]
        public void EqualityThing() {
            var s = @"a.v0 == b.v0 && a.v1 == b.v1 || a.v0 < b.v0 && a.v1 < b.v1";
            var a = SketchParser.Expression.Parse(s);
            var b = SketchParser.Expression.Parse(s);

            Assert.AreEqual(BitTernaryFlattener.Normalize(a),BitTernaryFlattener.Normalize(b));
        }



        [TestMethod]
        public void EqualityThing2() {
            var A = Var("A");;
            var B = Var("B");
            var C = Var("C");
            var D = Var("D");
            var E = Var("E");
            var F = Var("F");

            var rough = new Ternary(
                Not(Or(And(A, B), And(C, D))),
                new Ternary(E, F, E),
                Or(And(A, B), And(C, D))
            );
            var clean = Or(Or(And(A, B), And(C, D)), And(E, F));

            Assert.AreEqual(clean, BitTernaryFlattener.Normalize(rough));
        }

        [TestMethod]
        public void ReturnsStruct() {
            var fn = SketchParser.FunctionDefinition.Parse(@"
something main(something a, ref something b, ref something c) {
    b = new something(v0=3,v1=a.v1);
    c = a;
    global = b;
    return new something(v0=b.v0,v1=b.v1);
}
");
            var x = SymbolicInterpreter.Evaluate(fn);


            Assert.AreEqual(
                new StructNew(new("something"), new[] {
                    Var("v0").Assign(new Literal(3)) ,
                    Var("v1").Assign(Var("a.v1")),
                }),
                x.RefVariables[new("b")]
            );

            Assert.AreEqual(
                Var("a"),
                x.RefVariables[new("c")]
            );

            Assert.AreEqual(
                x.RefVariables[new("b")],
                x.Globals[new("global")]
            );

            Assert.AreEqual(
                new StructNew(new("something"), new[] {
                    Var("v0").Assign(new Literal(3)),
                    Var("v1").Assign(Var("a.v1"))
                }),
                x.ReturnValue
            );
        }
        //[TestMethod]
        //public void New_compare_In_0() {
        //    var x = SymbolicInterpreter.Evaluate(new_compare_In_0, compare_In_0);

        //    var out_val = x.RefVariables[new("_out")];

        //    var norm_1 = BitTernaryFlattener.Normalize(out_val);
        //    var norm_2 = NegationNormalForm.Normalize(norm_1);
        //    var norm_3 = DisjunctiveNormalForm.Normalize(norm_2);
        //    Assert.AreEqual(new_compare_In_0_normalized, norm_3);
        //}


        static UnaryOperation Not(IExpression v) => UnaryOp.Not.Of(v);
        static InfixOperation And(params IExpression[] v) => Op.And.Of(v);
        static InfixOperation Or(params IExpression[] v) => Op.Or.Of(v);

        static VariableRef Var(string s) => new(new(s));


        static IEnumerable<object[]> NegationNormalFormCases => new[] {
            new object[] { 
                Not(Or(Var("a"),Var("b"))),
                And(Not(Var("a")),Not(Var("b")))
            },
            new object[] {
                And(
                    Or(
                        Or(
                            Var("a"),Var("b")
                        ),
                        Not(
                            And(
                                Var("c"),Var("d")
                            )
                        ),
                        And (
                            Not(
                                Not(
                                    Not(
                                        Var("e")
                                    )
                                )
                            )
                        )
                    )
                ),

                And(
                    Or(
                        Or(
                            Var("a"),Var("b")
                        ),
                        Or(
                            Not(
                                Var("c")
                            ),
                            Not(
                                Var("d")
                            )
                        ),
                        And (
                            Not(
                                Var("e")
                            )
                        )
                    )
                )
            }

        };

        [DataTestMethod]
        [DynamicData(nameof(NegationNormalFormCases))]
        public void TestNegationNormalForm(object rough, object normalized) {
            Assert.AreEqual(normalized, NegationNormalForm.Normalize((IExpression)rough));
        }

    }
}