using System;
using System.Collections.Generic;
using System.Linq;
using JSL_SentinelPro.src.Models;
using LibreHardwareMonitor.Hardware;

namespace JSL_SentinelPro.src.Native
{
    /// <summary>
    /// Wrapper nativo para LibreHardwareMonitorLib.
    /// Proporciona una interfaz simplificada para lectura de sensores de hardware.
    /// </summary>
    public class LibreHardwareMonitorWrapper : IDisposable
    {
        private readonly Computer _computer;
        private bool _isDisposed;

        public LibreHardwareMonitorWrapper()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = false,
                IsControllerEnabled = false
            };
            _computer.Open();
        }

        /// <summary>
        /// Actualiza todos los valores de sensores.
        /// </summary>
        public void Update()
        {
            if (_isDisposed) return;
            try
            {
                _computer.Accept(new UpdateVisitor());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibreHardwareMonitorWrapper] Update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene lecturas de temperatura de todos los sensores disponibles.
        /// </summary>
        public List<TemperatureReading> GetTemperatureReadings()
        {
            var readings = new List<TemperatureReading>();
            if (_isDisposed) return readings;

            try
            {
                Update();
                foreach (var hardware in _computer.Hardware)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            readings.Add(new TemperatureReading
                            {
                                HardwareName = hardware.Name,
                                SensorName = sensor.Name ?? "Unknown",
                                ValueCelsius = Math.Round(sensor.Value.Value, 1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibreHardwareMonitorWrapper] Temperature error: {ex.Message}");
            }
            return readings.OrderByDescending(r => r.ValueCelsius).ToList();
        }

        /// <summary>
        /// Obtiene velocidades de ventiladores.
        /// </summary>
        public Dictionary<string, double> GetFanSpeeds()
        {
            var fans = new Dictionary<string, double>();
            if (_isDisposed) return fans;

            try
            {
                Update();
                foreach (var hardware in _computer.Hardware)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                        {
                            fans[$"{hardware.Name} - {sensor.Name}"] = Math.Round(sensor.Value.Value, 0);
                        }
                    }
                }
            }
            catch { }
            return fans;
        }

        /// <summary>
        /// Obtiene voltajes de sensores.
        /// </summary>
        public Dictionary<string, double> GetVoltages()
        {
            var voltages = new Dictionary<string, double>();
            if (_isDisposed) return voltages;

            try
            {
                Update();
                foreach (var hardware in _computer.Hardware)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Voltage && sensor.Value.HasValue)
                        {
                            voltages[$"{hardware.Name} - {sensor.Name}"] = Math.Round(sensor.Value.Value, 3);
                        }
                    }
                }
            }
            catch { }
            return voltages;
        }

        /// <summary>
        /// Obtiene cargas de sensores (CPU/GPU load).
        /// </summary>
        public Dictionary<string, double> GetLoads()
        {
            var loads = new Dictionary<string, double>();
            if (_isDisposed) return loads;

            try
            {
                Update();
                foreach (var hardware in _computer.Hardware)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                        {
                            loads[$"{hardware.Name} - {sensor.Name}"] = Math.Round(sensor.Value.Value, 1);
                        }
                    }
                }
            }
            catch { }
            return loads;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _computer?.Close();
                _isDisposed = true;
            }
        }

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer) { computer.Traverse(this); }
            public void VisitHardware(IHardware hardware) { hardware.Update(); hardware.Traverse(this); }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }
    }
}
