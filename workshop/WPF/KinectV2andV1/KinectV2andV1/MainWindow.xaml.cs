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

using Microsoft.Kinect;
using KinectV1Lib;

namespace KinectV2andV1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private KinectSensor v2Sensor = null;
        private InfraredFrameReader v2IRReader = null;

        private V1Sensor v1Sensor = null;

        public MainWindow()
        {
            InitializeComponent();
            
            v1Sensor = KinectV1Lib.KV1.GetDefault();

            v2Sensor = KinectSensor.GetDefault();
            v2IRReader = v2Sensor.InfraredFrameSource.OpenReader();
            v2IRReader.FrameArrived += v2IRReader_FrameArrived;
        }

        void v2IRReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            using(var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {

                }
            }
        }
    }
}
