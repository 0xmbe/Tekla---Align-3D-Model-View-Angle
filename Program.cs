using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using TeklaMacroBuilder;
using TSM = Tekla.Structures.Model;
using TS3D = Tekla.Structures.Geometry3d;
using TSMUI = Tekla.Structures.Model.UI;
class Program
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    static void Main(string[] args)
    {
        try
        {
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle; // Hide console:
            ShowWindow(handle, SW_HIDE);
            TSM.Model model = new TSM.Model();
            if (model.GetConnectionStatus())
            {
                TSMUI.View activeView = TSMUI.ViewHandler.GetActiveView(); // Get the active view
                TSMUI.ViewHandler viewHandler = new TSMUI.ViewHandler();
                TS3D.CoordinateSystem displayCoordinateSystem = activeView.DisplayCoordinateSystem; // Get the display coordinate system 
                TS3D.Vector axisX = displayCoordinateSystem.AxisX;      // Get the axes
                TS3D.Vector axisY = displayCoordinateSystem.AxisY;

                double angle_Z = GetAngleZ(axisY);                      // Calculate the angle Z: 
                double angle_X = GetAngleX(displayCoordinateSystem);    // Calculate the angle X: ==> It is not as easy as the Z angle :)

                // DEBUG:
                //Console.WriteLine($"Z: {angle_Z}"); // OK
                //Console.WriteLine($"X: {angle_X}"); // OK

                DetermineRequiredXandYrotationDirection(angle_X, angle_Z, out int x, out int z);
                RotateViewInDirection(x, z); // Rotate view with Macro:
            }
            else
            {
                Console.WriteLine("Tekla Structures is not running or no model is open.");
                Console.ReadLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.ReadLine();
        }
    }
    private static void DetermineRequiredXandYrotationDirection(double angle_X, double angle_Z, out int x, out int z)
    {
        x = 1337;
        z = 1337;

        // See Tekla angles PNG to understand how it calculates angles.
        // That is the reason for more parameters at Yp and Yn
        double viewDirection_Yp_Threshold_Z1 = -45;
        double viewDirection_Yp_Threshold_Z0 = 0;
        double viewDirection_Yp_Threshold_Z2 = 45;

        double viewDirection_Yn_Threshold_Z1 = 135;
        double viewDirection_Yn_Threshold_Z180 = 180;
        double viewDirection_Yn_Threshold_Z180n = -180;
        double viewDirection_Yn_Threshold_Z2 = -135;

        double viewDirection_Xp_Threshold_Z1 = 45;
        double viewDirection_Xp_Threshold_Z2 = 135;

        double viewDirection_Xn_Threshold_Z1 = -135;
        double viewDirection_Xn_Threshold_Z2 = -45;


        // Zn is more complex and because we don't normally use it to look from down up I am not implementing it at the moment
        //double viewDirection_Zp_Threshold_X1 = 225;
        //double viewDirection_Zp_Threshold_X0 = 0;
        //double viewDirection_Zp_Threshold_X2 = -45;

        double viewDirection_Zn_Threshold_X1 = 45;
        double viewDirection_Zn_Threshold_X2 = 135;

        // Determine nearest angle rotation for X and Z rotation, for Xp,Xn,Yp,Yn:
        if ((angle_Z >= viewDirection_Yp_Threshold_Z1 && angle_Z <= viewDirection_Yp_Threshold_Z0) ||
            (angle_Z >= viewDirection_Yp_Threshold_Z0 && angle_Z <= viewDirection_Yp_Threshold_Z2))
        {
            z = 0;
            x = 0;
        }
        if ((angle_Z >= viewDirection_Yn_Threshold_Z1 && angle_Z <= viewDirection_Yn_Threshold_Z180) ||
            (angle_Z >= viewDirection_Yn_Threshold_Z180n && angle_Z <= viewDirection_Yn_Threshold_Z2))
        {
            z = 180;
            x = 0;
        }
        if (angle_Z >= viewDirection_Xp_Threshold_Z1 && angle_Z <= viewDirection_Xp_Threshold_Z2)
        {
            z = 90;
            x = 0;
        }
        if (angle_Z >= viewDirection_Xn_Threshold_Z1 && angle_Z <= viewDirection_Xn_Threshold_Z2)
        {
            z = -90;
            x = 0;
        }

        // Determine Zp and Zn:
        //if (angle_X >= viewDirection_Zp_Threshold_X1 && angle_X <= viewDirection_Zp_Threshold_X2)
        //{
        //    x = -90;
        //}
        if (angle_X >= viewDirection_Zn_Threshold_X1 && angle_X <= viewDirection_Zn_Threshold_X2)
        {
            x = 90;
        }
        //else
        //{
        //    x = -90;
        //}
    }

    private static double GetAngleX(TS3D.CoordinateSystem displayCoordinateSystem)
    {
        Vector3 axisX_ = new Vector3(
            (float)displayCoordinateSystem.AxisY.X,
            (float)displayCoordinateSystem.AxisY.Y,
            (float)displayCoordinateSystem.AxisY.Z
            );
        axisX_ = Vector3.Normalize(axisX_);                                     // Normalize the vectors (if not already normalized)
        Vector3 unitZ = new Vector3(0, 0, 1);                                   // Calculate the dot product with the unit Z vector
        double dotProduct_axis_X = Vector3.Dot(axisX_, unitZ);
        double dotProduct_angle_X_angleRadians = Math.Acos(dotProduct_axis_X);  // Calculate the angle in radians   
        double angle_X = dotProduct_angle_X_angleRadians * (180.0 / Math.PI);   // Convert the angle to degrees
        return angle_X;
    }
    private static double GetAngleZ(TS3D.Vector axisY)
    {
        double angle_Z = Math.Atan2(axisY.X, axisY.Y) * (180.0 / Math.PI);
        return angle_Z;
    }
    static void RotateViewInDirection(int x, int z)
    {
        if (x != 1337 && z != 1337)
        {
            MacroBuilder macroBuilder = new MacroBuilder();
            macroBuilder.Callback("acmd_interrupt", "", "main_frame");                      // End Previous Command:
            macroBuilder.CommandEnd();
            macroBuilder.Callback("acmd_display_selected_object_dialog", "", "main_frame"); // Get View Properties:
            macroBuilder.PushButton("v1_get", "dia_view_dialog");
            macroBuilder.ValueChange("dia_view_dialog", "v1_rotate_z", z.ToString());       // Set View Properties:
            macroBuilder.ValueChange("dia_view_dialog", "v1_rotate_x", x.ToString());
            macroBuilder.PushButton("v1_modify", "dia_view_dialog");                        // Modify Value of View Properties:
            macroBuilder.PushButton("v1_ok", "dia_view_dialog");
            macroBuilder.Run();                                                             // Run the command:
        }
        else
        {
            // Angle not determined ...
        }
    }
}





