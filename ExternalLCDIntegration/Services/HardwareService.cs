using System;
using System.Collections.Generic;
using System.Text;
using OpenHardwareMonitor.Hardware;

namespace ExternalLCDIntegration.Services
{
    public static class HardwareService
    {
        public static List<string> GetCPUInfo()
        {
            var updateVisitor = new UpdateVisitor();
            var computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.Accept(updateVisitor);
            var returnValues = new List<string>();
            for (var i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (var j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                           returnValues.Add(computer.Hardware[i].Sensors[j].Name + ":" + computer.Hardware[i].Sensors[j].Value.ToString() + "\r");
                    }
                }
            }
            computer.Close();
            return returnValues;
        }

    }
}
