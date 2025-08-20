using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Model;
using NUnit.Framework;



namespace UnitTests
{
    public class ToothProfileControlTests
    {
        [Test]
        public void ProfileControlTests() {
            Form form = new ToothProfileControlForm();
            System.Windows.Forms.Application.Run(form);
        }
        
    }

    public class ToothProfileControlForm : Form
    {
        public ToothProfileControlForm()
        {
            // Set up the form.
            this.Text = "Tooth Profile Renderer";
            this.Size = new System.Drawing.Size(1000, 800);
            
            // Create a scrollable container for the profile control.
            var scrollablePanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true, // Enable scrolling.
                
            };
            

            // Set up ToothProfileControl.
            var profileControl = new ToothProfileControl
            {
                Dock = DockStyle.None, // Disable fill docking to allow scrolling.
              //  ToothProfile = CreateSampleToothProfile(),
                ToothProfile = CreateToothProfileUsingCalculator(),
                Scale = 2, // Zoom level
                Offset = new PointF(0, 0),
                Location = new Point(0, 0) // Align to the top-left corner.

            };

            // Adjust the ToothProfileControl size when the scrollablePanel resizes.
            scrollablePanel.Resize += (sender, args) =>
            {
                profileControl.Size = scrollablePanel.ClientSize; // Make it match panel size.
                profileControl.Invalidate(); // Trigger a repaint in case content should adjust.
            };

            // Add the profileControl to the scrollable panel.
            scrollablePanel.Controls.Add(profileControl);

            // Add the scrollable panel to the form.
            this.Controls.Add(scrollablePanel);

            // Initialize the size of the profile control to match the panel.
            profileControl.Size = scrollablePanel.ClientSize;


        }

        private ToothProfile CreateToothProfileUsingCalculator()
        {
            ToothProfileCalculator calculator = new ToothProfileCalculator();
            var gear = new Gear(GearContext.ExternalNormalHelical, CalculationMode.Automatic);
            return calculator.Calculate(gear);
           
            
        }
        
        
        private ToothProfile CreateSampleToothProfile()
        {
            var toothProfile = new ToothProfile();
            float[] dashValues = { 5, 2, 15, 4 };
            var blackPen = new Pen(Color.DarkGray, (float)0.25);
            blackPen.DashPattern = dashValues;
            var rootCircle = new Feature(FeatureType.Circle,new List<GearPoint>
            {
                new GearPoint(0, 0) // Circle's center point.
            });
            rootCircle.Parameters.Add("Radius", 13.75); // Assuming the Feature class has a `Radius` property for circles.
            rootCircle.Pen = blackPen;
            toothProfile.AddFeature("RootCircle", rootCircle);
            
            var baseCircle = new Feature(FeatureType.Circle,new List<GearPoint>
            {
                new GearPoint(0, 0) // Circle's center point.
            });
            baseCircle.Parameters.Add("Radius", 14.1); // Assuming the Feature class has a `Radius` property for circles.
            baseCircle.Pen = blackPen;
            toothProfile.AddFeature("BaseCircle", baseCircle);
            
            var pitchCircle = new Feature(FeatureType.Circle,new List<GearPoint>
            {
                new GearPoint(0, 0) // Circle's center point.
            });
            pitchCircle.Parameters.Add("Radius", 15.0); // Assuming the Feature class has a `Radius` property for circles.
            pitchCircle.Pen = blackPen;
            toothProfile.AddFeature("PitchCircle", pitchCircle);
            
            var outsideCircle = new Feature(FeatureType.Circle,new List<GearPoint>
            {
                new GearPoint(0, 0) // Circle's center point.
            });
            outsideCircle.Parameters.Add("Radius", 16.0); // Assuming the Feature class has a `Radius` property for circles.
            outsideCircle.Pen = blackPen;
            toothProfile.AddFeature("outsideCircle", outsideCircle);
            
            // Add centre root line rhs
            var centreToRootRhs = new Feature(FeatureType.StraightLine, new List<GearPoint>
            {
                new GearPoint(0, 0), new GearPoint(13.74, 0.51)});
            centreToRootRhs.Pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            toothProfile.AddFeature("CentreToRootRhs",centreToRootRhs);
            var centreToRootLhs = new Feature(FeatureType.StraightLine, new List<GearPoint>
            {
                new GearPoint(0, 0), new GearPoint(13.51, -2.34)});
            centreToRootLhs.Pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            toothProfile.AddFeature("CentreToRootLhs",centreToRootLhs);
            
            var rootRhsToRelief = new Feature(FeatureType.StraightLine, new List<GearPoint>
            {
                new GearPoint(13.74, 0.51), new GearPoint(13.75, 0.19)});
            rootRhsToRelief.Pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 2);
            toothProfile.AddFeature("RootRhsToRelief",rootRhsToRelief);
            
            var rootLhsToRelief = new Feature(FeatureType.StraightLine, new List<GearPoint>
            {
                new GearPoint(13.51, -2.34), new GearPoint(13.6, -2.02)});
            rootLhsToRelief.Pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 2);
            toothProfile.AddFeature("RootLhsToRelief",rootLhsToRelief);
            
            var rhsRootRelief =  new Feature(FeatureType.Arc, new List<GearPoint>
            {
                new GearPoint(13.75, 0.19),new GearPoint(13.93, 0),   new GearPoint(13.94, 0.19)
            });
            rhsRootRelief.Pen = new System.Drawing.Pen(System.Drawing.Color.Green, 2);
            
            toothProfile.AddFeature("RhsRootRelief",rhsRootRelief);
            
            var lhsRootRelief =  new Feature(FeatureType.Arc, new List<GearPoint>
            {
                new GearPoint(13.8, -1.87),new GearPoint(13.6, -2.02),   new GearPoint(13.78, -2.06)
            });
            lhsRootRelief.Pen = new System.Drawing.Pen(System.Drawing.Color.Green, 2);
            
            toothProfile.AddFeature("LhsRootRelief",lhsRootRelief);
            
            var rhsReliefToBase = new Feature(FeatureType.StraightLine, new List<GearPoint>
            {
                new GearPoint(13.93, 0), new GearPoint(14.09, 0)});
            rhsReliefToBase.Pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            toothProfile.AddFeature("RhsReliefToBase",rhsReliefToBase);
            
            var lhsReliefToBase = new Feature(FeatureType.StraightLine, new List<GearPoint>
            {
                new GearPoint(13.8, -1.87), new GearPoint(13.96, -1.89)});
            lhsReliefToBase.Pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            toothProfile.AddFeature("LhsReliefToBase",lhsReliefToBase);
            
            var rhsInvolute = new Feature(FeatureType.Spline, new List<GearPoint>
            {
                new GearPoint(14.09, 0), new GearPoint(14.46, -0.05), new GearPoint(15, -0.22), new GearPoint(15.44, -0.41), new GearPoint(15.85, -0.63)});
            rhsInvolute.Pen = new System.Drawing.Pen(System.Drawing.Color.BlueViolet, 2);
            toothProfile.AddFeature("RhsInvolute",rhsInvolute);
            
            var lhsInvolute = new Feature(FeatureType.Spline, new List<GearPoint>
            {
                new GearPoint(13.96, -1.89), new GearPoint(14.44, -1.88), new GearPoint(14.88, -1.79), new GearPoint(15.31, -1.68), new GearPoint(15.79, -1.5)});
            lhsInvolute.Pen = new System.Drawing.Pen(System.Drawing.Color.BlueViolet, 2);
            toothProfile.AddFeature("LhsInvolute",lhsInvolute);
            
            var rhsTipRelief =  new Feature(FeatureType.Arc, new List<GearPoint>
            {
                new GearPoint(15.98, -0.86),new GearPoint(15.85, -0.63),   new GearPoint(15.72, -0.84)
            });
            rhsTipRelief.Pen = new System.Drawing.Pen(System.Drawing.Color.Green, 2);
            
            toothProfile.AddFeature("rhsTipRelief",rhsTipRelief);
    
          
            
            var lhsTipRelief =  new Feature(FeatureType.Arc, new List<GearPoint>
            {
                new GearPoint(15.79, -1.5),new GearPoint(15.93, -1.25),   new GearPoint(15.7, -1.27)
            });
            lhsTipRelief.Pen = new System.Drawing.Pen(System.Drawing.Color.Green, 2);
            
            toothProfile.AddFeature("lhsTipRelief",lhsTipRelief);
            
            var tip = new Feature(FeatureType.Arc, new List<GearPoint>
            {
                new GearPoint(15.95, -1.29),new GearPoint(15.98, -0.86),  new GearPoint(0,0)});
            tip.Pen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
            toothProfile.AddFeature("tip",tip);
            
            // var arc3Point = new Feature(FeatureType.Arc3Point, new List<GearPoint>
            // {
            //     new GearPoint(-10,-10),new GearPoint(20,20),  new GearPoint(30,30)});
            // arc3Point.Pen = new System.Drawing.Pen(System.Drawing.Color.DarkOrange, 2);
            // toothProfile.AddFeature("arc3point",arc3Point);
            //
            return toothProfile;
        }


    }
    
   

}