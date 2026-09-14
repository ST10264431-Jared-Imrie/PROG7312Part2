using System;

namespace SmartX.API.Models
{
    /// <summary>
    /// Value struct representing smart meter power readings.
    /// Implements custom operator overloading for power aggregation and threshold comparisons.
    /// </summary>
    public struct SmartMeter : IEquatable<SmartMeter>, IComparable<SmartMeter>
    {
        public double Wattage { get; set; }

        public SmartMeter(double wattage)
        {
            Wattage = wattage;
        }

        // Operator '+' for power aggregation
        public static SmartMeter operator +(SmartMeter a, SmartMeter b)
        {
            return new SmartMeter(a.Wattage + b.Wattage);
        }

        public static SmartMeter operator +(SmartMeter a, double wattage)
        {
            return new SmartMeter(a.Wattage + wattage);
        }

        // Comparison operators
        public static bool operator >(SmartMeter a, SmartMeter b)
        {
            return a.Wattage > b.Wattage;
        }

        public static bool operator <(SmartMeter a, SmartMeter b)
        {
            return a.Wattage < b.Wattage;
        }

        public static bool operator >=(SmartMeter a, SmartMeter b)
        {
            return a.Wattage >= b.Wattage;
        }

        public static bool operator <=(SmartMeter a, SmartMeter b)
        {
            return a.Wattage <= b.Wattage;
        }

        public static bool operator ==(SmartMeter a, SmartMeter b)
        {
            return Math.Abs(a.Wattage - b.Wattage) < 0.0001;
        }

        public static bool operator !=(SmartMeter a, SmartMeter b)
        {
            return !(a == b);
        }

        public bool Equals(SmartMeter other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is SmartMeter meter && Equals(meter);
        }

        public override int GetHashCode()
        {
            return Wattage.GetHashCode();
        }

        public int CompareTo(SmartMeter other)
        {
            return Wattage.CompareTo(other.Wattage);
        }

        public override string ToString()
        {
            return $"{Wattage:F2} W";
        }
    }
}
