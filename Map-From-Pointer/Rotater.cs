using System;
using System.Diagnostics;
using System.Threading;
using HackFramework;

namespace Map {
    class Rotater {
        private static bool _rotate = false;

        private static Thread RotaterThread;
        private static Vec3   sp     = Program.Position.Value;
        private const  int    SLEEP  = 1;
        public static int    Radius = 200;

        static void Start() {
            if ( RotaterThread != null ) return;

            RotaterThread = new Thread( () => {
                Thread.Sleep( 200 );
                double time = 0;
                SerCenter();
                while ( _rotate ) {
                    var cp = sp.Add( new Vec3( Math.Sin( time * ( Math.E - 2.6 ) ) * Radius, Math.Cos( time * ( Math.PI - 3 ) ) * Radius, -Math.Sin( time * ( Math.E + Math.PI - 5.7 ) ) * Radius ) );
                    Program.Position.Value = cp;
                    Thread.Sleep( SLEEP );
                    time += SLEEP / 100d;
                }
            } );
            RotaterThread.Start();
        }

        private static void Stop() {
            RotaterThread.Abort();
            RotaterThread          = null;
            Program.Position.Value = sp;
        }

        public static void SerCenter() {
            sp = Program.Position.Value;
        }
        public static bool Rotate {
            [DebuggerStepThrough] get {
                if ( RotaterThread == null )
                    return _rotate = false;

                return _rotate = true;
            }
            [DebuggerStepThrough] set {
                _rotate = value;

                if ( value ) {
                    Start();
                }
                else {
                    Stop();
                }
            }
        }
    }
}
