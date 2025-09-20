using System;
using System.Collections.Generic;
using System.Globalization;

namespace Raices;

internal sealed class ExpressionEvaluator
{
    private enum TokenType
    {
        Number,
        Variable,
        Operator,
        Function,
        LeftParenthesis,
        RightParenthesis,
        ArgumentSeparator
    }

    private readonly struct Token
    {
        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; }

        public string Value { get; }
    }

    private static readonly Dictionary<string, int> OperatorPrecedence = new()
    {
        {"+", 2},
        {"-", 2},
        {"*", 3},
        {"/", 3},
        {"^", 4}
    };

    private static readonly HashSet<string> RightAssociativeOperators = new()
    {
        "^"
    };

    private static readonly Dictionary<string, Func<double, double>> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        {"sin", Math.Sin},
        {"cos", Math.Cos},
        {"tan", Math.Tan},
        {"asin", Math.Asin},
        {"acos", Math.Acos},
        {"atan", Math.Atan},
        {"sqrt", Math.Sqrt},
        {"abs", Math.Abs},
        {"sign", x => Math.Sign(x)},
        {"exp", Math.Exp},
        {"log", Math.Log},
        {"ln", Math.Log},
        {"log10", Math.Log10},
        {"sinh", Math.Sinh},
        {"cosh", Math.Cosh},
        {"tanh", Math.Tanh},
        {"neg", x => -x}
    };

    private static readonly Dictionary<string, double> Constants = new(StringComparer.OrdinalIgnoreCase)
    {
        {"pi", Math.PI},
        {"π", Math.PI},
        {"e", Math.E},
        {"tau", Math.Tau}
    };

    public double Evaluate(string expression, double x)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("La expresión no puede estar vacía.", nameof(expression));
        }

        var tokens = Tokenize(expression);
        var rpn = ConvertToReversePolish(tokens);
        return EvaluateReversePolish(rpn, x);
    }

    private static IReadOnlyList<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        Token? previousToken = null;

        for (int i = 0; i < expression.Length;)
        {
            char current = expression[i];

            if (char.IsWhiteSpace(current))
            {
                i++;
                continue;
            }

            if (char.IsDigit(current) || current is '.' or ',')
            {
                int start = i;
                bool hasExponent = false;
                i++;

                while (i < expression.Length)
                {
                    char c = expression[i];

                    if (char.IsDigit(c) || c is '.' or ',')
                    {
                        i++;
                        continue;
                    }

                    if ((c == 'e' || c == 'E') && !hasExponent)
                    {
                        hasExponent = true;
                        i++;

                        if (i < expression.Length && (expression[i] == '+' || expression[i] == '-'))
                        {
                            i++;
                        }

                        continue;
                    }

                    break;
                }

                string numberText = expression[start..i];
                string normalized = numberText.Replace(',', '.');

                if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    throw new FormatException($"Número inválido en la expresión: '{numberText}'.");
                }

                var token = new Token(TokenType.Number, normalized);
                tokens.Add(token);
                previousToken = token;
                continue;
            }

            if (char.IsLetter(current))
            {
                int start = i;
                i++;

                while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                {
                    i++;
                }

                string identifier = expression[start..i];

                if (string.Equals(identifier, "x", StringComparison.OrdinalIgnoreCase))
                {
                    var token = new Token(TokenType.Variable, "x");
                    tokens.Add(token);
                    previousToken = token;
                    continue;
                }

                if (Constants.TryGetValue(identifier, out double constantValue))
                {
                    var token = new Token(TokenType.Number, constantValue.ToString("G17", CultureInfo.InvariantCulture));
                    tokens.Add(token);
                    previousToken = token;
                    continue;
                }

                var functionToken = new Token(TokenType.Function, identifier);
                tokens.Add(functionToken);
                previousToken = functionToken;
                continue;
            }

            if (current == ',')
            {
                var token = new Token(TokenType.ArgumentSeparator, ",");
                tokens.Add(token);
                previousToken = token;
                i++;
                continue;
            }

            if (current == '(')
            {
                var token = new Token(TokenType.LeftParenthesis, "(");
                tokens.Add(token);
                previousToken = token;
                i++;
                continue;
            }

            if (current == ')')
            {
                var token = new Token(TokenType.RightParenthesis, ")");
                tokens.Add(token);
                previousToken = token;
                i++;
                continue;
            }

            if (IsOperator(current))
            {
                string op = current.ToString();
                bool isUnary = previousToken == null || previousToken.Value.Type is TokenType.Operator or TokenType.LeftParenthesis or TokenType.ArgumentSeparator;

                if (isUnary && op == "-")
                {
                    var token = new Token(TokenType.Function, "neg");
                    tokens.Add(token);
                    previousToken = token;
                    i++;
                    continue;
                }

                if (isUnary && op == "+")
                {
                    i++;
                    continue;
                }

                var operatorToken = new Token(TokenType.Operator, op);
                tokens.Add(operatorToken);
                previousToken = operatorToken;
                i++;
                continue;
            }

            throw new FormatException($"Carácter inesperado en la expresión: '{current}'.");
        }

        return tokens;
    }

    private static Queue<Token> ConvertToReversePolish(IReadOnlyList<Token> tokens)
    {
        var output = new Queue<Token>();
        var stack = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Variable:
                    output.Enqueue(token);
                    break;
                case TokenType.Function:
                    stack.Push(token);
                    break;
                case TokenType.Operator:
                    while (stack.Count > 0 &&
                           (stack.Peek().Type == TokenType.Function ||
                            (stack.Peek().Type == TokenType.Operator &&
                             (OperatorPrecedence[stack.Peek().Value] > OperatorPrecedence[token.Value] ||
                              (OperatorPrecedence[stack.Peek().Value] == OperatorPrecedence[token.Value] &&
                               !RightAssociativeOperators.Contains(token.Value)))))
                    {
                        output.Enqueue(stack.Pop());
                    }

                    stack.Push(token);
                    break;
                case TokenType.LeftParenthesis:
                    stack.Push(token);
                    break;
                case TokenType.RightParenthesis:
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.LeftParenthesis)
                    {
                        output.Enqueue(stack.Pop());
                    }

                    if (stack.Count == 0)
                    {
                        throw new FormatException("Paréntesis desbalanceados en la expresión.");
                    }

                    stack.Pop();

                    if (stack.Count > 0 && stack.Peek().Type == TokenType.Function)
                    {
                        output.Enqueue(stack.Pop());
                    }

                    break;
                case TokenType.ArgumentSeparator:
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.LeftParenthesis)
                    {
                        output.Enqueue(stack.Pop());
                    }

                    if (stack.Count == 0)
                    {
                        throw new FormatException("Separador de argumentos inesperado.");
                    }

                    break;
            }
        }

        while (stack.Count > 0)
        {
            var token = stack.Pop();

            if (token.Type is TokenType.LeftParenthesis or TokenType.RightParenthesis)
            {
                throw new FormatException("Paréntesis desbalanceados en la expresión.");
            }

            output.Enqueue(token);
        }

        return output;
    }

    private double EvaluateReversePolish(Queue<Token> tokens, double x)
    {
        var values = new Stack<double>();

        while (tokens.Count > 0)
        {
            var token = tokens.Dequeue();

            switch (token.Type)
            {
                case TokenType.Number:
                    values.Push(double.Parse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture));
                    break;
                case TokenType.Variable:
                    values.Push(x);
                    break;
                case TokenType.Operator:
                    if (values.Count < 2)
                    {
                        throw new InvalidOperationException("La expresión es inválida.");
                    }

                    double right = values.Pop();
                    double left = values.Pop();
                    values.Push(ApplyOperator(token.Value, left, right));
                    break;
                case TokenType.Function:
                    if (!Functions.TryGetValue(token.Value, out var func))
                    {
                        throw new InvalidOperationException($"Función desconocida: {token.Value}.");
                    }

                    if (values.Count < 1)
                    {
                        throw new InvalidOperationException("La expresión es inválida.");
                    }

                    double argument = values.Pop();
                    values.Push(func(argument));
                    break;
                default:
                    throw new InvalidOperationException("Token inesperado en la evaluación.");
            }
        }

        if (values.Count != 1)
        {
            throw new InvalidOperationException("La expresión es inválida.");
        }

        return values.Pop();
    }

    private static bool IsOperator(char c) => c is '+' or '-' or '*' or '/' or '^';

    private static double ApplyOperator(string op, double left, double right)
    {
        return op switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => right == 0 ? throw new DivideByZeroException("Se intentó dividir entre cero.") : left / right,
            "^" => Math.Pow(left, right),
            _ => throw new InvalidOperationException($"Operador desconocido: {op}.")
        };
    }
}
