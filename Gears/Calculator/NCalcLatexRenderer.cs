using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NCalc;
using NCalc.Domain;
using NCalc.Factories;
using NCalc.Visitors;

namespace Bolsover.Gears.Calculator;

/// <summary>
/// Renders an NCalc expression as LaTeX, so that a formula need only be maintained once - in its NCalc form.
/// </summary>
/// <remarks>
/// The expression is parsed by NCalc itself and the resulting syntax tree is rendered, so the output does not
/// depend on how the source happens to be spaced or bracketed: parentheses are emitted only where operator
/// precedence requires them. Parameters (<c>[key]</c>) are rendered by a caller-supplied lookup, normally the
/// LaTeX <c>Symbol</c> of the variable with that key.
/// </remarks>
public static class NCalcLatexRenderer
{
    /// <summary>
    /// Renders the NCalc expression as LaTeX.
    /// </summary>
    /// <param name="nCalc">The NCalc expression, e.g. <c>[db1] / Cos([alphawt])</c>.</param>
    /// <param name="symbolLookup">Returns the LaTeX symbol for a parameter name, or null if unknown.</param>
    /// <returns>The LaTeX rendering, e.g. <c>\frac{d_{b1}}{\cos\alpha_{wt}}</c>.</returns>
    public static string Render(string nCalc, Func<string, string> symbolLookup)
    {
        if (string.IsNullOrWhiteSpace(nCalc)) return string.Empty;
        var expression = LogicalExpressionFactory.Create(nCalc, ExpressionOptions.None);
        return expression.Accept(new LatexVisitor(symbolLookup ?? (_ => null))).Tex;
    }

    private enum Precedence
    {
        Ternary,
        Or,
        And,
        Comparison,
        Additive,
        Multiplicative,
        Unary,
        Power,
        Atom
    }

    private enum TermKind
    {
        Number,
        Symbol,

        // Ends in an operator applied without brackets (e.g. \cos\beta): a following factor would read as its argument.
        OpenFunction,
        Other
    }

    private readonly struct Term
    {
        public Term(string tex, Precedence precedence, TermKind kind = TermKind.Other, bool isNegation = false)
        {
            Tex = tex;
            Precedence = precedence;
            Kind = kind;
            IsNegation = isNegation;
        }

        public string Tex { get; }
        public Precedence Precedence { get; }
        public TermKind Kind { get; }
        public bool IsNegation { get; }
    }

    private sealed class LatexVisitor : ILogicalExpressionVisitor<Term>
    {
        private static readonly Regex TrailingCommand = new(@"\\[a-zA-Z]+$");

        private readonly Func<string, string> _symbolLookup;

        public LatexVisitor(Func<string, string> symbolLookup)
        {
            _symbolLookup = symbolLookup;
        }

        public Term Visit(TernaryExpression expression)
        {
            var condition = expression.LeftExpression.Accept(this);
            var whenTrue = expression.MiddleExpression.Accept(this);
            var whenFalse = expression.RightExpression.Accept(this);
            return Generic("if", condition, whenTrue, whenFalse);
        }

        public Term Visit(BinaryExpression expression)
        {
            var left = expression.LeftExpression.Accept(this);
            var right = expression.RightExpression.Accept(this);

            switch (expression.Type)
            {
                case BinaryExpressionType.Div:
                    return new Term($@"\frac{{{left.Tex}}}{{{right.Tex}}}", Precedence.Atom);
                case BinaryExpressionType.Times:
                    return Product(left, right);
                case BinaryExpressionType.Plus:
                    return Infix(left, " + ", right, Precedence.Additive, false);
                case BinaryExpressionType.Minus:
                    return Infix(left, " - ", right, Precedence.Additive, true);
                case BinaryExpressionType.Modulo:
                    return Infix(left, @" \bmod ", right, Precedence.Multiplicative, true);
                case BinaryExpressionType.Exponentiation:
                    return Power(left, right);
                case BinaryExpressionType.Equal:
                    return Infix(left, " = ", right, Precedence.Comparison, true);
                case BinaryExpressionType.NotEqual:
                    return Infix(left, @" \ne ", right, Precedence.Comparison, true);
                case BinaryExpressionType.Lesser:
                    return Infix(left, " < ", right, Precedence.Comparison, true);
                case BinaryExpressionType.LesserOrEqual:
                    return Infix(left, @" \le ", right, Precedence.Comparison, true);
                case BinaryExpressionType.Greater:
                    return Infix(left, " > ", right, Precedence.Comparison, true);
                case BinaryExpressionType.GreaterOrEqual:
                    return Infix(left, @" \ge ", right, Precedence.Comparison, true);
                case BinaryExpressionType.And:
                    return Infix(left, @" \land ", right, Precedence.And, false);
                case BinaryExpressionType.Or:
                    return Infix(left, @" \lor ", right, Precedence.Or, false);
                default:
                    throw new NotSupportedException($"Operator {expression.Type} cannot be rendered as LaTeX");
            }
        }

        public Term Visit(UnaryExpression expression)
        {
            var operand = expression.Expression.Accept(this);
            switch (expression.Type)
            {
                case UnaryExpressionType.Negate:
                    return new Term("-" + Wrap(operand, Precedence.Multiplicative), Precedence.Unary, isNegation: true);
                case UnaryExpressionType.Positive:
                    return operand;
                case UnaryExpressionType.Not:
                    return new Term(@"\lnot " + Wrap(operand, Precedence.Unary), Precedence.Unary);
                default:
                    throw new NotSupportedException($"Operator {expression.Type} cannot be rendered as LaTeX");
            }
        }

        public Term Visit(ValueExpression expression)
        {
            switch (expression.Value)
            {
                case double d:
                    return new Term(d.ToString("R", CultureInfo.InvariantCulture), Precedence.Atom, TermKind.Number);
                case float or decimal or int or long:
                    return new Term(Convert.ToString(expression.Value, CultureInfo.InvariantCulture), Precedence.Atom,
                        TermKind.Number);
                default:
                    return new Term($@"\mathrm{{{expression.Value}}}", Precedence.Atom);
            }
        }

        public Term Visit(Identifier identifier)
        {
            var name = identifier.Name;
            var symbol = _symbolLookup(name) ??
                         (name.Equals("pi", StringComparison.OrdinalIgnoreCase) ? @"\pi" : $@"\mathrm{{{name}}}");

            // A compound symbol such as x_{1}+x_{2} must be bracketed when used as a factor.
            var precedence = symbol.IndexOfAny(new[] { '+', '-' }) >= 0 ? Precedence.Additive : Precedence.Atom;
            return new Term(symbol, precedence, TermKind.Symbol);
        }

        public Term Visit(LogicalExpressionList list)
        {
            return new Term(string.Join(", ", list.Select(e => e.Accept(this).Tex)), Precedence.Ternary);
        }

        public Term Visit(Function function)
        {
            var name = function.Identifier.Name;
            var args = function.Parameters.Select(p => p.Accept(this)).ToArray();

            switch (name.ToLowerInvariant())
            {
                case "sin":
                case "cos":
                case "tan":
                case "ln":
                    return Operator(@"\" + name.ToLowerInvariant(), args[0]);
                case "inv":
                    return Operator(@"\mathrm{inv}", args[0]);
                case "log10":
                    return Operator(@"\log_{10}", args[0]);
                case "log":
                    return args.Length > 1 ? Operator($@"\log_{{{args[1].Tex}}}", args[0]) : Operator(@"\log", args[0]);
                case "asin":
                case "acos":
                case "atan":
                    return new Term($@"\{name.Substring(1).ToLowerInvariant()}^{{-1}}{Parenthesise(args[0].Tex)}",
                        Precedence.Atom);
                case "sqrt":
                    return new Term($@"\sqrt{{{args[0].Tex}}}", Precedence.Atom);
                case "pow":
                    return Power(args[0], args[1]);
                case "exp":
                    return new Term($"e^{{{args[0].Tex}}}", Precedence.Power);
                case "abs":
                    return new Term($@"\left|{args[0].Tex}\right|", Precedence.Atom);
                case "max":
                case "min":
                    return new Term($@"\{name.ToLowerInvariant()}{Parenthesise(string.Join(", ", args.Select(a => a.Tex)))}",
                        Precedence.Atom);
                default:
                    return Generic(name, args);
            }
        }

        private static Term Generic(string name, params Term[] args)
        {
            return new Term($@"\mathrm{{{name}}}{Parenthesise(string.Join(", ", args.Select(a => a.Tex)))}",
                Precedence.Atom);
        }

        // An operator such as \cos takes a bare argument when that argument is a single symbol or number.
        private static Term Operator(string command, Term argument)
        {
            if (argument.Precedence == Precedence.Atom &&
                argument.Kind is TermKind.Symbol or TermKind.Number)
            {
                return new Term(Join(command, argument.Tex), Precedence.Multiplicative, TermKind.OpenFunction);
            }

            return new Term(command + Parenthesise(argument.Tex), Precedence.Atom);
        }

        private static Term Product(Term left, Term right)
        {
            var leftTex = Wrap(left, Precedence.Multiplicative);
            var rightTex = right.IsNegation ? Parenthesise(right.Tex) : Wrap(right, Precedence.Multiplicative);

            // Juxtaposition (zm_{n}) reads naturally, except where it would be ambiguous.
            var needsDot = right.Kind == TermKind.Number || left.Kind == TermKind.OpenFunction ||
                           (char.IsDigit(leftTex[leftTex.Length - 1]) && char.IsDigit(rightTex[0]));
            var tex = needsDot ? $@"{leftTex} \cdot {rightTex}" : Join(leftTex, rightTex);
            var kind = right.Kind == TermKind.OpenFunction ? TermKind.OpenFunction : TermKind.Other;
            return new Term(tex, Precedence.Multiplicative, kind);
        }

        private static Term Power(Term power, Term exponent)
        {
            // Only a bare symbol or number may carry a superscript unbracketed; \frac{a}{b}^{2} would mislead.
            var baseTex = power.Precedence == Precedence.Atom && power.Kind is TermKind.Symbol or TermKind.Number
                ? power.Tex
                : Parenthesise(power.Tex);
            return new Term($"{baseTex}^{{{exponent.Tex}}}", Precedence.Power);
        }

        // For non-associative operators (minus, comparisons) an equal-precedence right operand must be bracketed.
        private static Term Infix(Term left, string op, Term right, Precedence precedence, bool rightNeedsStrict)
        {
            var leftTex = Wrap(left, precedence);
            var rightTex = right.IsNegation || (rightNeedsStrict && right.Precedence == precedence)
                ? Parenthesise(right.Tex)
                : Wrap(right, precedence);
            return new Term(leftTex + op + rightTex, precedence);
        }

        private static string Wrap(Term term, Precedence context)
        {
            return term.Precedence < context ? Parenthesise(term.Tex) : term.Tex;
        }

        private static string Parenthesise(string tex) => $@"\left({tex}\right)";

        // A command such as \alpha must be separated from a following letter, or it would be read as \alpham.
        private static string Join(string left, string right)
        {
            return TrailingCommand.IsMatch(left) && char.IsLetter(right[0]) ? left + " " + right : left + right;
        }
    }
}
