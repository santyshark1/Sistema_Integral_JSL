using System;
using System.Collections.Generic;
using System.Linq;
using JSL_SentinelPro.src.Models;
using LibreHardwareMonitor.Hardware;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Monitoreo de temperaturas reales usando LibreHardwareMonitor.
    /// </summary>
    public class TemperatureMonitor : IDisposable
    {
        private readonly Computer _computer;
        private bool _isDisposed;

        public TemperatureMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsStorageEnabled = true
            };
            _computer.Open();
        }

        /// <summary>
        /// Obtiene todas las lecturas de temperatura disponibles.
        /// </summary>
        public List<TemperatureReading> GetAllTemperatures()
        {
            var readings = new List<TemperatureReading>();
            if (_isDisposed) return readings;

            try
            {
                _computer.Accept(new UpdateVisitor());

                foreach (var hardware in _computer.Hardware)
                {
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            readings.Add(new TemperatureReading
                            {
                                HardwareName = hardware.Name,
                                SensorName = sensor.Name ?? "Sensor desconocido",
                                ValueCelsius = Math.Round(sensor.Value.Value, 1)
                            });
                        }
                    }

                    foreach (var subHardware in hardware.SubHardware)
                    {
                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                readings.Add(new TemperatureReading
                                {
                                    HardwareName = $"{hardware.Name} - {subHardware.Name}",
                                    SensorName = sensor.Name ?? "Sensor desconocido",
                                    ValueCelsius = Math.Round(sensor.Value.Value, 1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TemperatureMonitor] Error: {ex.Message}");
            }

            return readings.OrderByDescending(r => r.ValueCelsius).ToList();
        }

        /// <summary>
        /// Obtiene la temperatura maxima actual.
        /// </summary>
        public double GetMaxTemperature()
        {
            var temps = GetAllTemperatures();
            return temps.Count > 0 ? temps.Max(t => t.ValueCelsius) : 0;
        }

        /// <summary>
        /// Obtiene las RPM de los ventiladores.
        /// </summary>
        public Dictionary<string, double> GetFanSpeeds()
        {
            var fans = new Dictionary<string, double>();
            if (_isDisposed) return fans;

            try
            {
                _computer.Accept(new UpdateVisitor());
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
