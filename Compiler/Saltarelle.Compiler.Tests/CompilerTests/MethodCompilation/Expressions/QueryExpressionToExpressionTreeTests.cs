﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Saltarelle.Compiler.Roslyn;
using Saltarelle.Compiler.ScriptSemantics;

namespace Saltarelle.Compiler.Tests.CompilerTests.MethodCompilation.Expressions {
	[TestFixture]
	public class QueryExpressionToExpressionTreeTests : MethodCompilerTestBase {
		private MockMetadataImporter CreateDefaultMetadataImporter() {
			return new MockMetadataImporter {
				GetMethodSemantics = m => {
					if (m.ContainingType.FullyQualifiedName() == "System.Linq.Enumerable") {
						if (m.Name == "Cast") {
							return MethodScriptSemantics.InlineCode("{" + m.Parameters[0].Name + "}.$Cast({" + m.TypeParameters[0].Name + "})");
						}
						else {
							return MethodScriptSemantics.InlineCode("{" + m.Parameters[0].Name + "}.$" + m.Name + "(" + string.Join(", ", m.Parameters.Skip(1).Select(p => "{" + p.Name + "}")) + ")");
						}
					}
					else {
						return MethodScriptSemantics.NormalMethod("$" + m.Name, ignoreGenericArguments: true);
					}
				},
				GetTypeSemantics = t => TypeScriptSemantics.NormalType(t.Name, ignoreGenericArguments: true)
			};
		}

		private static readonly Lazy<MetadataReference> _mscorlibLazy = new Lazy<MetadataReference>(() => new MetadataFileReference(typeof(object).Assembly.Location));
		private static readonly Lazy<MetadataReference[]> _referencesLazy = new Lazy<MetadataReference[]>(() => new[] { _mscorlibLazy.Value, Common.ExpressionAssembly });

		private void AssertCorrect(string csharp, string expected, IMetadataImporter metadataImporter = null) {
			AssertCorrect(@"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
interface IOrderedEnumerable<T> : IEnumerable<T> {}
interface IGrouping<K,T> : IEnumerable<T> {
	K Key { get; }
}
static class Enumerable {
	public static IEnumerable<T> Cast<T>(this IEnumerable obj) { return null; }
	public static IEnumerable<T> Where<T>(this IEnumerable<T> obj, Expression<Func<T,bool>> predicate) { return null; }
	public static IEnumerable<U> Select<T,U>(this IEnumerable<T> obj, Expression<Func<T,U>> selector) { return null; }
	public static IEnumerable<V> SelectMany<T,U,V>(this IEnumerable<T> obj, Expression<Func<T,IEnumerable<U>>> selector, Expression<Func<T,U,V>> resultSelector) { return null; }
	public static IEnumerable<V> Join<T,U,K,V>(this IEnumerable<T> obj, IEnumerable<U> inner, Expression<Func<T,K>> outerKeySelector, Expression<Func<U,K>> innerKeySelector, Expression<Func<T,U,V>> resultSelector) { return null; }
	public static IEnumerable<V> GroupJoin<T,U,K,V>(this IEnumerable<T> obj, IEnumerable<U> inner, Expression<Func<T,K>> outerKeySelector, Expression<Func<U,K>> innerKeySelector, Expression<Func<T,IEnumerable<U>,V>> resultSelector) { return null; }
	public static IOrderedEnumerable<T> OrderBy<T,K>(this IEnumerable<T> obj, Expression<Func<T,K>> keySelector) { return null; }
	public static IOrderedEnumerable<T> OrderByDescending<T,K>(this IEnumerable<T> obj, Expression<Func<T,K>> keySelector) { return null; }
	public static IOrderedEnumerable<T> ThenBy<T,K>(this IOrderedEnumerable<T> obj, Expression<Func<T,K>> keySelector) { return null; }
	public static IOrderedEnumerable<T> ThenByDescending<T,K>(this IOrderedEnumerable<T> obj, Expression<Func<T,K>> keySelector) { return null; }
	public static IEnumerable<IGrouping<K,T>> GroupBy<T,K>(this IEnumerable<T> obj, Expression<Func<T,K>> keySelector) { return null; }
	public static IEnumerable<IGrouping<K,E>> GroupBy<T,K,E>(this IEnumerable<T> obj, Expression<Func<T,K>> keySelector, Expression<Func<T,E>> elementSelector) { return null; }
}
class C {
	" + csharp + @"
}", expected, references: _referencesLazy.Value, addSkeleton: false, metadataImporter: metadataImporter ?? CreateDefaultMetadataImporter(), runtimeLibrary: new MockRuntimeLibrary { Upcast = (e, _1, _2, _) => e }, collapseWhitespace: true);

		}

		[Test]
		public void QueryExpressionWithFromAndSelectWorks() {
			AssertCorrect(@"
void M() {
	string[] args = null;
	// BEGIN
	var result = from a in args select int.Parse(a);
	// END
}",
@"	var $tmp1 = {sm_Expression}.$Parameter({sm_String}, '$a');
	var $result = {sm_Enumerable}.$Select(
		$args,
		{sm_Expression}.$Lambda(
			{sm_Expression}.$Call(
				null,
				$GetMember({to_Int32}, 'Parse'),
				[$tmp1]
			),
			[$tmp1]
		)
	);
");
		}

		[Test]
		public void StatementLambdaInsideQueryExpression() {
			Assert.Fail("TODO");
			AssertCorrect(@"
static int M(Func<string> f) { return 0; }
void M() {
	string[] args = null;
	// BEGIN
	var result = from a in args let b = a + ""X"" select M(() => { string c = a + b; return c; });
	// END
}",
@"	var $result = $args.$Select(function($a) {
		return { $a: $a, $b: $a + 'X' };
	}).$Select(function($tmp1) {
		return {sm_C}.$M(function() {
			var $c = $tmp1.$a + $tmp1.$b;
			return $c;
		});
	});
");
		}

		[Test]
		public void QueryExpressionWithSingleFromAndExplicitTypeWorks() {
			AssertCorrect(@"
void M() {
	object[] args = null;
	// BEGIN
	var result = from string a in args select int.Parse(a);
	// END
}",
@"	var $tmp1 = {sm_Expression}.$Parameter({sm_String}, '$a');
	var $result = {sm_Enumerable}.$Select(
		{sm_Enumerable}.$Cast($args),
		{sm_Expression}.$Lambda(
			{sm_Expression}.$Call(
				null,
				$GetMember({to_Int32}, 'Parse'),
				[$tmp1]
			),
			[$tmp1]
		)
	);
");
		}

		[Test]
		public void QueryExpressionWithLetWorks() {
			AssertCorrect(@"
void M() {
	string[] args = null;
	// BEGIN
	var result = from a in args let b = int.Parse(a) select a + b.ToString();
	// END
}",
@"	var $tmp2 = $GetTransparentType({sm_String}, '$a', {sm_Int32}, '$b');
	var $tmp3 = {sm_Expression}.$Parameter({sm_String}, '$a');
	var $tmp4 = {sm_Expression}.$Parameter($tmp2, '$tmp1');
	var $result =
	{sm_Enumerable}.$Select(
		{sm_Enumerable}.$Select(
			$args,
			{sm_Expression}.$Lambda(
				{sm_Expression}.$New(
					$GetTransparentTypeConstructor($tmp2),
					[$tmp3, {sm_Expression}.$Call(null, $GetMember({to_Int32}, 'Parse'), [$tmp3])],
					[$GetTransparentTypeMember($tmp2, '$a'), $GetTransparentTypeMember($tmp2, '$b')]
				),
				[$tmp3]
			)
		),
		{sm_Expression}.$Lambda(
			{sm_Expression}.$Add(
				{sm_Expression}.$Property($tmp4, $GetTransparentTypeMember($tmp2, '$a')),
				{sm_Expression}.$Call(
					{sm_Expression}.$Property($tmp4, $GetTransparentTypeMember($tmp2, '$b')),
					$GetMember({to_Int32}, 'ToString'),
					[]
				),
				{sm_String}
			),
			[$tmp4]
		)
	);
");
		}

		[Test]
		public void QueryExpressionWithLetWithBindThisToFirstArgumentWorks() {
			Assert.Fail("TODO");
			var metadataImporter = CreateDefaultMetadataImporter();
			metadataImporter.GetDelegateSemantics = d => new DelegateScriptSemantics(bindThisToFirstParameter: true);
			AssertCorrect(@"
void M() {
	string[] args = null;
	// BEGIN
	var result = from a in args let b = int.Parse(a) select a + b.ToString();
	// END
}",
@"	var $result = $args.$Select($BindFirstParameterToThis(function($a) {
		return { $a: $a, $b: {sm_Int32}.$Parse($a) };
	})).$Select($BindFirstParameterToThis(function($tmp1) {
		return $tmp1.$a + $tmp1.$b.$ToString();
	}));
", metadataImporter);
		}

		[Test]
		public void QueryExpressionWithTwoLetsWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	string[] args = null;
	// BEGIN
	var result = from a in args let b = int.Parse(a) let c = b + 1 select a + b.ToString() + c.ToString();
	// END
}",
@"	var $result = $args.$Select(function($a) {
		return { $a: $a, $b: {sm_Int32}.$Parse($a) };
	}).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $c: $tmp1.$b + 1 };
	}).$Select(function($tmp2) {
		return $tmp2.$tmp1.$a + $tmp2.$tmp1.$b.$ToString() + $tmp2.$c.$ToString();
	});
");
		}

		[Test]
		public void TwoFromClausesFollowedBySelectWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 select i + j;
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, function($i, $j) {
		return $i + $j;
	});
");
		}

		[Test]
		public void CastInSecondFromClauseWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from int j in arr2 select i + j;
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2.$Cast({ga_Int32});
	}, function($i, $j) {
		return $i + $j;
	});
");
		}

		[Test]
		public void TwoFromClausesFollowedByLetWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 let k = i + j select i + j + k;
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, function($i, $j) {
		return { $i: $i, $j: $j };
	}).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $k: $tmp1.$i + $tmp1.$j };
	}).$Select(function($tmp2) {
		return $tmp2.$tmp1.$i + $tmp2.$tmp1.$j + $tmp2.$k;
	});
");
		}

		[Test]
		public void TwoFromClausesFollowedByLetWorksWithBindThisToFirstParameter() {
			Assert.Fail("TODO");
			var metadataImporter = CreateDefaultMetadataImporter();
			metadataImporter.GetDelegateSemantics = d => new DelegateScriptSemantics(bindThisToFirstParameter: d.DelegateInvokeMethod.Parameters.Length == 2);

			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 let k = i + j select i + j + k;
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, $BindFirstParameterToThis(function($i, $j) {
		return { $i: $i, $j: $j };
	})).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $k: $tmp1.$i + $tmp1.$j };
	}).$Select(function($tmp2) {
		return $tmp2.$tmp1.$i + $tmp2.$tmp1.$j + $tmp2.$k;
	});
", metadataImporter);
		}

		[Test]
		public void SelectManyFollowedBySelectWorksWhenTheTargetIsTransparentAndTheCollectionsAreCorrelated() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C1 {
	public int[] Result;
	public int X;
}
C1 F(int i) {
	return null;
}

void M() {
	int[] outer = null;
	// BEGIN
	var result = from i in outer let j = F(i) from k in j.Result select i + j.X + k;
	// END
}",
@"	var $result = $outer.$Select($Bind(function($i) {
		return { $i: $i, $j: this.$F($i) };
	}, this)).$SelectMany(function($tmp1) {
		return $tmp1.$j.$Result;
	}, function($tmp1, $k) {
		return $tmp1.$i + $tmp1.$j.$X + $k;
	});
");
		}

		[Test]
		public void SelectManyFollowedByLetWorksWhenTheTargetIsTransparentAndTheCollectionsAreCorrelated() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C1 {
	public int[] Result;
	public int X;
}
C1 F(int i) {
	return null;
}

void M() {
	int[] outer = null;
	// BEGIN
	var result = from i in outer let j = F(i) from k in j.Result let l = i + j.X + k select i + j.X + k + l;
	// END
}",
@"	var $result = $outer.$Select($Bind(function($i) {
		return { $i: $i, $j: this.$F($i) };
	}, this)).$SelectMany(function($tmp1) {
		return $tmp1.$j.$Result;
	}, function($tmp1, $k) {
		return { $tmp1: $tmp1, $k: $k };
	}).$Select(function($tmp2) {
		return { $tmp2: $tmp2, $l: $tmp2.$tmp1.$i + $tmp2.$tmp1.$j.$X + $tmp2.$k };
	}).$Select(function($tmp3) {
		return $tmp3.$tmp2.$tmp1.$i + $tmp3.$tmp2.$tmp1.$j.$X + $tmp3.$tmp2.$k + $tmp3.$l;
	});
");
		}

		[Test]
		public void ThreeFromClausesFollowedBySelectWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null, arr3 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 from k in arr3 select i + j + k;
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, function($i, $j) {
		return { $i: $i, $j: $j };
	}).$SelectMany(function($tmp1) {
		return $arr3;
	}, function($tmp1, $k) {
		return $tmp1.$i + $tmp1.$j + $k;
	});
");
		}

		[Test]
		public void GroupByWithSimpleValue() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C1 { public int field; }

void M() {
	C1[] arr = null;
	// BEGIN
	var result = from i in arr group i by i.field;
	// END
}",
@"	var $result = $arr.$GroupBy(function($i) {
		return $i.$field;
	});
");
		}

		[Test]
		public void GroupByWithProjectedValue() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C1 { public int field, something; }

void M() {
	C1[] arr = null;
	// BEGIN
	var result = from i in arr group i.something by i.field;
	// END
}",
@"	var $result = $arr.$GroupBy(function($i) {
		return $i.$field;
	}, function($i) {
		return $i.$something;
	});
");
		}

		[Test]
		public void GroupByWhenThereIsATransparentIdentifer() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C1 { public int field; }

int F(C1 x) { return 0; }

void M() {
	C1[] arr = null;
	// BEGIN
	var result = from i in arr let j = F(i) group i by i.field;
	// END
}",
@"	var $result = $arr.$Select($Bind(function($i) {
		return { $i: $i, $j: this.$F($i) };
	}, this)).$GroupBy(function($tmp1) {
		return $tmp1.$i.$field;
	}, function($tmp1) {
		return $tmp1.$i;
	});
");
		}

		[Test]
		public void JoinWithTypeCast() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CI { public int keyi, valuei; }
class CJ { public int keyj, valuej; }

void M() {
	CI[] arr1 = null;
	object[] arr2 = null;
	// BEGIN
	var result = from i in arr1 join CJ j in arr2 on i.keyi equals j.keyj select i.valuei + j.valuej;
	// END
}",
@"	var $result = $arr1.$Join($arr2.$Cast({ga_CJ}), function($i) {
		return $i.$keyi;
	}, function($j) {
		return $j.$keyj;
	}, function($i, $j) {
		return $i.$valuei + $j.$valuej;
	});
");
		}

		[Test]
		public void JoinFollowedBySelect() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CI { public int keyi, valuei; }
class CJ { public int keyj, valuej; }

void M() {
	CI[] arr1 = null;
	CJ[] arr2 = null;
	// BEGIN
	var result = from i in arr1 join j in arr2 on i.keyi equals j.keyj select i.valuei + j.valuej;
	// END
}",
@"	var $result = $arr1.$Join($arr2, function($i) {
		return $i.$keyi;
	}, function($j) {
		return $j.$keyj;
	}, function($i, $j) {
		return $i.$valuei + $j.$valuej;
	});
");
		}

		[Test]
		public void JoinFollowedByLet() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CI { public int keyi, valuei; }
class CJ { public int keyj, valuej; }

void M() {
	CI[] arr1 = null;
	CJ[] arr2 = null;
	// BEGIN
	var result = from i in arr1 join j in arr2 on i.keyi equals j.keyj let k = i.valuei + j.valuej select i.valuei + j.valuej + k;
	// END
}",
@"	var $result = $arr1.$Join($arr2, function($i) {
		return $i.$keyi;
	}, function($j) {
		return $j.$keyj;
	}, function($i, $j) {
		return { $i: $i, $j: $j };
	}).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $k: $tmp1.$i.$valuei + $tmp1.$j.$valuej };
	}).$Select(function($tmp2) {
		return $tmp2.$tmp1.$i.$valuei + $tmp2.$tmp1.$j.$valuej + $tmp2.$k;
	});
");
		}

		[Test]
		public void JoinFollowedBySelectWhenThereIsATransparentIdentifier() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CJ { public int keyj, valuej; }
class CK { public int keyk, valuek; }
CJ F(int i) { return null; }

void M() {
	int[] arr1 = null;
	CK[] arr2 = null;
	// BEGIN
	var result = from i in arr1 let j = F(i) join k in arr2 on j.keyj equals k.keyk select i + j.valuej + k.valuek;
	// END
}",
@"	var $result = $arr1.$Select($Bind(function($i) {
		return { $i: $i, $j: this.$F($i) };
	}, this)).$Join($arr2, function($tmp1) {
		return $tmp1.$j.$keyj;
	}, function($k) {
		return $k.$keyk;
	}, function($tmp1, $k) {
		return $tmp1.$i + $tmp1.$j.$valuej + $k.$valuek;
	});
");
		}

		[Test]
		public void GroupJoinFollowedBySelect() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CI { public int keyi, valuei; }
class CJ { public int keyj, valuej; }
static int F(CI i, IEnumerable<CJ> g) { return 0; }

void M() {
	CI[] arr1 = null;
	CJ[] arr2 = null;
	// BEGIN
	var result = from i in arr1 join j in arr2 on i.keyi equals j.keyj into g select F(i, g);
	// END
}",
@"	var $result = $arr1.$GroupJoin($arr2, function($i) {
		return $i.$keyi;
	}, function($j) {
		return $j.$keyj;
	}, function($i, $g) {
		return {sm_C}.$F($i, $g);
	});
");
		}

		[Test]
		public void GroupJoinFollowedByLet() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CI { public int keyi, valuei; }
class CJ { public int keyj, valuej; }
static int F(CI i, IEnumerable<CJ> j) { return 0; }

void M() {
	CI[] arr1 = null;
	CJ[] arr2 = null;
	int[] outer = null;
	// BEGIN
	var result = from i in arr1 join j in arr2 on i.keyi equals j.keyj into g let k = F(i, g) select F(i, g) + k;
	// END
}",
@"	var $result = $arr1.$GroupJoin($arr2, function($i) {
		return $i.$keyi;
	}, function($j) {
		return $j.$keyj;
	}, function($i, $g) {
		return { $i: $i, $g: $g };
	}).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $k: {sm_C}.$F($tmp1.$i, $tmp1.$g) };
	}).$Select(function($tmp2) {
		return {sm_C}.$F($tmp2.$tmp1.$i, $tmp2.$tmp1.$g) + $tmp2.$k;
	});
");
		}

		[Test]
		public void GroupJoinFollowedBySelectWhenThereIsATransparentIdentifier() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class CJ { public int keyj; }
class CK { public int keyk; }

static CJ F1(int i) { return null; }
static int F2(int i, CJ j, IEnumerable<CK> k) { return 0; }

void M() {
	int[] arr1 = null;
	CK[] arr2 = null;
	// BEGIN
	var result = from i in arr1 let j = F1(i) join k in arr2 on j.keyj equals k.keyk into g select F2(i, j, g);
	// END
}",
@"	var $result = $arr1.$Select(function($i) {
		return { $i: $i, $j: {sm_C}.$F1($i) };
	}).$GroupJoin($arr2, function($tmp1) {
		return $tmp1.$j.$keyj;
	}, function($k) {
		return $k.$keyk;
	}, function($tmp1, $g) {
		return {sm_C}.$F2($tmp1.$i, $tmp1.$j, $g);
	});
");
		}

		[Test]
		public void WhereWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr = null;
	// BEGIN
	var result = from i in arr where i > 5 select i + 1;
	// END
}",
@"	var $result = $arr.$Where(function($i) {
		return $i > 5;
	}).$Select(function($i) {
		return $i + 1;
	});
");
		}

		[Test]
		public void WhereWorksWhenThereIsATransparentIdentifier() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr = null;
	// BEGIN
	var result = from i in arr let j = i + 1 where i > j select i + j;
	// END
}",
@"	var $result = $arr.$Select(function($i) {
		return { $i: $i, $j: $i + 1 };
	}).$Where(function($tmp1) {
		return $tmp1.$i > $tmp1.$j;
	}).$Select(function($tmp1) {
		return $tmp1.$i + $tmp1.$j;
	});
");
		}

		[Test]
		public void TrivialSelectIsEliminatedAfterWhere() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr = null;
	// BEGIN
	var result = from i in arr where i > 5 select i;
	// END
}",
@"	var $result = $arr.$Where(function($i) {
		return $i > 5;
	});
");
		}

		[Test]
		public void TrivialSelectIsNotEliminatedWhenTheOnlyOperation() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr = null;
	// BEGIN
	var result = from i in arr select i;
	// END
}",
@"	var $result = $arr.$Select(function($i) {
		return $i;
	});
");
		}

		[Test]
		public void OrderingWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C1 { public int field1; }

void M() {
	C1[] arr = null;
	// BEGIN
	var result = from i in arr orderby i.field1 select i;
	// END
}",
@"	var $result = $arr.$OrderBy(function($i) {
		return $i.$field1;
	});
");
		}

		[Test]
		public void OrderingWorksWhenThereIsATransparentIdentifier() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr = null;
	// BEGIN
	var result = from i in arr let j = i + 1 orderby i + j select i;
	// END
}",
@"	var $result = $arr.$Select(function($i) {
		return { $i: $i, $j: $i + 1 };
	}).$OrderBy(function($tmp1) {
		return $tmp1.$i + $tmp1.$j;
	}).$Select(function($tmp1) {
		return $tmp1.$i;
	});
");
		}

		[Test]
		public void ThenByWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C2 { public int field1, field2; }
void M() {
	C2[] arr = null;
	// BEGIN
	var result = from i in arr orderby i.field1, i.field2 select i;
	// END
}",
@"	var $result = $arr.$OrderBy(function($i) {
		return $i.$field1;
	}).$ThenBy(function($i) {
		return $i.$field2;
	});
");
		}

		[Test]
		public void OrderingDescendingWorks() {
			Assert.Fail("TODO");
			AssertCorrect(@"
class C2 { public int field1, field2; }
void M() {
	C2[] arr = null;
	// BEGIN
	var result = from i in arr orderby i.field1 descending, i.field2 descending select i;
	// END
}",
@"	var $result = $arr.$OrderByDescending(function($i) {
		return $i.$field1;
	}).$ThenByDescending(function($i) {
		return $i.$field2;
	});
");
		}

		[Test]
		public void QueryContinuation() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 select i + j into a where a > 5 select a + 1;
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, function($i, $j) {
		return $i + $j;
	}).$Where(function($a) {
		return $a > 5;
	}).$Select(function($a) {
		return $a + 1;
	});
");
		}

		[Test]
		public void NestedQueries() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 let l = new { i, j } group l by l.i into g select new { g.Key, a = from q in g select new { q.i, q.j } };
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, function($i, $j) {
		return { $i: $i, $j: $j };
	}).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $l: { $i: $tmp1.$i, $j: $tmp1.$j } };
	}).$GroupBy(function($tmp2) {
		return $tmp2.$l.$i;
	}, function($tmp2) {
		return $tmp2.$l;
	}).$Select(function($g) {
		return { $Key: $g.get_Key(), $a: $g.$Select(function($q) {
			return { $i: $q.$i, $j: $q.$j };
		}) };
	});
");
		}

		[Test]
		public void NestedQueryUsingRangeVariableFromOuter() {
			Assert.Fail("TODO");
			AssertCorrect(@"
void M() {
	int[] arr1 = null, arr2 = null;
	// BEGIN
	var result = from i in arr1 from j in arr2 let k = new[] { i, j } select (from l in k let m = l + 1 select l + m + i);
	// END
}",
@"	var $result = $arr1.$SelectMany(function($i) {
		return $arr2;
	}, function($i, $j) {
		return { $i: $i, $j: $j };
	}).$Select(function($tmp1) {
		return { $tmp1: $tmp1, $k: [$tmp1.$i, $tmp1.$j] };
	}).$Select(function($tmp2) {
		return $tmp2.$k.$Select(function($l) {
			return { $l: $l, $m: $l + 1 };
		}).$Select(function($tmp3) {
			return $tmp3.$l + $tmp3.$m + $tmp2.$tmp1.$i;
		});
	});
");
		}

		[Test]
		public void RangeVariablesAreNotInScopeInJoinEquals() {
			Assert.Fail("TODO");
			AssertCorrect(@"
int b;
void M() {
	int[] arr = null;
	// BEGIN
	var result = from a in arr let a2 = a select (from b in arr let b2 = b join c in arr on b equals b + a into g select g);
	// END
}",
@"	var $result = $arr.$Select(function($a) {
		return { $a: $a, $a2: $a };
	}).$Select($Bind(function($tmp1) {
		return $arr.$Select(function($b) {
			return { $b: $b, $b2: $b };
		}).$GroupJoin($arr, function($tmp2) {
			return $tmp2.$b;
		}, $Bind(function($c) {
			return this.$b + $tmp1.$a;
		}, this), function($tmp2, $g) {
			return $g;
		});
	}, this));
");
		}

		[Test]
		public void ExpressionAreEvaluatedInTheCorrectOrderWhenAJoinClauseRequiresAdditionalStatements() {
			Assert.Fail("TODO");
			AssertCorrect(@"
static int[] F1() { return null; }
static int[] F2() { return null; }
static int[] P { get; set; }

void M() {
	// BEGIN
	var result = from a in F1() join b in (P = F2()) on a equals b select a + b;
	// END
}",
@"	var $result = {sm_C}.$F1().$Join((function() {
		var $tmp2 = {sm_C}.$F2();
		{sm_C}.set_P($tmp2);
		return $tmp2;
	})(), function($a) {
		return $a;
	}, function($b) {
		return $b;
	}, function($a, $b) {
		return $a + $b;
	});
");
		}

		[Test]
		public void ExpressionAreEvaluatedInTheCorrectOrderWhenAJoinClauseRequiresAdditionalStatementsWithRequiresContext() {
			Assert.Fail("TODO");
			AssertCorrect(@"
int[] F1() { return null; }
int[] F2(int x) { return null; }
int[] P { get; set; }

void F(ref int x) {}

void M() {
	int c = 0;
	F(ref c);
	// BEGIN
	var result = from a in F1() join b in (P = F2(c)) on a equals b select a + b;
	// END
}",
@"	var $result = this.$F1().$Join($Bind(function() {
		var $tmp2 = this.$this;
		var $tmp3 = this.$this.$F2(this.$c.$);
		$tmp2.set_P($tmp3);
		return $tmp3;
	}, { $c: $c, $this: this })(), function($a) {
		return $a;
	}, function($b) {
		return $b;
	}, function($a, $b) {
		return $a + $b;
	});
");

		}
	}
}