using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Bolsover.Gears.Builder;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;
using BrightIdeasSoftware;
using static Bolsover.Gears.Calculator.LaTeXUtils;
using static Bolsover.Gears.Calculator.ConversionUtils;


namespace Bolsover.Gears.Gui.Presenters
{
    public class GearViewPresenter
    {
        // private AlibreToothBuilder _alibreToothBuilder;
        // private IGearCalculator _gearCalculator;
        // private GearPairDesignOutputParams _gearPairDesignOutputParams;
        // private IToothPointsBuilder _toothPointsBuilder;
        private readonly GearView _view;
        private Gear _gear;
        // public GearPairDesignInputParams Model;

        public GearViewPresenter(GearView view)
        {
            _view = view;
            Initialise();
        }

        public void InitialiseGear(Gear gear)
        {
            _gear = gear;
            gear.Calculate();
            var pinion = gear.Gears[0];
            var wheel = gear.Gears[1];
            var normalModule = gear.GetVariable(VariableNames.NormalModule);
            var pressureAngle = gear.GetVariable(VariableNames.NormalPressureAngle);
            var helixAngle = gear.GetVariable(VariableNames.HelixAngle);
            var wheelTeeth = wheel.GetVariable(VariableNames.Teeth);
            var pinionTeeth = pinion.GetVariable(VariableNames.Teeth);
            var circularBacklash = gear.GetVariable(VariableNames.CircularBacklash);
            var rootFilletFactor = gear.GetVariable(VariableNames.RootFilletFactor);
            var tipReliefFactor = gear.GetVariable(VariableNames.TipReliefFactor);
            var standardCentreDistance = gear.GetVariable(VariableNames.StandardCentreDistance);
            var pinionProfileShift = pinion.GetVariable(VariableNames.NormalCoefficientOfProfileShift);
            var wheelProfileShift = wheel.GetVariable(VariableNames.NormalCoefficientOfProfileShift);
            var sumProfileShift = gear.GetVariable(VariableNames.SumNormalCoefficientOfProfileShift);

            _view.moduleNumericUpDown.Value = (decimal)normalModule.Value;
            _view.pressureAngleNumericUpDown.Value = (decimal)ConversionUtils.Degrees(pressureAngle.Value);
            _view.helixAngleNumericUpDown.Value = (decimal)ConversionUtils.Degrees(helixAngle.Value);
            _view.gearTeethNumericUpDown.Value = (decimal)wheelTeeth.Value;
            _view.pinionTeethNumericUpDown.Value = (decimal)pinionTeeth.Value;
            _view.rootFilletFactorNumericUpDown.Value = (decimal)rootFilletFactor.Value;
            _view.addendumFilletFactorNumericUpDown.Value = (decimal)tipReliefFactor.Value;
            _view.operatingCentreDistanceNumericUpDown.Value = (decimal)standardCentreDistance.Value;
            _view.normalBacklashNumericUpDown.Value = (decimal)circularBacklash.Value;
            _view.pinionProfileShiftNumericUpDown.Value = (decimal)pinionProfileShift.Value;
            _view.gearProfileShiftNumericUpDown.Value = (decimal)wheelProfileShift.Value;
            _view.totalNormalProfileShiftTextBox.Text = sumProfileShift.Value.ToString();
            SetupLabelImages(gear);
            
           var rowdata = GetDisplayData();
            _view.objectListView1.SetObjects(rowdata);
            SetupObjectListView();
        }

        private IEnumerable<DisplayData> GetDisplayData()
        {
          return  DisplayDataBuilderProvider.Get(_gear).BuildDisplayData(_gear);
        }

        private void Initialise()
        {
           SetupEventListeners();
           SetupObjectListView();
          }


        private void SetupLabelImages(Gear gear)

        {
            _view.normalModuleSymbolLabel.Text = "";
            _view.normalPressureAngleSymbolLabel.Text = "";
            _view.helixAngleSymbolLabel.Text = "";
            _view.teethLabel.Text = "";
            _view.circularBacklashSymbolLabel.Text = "";
            _view.rootFilletFactorSymbolLabel.Text = "";
            _view.tipReliefFactorSymbolLabel.Text = "";
            _view.operatingCentreSymbolLabel.Text = "";
            _view.totalProfileShiftSymbolLabel.Text = "";
            _view.profileShiftSymbolLabel.Text = "";
            _view.noteLabel.Text = "";
            _view.heightSymbol.Text = "";
           
            _view.normalModuleSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.NormalModule).Symbol);
            _view.normalPressureAngleSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.NormalPressureAngle).Symbol);
            _view.helixAngleSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.HelixAngle).Symbol);
            _view.teethLabel.Image = CreateImageFromLatex(gear.Gears[0].GetVariable(VariableNames.Teeth).Symbol + "," + gear.Gears[0].GetVariable(VariableNames.Teeth).Symbol);
            _view.circularBacklashSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.CircularBacklash).Symbol);
            _view.rootFilletFactorSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.RootFilletFactor).Symbol);
            _view.tipReliefFactorSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.TipReliefFactor).Symbol);
            _view.operatingCentreSymbolLabel.Image = CreateImageFromLatex(gear.GetVariable(VariableNames.StandardCentreDistance).Symbol);
            _view.totalProfileShiftSymbolLabel.Image = CreateImageFromLatex( gear.GetVariable(VariableNames.SumNormalCoefficientOfProfileShift).Symbol);
            _view.profileShiftSymbolLabel.Image =
                CreateImageFromLatex( gear.Gears[0].GetVariable(VariableNames.NormalCoefficientOfProfileShift).Symbol + "," + gear.Gears[1].GetVariable(VariableNames.NormalCoefficientOfProfileShift).Symbol);
            _view.heightSymbol.Image = CreateImageFromLatex( gear.GetVariable(VariableNames.HeightOfGear).Symbol);
        }

        private void SetupEventListeners()
        {
            _view.EditGearNumberOfTeethEvent += ViewOnEditGearNumberOfTeethEvent;
            _view.EditPinionNumberOfTeethEvent += ViewOnEditPinionNumberOfTeethEvent;
             _view.EditModuleEvent += ViewOnEditModuleEvent;
             _view.EditHelixAngleEvent += ViewOnEditHelixAngleEvent;
             _view.CancelEvent += ViewOnCancelEvent;
            // _view.BuildGearEvent += ViewOnBuildWheelEvent;
            // _view.BuildPinionEvent += ViewOnBuildPinionEvent;
             _view.EditPressureAngleEvent += ViewOnEditPressureAngleEvent;
             _view.EditGearHeightEvent += ViewOnEditGearHeightEvent;
            // _view.AutoManualEvent += ViewOnAutoManualEvent;
             _view.EditCentreDistanceEvent += ViewOnEditCentreDistanceEvent;
             _view.EditPinionProfileShiftEvent += ViewOnEditPinionProfileShiftEvent;
             _view.EditGearProfileShiftEvent += ViewOnEditGearProfileShiftEvent;
             _view.EditNormalBacklashEvent += ViewOnEditCircularBacklashEvent;
            // _view.EditGearStyleEvent += ViewOnEditGearStyleEvent;
             _view.EditRootFilletFactorEvent += ViewOnEditRootFilletFactorEvent;
             _view.EditAddendumFilletFactorEvent += ViewOnEditAddendumFilletFactorEvent;
            // ((GearDesignOutputParams) _gearPairDesignOutputParams.GearDesignOutput).GearChanged +=
            //     GearDesignOutputOnGearChanged;
            // ((GearDesignOutputParams) _gearPairDesignOutputParams.PinionDesignOutput).GearChanged +=
            //     GearDesignOutputOnGearChanged;
            // Model.Gear.GearChanged += GearDesignInputParamsOnGearChanged;
            // Model.Pinion.GearChanged += GearDesignInputParamsOnGearChanged;
            // Model.GearChanged += GearDesignInputParamsOnGearChanged;
            // _view.objectListView1.FormatRow += delegate(object sender1, FormatRowEventArgs e)
            // {
            //     var data = (DisplayData) e.Model;
            //     if (data.IsError) e.Item.BackColor = Color.Red;
            // };
        }

        private void SetupObjectListView()
        {
            _view.olvColumn1.AspectGetter = rowObject => ((DisplayData) rowObject).Item;
            _view.olvColumn2.AspectGetter = rowObject => ((DisplayData) rowObject).MetricValue;
            _view.olvColumn3.AspectGetter = rowObject => ((DisplayData) rowObject).ImperialValue;
            _view.olvColumn4.AspectGetter = rowObject => ((DisplayData) rowObject).Note;
            // _view.olvColumn5.ImageGetter = rowObject => ((DisplayData) rowObject).Symbol;
            // _view.olvColumn6.ImageGetter = rowObject => ((DisplayData) rowObject).Formula;
        }

       

        
        private void ViewOnEditAddendumFilletFactorEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.GetVariable(VariableNames.TipReliefFactor);
                variable.Value = newValue;
            }
          
            Calculate();
        }

        private void ViewOnEditRootFilletFactorEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.GetVariable(VariableNames.RootFilletFactor);
                variable.Value = newValue;
            }
          
            Calculate();
        }

       

        private void ViewOnCancelEvent(object sender, EventArgs e)
        {
            _view.FindForm()!.Close();
        }

        private void ViewOnEditHelixAngleEvent(object sender, EventArgs e)
        {
           
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = Radians((double) numericUpDown.Value);
                var variable = _gear.GetVariable(VariableNames.HelixAngle);
                variable.Value = newValue;
            }
          
            Calculate();
        }

        private void ViewOnEditModuleEvent(object sender, EventArgs e)
        {
           
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.GetVariable(VariableNames.NormalModule);
                variable.Value = newValue;
            }
          
            Calculate();
        }

        private void ViewOnEditGearNumberOfTeethEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.Gears[1].GetVariable(VariableNames.Teeth);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnEditPinionNumberOfTeethEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.Gears[0].GetVariable(VariableNames.Teeth);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnEditCircularBacklashEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.GetVariable(VariableNames.CircularBacklash);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnEditGearProfileShiftEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.Gears[1].GetVariable(VariableNames.NormalCoefficientOfProfileShift);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnEditPinionProfileShiftEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.Gears[0].GetVariable(VariableNames.NormalCoefficientOfProfileShift);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnEditCentreDistanceEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.GetVariable(VariableNames.WorkingCentreDistanceax);
                variable.Value = newValue;
            }
          
            Calculate();
        }

        private void ViewOnAutoManualEvent(object sender, EventArgs e)
        {
           
        }

        private void ViewOnEditGearHeightEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = (double) numericUpDown.Value;
                var variable = _gear.GetVariable(VariableNames.HeightOfGear);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnEditPressureAngleEvent(object sender, EventArgs e)
        {
            if (sender is NumericUpDown numericUpDown)
            {
                var newValue = Radians((double) numericUpDown.Value);
                var variable = _gear.GetVariable(VariableNames.NormalPressureAngle);
                variable.Value = newValue;
            }
       
            Calculate();
        }

        private void ViewOnBuildPinionEvent(object sender, EventArgs e)
        {
            Calculate();
            // var gearDetails = GetPinionDetails();
            // SetupBuilderForGearType(false);
            // var tooth = _toothPointsBuilder.Build(_gearPairDesignOutputParams.PinionDesignOutput);
            // _alibreToothBuilder ??= new AlibreToothBuilder();
            // _alibreToothBuilder.Build(tooth, gearDetails.SaveFile, gearDetails.Template,
            //     _gearPairDesignOutputParams.PinionDesignOutput);
        }

       

        private void ViewOnBuildWheelEvent(object sender, EventArgs e)
        {
            Calculate();
            // var gearDetails = GetGearDetails();
            // SetupBuilderForGearType(true);
            // var tooth = _toothPointsBuilder.Build(_gearPairDesignOutputParams.GearDesignOutput);
            // _alibreToothBuilder ??= new AlibreToothBuilder();
            // _alibreToothBuilder.Build(tooth, gearDetails.SaveFile, gearDetails.Template,
            //     _gearPairDesignOutputParams.GearDesignOutput);
        }

        private (string SaveFile, string Template) GetGearDetails()
        {
            //var isHelical = Model.Gear.Style.HasFlag(Helical);
            var isHelical = false;
            var saveFile = isHelical
                ? "HelicalWheelPleaseSaveAs.AD_PRT"
                : "WheelPleaseSaveAs.AD_PRT";

            var template = isHelical
                ? "HelicalWheelTemplate.AD_PRT"
                : "WheelTemplate.AD_PRT";

            return (saveFile, template);
        }

        private (string SaveFile, string Template) GetPinionDetails()
        {
            // var isHelical = Model.Pinion.Style.HasFlag(Helical);
            var isHelical = false;
            var saveFile = isHelical
                ? "HelicalPinionPleaseSaveAs.AD_PRT"
                : "PinionPleaseSaveAs.AD_PRT";

            var template = isHelical
                ? "HelicalPinionTemplate.AD_PRT"
                : "PinionTemplate.AD_PRT";

            return (saveFile, template);
        }

        private void Calculate()
        {
            _view.totalNormalProfileShiftTextBox.Text = _gear.GetVariable(VariableNames.SumNormalCoefficientOfProfileShift).Value.ToString("0.0000");
            var rowdata = GetDisplayData();
            _view.objectListView1.SetObjects(rowdata);
           
        }

      
    }
}