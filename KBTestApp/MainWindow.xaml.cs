using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;
using System.Data;
using System.Threading;

namespace G910ProfileLoader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public Keyboard keyboard;

        public MainWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();

            String profile;

            // Check for profile startup
            if (args.Length == 2)
            {
                profile = args[1];
            }
            else
            {
                // Load the Stock Profile
                profile = "profiles/uk.kbp";
            }

            //profile = "counting.kbp";

            inputProfileName.Text = profile;

            LogitechGSDK.LogiLedInit();
            LogitechGSDK.LogiLedSaveCurrentLighting();

            // Load profile
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(profile))
            {
                String line;
                // Read and display lines from the file until the end of 
                // the file is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }

            keyboard = JsonConvert.DeserializeObject<Keyboard>(sb.ToString());
            keyboard.redraw(gridContainer);

        }

        private void key_register_event(object sender, MouseButtonEventArgs e)
        {
            var key = sender as Rectangle;
            key.Stroke = new SolidColorBrush(_colorPicker.SelectedColor);
            var rgb = RGBConverter(_colorPicker.SelectedColor);

            keyboard.changeKeyColour(key.Name, rgb.Item1, rgb.Item2, rgb.Item3, gridContainer);

        }

        private static Tuple<int, int, int> RGBConverter(System.Windows.Media.Color c)
        {
            return Tuple.Create(Convert.ToInt32(c.R.ToString()), Convert.ToInt32(c.G.ToString()), Convert.ToInt32(c.B.ToString()));
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //Open Windows Save Dialog to save the profile.
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = inputProfileName.Text;
            dlg.DefaultExt = ".kbp";
            dlg.Filter = "(.kbp)|*.kbp";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Save JSON
                string filename = dlg.FileName;
                string json = JsonConvert.SerializeObject(keyboard, Formatting.Indented);
                System.IO.StreamWriter file = new System.IO.StreamWriter(filename);
                file.WriteLine(json);
                file.Close();
            }
        }

    }




    // Define a Keyboard.
    public class Keyboard
    {
        public String model { get; set; }
        public String vendor { get; set; }
        public IDictionary<string, KeyboardKey> keys { get; set; }

        public void addKey(KeyboardKey k)
        {
            this.keys[k.name] = k;
        }

        public void changeKeyColour(String name, int r, int g, int b, Grid container)
        {
            this.keys[name].setRGB(r, g, b);
            this.keys[name].changeColour(container);
        }

        public void redraw(Grid container)
        {
            
            foreach (var key in this.keys)
            {
                //Console.WriteLine("redraw: {0}", key.Key);
                this.keys[key.Key].changeColour(container);
                //Thread.Sleep(200);
            }

        }

        public void dumpall()
        {
            string json = JsonConvert.SerializeObject(this);
            Console.WriteLine(json);
        }

    }

    // Define a Key, from Keyboard
    public class KeyboardKey : Keyboard
    {
        public string name { get; set; }
        public string code { get; set; }
        public int r { get; set; }
        public int g { get; set; }
        public int b { get; set; }

        // x and y aren't implemented, but would be useful to render keys
        // according to the x, relative to the keyboard - dynamically.
        public int x { get; set; }
        public int y { get; set; }


        public KeyboardKey()
        {
            // by default set all keys to black - no light.
            setRGB(255, 255, 255);
        }

        public void setKey(String name, String code)
        {
            this.name = name;
            this.code = code;
        }

        public void setRGB(int r, int g, int b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public void changeColour(Grid container)
        {
            int keyCode = Convert.ToInt32(this.code, 16);
            // Convert 255 to Logitech Percentages - don't want to save ther percentages rather keep them as rgb.
            var red = (this.r > 0) ? Convert.ToInt32(((float)this.r / 255f) * 100f) : 0;
            var green = (this.g > 0) ? Convert.ToInt32(((float)this.g / 255f) * 100f) : 0;
            var blue = (this.b > 0) ? Convert.ToInt32(((float)this.b / 255f) * 100f) : 0;
            
            // redraw all the keys on the gui.
            var key = container.FindName(this.name);
            if (key is Rectangle)
            {
                Rectangle keyref = key as Rectangle;
                SolidColorBrush mySolidColorBrush = new SolidColorBrush();

                mySolidColorBrush.Color = Color.FromArgb(255, (byte)this.r, (byte)this.g, (byte)this.b);

                keyref.Stroke = mySolidColorBrush;
            }
            
            LogitechGSDK.LogiLedSetLightingForKeyWithScanCode(keyCode, red, green, blue);
        }

    }

}


//LED SDK - Direct from Docs.
public class LogitechGSDK
{
    public const int LOGI_LED_BITMAP_WIDTH = 21;
    public const int LOGI_LED_BITMAP_HEIGHT = 6;
    public const int LOGI_LED_BITMAP_BYTES_PER_KEY = 4;
    public const int LOGI_LED_BITMAP_SIZE = LOGI_LED_BITMAP_WIDTH * LOGI_LED_BITMAP_HEIGHT * LOGI_LED_BITMAP_BYTES_PER_KEY;

    public const int LOGI_LED_DURATION_INFINITE = 0;

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedInit();

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSaveCurrentLighting();

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSetLighting(int redPercentage, int greenPercentage, int bluePercentage);

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedRestoreLighting();

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedFlashLighting(int redPercentage, int greenPercentage, int bluePercentage, int milliSecondsDuration, int milliSecondsInterval);
    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedPulseLighting(int redPercentage, int greenPercentage, int bluePercentage, int milliSecondsDuration, int milliSecondsInterval);

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedStopEffects();

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSetLightingFromBitmap(byte[] bitmap);

    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSetLightingForKeyWithScanCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);
    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSetLightingForKeyWithHidCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);
    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSetLightingForKeyWithQuartzCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);
    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool LogiLedSetLightingForKeyWithKeyNameCode(int keyCode, int redPercentage, int greenPercentage, int bluePercentage);
    [DllImport("LogitechLedEnginesWrapper ", CallingConvention = CallingConvention.Cdecl)]
    public static extern void LogiLedShutdown();
}