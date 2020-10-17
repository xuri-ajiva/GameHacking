using System;
using System.Threading;
using System.Xml.Serialization;
using HackFramework;
using Microsoft.Win32;

namespace Map
{
    public static class Program
    {
        public static  Memory           M;
        private static PointerLevelData setup;

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThread]
        [System.Diagnostics.DebuggerNonUserCode]
        public static void Main(string[] args)
        {
            const string filter = "Game Setup|*.gsec";

            setup = PointerLevelData.SampleSetup;
            var xr = new XmlSerializer(typeof(PointerLevelData));

            var of = new OpenFileDialog { Title = "Open Game Config", Filter = filter };
            if (of.ShowDialog() is bool by && by)
            {
                var s = of.OpenFile();
                setup = (PointerLevelData) xr.Deserialize(s);
                s.Close();
            }
            else
            {
                var sf = new SaveFileDialog { Title = "Save Example Layout", Filter = filter };
                if (sf.ShowDialog() is bool bx && bx)
                {
                    var s = sf.OpenFile();
                    xr.Serialize(s, setup);
                    s.Flush();
                    s.Close();
                }
            }

            M = new Memory();
            M.AttachLoop(setup.Process);

            Position = new DirectUpdate<Vec3>(M, setup);

            App app = new App();
            app.InitializeComponent();
            app.Run();
            Thread.Sleep(1000);
            Environment.Exit(0);
        }

        public static DirectUpdate<Vec3> Position;

        private const int PS = 5;

        public static Vec3 CalcPos()
        {
            var pos = Position.Value;

            float x = pos.X / setup.Scale;
            float y = pos.Y / setup.Scale;
            float z = pos.Z / setup.Scale;
            return new Vec3(x, y, z);
        }
    }
}
