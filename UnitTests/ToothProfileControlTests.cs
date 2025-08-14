using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
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
            this.Size = new System.Drawing.Size(800, 600);

            // Set up ToothProfileControl.
            var profileControl = new ToothProfileControl
            {
                Dock = DockStyle.Fill,
                ToothProfile = CreateSampleToothProfile(),
                Scale = 2, // Zoom level
                Offset = new System.Drawing.PointF(0, 0) // Panning offset
            };
            
            this.Controls.Add(profileControl);
        }
        private ToothProfile CreateSampleToothProfile()
        {
            var toothProfile = new ToothProfile();

            // Add root geometry
            toothProfile.AddGeometry("Root", new Geometry(GeometryType.Spline, new List<GearPoint>
            {
                new GearPoint(0, 0), new GearPoint(10, 20), new GearPoint(20, 30)
            }));

            // Add outside geometry
            toothProfile.AddGeometry("Outside", new Geometry(GeometryType.Arc, new List<GearPoint>
            {
                new GearPoint(20, 30), new GearPoint(30, 10), new GearPoint(25, 25)
            }));

            // Add centreline
            toothProfile.AddGeometry("Centreline", new Geometry(GeometryType.StraightLine, new List<GearPoint>
            {
                new GearPoint(0, 0), new GearPoint(30, 0)
            }));

            // Add a sample circle geometry

            Geometry circle = new Geometry(GeometryType.Circle,new List<GearPoint>
            {
                new GearPoint(0, 0) // Circle's center point.
            });
            circle.Parameters.Add("Radius", 100); // Assuming the Geometry class has a `Radius` property for circles.
            
            toothProfile.AddGeometry("Circle", circle);
           
    
            return toothProfile;
        }


    }
    
   

}