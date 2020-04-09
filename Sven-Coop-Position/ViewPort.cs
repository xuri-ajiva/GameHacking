using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HackFramework;

namespace Sven_Coop_Position {
    public partial class ViewPort : Form {

        public object            LockObj      = new object();
        public PanelDoubleBuffer doubleBuffer = new PanelDoubleBuffer();

        public ViewPort() {
            InitializeComponent();
            this.Controls.Add( this.doubleBuffer );
        }

        public void SetBackIng(Image image) {
            lock (this.LockObj) {
                this.doubleBuffer.BackgroundImage = image;
            }

            this.ClientSize = image.Size;
        }

        private void ViewPort_Load(object sender, EventArgs e) { this.refreshTimer.Start(); }

        private void RefreshTimer_Tick(object sender, EventArgs e) {
            lock (this.LockObj) {
                this.doubleBuffer.Refresh();

                for ( int i = 0; i < 100; i++ ) {
                    Application.DoEvents();
                }
            }
        }

        const int SPEED = 10;

        private void ViewPort_KeyDown(object sender, KeyEventArgs e) {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (e.KeyData) {
                case Keys.S: {
                    var st = new Thread( () => {
                        var of = new SaveFileDialog { Filter = "*.png|*.png" };

                        if ( of.ShowDialog() != DialogResult.OK ) return;

                        lock (this.LockObj) {
                            this.doubleBuffer.BackgroundImage.Save( of.FileName, ImageFormat.Png );
                        }

                        for ( int i = 0; i < 100; i++ ) {
                            Application.DoEvents();
                            Thread.Sleep( 1 );
                        }

                        Process.Start( of.FileName );
                    } );
                    st.SetApartmentState( ApartmentState.STA );
                    st.Start();
                    break;
                }
                case Keys.C: {
                    if ( MessageBox.Show( "Clear screen? ", "clear", MessageBoxButtons.YesNo ) == DialogResult.Yes ) {
                        Graphics.FromImage( this.doubleBuffer.BackgroundImage ).Clear( Program.ClearColor );
                    }

                    break;
                }
                case Keys.N: {
                    if ( MessageBox.Show( "New Ptr Address? ", "New Ptr", MessageBoxButtons.YesNo ) == DialogResult.Yes ) {
                        Program.RecalculatePtr();
                    }

                    break;
                }
                case Keys.Right:
                    Program.LastPos = Program.LastPos.Add( 1 * SPEED, 0, 0 );
                    Program.M.WriteMemory<Vec3>( Program.posPtr, Program.LastPos );

                    break;
                case Keys.Left:
                    Program.LastPos = Program.LastPos.Add( -1 * SPEED, 0, 0 );
                    Program.M.WriteMemory<Vec3>( Program.posPtr, Program.LastPos );

                    break;
                case Keys.Down:
                    Program.LastPos = Program.LastPos.Add( 0, 1 * SPEED, 0 );
                    Program.M.WriteMemory<Vec3>( Program.posPtr, Program.LastPos );

                    break;
                case Keys.Up:
                    Program.LastPos = Program.LastPos.Add( 0, -1 * SPEED, 0 );
                    Program.M.WriteMemory<Vec3>( Program.posPtr, Program.LastPos );

                    break;
            }

        }
    }
    public sealed class PanelDoubleBuffer : Panel {
        public PanelDoubleBuffer() : base() {
            this.DoubleBuffered        = true;
            this.Dock                  = DockStyle.Fill;
            this.BackgroundImageLayout = ImageLayout.Center;
        }

    }
}
