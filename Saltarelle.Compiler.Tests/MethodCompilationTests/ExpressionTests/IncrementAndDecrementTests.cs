﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Saltarelle.Compiler.Tests.MethodCompilationTests.ExpressionTests {
	[TestFixture]
	public class IncrementAndDecrementTests : MethodCompilerTestBase {
		protected void AssertCorrectForBoth(string csharp, string expected, INamingConventionResolver namingConvention = null) {
			AssertCorrect(csharp, expected, namingConvention);
			AssertCorrect(csharp.Replace("+", "-"), expected.Replace("+", "-"), namingConvention);
		}

		[Test]
		public void PrefixWorksForLocalVariables() {
			AssertCorrectForBoth(
@"public void M() {
	int i = 0;
	// BEGIN
	++i;
	// END
}
",
@"	++$i;
");
		}

		[Test]
		public void PostfixWorksForLocalVariables() {
			AssertCorrectForBoth(
@"public void M() {
	int i = 0;
	// BEGIN
	i++;
	// END
}
",
@"	$i++;
");
		}

		[Test]
		public void PrefixForPropertyWithMethodsWorksWhenTheReturnValueIsNotUsed() {
			AssertCorrectForBoth(
@"public int P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	++P;
	// END
}",
@"	this.set_$P(this.get_$P() + 1);
");
		}

		[Test]
		public void PostfixForPropertyWithMethodsWorksWhenTheReturnValueIsNotUsed() {
			AssertCorrectForBoth(
@"public int P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	P++;
	// END
}",
@"	this.set_$P(this.get_$P() + 1);
");
		}

		[Test]
		public void PrefixForPropertyWithMethodsWorksWhenTheReturnValueIsUsed() {
			AssertCorrectForBoth(
@"public int P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	int j = ++P;
	// END
}",
@"	var $tmp1 = this.get_$P() + 1;
	this.set_$P($tmp1);
	var $j = $tmp1;
");
		}

		[Test]
		public void PostfixForPropertyWithMethodsWorksWhenTheReturnValueIsUsed() {
			AssertCorrectForBoth(
@"public int P { get; set; }
public void M() {
	// BEGIN
	int j = P++;
	// END
}",
@"	var $tmp1 = this.get_$P();
	this.set_$P($tmp1 + 1);
	var $j = $tmp1;
");
		}

		[Test]
		public void PrefixForPropertyWithMethodsOnlyInvokesTheTargetOnce() {
			AssertCorrectForBoth(
@"class X { public int P { get; set; } }
public X F() { return null; }
public void M() {
	// BEGIN
	++F().P;
	// END
}",
@"	var $tmp1 = this.$F();
	$tmp1.set_$P($tmp1.get_$P() + 1);
");
		}

		[Test]
		public void PostfixForPropertyWithMethodsOnlyInvokesTheTargetOnce() {
			AssertCorrectForBoth(
@"class X { public int P { get; set; } }
public X F() { return null; }
public void M() {
	// BEGIN
	F().P++;
	// END
}",
@"	var $tmp1 = this.$F();
	$tmp1.set_$P($tmp1.get_$P() + 1);
");
		}

		[Test]
		public void PrefixForPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"public int F { get; set; }
public void M() {
	// BEGIN
	++F;
	// END
}",
@"	++this.$F;
");
		}

		[Test]
		public void PostfixForPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"public int F { get; set; }
public void M() {
	// BEGIN
	F++;
	// END
}",
@"	this.$F++;
");
		}

		[Test]
		public void PrefixForPropertyWithFieldImplementationDoesNotGenerateTemporary() {
			AssertCorrectForBoth(
@"class X { public int F { get; set; } }
public X F() { return null; }
public void M() {
	// BEGIN
	++F().F;
	// END
}",
@"	++this.$F().$F;
");
		}

		[Test]
		public void PostfixForPropertyWithFieldImplementationDoesNotGenerateTemporary() {
			AssertCorrectForBoth(
@"class X { public int F { get; set; } }
public X F() { return null; }
public void M() {
	// BEGIN
	F().F++;
	// END
}",
@"	this.$F().$F++;
");
		}

		[Test]
		public void PrefixForStaticPropertyWithSetMethodWorks() {
			AssertCorrectForBoth(
@"static int P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	++P;
	// END
}",
@"	{C}.set_$P({C}.get_$P() + 1);
");
		}

		[Test]
		public void PostfixForStaticPropertyWithSetMethodWorks() {
			AssertCorrectForBoth(
@"static int P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	P++;
	// END
}",
@"	{C}.set_$P({C}.get_$P() + 1);
");
		}

		[Test]
		public void PrefixForStaticPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"static int F { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	++F;
	// END
}",
@"	++{C}.$F;
");
		}

		[Test]
		public void PostfixForStaticPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"static int F { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	F++;
	// END
}",
@"	{C}.$F++;
");
		}

		[Test]
		public void PrefixForIndexerWithSetMethodWorks() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	++this[i, j];
	// END
}",
@"	this.set_$Item($i, $j, this.get_$Item($i, $j) + 1);
");
		}

		[Test]
		public void PostfixForIndexerWithSetMethodWorks() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	this[i, j]++;
	// END
}",
@"	this.set_$Item($i, $j, this.get_$Item($i, $j) + 1);
");
		}

		[Test]
		public void PrefixForIndexerWithMethodsWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1, k;
	// BEGIN
	k = ++this[i, j];
	// END
}",
@"	var $tmp1 = this.get_$Item($i, $j) + 1;
	this.set_$Item($i, $j, $tmp1);
	$k = $tmp1;
");
		}

		[Test]
		public void PostfixForIndexerWithMethodsWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1, k;
	// BEGIN
	k = this[i, j]++;
	// END
}",
@"	var $tmp1 = this.get_$Item($i, $j);
	this.set_$Item($i, $j, $tmp1 + 1);
	$k = $tmp1;
");
		}

		[Test, Ignore("Enable when invocations fixed")]
		public void PrefixForIndexerWithMethodsWorksWhenReorderingArguments() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public int F1() { return 0; }
public int F2() { return 0; }
public void M() {
	// BEGIN
	int i = ++this[y: F1(), x: F2()];
	// END
}",
@"	var $tmp1 = this.F1();
	var $tmp2 = this.F2();
	var $tmp3 = this.get_$Item($tmp2, $tmp1) + 1;
	this.set_$Item($tmp2, $tmp1, $tmp3);
	var $i = $tmp3;
");
		}

		[Test, Ignore("Enable when invocations fixed")]
		public void PostfixForIndexerWithMethodsWorksWhenReorderingArguments() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public int F1() { return 0; }
public int F2() { return 0; }
public void M() {
	// BEGIN
	int i = this[y: F1(), x: F2()]++;
	// END
}",
@"	var $tmp1 = this.$F1();
	var $tmp2 = this.$F2();
	var $tmp3 = this.get_$Item($tmp2, $tmp1);
	this.set_$Item($tmp2, $tmp1, $tmp3 + 1);
	var $i = $tmp3;
");
		}

		[Test]
		public void PrefixForIndexerWithMethodsOnlyInvokesIndexingArgumentsOnceAndInTheCorrectOrder() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public int F1() { return 0; }
public int F2() { return 0; }
public void M() {
	int i = 0;
	// BEGIN
	++this[F1(), F2()];
	// END
}",
@"	var $tmp1 = this.$F1();
	var $tmp2 = this.$F2();
	this.set_$Item($tmp1, $tmp2, this.get_$Item($tmp1, $tmp2) + 1);
");
		}

		[Test]
		public void PostfixForIndexerWithMethodsOnlyInvokesIndexingArgumentsOnceAndInTheCorrectOrder() {
			AssertCorrectForBoth(
@"int this[int x, int y] { get { return 0; } set {} }
public int F1() { return 0; }
public int F2() { return 0; }
public void M() {
	int i = 0;
	// BEGIN
	this[F1(), F2()]++;
	// END
}",
@"	var $tmp1 = this.$F1();
	var $tmp2 = this.$F2();
	this.set_$Item($tmp1, $tmp2, this.get_$Item($tmp1, $tmp2) + 1);
");
		}

		[Test]
		public void PrefixForPropertyImplementedAsNativeIndexerWorks() {
			AssertCorrectForBoth(
@"int this[int x] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	j = ++this[i];
	// END
}",
@"	$j = ++this[$i];
", namingConvention: new MockNamingConventionResolver { GetPropertyImplementation = p => p.IsIndexer ? PropertyImplOptions.NativeIndexer() : PropertyImplOptions.Field(p.Name) });
		}

		[Test]
		public void PostfixForPropertyImplementedAsNativeIndexerWorks() {
			AssertCorrectForBoth(
@"int this[int x] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	j = this[i]++;
	// END
}",
@"	$j = this[$i]++;
", namingConvention: new MockNamingConventionResolver { GetPropertyImplementation = p => p.IsIndexer ? PropertyImplOptions.NativeIndexer() : PropertyImplOptions.Field(p.Name) });
		}

		[Test]
		public void PrefixForInstanceFieldWorks() {
			AssertCorrectForBoth(
@"int a;
public void M() {
	int i = 0;
	// BEGIN
	++a;
	// END
}",
@"	++this.$a;
");
		}

		[Test]
		public void PostfixForInstanceFieldWorks() {
			AssertCorrectForBoth(
@"int a;
public void M() {
	int i = 0;
	// BEGIN
	a++;
	// END
}",
@"	this.$a++;
");
		}

		[Test]
		public void PrefixForStaticFieldWorks() {
			AssertCorrectForBoth(
@"static int a;
public void M() {
	int i = 0;
	// BEGIN
	++a;
	// END
}",
@"	++{C}.$a;
");
		}

		[Test]
		public void PostfixForStaticFieldWorks() {
			AssertCorrectForBoth(
@"static int a;
public void M() {
	int i = 0;
	// BEGIN
	a++;
	// END
}",
@"	{C}.$a++;
");
		}

		[Test]
		public void LiftedPrefixWorksForLocalVariables() {
			AssertCorrectForBoth(
@"public void M() {
	int? i = 0;
	// BEGIN
	++i;
	// END
}
",
@"	$i = $Lift($i + 1);
");
		}

		[Test]
		public void LiftedPostfixWorksForLocalVariables() {
			AssertCorrectForBoth(
@"public void M() {
	int? i = 0;
	// BEGIN
	i++;
	// END
}
",
@"	$i = $Lift($i + 1);
");
		}

		[Test]
		public void LiftedPrefixWorksForLocalVariablesWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"public void M() {
	int? i = 0;
	// BEGIN
	var j = ++i;
	// END
}
",
@"	var $j = $i = $Lift($i + 1);
");
		}

		[Test]
		public void LiftedPostfixWorksForLocalVariablesWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"public void M() {
	int? i = 0;
	// BEGIN
	var j = i++;
	// END
}
",
@"	var $tmp1 = $i;
	$i = $Lift($tmp1 + 1);
	var $j = $tmp1;
");
		}
		[Test]
		public void LiftedPrefixForPropertyWithMethodsWorksWhenTheReturnValueIsNotUsed() {
			AssertCorrectForBoth(
@"public int? P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	++P;
	// END
}",
@"	this.set_$P($Lift(this.get_$P() + 1));
");
		}

		[Test]
		public void LiftedPostfixForPropertyWithMethodsWorksWhenTheReturnValueIsNotUsed() {
			AssertCorrectForBoth(
@"public int? P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	P++;
	// END
}",
@"	this.set_$P($Lift(this.get_$P() + 1));
");
		}

		[Test]
		public void LiftedPrefixForPropertyWithMethodsWorksWhenTheReturnValueIsUsed() {
			AssertCorrectForBoth(
@"public int? P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	int j = ++P;
	// END
}",
@"	var $tmp1 = $Lift(this.get_$P() + 1);
	this.set_$P($tmp1);
	var $j = $tmp1;
");
		}

		[Test]
		public void LiftedPostfixForPropertyWithMethodsWorksWhenTheReturnValueIsUsed() {
			AssertCorrectForBoth(
@"public int? P { get; set; }
public void M() {
	// BEGIN
	int j = P++;
	// END
}",
@"	var $tmp1 = this.get_$P();
	this.set_$P($Lift($tmp1 + 1));
	var $j = $tmp1;
");
		}

		[Test]
		public void LiftedPrefixForPropertyWithMethodsOnlyInvokesTheTargetOnce() {
			AssertCorrectForBoth(
@"class X { public int? P { get; set; } }
public X F() { return null; }
public void M() {
	// BEGIN
	++F().P;
	// END
}",
@"	var $tmp1 = this.$F();
	$tmp1.set_$P($Lift($tmp1.get_$P() + 1));
");
		}

		[Test]
		public void LiftedPostfixForPropertyWithMethodsOnlyInvokesTheTargetOnce() {
			AssertCorrectForBoth(
@"class X { public int? P { get; set; } }
public X F() { return null; }
public void M() {
	// BEGIN
	F().P++;
	// END
}",
@"	var $tmp1 = this.$F();
	$tmp1.set_$P($Lift($tmp1.get_$P() + 1));
");
		}

		[Test]
		public void LiftedPrefixForPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"public int? F { get; set; }
public void M() {
	// BEGIN
	++F;
	// END
}",
@"	this.$F = $Lift(this.$F + 1);
");
		}

		[Test]
		public void LiftedPostfixForPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"public int? F { get; set; }
public void M() {
	// BEGIN
	F++;
	// END
}",
@"	this.$F = $Lift(this.$F + 1);
");
		}

		[Test]
		public void LiftedPrefixForPropertyWithFieldImplementationWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"public int? F { get; set; }
public void M() {
	// BEGIN
	var x = ++F;
	// END
}",
@"	var $x = this.$F = $Lift(this.$F + 1);
");
		}

		[Test]
		public void LiftedPostfixForPropertyWithFieldImplementationWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"public int? F { get; set; }
public void M() {
	// BEGIN
	var x = F++;
	// END
}",
@"	var $tmp1 = this.$F;
	this.$F = $Lift($tmp1 + 1);
	var $x = $tmp1;
");
		}

		[Test]
		public void LiftedPrefixForStaticPropertyWithSetMethodWorks() {
			AssertCorrectForBoth(
@"static int? P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	++P;
	// END
}",
@"	{C}.set_$P($Lift({C}.get_$P() + 1));
");
		}

		[Test]
		public void LiftedPostfixForStaticPropertyWithSetMethodWorks() {
			AssertCorrectForBoth(
@"static int? P { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	P++;
	// END
}",
@"	{C}.set_$P($Lift({C}.get_$P() + 1));
");
		}

		[Test]
		public void LiftedPrefixForStaticPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"static int? F { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	++F;
	// END
}",
@"	{C}.$F = $Lift({C}.$F + 1);
");
		}

		[Test]
		public void LiftedPostfixForStaticPropertyWithFieldImplementationWorks() {
			AssertCorrectForBoth(
@"static int? F { get; set; }
public void M() {
	int i = 0;
	// BEGIN
	F++;
	// END
}",
@"	{C}.$F = $Lift({C}.$F + 1);
");
		}

		[Test]
		public void LiftedPrefixForIndexerWithSetMethodWorks() {
			AssertCorrectForBoth(
@"int? this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	++this[i, j];
	// END
}",
@"	this.set_$Item($i, $j, $Lift(this.get_$Item($i, $j) + 1));
");
		}

		[Test]
		public void LiftedPostfixForIndexerWithSetMethodWorks() {
			AssertCorrectForBoth(
@"int? this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	this[i, j]++;
	// END
}",
@"	this.set_$Item($i, $j, $Lift(this.get_$Item($i, $j) + 1));
");
		}

		[Test]
		public void LiftedPrefixForIndexerWithMethodsWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int? this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	var x = ++this[i, j];
	// END
}",
@"	var $tmp1 = $Lift(this.get_$Item($i, $j) + 1);
	this.set_$Item($i, $j, $tmp1);
	var $x = $tmp1;
");
		}

		[Test]
		public void LiftedPostfixForIndexerWithMethodsWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int? this[int x, int y] { get { return 0; } set {} }
public void M() {
	int i = 0, j = 1;
	// BEGIN
	var k = this[i, j]++;
	// END
}",
@"	var $tmp1 = this.get_$Item($i, $j);
	this.set_$Item($i, $j, $Lift($tmp1 + 1));
	var $k = $tmp1;
");
		}

		[Test]
		public void LiftedPrefixForIndexerWithMethodsOnlyInvokesIndexingArgumentsOnceAndInTheCorrectOrder() {
			AssertCorrectForBoth(
@"int? this[int x, int y] { get { return 0; } set {} }
public int F1() { return 0; }
public int F2() { return 0; }
public void M() {
	int i = 0;
	// BEGIN
	++this[F1(), F2()];
	// END
}",
@"	var $tmp1 = this.$F1();
	var $tmp2 = this.$F2();
	this.set_$Item($tmp1, $tmp2, $Lift(this.get_$Item($tmp1, $tmp2) + 1));
");
		}

		[Test]
		public void LiftedPostfixForIndexerWithMethodsOnlyInvokesIndexingArgumentsOnceAndInTheCorrectOrder() {
			AssertCorrectForBoth(
@"int? this[int x, int y] { get { return 0; } set {} }
public int F1() { return 0; }
public int F2() { return 0; }
public void M() {
	int i = 0;
	// BEGIN
	this[F1(), F2()]++;
	// END
}",
@"	var $tmp1 = this.$F1();
	var $tmp2 = this.$F2();
	this.set_$Item($tmp1, $tmp2, $Lift(this.get_$Item($tmp1, $tmp2) + 1));
");
		}

		[Test]
		public void LiftedPrefixForPropertyImplementedAsNativeIndexerWorks() {
			AssertCorrectForBoth(
@"int? this[int x] { get { return 0; } set {} }
public void M() {
	int i = 0;
	// BEGIN
	++this[i];
	// END
}",
@"	this[$i] = $Lift(this[$i] + 1);
", namingConvention: new MockNamingConventionResolver { GetPropertyImplementation = p => p.IsIndexer ? PropertyImplOptions.NativeIndexer() : PropertyImplOptions.Field(p.Name) });
		}

		[Test]
		public void LiftedPostfixForPropertyImplementedAsNativeIndexerWorks() {
			AssertCorrectForBoth(
@"int? this[int x] { get { return 0; } set {} }
public void M() {
	int i = 0;
	// BEGIN
	this[i]++;
	// END
}",
@"	this[$i] = $Lift(this[$i] + 1);
", namingConvention: new MockNamingConventionResolver { GetPropertyImplementation = p => p.IsIndexer ? PropertyImplOptions.NativeIndexer() : PropertyImplOptions.Field(p.Name) });
		}

		[Test]
		public void LiftedPrefixForPropertyImplementedAsNativeIndexerWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int? this[int x] { get { return 0; } set {} }
public void M() {
	int i = 0;
	// BEGIN
	var x = ++this[i];
	// END
}",
@"	var $x = this[$i] = $Lift(this[$i] + 1);
", namingConvention: new MockNamingConventionResolver { GetPropertyImplementation = p => p.IsIndexer ? PropertyImplOptions.NativeIndexer() : PropertyImplOptions.Field(p.Name) });
		}

		[Test]
		public void LiftedPostfixForPropertyImplementedAsNativeIndexerWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int? this[int x] { get { return 0; } set {} }
public void M() {
	int i = 0;
	// BEGIN
	var x = this[i]++;
	// END
}",
@"	var $tmp1 = this[$i];
	this[$i] = $Lift($tmp1 + 1);
	var $x = $tmp1;
", namingConvention: new MockNamingConventionResolver { GetPropertyImplementation = p => p.IsIndexer ? PropertyImplOptions.NativeIndexer() : PropertyImplOptions.Field(p.Name) });
		}

		[Test]
		public void LiftedPrefixForArrayAccessWorks() {
			AssertCorrectForBoth(
@"public void M() {
	int?[] arr = null;
	int i = 0;
	// BEGIN
	++arr[i];
	// END
}",
@"	$arr[$i] = $Lift($arr[$i] + 1);
");
		}

		[Test]
		public void LiftedPostfixForArrayAccessWorks() {
			AssertCorrectForBoth(
@"public void M() {
	int?[] arr = null;
	int i = 0;
	// BEGIN
	arr[i]++;
	// END
}",
@"	$arr[$i] = $Lift($arr[$i] + 1);
");
		}

		[Test]
		public void LiftedPrefixForArrayAccessWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"public void M() {
	int?[] arr = null;
	int i = 0;
	// BEGIN
	var x = ++arr[i];
	// END
}",
@"	var $x = $arr[$i] = $Lift($arr[$i] + 1);
");
		}

		[Test]
		public void LiftedPostfixForArrayAccessWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"public void M() {
	int?[] arr = null;
	int i = 0;
	// BEGIN
	var x = arr[i]++;
	// END
}",
@"	var $tmp1 = $arr[$i];
	$arr[$i] = $Lift($tmp1 + 1);
	var $x = $tmp1;
");
		}

		[Test]
		public void LiftedPrefixForInstanceFieldWorks() {
			AssertCorrectForBoth(
@"int? a;
public void M() {
	int i = 0;
	// BEGIN
	++a;
	// END
}",
@"	this.$a = $Lift(this.$a + 1);
");
		}

		[Test]
		public void LiftedPostfixForInstanceFieldWorks() {
			AssertCorrectForBoth(
@"int? a;
public void M() {
	int i = 0;
	// BEGIN
	a++;
	// END
}",
@"	this.$a = $Lift(this.$a + 1);
");
		}

		[Test]
		public void LiftedPrefixForInstanceFieldWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int? a;
public void M() {
	int i = 0;
	// BEGIN
	var x = ++a;
	// END
}",
@"	var $x = this.$a = $Lift(this.$a + 1);
");
		}

		[Test]
		public void LiftedPostfixForInstanceFieldWorksWhenUsingTheReturnValue() {
			AssertCorrectForBoth(
@"int? a;
public void M() {
	int i = 0;
	// BEGIN
	var x = a++;
	// END
}",
@"	var $tmp1 = this.$a;
	this.$a = $Lift($tmp1 + 1);
	var $x = $tmp1;
");
		}

		[Test]
		public void LiftedPrefixForStaticFieldWorks() {
			AssertCorrectForBoth(
@"static int? a;
public void M() {
	int i = 0;
	// BEGIN
	++a;
	// END
}",
@"	{C}.$a = $Lift({C}.$a + 1);
");
		}

		[Test]
		public void LiftedPostfixForStaticFieldWorks() {
			AssertCorrectForBoth(
@"static int? a;
public void M() {
	int i = 0;
	// BEGIN
	a++;
	// END
}",
@"	{C}.$a = $Lift({C}.$a + 1);
");
		}
	}
}
