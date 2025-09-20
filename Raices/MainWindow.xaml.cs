using System.Globalization;
using System.Text;
using System.Windows;

namespace Raices;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ExpressionEvaluator _evaluator = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void CalculateNewton_Click(object sender, RoutedEventArgs e)
    {
        var log = new StringBuilder();

        try
        {
            var functionText = FunctionTextBox.Text;
            var derivativeText = DerivativeTextBox.Text;

            if (string.IsNullOrWhiteSpace(functionText))
            {
                throw new InvalidOperationException("Debes especificar una función f(x).");
            }

            double tolerance = ReadDouble(ToleranceTextBox.Text, 1e-6, "tolerancia");
            int maxIterations = ReadInt(MaxIterationsTextBox.Text, 100, "máximo de iteraciones");
            double x = ReadDouble(NewtonInitialGuessTextBox.Text, null, "aproximación inicial x₀");

            if (tolerance <= 0)
            {
                throw new InvalidOperationException("La tolerancia debe ser un número positivo.");
            }

            if (maxIterations <= 0)
            {
                throw new InvalidOperationException("El máximo de iteraciones debe ser mayor que cero.");
            }

            log.AppendLine("Método de Newton-Raphson");
            log.AppendLine($"Función: f(x) = {functionText}");

            if (!string.IsNullOrWhiteSpace(derivativeText))
            {
                log.AppendLine($"Derivada proporcionada: f'(x) = {derivativeText}");
            }
            else
            {
                log.AppendLine("Derivada numérica: se utilizará aproximación por diferencia central.");
            }

            log.AppendLine($"Tolerancia: {FormatDouble(tolerance)}");
            log.AppendLine($"Máximo de iteraciones: {maxIterations}");
            log.AppendLine(new string('-', 60));

            bool converged = false;
            double fx = double.NaN;

            for (int iteration = 1; iteration <= maxIterations; iteration++)
            {
                fx = _evaluator.Evaluate(functionText, x);
                log.AppendLine($"Iteración {iteration}: x = {FormatDouble(x)}, f(x) = {FormatDouble(fx)}");

                if (double.IsNaN(fx) || double.IsInfinity(fx))
                {
                    throw new InvalidOperationException("La evaluación de la función devolvió un valor no válido.");
                }

                if (Math.Abs(fx) < tolerance)
                {
                    converged = true;
                    break;
                }

                double derivativeValue = !string.IsNullOrWhiteSpace(derivativeText)
                    ? _evaluator.Evaluate(derivativeText, x)
                    : NumericalDerivative(functionText, x);

                if (Math.Abs(derivativeValue) < 1e-12)
                {
                    throw new InvalidOperationException("La derivada se aproxima a cero. El método de Newton no puede continuar.");
                }

                double next = x - fx / derivativeValue;

                if (double.IsNaN(next) || double.IsInfinity(next))
                {
                    throw new InvalidOperationException("Se produjo un valor no válido durante la iteración.");
                }

                double change = Math.Abs(next - x);

                if (change < tolerance)
                {
                    x = next;
                    fx = _evaluator.Evaluate(functionText, x);
                    converged = true;
                    log.AppendLine($"Convergencia alcanzada: |xₙ₊₁ - xₙ| = {FormatDouble(change)} < tolerancia");
                    break;
                }

                x = next;
            }

            if (converged)
            {
                log.AppendLine(new string('-', 60));
                log.AppendLine($"Raíz aproximada: x ≈ {FormatDouble(x)}");
                log.AppendLine($"f(x) ≈ {FormatDouble(fx)}");
            }
            else
            {
                log.AppendLine(new string('-', 60));
                log.AppendLine("No se alcanzó la convergencia con los parámetros proporcionados.");
            }
        }
        catch (Exception ex)
        {
            log.AppendLine();
            log.AppendLine($"Error: {ex.Message}");
        }
        finally
        {
            ResultsTextBox.Text = log.ToString();
        }
    }

    private void CalculateSecant_Click(object sender, RoutedEventArgs e)
    {
        var log = new StringBuilder();

        try
        {
            var functionText = FunctionTextBox.Text;

            if (string.IsNullOrWhiteSpace(functionText))
            {
                throw new InvalidOperationException("Debes especificar una función f(x).");
            }

            double tolerance = ReadDouble(ToleranceTextBox.Text, 1e-6, "tolerancia");
            int maxIterations = ReadInt(MaxIterationsTextBox.Text, 100, "máximo de iteraciones");
            double x0 = ReadDouble(SecantFirstGuessTextBox.Text, null, "aproximación inicial x₀");
            double x1 = ReadDouble(SecantSecondGuessTextBox.Text, null, "aproximación inicial x₁");

            if (tolerance <= 0)
            {
                throw new InvalidOperationException("La tolerancia debe ser un número positivo.");
            }

            if (maxIterations <= 0)
            {
                throw new InvalidOperationException("El máximo de iteraciones debe ser mayor que cero.");
            }

            log.AppendLine("Método de la secante");
            log.AppendLine($"Función: f(x) = {functionText}");
            log.AppendLine($"Tolerancia: {FormatDouble(tolerance)}");
            log.AppendLine($"Máximo de iteraciones: {maxIterations}");
            log.AppendLine(new string('-', 60));

            double f0 = _evaluator.Evaluate(functionText, x0);
            double f1 = _evaluator.Evaluate(functionText, x1);

            log.AppendLine($"x₀ = {FormatDouble(x0)}, f(x₀) = {FormatDouble(f0)}");
            log.AppendLine($"x₁ = {FormatDouble(x1)}, f(x₁) = {FormatDouble(f1)}");

            bool converged = false;

            for (int iteration = 1; iteration <= maxIterations; iteration++)
            {
                double denominator = f1 - f0;

                if (Math.Abs(denominator) < 1e-12)
                {
                    throw new InvalidOperationException("El denominador es demasiado pequeño. El método de la secante no puede continuar.");
                }

                double x2 = x1 - f1 * (x1 - x0) / denominator;

                if (double.IsNaN(x2) || double.IsInfinity(x2))
                {
                    throw new InvalidOperationException("Se produjo un valor no válido durante la iteración.");
                }

                double f2 = _evaluator.Evaluate(functionText, x2);
                log.AppendLine($"Iteración {iteration}: x = {FormatDouble(x2)}, f(x) = {FormatDouble(f2)}");

                if (Math.Abs(f2) < tolerance || Math.Abs(x2 - x1) < tolerance)
                {
                    x0 = x1;
                    f0 = f1;
                    x1 = x2;
                    f1 = f2;
                    converged = true;
                    break;
                }

                x0 = x1;
                f0 = f1;
                x1 = x2;
                f1 = f2;
            }

            log.AppendLine(new string('-', 60));

            if (converged)
            {
                log.AppendLine($"Raíz aproximada: x ≈ {FormatDouble(x1)}");
                log.AppendLine($"f(x) ≈ {FormatDouble(f1)}");
            }
            else
            {
                log.AppendLine("No se alcanzó la convergencia con los parámetros proporcionados.");
            }
        }
        catch (Exception ex)
        {
            log.AppendLine();
            log.AppendLine($"Error: {ex.Message}");
        }
        finally
        {
            ResultsTextBox.Text = log.ToString();
        }
    }

    private double NumericalDerivative(string functionText, double x)
    {
        double h = Math.Max(1e-6, Math.Abs(x) * 1e-5);
        double forward = _evaluator.Evaluate(functionText, x + h);
        double backward = _evaluator.Evaluate(functionText, x - h);
        return (forward - backward) / (2 * h);
    }

    private static double ReadDouble(string? text, double? defaultValue, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (defaultValue.HasValue)
            {
                return defaultValue.Value;
            }

            throw new InvalidOperationException($"Debes especificar un valor para {fieldName}.");
        }

        if (TryParseDouble(text, out double value))
        {
            return value;
        }

        throw new InvalidOperationException($"No se pudo interpretar el valor numérico de {fieldName}.");
    }

    private static int ReadInt(string? text, int defaultValue, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return defaultValue;
        }

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int value) ||
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        throw new InvalidOperationException($"No se pudo interpretar el valor entero de {fieldName}.");
    }

    private static bool TryParseDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
               double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
               double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("G17", CultureInfo.InvariantCulture);
    }
}
