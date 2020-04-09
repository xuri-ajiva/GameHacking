using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HackFramework;

namespace Sven_Coop_Map {
    public static class Program {
        public static   Memory   M;
        static readonly IntPtr[] Offsets = { (IntPtr) 0x570EE00, (IntPtr) 0x88 };
        private const   string   PROCESS = "svencoop";


        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main(string[] args) {
            M = new Memory();
            M.AttachLoop( PROCESS );
            
            Position = new DirectUpdate<Vec3>(M, Offsets, "hw.dll" );

            new Thread( () => {
                const int sleep  = 1;
                const int radius = 200;
                Thread.Sleep( 2000 );
                var    sp   = Position.Value;
                double time = 0;

                while ( true ) {
                    var cp = sp.Add( new Vec3( Math.Sin( time * ( Math.E - 2.6 ) ) * radius, Math.Cos( time * ( Math.PI - 3 ) ) * radius, -Math.Sin( time * ( Math.E + Math.PI - 5.7 ) ) * radius ) );
                    Position.Value = cp;
                    Thread.Sleep( sleep );
                    time += sleep / 100d;
                }
            } );//.Start();

            App app = new App();
            app.InitializeComponent();
            app.Run();
            Thread.Sleep( 1000 );
            Environment.Exit( 0 );
        }

        public static DirectUpdate<Vec3> Position;

        private const int SIZE   = 1000;
        private const int FACTOR = 100;
        private const int PS     = 5;

        public static Vec3 CalcPos() {
            var pos = Position.Value;

            float x = pos.X / ( SIZE / FACTOR );
            float y = pos.Y / ( SIZE / FACTOR );
            float z = pos.Z / ( SIZE / FACTOR );
            return new Vec3( x, y, z );
        }
    }
}
