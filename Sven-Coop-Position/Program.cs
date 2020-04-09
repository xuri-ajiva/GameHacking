using System;
using System.Collections.Generic;
using System.Threading;
using HackFramework;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;


namespace Sven_Coop_Position {
    class Program {
        private const int    SIZE           = 1000;
        private const int    FACTOR         = 50;
        private const int    PS             = 20;
        private const double MAGIC_DISTANCE = SIZE / FACTOR * PS;
        public static Color  ClearColor => Color.FromArgb( 255, 43, 43, 43 );

        private static  Memory   M;
        private static  IntPtr   BasePtr;
        static readonly IntPtr[] Offsets = { (IntPtr) 0x570EE00, (IntPtr) 0x88 };
        private const   string   PROCESS = "svencoop";

        private static void Main(string[] args) {
            M = new Memory();
            M.AttachLoop( PROCESS );
            RecalculatePtr();
            var img = new Bitmap( SIZE, SIZE );
            var vp  = new ViewPort();
            vp.SetBackIng( img );
            new Thread( () => {
                var g = Graphics.FromImage( img );
                g.Clear( ClearColor );
                var r = new Random();

                foreach ( var updatePo in UpdatePos( M ) ) {
                    var p1 = CalcPos( updatePo.Item1 );
                    var p2 = CalcPos( updatePo.Item2 );
                    var h  = ( (int) updatePo.Item2.Z + 111111 );
                    
                    int cr = (int) ( ( ( h >> 1 ) / 1 ) % 255 );
                    int cg = (int) ( ( ( h >> 3 ) / 1 ) % 255 );
                    int cb = (int) ( ( ( h >> 5 ) / 1 ) % 255 );
                    //Console.WriteLine( $"Pos: {x}, {y}" );

                    lock (vp.LockObj) {
                        g.FillRectangle( Brushes.White,                                       p2.X, p2.Y, PS, PS );
                        g.FillRectangle( new SolidBrush( Color.FromArgb( 255, cr, cg, cb ) ), p1.X, p1.Y, PS, PS );
                    }

                    try {
                        vp?.Invoke( new Action( () => { vp.doubleBuffer.Refresh(); } ) );
                    } catch (Exception e) { }
                }

                Console.WriteLine( "Application Closed!" );

                try {
                    vp?.Invoke( new Action( () => { vp.Close(); } ) );
                } catch (Exception e) {
                    Console.WriteLine( e );
                }

                Thread.Sleep( 1000 );
                Main( args );
            } ).Start();
            new Thread( () => {
                const int sleep  = 5;
                const int radius = 2500;
                Thread.Sleep( 2000 );
                var    sp   = LastPos;
                double time = 0;

                while ( true ) {
                    var cp = sp.Add( new Vec3( Math.Sin( time * ( Math.E - 2.6 )) * radius, Math.Cos( time * ( Math.PI - 3 )  ) * radius, -Math.Sin( time * ( Math.E+Math.PI - 5.7 ) ) * radius ) );
                    M.WriteMemory<Vec3>( posPtr, cp );
                    Thread.Sleep( sleep );
                    time += sleep / 500d;
                }
            } ).Start();
            Application.Run( vp );
        }

        static int SinToByte(double sin) => (int) ( sin + 1 ) * 127;

        private static Vec2 CalcPos(Vec3 pos) {
            float x = pos.X / ( SIZE / FACTOR ) + SIZE / 2;
            float y = pos.Y / ( SIZE / FACTOR ) + SIZE / 2;
            return new Vec2( x, y );
        }

        public static void RecalculatePtr() {
            Console.WriteLine( "MiProcessHandle: " + M.MiProcessHandle );
            BasePtr = M.GetModuleAddress( "hw.dll" );
            Console.WriteLine( "BasePtr: " + BasePtr );
            posPtr = M.GetMultiLevelPointer( BasePtr, Offsets, false );
            Console.WriteLine( "posPtr: " + BasePtr );
        }

        public static  Vec3   LastPos = Vec3.Zero;
        private static IntPtr posPtr  = IntPtr.Zero;

        static IEnumerable<(Vec3, Vec3)> UpdatePos(Memory m) {
            while ( posPtr == IntPtr.Zero ) {
                Thread.Sleep( 100 );
            }

            while ( !m.MiProcess.HasExited ) {
                Thread.Sleep( 50 );
                var pos = m.ReadMemory<Vec3>( posPtr );

                if ( pos.DistanceTo( LastPos ) < MAGIC_DISTANCE ) continue;

                yield return ( LastPos, pos );

                LastPos = pos;
                Console.WriteLine( pos );
            }
        }

    }
}
