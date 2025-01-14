﻿namespace Lox;

public sealed class Environment
{
	private readonly Environment? enclosing;
	private readonly Dictionary<string, object> values = new();
	public Environment() => enclosing = null;
	public Environment(Environment enclosing) => this.enclosing = enclosing;

	public object Get(Token name)
	{
		if (values.ContainsKey(name.Lexeme))
			return values[name.Lexeme];
		if (enclosing != null)
			return enclosing.Get(name);
		throw new UndefinedVariable(name);
	}

	public sealed class UndefinedVariable : Exception
	{
		public UndefinedVariable(Token name) : base(name.Lexeme) { }
	}

	public void Assign(Token name, object value)
	{
		if (values.ContainsKey(name.Lexeme))
		{
			values[name.Lexeme] = value;
			return;
		}
		if (enclosing == null)
			throw new UndefinedVariable(name);
		enclosing.Assign(name, value);
	}

	public void Define(string name, object value)
	{
		if (values.ContainsKey(name))
			throw new DuplicateVariableName(name);
		values.Add(name, value);
	}

	public sealed class DuplicateVariableName : Exception
	{
		public DuplicateVariableName(string name) : base(name) { }
	}
}