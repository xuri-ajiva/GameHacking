//Copyright (c) 2018 Bruce Greene

//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights to 
//use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
//of the Software, and to permit persons to whom the Software is furnished to do 
//so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all 
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS 
//FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
//IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
//WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace Map
{
    /// <summary>
    /// Plot a trace in 3D space with marker, axes and bounding box.
    /// </summary>
    /// <remarks>
    /// This class utilizes the Helix Toolkit which is licensed under the MIT License.
    /// 
    /// The MIT License (MIT)
    /// Copyright(c) 2018 Helix Toolkit contributors
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a
    /// copy of this software and associated documentation files (the
    /// "Software"), to deal in the Software without restriction, including
    /// without limitation the rights to use, copy, modify, merge, publish,
    /// distribute, sublicense, and/or sell copies of the Software, and to
    /// permit persons to whom the Software is furnished to do so, subject to
    /// the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be included
    /// in all copies or substantial portions of the Software.
    /// 
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
    /// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    /// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    /// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    /// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    /// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    /// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    /// </remarks>
    public class HelixPlot : HelixViewport3D
    {
        private TruncatedConeVisual3D marker;
        private BillboardTextVisual3D coords;
        private double labelOffset, minDistanceSquared;
        private string coordinateFormat;
        private List<LinesVisual3D> trace;
        private LinesVisual3D path;
        private Point3D point0;  // last point
        private Vector3D delta0;  // (dx,dy,dz)

        /// <summary>Initializes a new instance of the <see cref="HelixPlot"/> class.</summary>
        public HelixPlot()
            : base()
        {
            this.ZoomExtentsWhenLoaded = true;
            this.ShowCoordinateSystem  = false;
            this.ShowViewCube          = false;
            this.ShowFrameRate         = true; // very useful diagnostic info
            this.ShowTriangleCountInfo = true; // very useful diagnostic info

            // Default configuration:
            this.AxisLabels    = "X,Y,Z";
            this.BoundingBox   = new Rect3D(0, 0, 0, 100, 100, 50);
            this.TickSize      = 10;
            this.MinDistance   = 0.1;
            this.DecimalPlaces = 1;
            this.Background    = Brushes.White;
            this.AxisBrush     = Brushes.Gray;
            this.MarkerBrush   = Brushes.Red;
            this.Elements      = EElements.All;
            CreateElements();
        }

        /// <summary>Axis labels separated by commas ("X,Y,Z" default).</summary>
        public string AxisLabels { get; set; }

        /// <summary>XYZ bounding box for the 3D plot.</summary>
        public Rect3D BoundingBox { get; set; }

        /// <summary>Distance between ticks on the XY grid.</summary>
        public double TickSize { get; set; }

        /// <summary>A point closer than this distance from the previous point will not be plotted.</summary>
        public double MinDistance { get; set; }

        /// <summary>Number of decimal places for the marker coordinates.</summary>
        public int DecimalPlaces { get; set; }

        /// <summary>Brush used for the axes, grid and bounding box.</summary>
        public SolidColorBrush AxisBrush { get; set; }

        /// <summary>Brush used for the marker cone and coordinates.</summary>
        public SolidColorBrush MarkerBrush { get; set; }

        /// <summary>Determines which plot elements are included.</summary>
        /// <seealso cref="Elements"/>
        [Flags]
        public enum EElements
        {
            /// <summary>Traces only.</summary>
            None = 0x00,
            /// <summary>XYZ axes.</summary>
            Axes = 0x01,
            /// <summary>XY grid.</summary>
            Grid = 0x02,
            /// <summary>XYZ bounding box.</summary>
            BoundingBox = 0x04,
            /// <summary>Marker cone and coordinates.</summary>
            Marker = 0x08,
            /// <summary>Axes, grid, bounding box and marker.</summary>
            All = 0x0F
        };

        /// <summary>Determines which plot elements are included.</summary>
        public EElements Elements { get; set; }

        /// <summary>Gets the current trace color.</summary>
        public Color TraceColor { get { return (this.path != null) ? this.path.Color : Colors.Black; } }

        /// <summary>Gets the current trace thickness.</summary>
        public double TraceThickness { get { return (this.path != null) ? this.path.Thickness : 1; } }

        /// <summary>Creates the plot elements.</summary>
        /// <remarks>Changes to the bounding box and other parameters will not take effect until this method is called.</remarks>
        public void CreateElements()
        {
            this.Children.Clear();
            this.Children.Add(new DefaultLights());

            string[] labels = this.AxisLabels.Split(',');
            if (labels.Length < 3)
                labels = new string[] { "X", "Y", "Z" };

            double bbSize        = Math.Max(Math.Max(this.BoundingBox.SizeX, this.BoundingBox.SizeY), this.BoundingBox.SizeZ);
            double lineThickness = bbSize        / 1000;
            double arrowOffset   = lineThickness * 30;
            this.labelOffset        = lineThickness    * 50;
            this.minDistanceSquared = this.MinDistance * this.MinDistance;

            if (this.Elements.HasFlag(EElements.Grid))
            {
                var grid = new GridLinesVisual3D();
                grid.Center        = new Point3D(this.BoundingBox.X + 0.5 * this.BoundingBox.SizeX, this.BoundingBox.Y + 0.5 * this.BoundingBox.SizeY, this.BoundingBox.Z);
                grid.Length        = this.BoundingBox.SizeX;
                grid.Width         = this.BoundingBox.SizeY;
                grid.MinorDistance = this.TickSize;
                grid.MajorDistance = bbSize;
                grid.Thickness     = lineThickness;
                grid.Fill          = this.AxisBrush;
                this.Children.Add(grid);
            }

            if (this.Elements.HasFlag(EElements.Axes))
            {
                var arrow = new ArrowVisual3D();
                arrow.Point2   = new Point3D((this.BoundingBox.X + this.BoundingBox.SizeX) + arrowOffset, 0.0, 0.0);
                arrow.Diameter = lineThickness * 5;
                arrow.Fill     = this.AxisBrush;
                this.Children.Add(arrow);

                var label = new BillboardTextVisual3D();
                label.Text       = labels[0];
                label.FontWeight = FontWeights.Bold;
                label.Foreground = this.AxisBrush;
                label.Position   = new Point3D((this.BoundingBox.X + this.BoundingBox.SizeX) + this.labelOffset, 0.0, 0.0);
                this.Children.Add(label);

                arrow          = new ArrowVisual3D();
                arrow.Point2   = new Point3D(0.0, (this.BoundingBox.Y + this.BoundingBox.SizeY) + arrowOffset, 0.0);
                arrow.Diameter = lineThickness * 5;
                arrow.Fill     = this.AxisBrush;
                this.Children.Add(arrow);

                label            = new BillboardTextVisual3D();
                label.Text       = labels[1];
                label.FontWeight = FontWeights.Bold;
                label.Foreground = this.AxisBrush;
                label.Position   = new Point3D(0.0, (this.BoundingBox.Y + this.BoundingBox.SizeY) + this.labelOffset, 0.0);
                this.Children.Add(label);

                if (this.BoundingBox.SizeZ > 0)
                {
                    arrow          = new ArrowVisual3D();
                    arrow.Point2   = new Point3D(0.0, 0.0, (this.BoundingBox.Z + this.BoundingBox.SizeZ) + arrowOffset);
                    arrow.Diameter = lineThickness * 5;
                    arrow.Fill     = this.AxisBrush;
                    this.Children.Add(arrow);

                    label            = new BillboardTextVisual3D();
                    label.Text       = labels[2];
                    label.FontWeight = FontWeights.Bold;
                    label.Foreground = this.AxisBrush;
                    label.Position   = new Point3D(0.0, 0.0, (this.BoundingBox.Z + this.BoundingBox.SizeZ) + this.labelOffset);
                    this.Children.Add(label);
                }
            }

            if (this.Elements.HasFlag(EElements.BoundingBox) && this.BoundingBox.SizeZ > 0)
            {
                var box = new BoundingBoxWireFrameVisual3D();
                box.BoundingBox = this.BoundingBox;
                box.Thickness   = 1;
                box.Color       = this.AxisBrush.Color;
                this.Children.Add(box);
            }

            if (this.Elements.HasFlag(EElements.Marker))
            {
                this.marker            = new TruncatedConeVisual3D();
                this.marker.Height     = this.labelOffset;
                this.marker.BaseRadius = 0.0;
                this.marker.TopRadius  = this.labelOffset / 5;
                this.marker.TopCap     = true;
                this.marker.Origin     = new Point3D(0.0, 0.0, 0.0);
                this.marker.Normal     = new Vector3D(-1.0, -1.0, 1.0);
                this.marker.Fill       = this.MarkerBrush;
                this.Children.Add(this.marker);

                this.coords            = new BillboardTextVisual3D();
                this.coordinateFormat  = string.Format("{{0:F{0}}}, {{1:F{0}}}, {{2:F{0}}}", this.DecimalPlaces, this.DecimalPlaces, this.DecimalPlaces); // "{0:F2}, {1:F2}, {2:F2}"
                this.coords.Text       = string.Format(this.coordinateFormat, 0.0, 0.0, 0.0);
                this.coords.Foreground = this.MarkerBrush;
                this.coords.Position   = new Point3D(-this.labelOffset, -this.labelOffset, this.labelOffset);
                this.Children.Add(this.coords);
            }
            else
            {
                this.marker = null;
                this.coords = null;
            }

            if (this.trace != null)
            {
                foreach (LinesVisual3D p in this.trace)
                    this.Children.Add(p);
                this.path = this.trace[this.trace.Count - 1];
            }
        }

        /// <summary>Clears all traces.</summary>
        public void Clear()
        {
            this.trace = null;
            this.path  = null;
            CreateElements();
        }

        /// <summary>
        /// Creates a new trace.
        /// </summary>
        /// <remarks>Existing traces will remain in the plot until <see cref="Clear"/> or <see cref="CreateElements"/> is called.</remarks>
        /// <param name="point">The (X,Y,Z) location.</param>
        /// <param name="color">The initial color.</param>
        /// <param name="thickness">The initial line thickness.</param>
        /// <returns>The trace count.</returns>
        /// <seealso cref="Clear"/>
        public void NewTrace(Point3D point, Color color, double thickness = 1)
        {
            this.path           = new LinesVisual3D();
            this.path.Color     = color;
            this.path.Thickness = thickness;
            this.trace          = new List<LinesVisual3D>();
            this.trace.Add(this.path);
            this.Children.Add(this.path);
            this.point0 = point;
            this.delta0 = new Vector3D();

            if (this.marker != null)
            {
                this.marker.Origin   = point;
                this.coords.Position = new Point3D(point.X - this.labelOffset, point.Y - this.labelOffset, point.Z + this.labelOffset);
                this.coords.Text     = string.Format(this.coordinateFormat, point.X, point.Y, point.Z);
            }
        }

        /// <summary>
        /// Creates a new trace.
        /// </summary>
        /// <remarks>Existing traces will remain in the plot until <see cref="Clear"/> or <see cref="CreateElements"/> is called.</remarks>
        /// <param name="x">The initial X location.</param>
        /// <param name="y">The initial Y location.</param>
        /// <param name="z">The initial Z location.</param>
        /// <param name="color">The initial color.</param>
        /// <param name="thickness">The initial line thickness.</param>
        /// <returns>The trace count.</returns>
        /// <seealso cref="Clear"/>
        public void NewTrace(double x, double y, double z, Color color, double thickness = 1)
        {
            NewTrace(new Point3D(x, y, z), color, thickness);
        }

        /// <summary>
        /// Adds a point to the current trace with a specified color.
        /// </summary>
        /// <param name="point">The (X,Y,Z) location.</param>
        /// <param name="color">The color.</param>
        /// <param name="thickness">The line thickness (optional).</param>
        /// <seealso cref="AddPoint(double, double, double, Color, double)"/>
        public void AddPoint(Point3D point, Color color, double thickness = -1)
        {
            if (this.trace == null)
            {
                NewTrace(point, color, (thickness > 0) ? thickness : 1);
                return;
            }

            if ((point - this.point0).LengthSquared < this.minDistanceSquared) return; // less than min distance from last point

            if (this.path.Color != color || (thickness > 0 && this.path.Thickness != thickness))
            {
                if (thickness <= 0)
                    thickness = this.path.Thickness;

                this.path           = new LinesVisual3D();
                this.path.Color     = color;
                this.path.Thickness = thickness;
                this.trace.Add(this.path);
                this.Children.Add(this.path);
            }

            // If line segments AB and BC have the same direction (small cross product) then remove point B.
            bool sameDir = false;
            var  delta   = new Vector3D(point.X - this.point0.X, point.Y - this.point0.Y, point.Z - this.point0.Z);
            delta.Normalize();  // use unit vectors (magnitude 1) for the cross product calculations
            if (this.path.Points.Count > 0)
            {
                double xp2 = Vector3D.CrossProduct(delta, this.delta0).LengthSquared;
                sameDir = (xp2 < 0.0005);  // approx 0.001 seems to be a reasonable threshold from logging xp2 values
                //if (!sameDir) Title = string.Format("xp2={0:F6}", xp2);
            }

            if (sameDir)  // extend the current line segment
            {
                this.path.Points[this.path.Points.Count - 1] =  point;
                this.point0                                  =  point;
                this.delta0                                  += delta;
            }
            else  // add a new line segment
            {
                this.path.Points.Add(this.point0);
                this.path.Points.Add(point);
                this.point0 = point;
                this.delta0 = delta;
            }

            if (this.marker != null)
            {
                this.marker.Origin   = point;
                this.coords.Position = new Point3D(point.X - this.labelOffset, point.Y - this.labelOffset, point.Z + this.labelOffset);
                this.coords.Text     = string.Format(this.coordinateFormat, point.X, point.Y, point.Z);
            }
        }

        /// <summary>
        /// Adds a point to the current trace.
        /// </summary>
        /// <param name="point">The (X,Y,Z) location.</param>
        /// <seealso cref="AddPoint(Point3D, Color, double)"/>
        public void AddPoint(Point3D point)
        {
            if (this.path == null)
            {
                NewTrace(point, Colors.Black, 1);
                return;
            }

            AddPoint(point, this.path.Color, this.path.Thickness);
        }

        /// <summary>
        /// Adds a point to the current trace with a specified color.
        /// </summary>
        /// <param name="x">The X location.</param>
        /// <param name="y">The Y location.</param>
        /// <param name="z">The Z location.</param>
        /// <param name="color">The color.</param>
        /// <param name="thickness">The line thickness (optional).</param>
        /// <seealso cref="AddPoint(Point3D, Color, double)"/>
        public void AddPoint(double x, double y, double z, Color color, double thickness = -1)
        {
            AddPoint(new Point3D(x, y, z), color, thickness);
        }

        /// <summary>
        /// Adds a point to the current trace.
        /// </summary>
        /// <param name="x">The X location.</param>
        /// <param name="y">The Y location.</param>
        /// <param name="z">The Z location.</param>
        /// <seealso cref="AddPoint(double, double, double, Color, double)"/>
        public void AddPoint(double x, double y, double z)
        {
            if (this.path == null) return;

            AddPoint(new Point3D(x, y, z), this.path.Color, this.path.Thickness);
        }
    }
}
