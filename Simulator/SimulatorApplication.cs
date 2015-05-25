using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using AppInstall.Framework;
using AppInstall.OS;
using AppInstall.Simulation;

namespace AppInstall
{
    class Application
    {
        public static string ApplicationName { get { return "AppInstall Simulator"; } }
        public static string ApplicationDescription { get { return "AppInstall Vehicle Simulation Tool"; } }

        private string[] InputFiles;

        public Application(string[] args)
        {
            //string dir = Directory.GetCurrentDirectory();
            //InputFiles = args.Select((p) => dir + "\\" + p).ToArray();
            InputFiles = args;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public float X;
            public float Y;
            public float Z;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Pack = 1, Size = 48)]
        struct Triangle
        {
            [System.Runtime.InteropServices.FieldOffset(0)]
            public Vertex v1;
            [System.Runtime.InteropServices.FieldOffset(16)]
            public Vertex v2;
            [System.Runtime.InteropServices.FieldOffset(32)]
            public Vertex v3;
        };

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        struct Sphere
        {
            public Vertex center;
            public float radius;
        }

        public void Main()
        {

            
            var c = new Simulation.OpenCL.Context(Platform.DefaultLog.SubContext("OpenCL"));



            var data = new Triangle[] {
                new Triangle() {
                    v1 = new Vertex() { X = -20, Y = 3, Z = 4},
                    v2 = new Vertex() { X = 5, Y = 9, Z = 10},
                    v3 = new Vertex() { X = 8, Y = 9, Z = 10}
                },
                new Triangle() {
                    v1 = new Vertex() { X = 2, Y = 3, Z = 4},
                    v2 = new Vertex() { X = 5, Y = 9, Z = 10},
                    v3 = new Vertex() { X = 8, Y = 9, Z = 10}
                }
            };
            var vertices = new Cloo.ComputeBuffer<Triangle>(c.context, Cloo.ComputeMemoryFlags.ReadOnly | Cloo.ComputeMemoryFlags.UseHostPointer, data);
            var spheres = new Cloo.ComputeBuffer<Sphere>(c.context, Cloo.ComputeMemoryFlags.WriteOnly | Cloo.ComputeMemoryFlags.AllocateHostPointer, data.Count());
            Sphere[] sphereData = new Sphere[data.Count()];

            //try {
            var program0 = c.CreateProgram("BoundingSphere.c");
            var kernel = program0.CreateKernel("bounding_sphere");

            Platform.DefaultLog.Log("kernel stats for " + kernel.FunctionName);
            Platform.DefaultLog.Log("  work group size: " + kernel.GetWorkGroupSize(c.BestDevice.OpenCLDevice));
            Platform.DefaultLog.Log("  compile work group size: " + string.Join(", ", kernel.GetCompileWorkGroupSize(c.BestDevice.OpenCLDevice).Select((i) => i.ToString())));
            Platform.DefaultLog.Log("  preferred work group size multiple: " + kernel.GetPreferredWorkGroupSizeMultiple(c.BestDevice.OpenCLDevice));
            Platform.DefaultLog.Log("  private memory: " + kernel.GetPrivateMemorySize(c.BestDevice.OpenCLDevice));
            Platform.DefaultLog.Log("  local memory: " + kernel.GetLocalMemorySize(c.BestDevice.OpenCLDevice));

            var program = c.CreateProgram("SphereTree.c");
                kernel.SetMemoryArgument(0, vertices);
                kernel.SetMemoryArgument(1, spheres);

                //c.BestDevice.Queue.WriteToBuffer(data, )
                c.BestDevice.Queue.Execute(kernel, new long[] { 0 }, new long[] { data.Count() }, new long[] { 1 }, null);
                var marker = c.BestDevice.Queue.AddMarker();
                c.BestDevice.Queue.ReadFromBuffer(spheres, ref sphereData, true, new Cloo.ComputeEventBase[] { marker });
                
            //} catch (Exception ex) {
            //    Platform.DefaultLog.Log("build error: " + ex, LogType.Error);
            //    throw;
            //}
            
            


            


            Platform.DefaultLog.Log("x");
            Platform.DefaultLog.Log("v = " + new Quaternion(0, 0, 0).Rotate(new Vector3D<double>(1, 0, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(0, Math.PI / 2, Math.PI / 2).Rotate(new Vector3D<double>(1, 0, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, Math.PI / 2, 0).Rotate(new Vector3D<double>(1, 0, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, 0, Math.PI / 2).Rotate(new Vector3D<double>(1, 0, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, Math.PI / 2, Math.PI / 2).Rotate(new Vector3D<double>(1, 0, 0)));
            Platform.DefaultLog.Log("y");
            Platform.DefaultLog.Log("v = " + new Quaternion(0, 0, 0).Rotate(new Vector3D<double>(0, 1, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(0, Math.PI / 2, Math.PI / 2).Rotate(new Vector3D<double>(0, 1, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, Math.PI / 2, 0).Rotate(new Vector3D<double>(0, 1, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, 0, Math.PI / 2).Rotate(new Vector3D<double>(0, 1, 0)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, Math.PI / 2, Math.PI / 2).Rotate(new Vector3D<double>(0, 1, 0)));
            Platform.DefaultLog.Log("z");
            Platform.DefaultLog.Log("v = " + new Quaternion(0, 0, 0).Rotate(new Vector3D<double>(0, 0, 1)));
            Platform.DefaultLog.Log("v = " + new Quaternion(0, Math.PI / 2, Math.PI / 2).Rotate(new Vector3D<double>(0, 0, 1)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, Math.PI / 2, 0).Rotate(new Vector3D<double>(0, 0, 1)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, 0, Math.PI / 2).Rotate(new Vector3D<double>(0, 0, 1)));
            Platform.DefaultLog.Log("v = " + new Quaternion(Math.PI / 2, Math.PI / 2, Math.PI / 2).Rotate(new Vector3D<double>(0, 0, 1)));


            Platform.DefaultLog.Log("q = " + new Quaternion(42, 12, 20).Inverse());

            Platform.DefaultLog.Log("q = " + Vector3D<float>.CrossProduct(new Vector3D<float>(1, 2, 3), new Vector3D<float>(4, 5, 6)));

           // Window3D win = new Window3D();
           // win.Show(Platform.DefaultLog.SubContext("graphics engine"), ApplicationControl.ShutdownToken);
           //
           // Object3D objj = new STLObject("C:\\Data\\Code\\Obj-C\\3Dtest\\3Dtest\\3DAssets\\s1.stl", new AppInstall.UI.Color(0.8f, 0.7f, 0.6f));
           // objj.Transform(new Vector3D<float>(-60f, -60f, 0f), new Framework.Quaternion(), new Vector3D<float>(0.05f, 0.05f, 0.05f));
           // win.AddObject(objj);
            


            Platform.DefaultLog.Log("started");



            var nonExistantFiles = InputFiles.Where((file) => !File.Exists(file)).ToArray();
            foreach (var file in nonExistantFiles)
                Platform.DefaultLog.Log("the input file \"" + file + "\" could not be found", LogType.Warning);
            if (nonExistantFiles.Any())
                return;

            if (!InputFiles.Any()) {
                Platform.DefaultLog.Log("no input files specified", LogType.Warning);
                return;
            }


            SimulationContext s = null;
            try {
                Platform.DefaultLog.Log("creating simulation...");
                s = new SimulationContext(InputFiles);
                Platform.DefaultLog.Log("starting simulation...");
                s.Start(Platform.DefaultLog.SubContext("simulation engine"), ApplicationControl.ShutdownToken);
            } catch (Exception ex) {
                Platform.DefaultLog.Log("error in simulation: " + ex.Message, LogType.Error);
            }

            Platform.DefaultLog.Log("simulation running");


            if (s != null)
                while (!ApplicationControl.ShutdownToken.WaitHandle.WaitOne(100))
                    Platform.DefaultLog.Log("frequency: " + s.Frequency);
        }
    }

}
