using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Helpers.CSharp;

/// <summary>
/// Helpers for building common Roslyn block-emission statements.
/// </summary>
public static class BlockSyntaxFactory
{
    /// <summary>
    /// Creates a <c>nameof(...)</c> expression.
    /// </summary>
    public static ExpressionSyntax CreateNameofExpression(string variableName)
        => SyntaxFactory.ParseExpression($"nameof({variableName})");

    /// <summary>
    /// Creates a local declaration statement.
    /// </summary>
    public static LocalDeclarationStatementSyntax CreateVariableDeclaration(
        string typeName,
        string variableName,
        ExpressionSyntax initializer)
        => SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(typeName),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))));

    /// <summary>
    /// Creates either a declaration or an assignment depending on whether the variable already exists.
    /// </summary>
    public static StatementSyntax CreateVariableDeclarationOrAssignment(
        string typeName,
        string variableName,
        ExpressionSyntax valueExpression,
        bool assignToExistingVariable)
        => assignToExistingVariable
            ? CreateAssignment(variableName, valueExpression)
            : CreateVariableDeclaration(typeName, variableName, valueExpression);

    /// <summary>
    /// Creates a simple assignment statement.
    /// </summary>
    public static ExpressionStatementSyntax CreateAssignment(
        string leftExpression,
        ExpressionSyntax rightExpression)
        => CreateAssignment(SyntaxFactory.ParseExpression(leftExpression), rightExpression);

    /// <summary>
    /// Creates a simple assignment statement.
    /// </summary>
    public static ExpressionStatementSyntax CreateAssignment(
        ExpressionSyntax leftExpression,
        ExpressionSyntax rightExpression)
        => SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            leftExpression,
            rightExpression));

    /// <summary>
    /// Creates a general invocation expression.
    /// </summary>
    public static InvocationExpressionSyntax CreateInvocation(
        ExpressionSyntax expression,
        IEnumerable<ArgumentSyntax> arguments)
        => SyntaxFactory.InvocationExpression(
            expression,
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

    /// <summary>
    /// Creates a general invocation expression.
    /// </summary>
    public static InvocationExpressionSyntax CreateInvocation(
        ExpressionSyntax expression,
        params ArgumentSyntax[] arguments)
        => CreateInvocation(expression, arguments.AsEnumerable());

    /// <summary>
    /// Creates a member invocation expression.
    /// </summary>
    public static InvocationExpressionSyntax CreateMemberInvocation(
        ExpressionSyntax receiver,
        string methodName,
        params ArgumentSyntax[] arguments)
        => CreateInvocation(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver,
                SyntaxFactory.IdentifierName(methodName)),
            arguments);

    /// <summary>
    /// Creates an <c>await ...ConfigureAwait(false)</c> expression.
    /// </summary>
    public static AwaitExpressionSyntax CreateAwaitConfigureAwaitFalse(ExpressionSyntax invocationExpression)
        => SyntaxFactory.AwaitExpression(
            CreateMemberInvocation(
                invocationExpression,
                "ConfigureAwait",
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))));

    /// <summary>
    /// Creates a call on <c>data</c> with a single <c>nameof(variableName)</c> argument.
    /// </summary>
    public static ExpressionStatementSyntax CreateDataMethodWithNameofArgument(
        string methodName,
        string variableName)
        => SyntaxFactory.ExpressionStatement(CreateMemberInvocation(
            SyntaxFactory.IdentifierName("data"),
            methodName,
            SyntaxFactory.Argument(CreateNameofExpression(variableName))));

    /// <summary>
    /// Creates the standard safe-mode catch clause used by generated blocks.
    /// </summary>
    public static CatchClauseSyntax CreateSafeModeCatchClause()
        => SyntaxFactory.CatchClause(
            declaration: SyntaxFactory.CatchDeclaration(
                SyntaxFactory.ParseTypeName("Exception"),
                SyntaxFactory.Identifier("safeException")),
            filter: null,
            block: SyntaxFactory.Block(
                CreateAssignment(
                    "data.ERROR",
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("safeException"),
                            SyntaxFactory.IdentifierName("PrettyPrint")),
                        SyntaxFactory.ArgumentList())),
                CreateSafeModeLogStatement()));

    /// <summary>
    /// Creates a block that sets <c>data.STATUS</c> and optionally returns.
    /// </summary>
    public static BlockSyntax CreateStatusBlock(string status, bool shouldReturn)
    {
        var statements = new List<StatementSyntax>
        {
            CreateAssignment(
                "data.STATUS",
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(status)))
        };

        if (shouldReturn)
        {
            statements.Add(SyntaxFactory.ReturnStatement());
        }

        return SyntaxFactory.Block(statements);
    }

    private static StatementSyntax CreateSafeModeLogStatement()
        => SyntaxFactory.ExpressionStatement(CreateMemberInvocation(
            SyntaxFactory.ParseExpression("data.Logger"),
            "Log",
            [
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression("$\"[SAFE MODE] Exception caught and saved to data.ERROR: {data.ERROR}\"")),
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression("LogColors.Tomato"))
            ]));
}
