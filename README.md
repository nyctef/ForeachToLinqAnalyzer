Experimental: Roslyn analyzer for converting foreach statements to LINQ method chains

Currently only deals with one or two specific cases - hoping to make more general

Interesting code:
 - [the bit that finds code to fix](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer/DiagnosticAnalyzer.cs)
 - [the bit that does the code fix](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer/CodeFixProvider.cs)
 - [the tests](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer.Test/UnitTests.cs)
 
Everything else is boilerplate
