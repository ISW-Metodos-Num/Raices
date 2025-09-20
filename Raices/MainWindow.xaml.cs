using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Raices;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<IterStep> _steps = new();
    public MainWindow()
    {
        InitializeComponent();
        dgIteraciones.ItemsSource = _steps;
    }

    // f(x) = 4x^3 - 6x^2 + 7x - 2.3
    private static double f(double x) =>
        4 * Math.Pow(x, 3) - 6 * Math.Pow(x, 2) + 7 * x - 2.3;

    // g(x) = x^2 * sqrt(|cos x|) - 5
    private static double g(double x) =>
        Math.Pow(x, 2) * Math.Sqrt(Math.Abs(Math.Cos(x))) - 5;

    private void Calcular_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Func<double, double> func = cbFuncion.SelectedIndex switch
            {
                0 => f,
                1 => g,
                _ => f
            };

            double xi = ParseDouble(txtXi.Text);
            double xf = ParseDouble(txtXf.Text);
            double eamax = ParseDouble(txtEamax.Text);

            if (double.IsNaN(xi) || double.IsNaN(xf) || double.IsNaN(eamax))
                throw new ArgumentException("Verifica los valores numéricos de xi, xf y eamax.");

            // Si el usuario quiere interpretar como %, convertir a fracción
            if (chkEaPorc.IsChecked == true) eamax /= 100.0;

            if (eamax <= 0)
                throw new ArgumentException("ea máx debe ser mayor que 0 (usa fracción, p.ej. 0.001 = 0.1%).");

            _steps.Clear();

            double raiz;
            int it;

            // Validación: cambio de signo
            if (func(xi) * func(xf) > 0)
                throw new ArgumentException("El intervalo no acota una raíz (no hay cambio de signo).");

            if (rbBiseccion.IsChecked == true)
            {
                raiz = BiseccionConTabla(func, xi, xf, eamax, out it);
                txtResumenMetodo.Text = "Método: Bisección";
            }
            else
            {
                raiz = ReglaFalsaConTabla(func, xi, xf, eamax, out it);
                txtResumenMetodo.Text = "Método: Regla Falsa";
            }

            lblRaiz.Text = raiz.ToString("F6", CultureInfo.InvariantCulture);
            lblFRAIZ.Text = func(raiz).ToString("F6", CultureInfo.InvariantCulture);
            lblIter.Text = _steps.Count.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Limpiar_Click(object sender, RoutedEventArgs e)
    {
        _steps.Clear();
        lblRaiz.Text = lblFRAIZ.Text = lblIter.Text = "";
        txtResumenMetodo.Text = "";
    }

    private static double ParseDouble(string s)
    {
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out double v)) return v;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
        return double.NaN;
    }

    // ------------------------------
    // MÉTODO: BISECCIÓN (con tabla)
    // ------------------------------
    private static double BiseccionConTabla(Func<double, double> f, double xi, double xf, double eamax, out int iteraciones)
    {
        double fxi = f(xi);
        double fxf = f(xf);
        double xr = xi, xrPrev = xr;
        iteraciones = 0;
        const int iterMax = 1000;

        var win = Application.Current.Windows[0] as MainWindow;
        var tabla = win!._steps;

        while (iteraciones < iterMax)
        {
            xrPrev = xr;
            xr = 0.5 * (xi + xf);
            double fxr = f(xr);

            double ea = (iteraciones == 0) ? double.PositiveInfinity : Math.Abs((xr - xrPrev) / xr);

            tabla.Add(new IterStep
            {
                N = iteraciones + 1,
                Xi = xi,
                Xf = xf,
                Xr = xr,
                FXi = fxi,
                FXu = fxf,
                FXr = fxr,
                EaPercent = double.IsInfinity(ea) ? double.NaN : (ea * 100.0)
            });

            iteraciones++;

            if (fxr == 0.0 || ea <= eamax) break;

            if (fxi * fxr < 0)
            {
                xf = xr;
                fxf = fxr;
            }
            else
            {
                xi = xr;
                fxi = fxr;
            }
        }
        return xr;
    }

    // --------------------------------
    // MÉTODO: REGLA FALSA (con tabla)
    // --------------------------------
    private static double ReglaFalsaConTabla(Func<double, double> f, double xi, double xf, double eamax, out int iteraciones)
    {
        double fxi = f(xi);
        double fxf = f(xf);
        double xr = xi, xrPrev = xr;
        iteraciones = 0;
        const int iterMax = 1000;

        var win = Application.Current.Windows[0] as MainWindow;
        var tabla = win!._steps;

        while (iteraciones < iterMax)
        {
            xrPrev = xr;
            xr = xf - fxf * (xi - xf) / (fxi - fxf);  // intersección de la secante con el eje x
            double fxr = f(xr);

            double ea = (iteraciones == 0) ? double.PositiveInfinity : Math.Abs((xr - xrPrev) / xr);

            tabla.Add(new IterStep
            {
                N = iteraciones + 1,
                Xi = xi,
                Xf = xf,
                Xr = xr,
                FXi = fxi,
                FXu = fxf,
                FXr = fxr,
                EaPercent = double.IsInfinity(ea) ? double.NaN : (ea * 100.0)
            });

            iteraciones++;

            if (fxr == 0.0 || ea <= eamax) break;

            if (fxi * fxr < 0)
            {
                xf = xr;
                fxf = fxr;
            }
            else
            {
                xi = xr;
                fxi = fxr;
            }
        }
        return xr;
    }
}


public class IterStep
{
    public int N { get; set; }
    public double Xi { get; set; }
    public double Xf { get; set; }      // xu (límite superior)
    public double FXi { get; set; }     // f(xi)
    public double FXu { get; set; }     // f(xu)
    public double Xr { get; set; }
    public double FXr { get; set; }
    public double EaPercent { get; set; } // error aprox. relativo en %
}