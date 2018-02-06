using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectV1Lib
{
    using Microsoft.Kinect;

    public class V1Sensor
    {
        private KinectSensor sensor;

        public V1Sensor()
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (this.sensor != null)
            {
                if (this.sensor != null)
                {
                    this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    this.sensor.SkeletonStream.Enable();
                    this.sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
                }
            }

            if (this.sensor != null)
            {
                this.sensor.Start();
            }
        }
    }

    public static class KV1
    {
        public static V1Sensor GetDefault()
        {
            return new V1Sensor();
        }
    }
}
