﻿using System.Globalization;

namespace Lox;

// ReSharper disable once ClassTooBig
public sealed class Interpreter : ExpressionVisitor<object>, StatementVisitor<object>
{
	private Environment environment = new();

	public void Interpret(List<Statement> statements)
	{
		foreach (var statement in statements)
			Execute(statement);
	}

	private void Execute(Statement statement) => statement.Accept(this);
	private object EvaluateExpression(Expression expression) => expression.Accept(this);
	public object VisitLiteralExpression(Expression.LiteralExpression literal) => literal.Literal ?? new object();

	public object VisitGroupingExpression(Expression.GroupingExpression groupingExpression) =>
		EvaluateExpression(groupingExpression.expression);

	public object VisitBinaryExpression(Expression.BinaryExpression binaryExpression)
	{
		var left = EvaluateExpression(binaryExpression.LeftExpression);
		var right = EvaluateExpression(binaryExpression.RightExpression);
		return EvaluateBinaryExpression(binaryExpression, left, right);
	}

	// ReSharper disable once CyclomaticComplexity
	private static object EvaluateBinaryExpression(Expression.BinaryExpression binaryExpression, object left,
		object right) =>
		binaryExpression.OperatorToken.Type switch
		{
			TokenType.Greater => EvaluateGreaterOperatorExpression(binaryExpression, left, right),
			TokenType.GreaterEqual => EvaluateGreaterEqualOperatorExpression(binaryExpression, left,
				right),
			TokenType.Less => EvaluateLessOperatorExpression(binaryExpression, left, right),
			TokenType.LessEqual => EvaluateLessEqualOperatorExpression(binaryExpression, left, right),
			TokenType.EqualEqual => IsEqual(left, right),
			TokenType.BangEqual => !IsEqual(left, right),
			TokenType.Minus => EvaluateMinusOperatorExpression(binaryExpression, left, right),
			TokenType.Plus => EvaluatePlusOperatorExpression(binaryExpression, left, right),
			TokenType.Slash => EvaluateSlashOperatorExpression(binaryExpression, left, right),
			TokenType.Star => EvaluateStarOperatorExpression(binaryExpression, left, right),
			_ => new object() // ncrunch: no coverage
		};

	private static object EvaluateStarOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left * (double)right;
	}

	private static object EvaluateSlashOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left / (double)right;
	}

	private static object EvaluateMinusOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left - (double)right;
	}

	private static object EvaluateLessEqualOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left <= (double)right;
	}

	private static object EvaluateLessOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left < (double)right;
	}

	private static object EvaluateGreaterEqualOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left >= (double)right;
	}

	private static object EvaluateGreaterOperatorExpression(Expression.BinaryExpression binaryExpression,
		object left, object right)
	{
		CheckNumberOperand(binaryExpression.OperatorToken, left, right);
		return (double)left > (double)right;
	}

	private static object EvaluatePlusOperatorExpression(Expression.BinaryExpression binaryExpression, object left,
		object right) =>
		left switch
		{
			double d when right is double rightExpressionNumber => d + rightExpressionNumber,
			string s when right is string rightExpressionNumber => s + rightExpressionNumber,
			double d when right is string s => d.ToString(CultureInfo.InvariantCulture) + s,
			string s when right is double d => s + d.ToString(CultureInfo.InvariantCulture),
			_ => throw new OperandMustBeANumberOrString(binaryExpression.OperatorToken)
		};

	private static bool IsEqual(object a, object b) => a.Equals(b);

	public sealed class OperandMustBeANumberOrString : Exception
	{
		public OperandMustBeANumberOrString(Token expressionOperator) : base(expressionOperator.Lexeme) { }
	}

	private static void CheckNumberOperand(Token expressionOperator, object firstOperand,
		object secondOperand)
	{
		if (firstOperand is double && secondOperand is double)
			return;
		throw new OperandMustBeANumber(expressionOperator);
	}

	private static void CheckNumberOperand(Token expressionOperator, object operand)
	{
		if (operand is double)
			return;
		throw new OperandMustBeANumber(expressionOperator);
	}

	public sealed class OperandMustBeANumber : Exception
	{
		public OperandMustBeANumber(Token expressionOperator) : base(expressionOperator.Lexeme) { }
	}

	public object VisitUnaryExpression(Expression.UnaryExpression unaryExpression)
	{
		var rightExpressionValue = EvaluateExpression(unaryExpression.RightExpression);
		return unaryExpression.OperatorToken.Type switch
		{
			TokenType.Bang => !IsTruthy(rightExpressionValue),
			TokenType.Minus => EvaluateMinusOperatorExpression(unaryExpression, rightExpressionValue),
			_ => new object() //ncrunch: no coverage
		};
	}

	private static object EvaluateMinusOperatorExpression(Expression.UnaryExpression unaryExpression,
		object rightExpressionValue)
	{
		CheckNumberOperand(unaryExpression.OperatorToken, rightExpressionValue);
		return -(double)rightExpressionValue;
	}

	private static bool IsTruthy(object value) =>
		value switch
		{
			bool a => a,
			_ => true
		};

	public object VisitAssignmentExpression(Expression.AssignmentExpression assignmentExpression)
	{
		var value = EvaluateExpression(assignmentExpression.value);
		environment.Assign(assignmentExpression.name, value);
		return value;
	}

	public object VisitVariableExpression(Expression.VariableExpression variableExpression) =>
		environment.Get(variableExpression.name);

	public object VisitLogicalExpression(Expression.LogicalExpression logicalExpression)
	{
		var leftExpressionValue = EvaluateExpression(logicalExpression.left);
		if (logicalExpression.operatorToken.Type == TokenType.Or)
		{
			if (IsTruthy(leftExpressionValue))
				return leftExpressionValue;
		}
		else
		{
			if (!IsTruthy(leftExpressionValue))
				return leftExpressionValue;
		}
		return EvaluateExpression(logicalExpression.right);
	}

	public object VisitPrintStatement(Statement.PrintStatement printStatement)
	{
		var value = EvaluateExpression(printStatement.expression);
		Console.Out.WriteLine(Stringify(value));
		return value;
	}

	private static string Stringify(object resultObject)
	{
		switch (resultObject)
		{
		case double:
		{
			var text = resultObject.ToString()!;
			return text;
		}
		default:
			return resultObject.ToString()!;
		}
	}

	public object VisitExpressionStatement(Statement.ExpressionStatement expressionStatement) =>
		EvaluateExpression(expressionStatement.expression);

	public object VisitVariableStatement(Statement.VariableStatement variableStatement)
	{
		var value = new object();
		if (variableStatement.initializer != null)
			value = EvaluateExpression(variableStatement.initializer);
		environment.Define(variableStatement.name.Lexeme, value);
		return new object();
	}

	public object VisitBlockStatement(Statement.BlockStatement blockStatement)
	{
		ExecuteBlock(blockStatement.statements, new Environment(environment));
		return new object();
	}

	private void ExecuteBlock(List<Statement> statements, Environment innerEnvironment)
	{
		var previous = environment;
		try
		{
			environment = innerEnvironment;
			foreach (var statement in statements)
				Execute(statement);
		}
		finally
		{
			environment = previous;
		}
	}

	public object VisitIfStatement(Statement.IfStatement ifStatement)
	{
		if (IsTruthy(EvaluateExpression(ifStatement.condition)))
			Execute(ifStatement.thenStatement);
		else if (ifStatement.elseStatement != null)
			Execute(ifStatement.elseStatement);
		return new object();
	}

	public object VisitWhileStatement(Statement.WhileStatement whileStatement)
	{
		while (IsTruthy(EvaluateExpression(whileStatement.condition)))
			Execute(whileStatement.bodyStatement);
		return new object();
	}
}