using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Xml.Serialization;
using HackFramework;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace Map {
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public struct Point3DPlus {
            public Point3DPlus(Point3D point, Color color, double thickness) {
                this.point     = point;
                this.color     = color;
                this.thickness = thickness;
            }

            public Point3D point;
            public Color   color;
            public double  thickness;
        }

        private List<Point3DPlus> points    = new List<Point3DPlus>();
        private List<Point3DPlus> totalData = new List<Point3DPlus>();
        private Stopwatch         stopwatch = Stopwatch.StartNew();

        public MainWindow() {
            InitializeComponent();

            var bw = new BackgroundWorker();
            bw.DoWork += GatherData;
            bw.RunWorkerAsync();
        }

        private void GatherData(object sender, DoWorkEventArgs e) {
            while ( true ) {
                Thread.Sleep( 50 ); // 50ms data sampling period

                // Generate a test trace: an upward spiral with square corners
                double t   = this.stopwatch.Elapsed.TotalSeconds * 0.25;
                var    pos = Vec3.Zero;
                //double x, y, z = t * 0.5;
                Color color = default;

                ReadFromGame( ref pos, ref color, t );

                if(pos == Vec3.Zero) continue;

                var point  = new Point3DPlus( new Point3D( pos.x, pos.y, pos.z ), color, 1.5 );
                var invoke = false;

                lock (this.points) {
                    this.points.Add( point );
                    invoke = ( this.points.Count == 1 );
                }

                if ( invoke ) this.Dispatcher.BeginInvoke( DispatcherPriority.Background, (Action) PlotData );
            }
        }

        private void ReadFromGame(ref Vec3 pos, ref Color color, double t) {
            color = MkColor( t / 10d );
            pos   = Program.CalcPos();
        }

        static Color MkColor(double t) { return Color.FromRgb( (byte) ( ( Math.Sin( t ) + 1 ) * 127 ), (byte) ( ( Math.Cos( t ) + 1 ) * 127 ), (byte) ( ( -Math.Sin( t ) + 1 ) * 127 ) ); }

        private void PlotData() {
            if ( this.points.Count == 1 ) {
                Point3DPlus point;

                lock (this.points) {
                    point = this.points[0];
                    this.points.Clear();
                }

                this.totalData.Add( point );
                this.plot.AddPoint( point.point, point.color, point.thickness );
            }
            else {
                Point3DPlus[] pointsArray;

                lock (this.points) {
                    pointsArray = this.points.ToArray();
                    this.points.Clear();
                }

                this.totalData.AddRange( pointsArray );
                foreach ( Point3DPlus point in pointsArray ) this.plot.AddPoint( point.point, point.color, point.thickness );
            }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e) {
            //this.plot.ZoomExtents( 500 ); // zoom to extents
            //plot.ResetCamera();  // orient and zoom
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            var item = (MenuItem) sender;

            switch (item.TabIndex) {
                case 1:
                    this.plot.Clear();
                    break;
                case 2:
                    try {
                        var s = new SaveFileDialog { Filter = "*.XML|*.XML" };

                        if ( s.ShowDialog( this ) == true ) {
                            var x  = new XmlSerializer( this.totalData.GetType() );
                            var fs = File.Open( Path.GetDirectoryName( s.FileName ) + "\\" + Path.GetFileNameWithoutExtension( s.FileName ) + ".SavedMap" + Path.GetExtension( s.FileName ), FileMode.OpenOrCreate );
                            x.Serialize( fs, this.totalData );
                            fs.Close();
                            x = null;

                            MessageBox.Show( "Saving succeeded!" );
                        }
                    } catch (Exception ex) {
                        MessageBox.Show( ex.ToString() );
                    }

                    break;
                case 3:
                    try {
                        var o = new OpenFileDialog { Filter = "*.XML|*.XML" };

                        if ( o.ShowDialog( this ) == true ) {
                            var x  = new XmlSerializer( this.totalData.GetType() );
                            var fs = File.Open( o.FileName, FileMode.Open );

                            lock (this.points) {
                                this.points.AddRange( (List<Point3DPlus>) x.Deserialize( fs ) );
                            }

                            fs.Close();
                            x = null;
                            PlotData();
                            MessageBox.Show( "Loading succeeded!" );
                        }
                    } catch (Exception ex) {
                        MessageBox.Show( ex.ToString() );
                    }

                    break;
                case 99:
                    Program.Position.RecalculatePtr();
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { new ConfigView().Show(); }
    }

}
