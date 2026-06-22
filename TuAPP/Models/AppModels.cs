namespace TuAPP.Models;

public enum WorkoutType { ClassicBoxing, Shadow, HeavyBag, Sparring, JumpRope }

public class WorkoutSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Now;
    public WorkoutType Type { get; set; } = WorkoutType.ClassicBoxing;
    public int RoundsCompleted { get; set; }
    public int TotalRounds { get; set; }
    public int TotalSeconds { get; set; }
    public double CaloriesBurned { get; set; }
    public string Notes { get; set; } = "";

    public string TypeLabel => Type switch
    {
        WorkoutType.ClassicBoxing => "Boxeo Clásico",
        WorkoutType.Shadow => "Sombra",
        WorkoutType.HeavyBag => "Costal",
        WorkoutType.Sparring => "Sparring",
        WorkoutType.JumpRope => "Cuerda",
        _ => "Entrenamiento"
    };

    // PROPIEDADES RESTAURADAS PARA LA PANTALLA DE COMPARTIR
    public string TypeIcon => Type switch
    {
        WorkoutType.ClassicBoxing => "🥊",
        WorkoutType.Shadow => "👤",
        WorkoutType.HeavyBag => "🥋",
        WorkoutType.Sparring => "🤺",
        WorkoutType.JumpRope => "🪢",
        _ => "🏋️"
    };

    public string DurationLabel
    {
        get
        {
            int min = TotalSeconds / 60;
            int sec = TotalSeconds % 60;
            return min > 0 ? $"{min}m {sec}s" : $"{sec}s";
        }
    }
}

public class AthleteProfile
{
    public string Name { get; set; } = "Pedro";
    public double WeightKg { get; set; } = 66.0;

    // EDAD RESTAURADA PARA EL ONBOARDING
    public int Age { get; set; } = 17;
    public double HeightCm { get; set; } = 170.0;

    public string BoxingCategory => WeightKg switch
    {
        <= 60 => "Superpluma",
        <= 64 => "Ligero",
        <= 67 => "Welter",
        <= 71 => "Superwelter",
        _ => "Otra"
    };
}

public class FightEvent
{
    public DateTime Date { get; set; } = new DateTime(2026, 7, 26);
    public double TargetWeightKg { get; set; } = 66.0;

    public int DaysLeft => Math.Max(0, (Date.Date - DateTime.Today).Days);

    public string DaysLeftText => DaysLeft switch
    {
        0 => "¡HOY ES EL DÍA! 🥊",
        1 => "¡MAÑANA! 🔥",
        _ => $"{DaysLeft} días"
    };
}