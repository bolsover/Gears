using System;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Gui.Views
{
    /// <summary>
    /// Bevel gear test bench: builds the closed profile sketch of a straight bevel gear blank, lists its construction points
    /// and shows the solid made by revolving the sketch 360 degrees about the X axis.
    /// </summary>
    public sealed class BevelTestForm : Form
    {
        private const int RevolveSegments = 144;

        private readonly HelixAssemblyViewport _viewport = new HelixAssemblyViewport();
        private readonly ElementHost _host = new ElementHost { Dock = DockStyle.Fill };
        private readonly NumericUpDown _numTeeth = Create(18, 1, 500, 0);
        private readonly NumericUpDown _numMateTeeth = Create(45, 1, 500, 0);
        private readonly NumericUpDown _numModule = Create(10.16m, 0.1m, 100, 3);
        private readonly NumericUpDown _numFaceWidth = Create(63.5m, 0.1m, 1000, 3);
        private readonly NumericUpDown _numShaftAngle = Create(90, 1, 179, 1);
        private readonly CheckBox _chkWireframe = new CheckBox { Text = "Wireframe", AutoSize = true };
        private readonly TextBox _report = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 9f)
        };

        private readonly Label _status = new Label { Dock = DockStyle.Bottom, Height = 22, TextAlign = ContentAlignment.MiddleLeft };

        public BevelTestForm()
        {
            Text = "Bevel tests - gear blank";
            Size = new Size(1100, 760);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);

            _host.Child = _viewport;
            BuildLayout();
            Shown += (s, e) => Rebuild(true);
        }

        /// <summary>The profile sketch that is currently shown; null when the inputs do not give a valid sketch.</summary>
        public BevelBlankProfile Profile { get; private set; }

        private static NumericUpDown Create(decimal value, decimal minimum, decimal maximum, int decimals)
        {
            return new NumericUpDown { Minimum = minimum, Maximum = maximum, DecimalPlaces = decimals, Value = value, Width = 90 };
        }

        private void BuildLayout()
        {
            var inputs = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6), WrapContents = true };
            AddInput(inputs, "Teeth", _numTeeth);
            AddInput(inputs, "Mating teeth", _numMateTeeth);
            AddInput(inputs, "Module (mm)", _numModule);
            AddInput(inputs, "Face width (mm)", _numFaceWidth);
            AddInput(inputs, "Shaft angle (deg)", _numShaftAngle);
            inputs.Controls.Add(_chkWireframe);

            var presetButton = new Button { Text = "18/45 sample", AutoSize = true };
            presetButton.Click += (s, e) => ApplySample();
            inputs.Controls.Add(presetButton);

            var fitButton = new Button { Text = "Fit view", AutoSize = true };
            fitButton.Click += (s, e) => _viewport.FitView();
            inputs.Controls.Add(fitButton);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            split.Panel1.Controls.Add(_host);
            split.Panel2.Controls.Add(_report);
            split.HandleCreated += (s, e) =>
            {
                try
                {
                    split.SplitterDistance = (int)(split.Width * 0.65);
                }
                catch (ArgumentException)
                {
                    // The window is smaller than the requested distance; keep the default.
                }
            };

            Controls.Add(split);
            Controls.Add(inputs);
            Controls.Add(_status);

            foreach (var numeric in new[] { _numTeeth, _numMateTeeth, _numModule, _numFaceWidth, _numShaftAngle })
            {
                numeric.ValueChanged += (s, e) => Rebuild(false);
            }

            _chkWireframe.CheckedChanged += (s, e) => Rebuild(false);
        }

        private static void AddInput(FlowLayoutPanel panel, string caption, NumericUpDown control)
        {
            panel.Controls.Add(new Label { Text = caption, AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            panel.Controls.Add(control);
        }

        private void ApplySample()
        {
            _numTeeth.Value = 18;
            _numMateTeeth.Value = 45;
            _numModule.Value = 10.16m;
            _numFaceWidth.Value = 63.5m;
            _numShaftAngle.Value = 90;
        }

        private void Rebuild(bool fit)
        {
            try
            {
                Profile = new BevelBlankProfile((int)_numTeeth.Value, (int)_numMateTeeth.Value, (double)_numModule.Value,
                    (double)_numFaceWidth.Value, (double)_numShaftAngle.Value);
                var mesh = Profile.BuildMesh(RevolveSegments);
                _viewport.ShowMesh(mesh, System.Windows.Media.Color.FromRgb(70, 130, 180), _chkWireframe.Checked, fit);
                _report.Text = Describe(Profile);
                _status.ForeColor = SystemColors.ControlText;
                _status.Text = "Revolved the closed profile sketch 360 degrees about the X axis.";
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                Profile = null;
                _viewport.ShowMesh(null, System.Windows.Media.Colors.Gray, false, false);
                _report.Text = string.Empty;
                _status.ForeColor = Color.Firebrick;
                _status.Text = ex.Message;
            }
        }

        private static string Describe(BevelBlankProfile p)
        {
            var text = new StringBuilder();
            string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
            text.AppendLine($"Gear {p.Teeth} teeth mating with {p.MateTeeth} teeth, shaft angle {F(p.ShaftAngleDeg)} deg");
            text.AppendLine($"Module {F(p.Module)} mm, face width {F(p.FaceWidth)} mm");
            text.AppendLine($"Pitch diameter {F(p.PitchDiameter)} mm");
            text.AppendLine($"Addendum {F(p.Addendum)} mm, dedendum {F(p.Dedendum)} mm, whole depth {F(p.WholeDepth)} mm");
            text.AppendLine($"Pitch cone angle {p.PitchConeAngleDeg.ToString("0.0000", CultureInfo.InvariantCulture)} deg");
            text.AppendLine($"Back cone angle  {p.BackConeAngleDeg.ToString("0.0000", CultureInfo.InvariantCulture)} deg");
            text.AppendLine($"Cone distance {F(p.ConeDistance)} mm");
            text.AppendLine();
            text.AppendLine("Reference points (X, Y) in mm:");
            AppendPoint(text, "pointA", p.PointA);
            AppendPoint(text, "pointB", p.PointB);
            AppendPoint(text, "pointC", p.PointC);
            AppendPoint(text, "pointD", p.PointD);
            AppendPoint(text, "pointE", p.PointE);
            text.AppendLine();
            text.AppendLine("Sketch points (X, Y) in mm:");
            AppendPoint(text, "pointF", p.PointF);
            AppendPoint(text, "pointD", p.PointD);
            AppendPoint(text, "pointE", p.PointE);
            AppendPoint(text, "pointG", p.PointG);
            AppendPoint(text, "pointH", p.PointH);
            AppendPoint(text, "pointI", p.PointI);
            text.AppendLine();
            text.AppendLine("Sketch lines:");
            foreach (var line in p.Lines)
            {
                text.AppendLine("  " + line.Key.PadRight(6) + " " + line.Value[0] + " -> " + line.Value[1] + "   length " + F((line.Value[1] - line.Value[0]).Length) + " mm");
            }

            return text.ToString();
        }

        private static void AppendPoint(StringBuilder text, string name, BevelBlankPoint point)
        {
            var x = point.X.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(10);
            var y = point.Y.ToString("0.###", CultureInfo.InvariantCulture).PadLeft(10);
            text.AppendLine("  " + name.PadRight(7) + " " + x + ", " + y);
        }
    }
}
