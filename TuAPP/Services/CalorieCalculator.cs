using TuAPP.Models;

namespace TuAPP.Services;

public static class CalorieCalculator
{
    // Valores MET (Metabolic Equivalent of Task) oficiales para boxeo
    private static readonly Dictionary<WorkoutType, double> MetValues = new()
    {
        { WorkoutType.ClassicBoxing, 12.8 },
        { WorkoutType.Shadow, 6.0 },
        { WorkoutType.HeavyBag, 10.0 },
        { WorkoutType.Sparring, 12.8 },
        { WorkoutType.JumpRope, 12.3 }
    };

    public static double Calculate(WorkoutType type, double weightKg, int durationSeconds)
    {
        if (weightKg <= 0 || durationSeconds <= 0) return 0;
        double met = MetValues.GetValueOrDefault(type, 8.0);
        double hours = durationSeconds / 3600.0;
        return Math.Round(met * weightKg * hours, 1);
    }
}