﻿namespace Lox;

public abstract class Statement
{
	public abstract T Accept<T>(StatementVisitor<T> statementVisitor);

	public sealed class ExpressionStatement : Statement
	{
		public readonly Expression expression;
		public ExpressionStatement(Expression expression) => this.expression = expression;
		public override T Accept<T>(StatementVisitor<T> statementVisitor) => statementVisitor.VisitExpressionStatement(this);
	}

	public sealed class PrintStatement : Statement
	{
		public readonly Expression expression;
		public PrintStatement(Expression expression) => this.expression = expression;
		public override T Accept<T>(StatementVisitor<T> statementVisitor) => statementVisitor.VisitPrintStatement(this);
	}

	public sealed class VariableStatement : Statement
	{
		public readonly Token name;
		public readonly Expression? initializer;

		public VariableStatement(Token name, Expression? initializer)
		{
			this.name = name;
			if (initializer != null)
				this.initializer = initializer;
		}

		public override T Accept<T>(StatementVisitor<T> statementVisitor) => statementVisitor.VisitVariableStatement(this);
	}

	public sealed class BlockStatement : Statement
	{
		public readonly List<Statement> statements;
		public BlockStatement(List<Statement> statements) => this.statements = statements;
		public override T Accept<T>(StatementVisitor<T> statementVisitor) => statementVisitor.VisitBlockStatement(this);
	}

	public sealed class IfStatement : Statement
	{
		public IfStatement(Expression condition, Statement thenStatement, Statement? elseStatement)
		{
			this.condition = condition;
			this.thenStatement = thenStatement;
			this.elseStatement = elseStatement;
		}

		public override T Accept<T>(StatementVisitor<T> statementVisitor) => statementVisitor.VisitIfStatement(this);
		public readonly Expression condition;
		public readonly Statement thenStatement;
		public readonly Statement? elseStatement;
	}

	public sealed class WhileStatement : Statement
	{
		public WhileStatement(Expression condition, Statement bodyStatement)
		{
			this.condition = condition;
			this.bodyStatement = bodyStatement;
		}

		public override T Accept<T>(StatementVisitor<T> statementVisitor) => statementVisitor.VisitWhileStatement(this);
		public readonly Expression condition;
		public readonly Statement bodyStatement;
	}
}

public interface StatementVisitor<out T>
{
	T VisitPrintStatement(Statement.PrintStatement printStatement);
	T VisitExpressionStatement(Statement.ExpressionStatement expressionStatement);
	T VisitVariableStatement(Statement.VariableStatement variableStatement);
	T VisitBlockStatement(Statement.BlockStatement blockStatement);
	T VisitIfStatement(Statement.IfStatement ifStatement);
	T VisitWhileStatement(Statement.WhileStatement whileStatement);
}