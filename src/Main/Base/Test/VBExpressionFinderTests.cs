﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Dom.VBNet;

namespace ICSharpCode.SharpDevelop.Tests
{
	[TestFixture]
	public class VBExpressionFinderTests
	{
		const string program1 = @"
Imports System
Imports System.Linq
		
Class MainClass ' a comment
	Dim under_score_field As Integer
	Sub SomeMethod()
		simple += 1
		Dim text = ""Text""
		For Each loopVarName In collection
		Next
	End Sub
End Class
";
		
		const string program2 = @"
Class MainClass
	Sub A
		Console.WriteLine(""Hello World!"")
	End Sub
End Class
		";
		
		const string program3 = @"
Class MainClass
	Sub A
		Console.WriteLine
	End Sub
End Class
		";
		
		VBNetExpressionFinder ef;
		
		[SetUp]
		public void Init()
		{
			HostCallback.GetCurrentProjectContent = delegate {
				return ParserService.CurrentProjectContent;
			};
			
			ef = new VBNetExpressionFinder(null);
		}
		
		void FindFull(string program, string location, string expectedExpression, ExpressionContext expectedContext)
		{
			int pos = program.IndexOf(location);
			if (pos < 0) Assert.Fail("location not found in program");
			ExpressionResult er = ef.FindFullExpression(program, pos);
			Assert.AreEqual(expectedExpression, er.Expression);
			Assert.AreEqual(expectedContext.ToString(), er.Context.ToString());
		}
		
		void Find(string program, string location, int offset, string expectedExpression, ExpressionContext expectedContext)
		{
			int pos = program.IndexOf(location);
			if (pos < 0) Assert.Fail("location not found in program");
			ExpressionResult er = ef.FindExpression(program, pos + offset);
			Assert.AreEqual(expectedExpression, er.Expression);
			Assert.AreEqual(expectedContext.ToString(), er.Context.ToString());
		}
		
		#region Find
		[Test]
		public void FindSimple()
		{
			Find(program2, "sole", 0,"Con", ExpressionContext.Default);
		}
		
		[Test]
		public void FindSimple2()
		{
			Find(program2, "Wri", 0, "Console.", ExpressionContext.Default);
		}
		
		[Test]
		public void FindSimple3()
		{
			Find(program3, "WriteLine", "WriteLine".Length, "Console.WriteLine", ExpressionContext.Default);
		}
		
		[Test]
		public void FindAfterBrace()
		{
			Find(program2, "WriteLine", "WriteLine(".Length, "", ExpressionContext.Default);
		}
		
		[Test]
		public void ForEachLoop()
		{
			Find(program1, "loopVarName", 4, "loop", ExpressionContext.Default);
		}
		#endregion
		
		#region FindFull
		[Test]
		public void Simple()
		{
			FindFull(program1, "mple += 1", "simple", ExpressionContext.Default);
		}
		
		[Test]
		public void SimpleBeginningOfExpression()
		{
			FindFull(program1, "simple += 1", "simple", ExpressionContext.Default);
		}
		
		[Test]
		public void Underscore()
		{
			FindFull(program1, "der_score_field", "under_score_field", ExpressionContext.IdentifierExpected);
		}
		
		[Test]
		public void IdentifierBeforeKeyword()
		{
			FindFull(program1, "arName", "loopVarName", ExpressionContext.Default);
		}
		
		[Test]
		public void LocalVariableDecl()
		{
			FindFull(program1, "ext", "text", ExpressionContext.IdentifierExpected);
		}
		
		[Test]
		public void Imports1()
		{
			FindFull(program1, "ystem", "System", ExpressionContext.Global);
		}
		
		[Test]
		public void Imports2()
		{
			FindFull(program1, "inq", "System.Linq", ExpressionContext.Global);
		}
		
		[Test]
		public void ClassName()
		{
			FindFull(program1, "ainClas", "MainClass", ExpressionContext.IdentifierExpected);
		}
		
		[Test]
		public void SubName()
		{
			FindFull(program1, "omeMe", "SomeMethod", ExpressionContext.IdentifierExpected);
		}
		
		#region Old Tests
		void OldTest(string expr, int offset)
		{
			string body = @"Class Test
	Sub A
		{0}.AnotherField
	End Sub
End Class";
			Assert.AreEqual(expr, ef.FindFullExpression(string.Format(body, expr), @"Class Test
	Sub A
		".Length + offset).Expression);
		}
		
		void OldTestFind(string expr, string expected, int offset)
		{
			string body = @"Class Test
	Sub A
		Dim x = abc + {0}
	End Sub
End Class";
			Assert.AreEqual(expected, ef.FindExpression(string.Format(body, expr), @"Class Test
	Sub A
		Dim x = abc + ".Length + offset).Expression);
		}
		
		[Test]
		public void FieldReference()
		{
			OldTest("abc", 1);
			OldTest("abc.def", 6);
		}
		
		[Test]
		public void WithFieldReference()
		{
			OldTest(".abc", 2);
			OldTest(".abc.def", 7);
		}
		
		[Test]
		public void MethodCall()
		{
			OldTest("abc.Method().Method()", 16);
		}
		
		[Test]
		public void ComplexMethodCall()
		{
			OldTest("abc.Method().Method(5, a.b, 5 + a)", 16);
		}
		
		[Test]
		public void PlusExpression()
		{
			OldTestFind("def", "de", 2);
		}
		
		[Test]
		public void PlusExpression2()
		{
			OldTestFind("def", "", 0);
		}
		#endregion
		#endregion
	}
}
