# Semgus-Operational

This project contains code to infer the operational semantics of a Semgus language, interpret programs constructed in such a language, and solve Semgus problems via enumeration. It currently supports the 2.0.0 foarmat.

See [semgus.org](http://semgus.org) for more information.

# Getting started

## CLI tools and basic SemGuS solvers

This section covers the installation of the Semgus command-line interface, which provides access to our baseline enumerative solvers.

### Installing binaries

The CLI is available as a pre-built binary for Windows, Mac, and Linux. [[Link]](https://github.com/SemGuS-git/Semgus-Operational/releases)

After downloading and unzipping the release for your platform, add the folder containing the executable to your `PATH`. You should then be able to run the CLI as described below.

### Compiling from source

The SemGuS parser, enumerative solvers, and CLI are built with .NET *Core* 6.0 using Visual Studio 2022. Older versions of Visual Studio may also be compatible (If Visual Studio is not available for your platform or you prefer to use other tools, additional configuration may be required to build the project).

To clone our source code from [GitHub](https://github.com/SemGuS-git/Semgus-Operational), run the following commands in your terminal:

```
git clone git@github.com:SemGuS-git/Semgus-Operational.git
cd Semgus-Operational
git submodule update -i -r
```

Open the solution file at `Semgus-Operational/Semgus-Interpreter/Semgus-Interpreter.sln` in Visual Studio, then build the project from VS.

You can then add the executable files to your PATH as discussed in the previous section.

### Validation

In your terminal, run `semgus-cli --help` to print the help string and verify that the CLI is installed.

## Using the Command-Line Tools

As an example, we will synthesize a function to compute the maximum of two integers. We will use a simple expression language consisting of arithmetic and logical operators, along with a ternary operator (i.e., an if-then-else expression).

To get started, clone the [Semgus benchmarks repository](https://github.com/SemGuS-git/Semgus-Benchmarks) to your preferred location:

`git clone git@github.com:SemGuS-git/Semgus-Benchmarks.git`

### Running Tests

We will be working with the `max2-exp.sl` Semgus file. This file defines the synthesis task described above. It also contains several embedded tests, which can be used to validate that the file is correct and that our solver infrastructure is compatible.

To perform these tests, run `semgus-cli test Semgus-Benchmarks/basic/max2-exp.sl` in your terminal.

You should see the following text output:

```
Pass: in max2-exp.sl run ($< $x $y) with {x:1 y:2}; expected {r:True}, got {r:True}
Pass: in max2-exp.sl run ($< $x $y) with {x:12 y:8}; expected {r:False}, got {r:False}
Pass: in max2-exp.sl run ($+ ($+ $1 $x) $y) with {x:1 y:2}; expected {r:4}, got {r:4}
Pass: in max2-exp.sl run ($+ ($+ $1 $x) $y) with {x:5 y:3}; expected {r:9}, got {r:9}
Pass: in max2-exp.sl run ($+ ($+ $1 $x) $y) with {x:0 y:0}; expected {r:any}, got {r:1}
Pass: in max2-exp.sl run ($ite ($< $x $y) $y $x) with {x:4 y:2}; expected {r:4}, got {r:4}
Pass: in max2-exp.sl run ($ite ($< $x $y) $y $x) with {x:2 y:5}; expected {r:5}, got {r:5}
Pass: in max2-exp.sl run ($ite ($< $x $y) $y $x) with {x:2 y:7}; expected {r:7}, got {r:7}
Pass: in max2-exp.sl run ($ite ($< $x $y) $y $x) with <synth-fun-soln>; expected True, got True
Pass: in max2-exp.sl run ($+ $0 ($ite ($< $x $y) $y $x)) with {x:4 y:2}; expected {r:4}, got {r:4}
Pass: in max2-exp.sl run ($+ $0 ($ite ($< $x $y) $y $x)) with {x:w2 y:5}; expected {r:5}, got {r:5}
Pass: in max2-exp.sl run ($+ $0 ($ite ($< $x $y) $y $x)) with {x:2 y:7}; expected {r:7}, got {r:7}
Pass: in max2-exp.sl run ($+ $0 ($ite ($< $x $y) $y $x)) with <synth-fun-soln>; expected True, got True
[All 13] tests passed for file max2-exp.sl
```

Lines 1-4 assert correct output from small test programs. Lines 5-8 and 9-13 check that each of a pair of specified programs is a valid solution; i.e., that they satisfy all constraints of the synthesis problem.

Equivalent information will be written to a `semgus-test.<datetime>.csv` file in your current working directory.

### Running a Solver

Next, run `semgus-cli solve Semgus-Benchmarks/basic/max2-exp.sl`.

You should see the following text output:

```
Starting BottomUpSolver with cost function Size
BottomUpSolver success (295 t, <duration>s)
Result program: ($ite ($< $x $y) $y $x)
```

Observe that the result program matches the expected solution.

The solver will generate two output files in which to store results: `semgus-solve.<datetime>.json` and `semgus-solve.<datetime>.csv`.
- The CSV file is a condensed summary of every synthesis task you have run. Each (file, solver) combination will produce a single row to indicate whether synthesis was successful, how much time was taken, and the syntax of the solution (if found).
- The JSON file contains detailed information about the work in this session, including the configuration parameters of each solver used and the time taken at each step in the synthesis process.

