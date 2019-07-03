//-----------------------------------------------------------------------------
// FILE:	    AssemblyInfo.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2017 by Neon Research, LLC.  All rights reserved.

using System.Reflection;
using System.Runtime.CompilerServices;

using Xunit;

[assembly: AssemblyTitle("Test.Neon.Stack.Couchbase")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration(Neon.Stack.Build.Configuration)]
[assembly: AssemblyCompany(Neon.Stack.Build.Company)]
[assembly: AssemblyProduct(Neon.Stack.Build.Product)]
[assembly: AssemblyCopyright(Neon.Stack.Build.Copyright)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion(Neon.Stack.Build.Version)]
[assembly: AssemblyFileVersion(Neon.Stack.Build.Version)]

// Prevent Xunit from running tests in parallel.

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
