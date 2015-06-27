[![Build status](https://ci.appveyor.com/api/projects/status/92aq5tbtbow7oibh/branch/master?svg=true)](https://ci.appveyor.com/project/nyctef/foreachtolinqanalyzer/branch/master)


Experimental: Roslyn analyzer for converting foreach statements to LINQ method chains

Currently only deals with one or two specific cases - hoping to make more general

Interesting code:
 - [the bit that finds code to fix](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer/DiagnosticAnalyzer.cs)
 - [the bit that does the code fix](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer/CodeFixProvider.cs)
 - the tests: [TurnsIfStatementsIntoWhereCalls](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer.Test/TurnsIfStatementsIntoWhereCalls.cs) / [TurnsSingleAssignmentIntoSelectCalls](ForeachToLinqAnalyzer/ForeachToLinqAnalyzer.Test/TurnsSingleAssignmentIntoSelectCalls.cs)
 
Everything else is boilerplate
