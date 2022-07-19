using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MovegenGenerator
{
    [Generator]
    public class MovegenGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not MovegenFuncFinder receiver)
            {
                return;
            }
            GenerateAttributeClass(context);
            var sb = new StringBuilder();
            sb.AppendLine(@"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Numerics;

namespace ShogiLibSharp
{
    public static partial class Movegen
    {
");
            foreach (var (method, makePublic) in receiver.Targets)
            {
                var oldMethodName = method.Identifier.ToString();
                var newMethodName = oldMethodName
                    .Substring(0, oldMethodName.Length - 4); // Impl で終わるメソッド名なはずなので、それを削除
                var modifiers = method.Modifiers.ToString();
                if (makePublic && modifiers.Contains("private"))
                {
                    modifiers = modifiers.Replace("private", "public");
                }
                sb.AppendLine($"        {modifiers} partial {method.ReturnType} {newMethodName}{method.ParameterList}");
                GenerateCode(context, sb, method.Body, "        ");
                sb.AppendLine();
            }

            sb.AppendLine(@"
    }
}
");
            context.AddSource("Movegen.g.cs", sb.ToString());
        }

        private void GenerateCode(GeneratorExecutionContext context, StringBuilder sb, SyntaxNode syntaxNode, string indent = "")
        {
            if (syntaxNode is BlockSyntax)
            {
                sb.AppendLine(indent + "{");
                foreach (var childNode in syntaxNode.ChildNodes())
                {
                    GenerateCode(context, sb, childNode, indent + "    ");
                }
                sb.AppendLine(indent + "}");
            }
            else if (syntaxNode is ForEachStatementSyntax foreachNode)
            {
                var type = context.Compilation
                    .GetSemanticModel(foreachNode.Expression.SyntaxTree)
                    .GetTypeInfo(foreachNode.Expression);

                if (type.Type.ToString() == "ShogiLibSharp.Bitboard")
                {
                    var nest = indent.Length;
                    sb.AppendLine(@$"
{indent}{{
{indent}    var __bb{nest} = {foreachNode.Expression};
{indent}    var __x{nest} = __bb{nest}.Lower();
{indent}    while (__x{nest} != 0UL)
{indent}    {{
{indent}        var {foreachNode.Identifier} = BitOperations.TrailingZeroCount(__x{nest});
");

                    GenerateCode(context, sb, foreachNode.Statement, indent + "        ");

                    sb.AppendLine(@$"
{indent}        __x{nest} &= __x{nest} - 1UL;
{indent}    }}
{indent}    __x{nest} = __bb{nest}.Upper();
{indent}    while (__x{nest} != 0UL)
{indent}    {{
{indent}        var {foreachNode.Identifier} = BitOperations.TrailingZeroCount(__x{nest}) + 63;
");

                    GenerateCode(context, sb, foreachNode.Statement, indent + "        ");

                    sb.AppendLine(@$"
{indent}        __x{nest} &= __x{nest} - 1UL;
{indent}    }}
{indent}}}
");
                }
                else
                {
                    sb.AppendLine(indent + $"foreach ({foreachNode.Type} {foreachNode.Identifier} in {foreachNode.Expression})");
                    GenerateCode(context, sb, foreachNode.Statement, indent);
                }
            }
            else if (syntaxNode is IfStatementSyntax ifNode)
            {
                sb.AppendLine(indent + $"if ({ifNode.Condition})");
                GenerateCode(context, sb, ifNode.Statement, indent);
                if (ifNode.Else != null)
                {
                    sb.AppendLine(indent + "else");
                    GenerateCode(context, sb, ifNode.Else.Statement, indent);
                }
            }
            else if (syntaxNode is ForStatementSyntax forNode)
            {
                sb.AppendLine(indent + $"for ({forNode.Initializers}; {forNode.Condition}; {forNode.Incrementors})");
                GenerateCode(context, sb, forNode.Statement, indent);
            }
            else if (syntaxNode is WhileStatementSyntax whileNode)
            {
                sb.AppendLine(indent + $"while ({whileNode.Condition})");
                GenerateCode(context, sb, whileNode.Statement, indent);
            }
            else if (syntaxNode is SwitchStatementSyntax switchNode)
            {
                sb.AppendLine(indent + $"switch ({switchNode.Expression})");
                sb.AppendLine(indent + "{");
                foreach (var sectionNode in switchNode.Sections)
                {
                    sb.AppendLine(indent + "    " + sectionNode.Labels);
                    foreach (var statementNode in sectionNode.Statements)
                    {
                        GenerateCode(context, sb, statementNode, indent + "    ");
                    }
                }
                sb.AppendLine(indent + "}");
            }
            else if (syntaxNode is StatementSyntax)
            {
                sb.AppendLine(indent + syntaxNode.ToString());
            }
        }

        private void GenerateAttributeClass(GeneratorExecutionContext context)
        {
            context.AddSource("InlineBitboardEnumeratorAttribute.cs", @"
using System;
namespace MovegenGenerator
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InlineBitboardEnumeratorAttribute : Attribute
    {
        public InlineBitboardEnumeratorAttribute() { }
    }
}
");

            context.AddSource("MakePublicAttribute.cs", @"
using System;
namespace MovegenGenerator
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MakePublicAttribute : Attribute
    {
        public MakePublicAttribute() { }
    }
}
");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
            context.RegisterForSyntaxNotifications(() => new MovegenFuncFinder());
        }

        class MovegenFuncFinder : ISyntaxReceiver
        {
            public List<(MethodDeclarationSyntax, bool)> Targets { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // InlineBitboardEnumerator 属性があり
                // メソッド名が Impl で終わるメソッドを展開の対象とする
                if (syntaxNode is MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodDeclaration)
                {
                    var attrs = methodDeclaration.AttributeLists
                        .SelectMany(x => x.Attributes)
                        .Select(x => x.Name.ToString());
                    var attrFound = attrs
                        .Any(x => x == "InlineBitboardEnumerator");
                    if (attrFound)
                    {
                        var makePublic = attrs.Any(x => x == "MakePublic");
                        Targets.Add((methodDeclaration, makePublic));
                    }
                }
            }
        }
    }
}
