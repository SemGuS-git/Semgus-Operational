#if USE_RUST

using Semgus.Solvers.Util;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Semgus.Solvers {
    public class Phosphate {
        [DllImport("semgus_solvers_rust")]
        private static extern void say_hello();
        [DllImport("semgus_solvers_rust")]
        private static extern Int32 add_numbers(Int32 number1, Int32 number2);
        [DllImport("semgus_solvers_rust")]
        private unsafe static extern Int32 array_examine(UIntPtr size, Int32* array_pointer);

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("semgus_solvers_rust")]
        private unsafe static extern Int32 string_examine([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport("semgus_solvers_rust")]
        private static extern RustStringHandle string_reverse([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport("semgus_solvers_rust")]
        private static extern RustStringHandle egg_demo([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport("semgus_solvers_rust")]
        private static extern RustStringHandle egg_reduce([MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport("semgus_solvers_rust")]
        private static extern Double egg_timer([MarshalAs(UnmanagedType.LPUTF8Str)] string str);


#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments


        public static double ReductionTimeInMs(string str) => egg_timer(str);

        public static void Hello() {
            SayHello();
            DoArrayStuff();
            DoStringStuff();
            DoEggStuff();
        }

        private static void DoEggStuff() {
            Log("About to do egg stuff");

            var s = "(ite (^ (+ 1 y) (+ y 1)) (+ 0 a) (* b 1))";
            Log($"egg input is {s}");

            using (var res = egg_demo(s)) {
                Log($"egg output is {res.AsString()}");
            }

            Log("Did egg stuff");
        }

        // WIP
        public static string EggReduce(string program) {
            using var res = egg_reduce(program);
            return res.AsString();
        }

        private static void DoStringStuff() {
            Log("About to do string stuff");
            System.String s = "Abc Def Ghi";
            Log($"String is \"{s}\"");

            using (var rev = string_reverse(s)) {
                Log($"Got back \"{rev.AsString()}\"");
            }
            Log("Did string stuff");
        }

        private static void SayHello() {
            Log("About to say hello");
            Log($"Sum is {add_numbers(1, 2)}");
            say_hello();
            Log("Said hello");
        }

        private static void DoArrayStuff() {
            Log("About to do array stuff");
            Int32[] x = new[] { 3, 6, 9, 12, 15 };
            unsafe {
                fixed (Int32* ptr = x) {
                    var k = array_examine((UIntPtr)x.Length, ptr);
                    Log($"Got back {k}");
                }
            }
            Log("Did array stuff");
        }

        private static void Log(string s) => Console.WriteLine($"[C#] {s}");
    }
}
#endif