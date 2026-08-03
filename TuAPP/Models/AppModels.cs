namespace TuAPP.Models;

public enum WorkoutType
{
    ClassicBoxing,
    Shadow,
    HeavyBag,
    Sparring,
    JumpRope,
    Sprints,
    Other
}

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

    public string Intensity { get; set; } = "Media";
    public string Feeling { get; set; } = "Normal";
    public int SprintsCompleted { get; set; } = 0;

    // ¡AQUÍ ESTÁN LOS 3 CAMPOS NUEVOS QUE FALTABAN PARA EL DIARIO!
    public string FocusArea { get; set; } = "General";
    public string SleepQuality { get; set; } = "Regular";
    public double PostWeight { get; set; } = 0;

    public string TypeLabel => Type switch
    {
        WorkoutType.ClassicBoxing => "Boxeo Clásico",
        WorkoutType.Shadow => "Sombra",
        WorkoutType.HeavyBag => "Costal",
        WorkoutType.Sparring => "Sparring",
        WorkoutType.JumpRope => "Cuerda",
        WorkoutType.Sprints => "Sprints / Cardio",
        WorkoutType.Other => "Diario / Libre",
        _ => "Entrenamiento"
    };

    public string TypeIcon => Type switch
    {
        WorkoutType.ClassicBoxing => "🥊",
        WorkoutType.Shadow => "👤",
        WorkoutType.HeavyBag => "🥋",
        WorkoutType.Sparring => "🤺",
        WorkoutType.JumpRope => "🪢",
        WorkoutType.Sprints => "🏃",
        WorkoutType.Other => "📓",
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

// ==========================================
// REGISTRO DE COMBATES PREVIOS ACTUALIZADO
// ==========================================
public class PastFight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Opponent { get; set; } = "";

    // Victoria, Victoria por KO, Victoria por RSC, Derrota, Derrota por KO, Derrota por RSC, Empate
    public string Result { get; set; } = "Victoria";

    public string Details { get; set; } = "";

    // Nuevos campos opcionales
    public DateTime? Date { get; set; } = DateTime.Now;
    public string FightType { get; set; } = ""; // Amateur, Profesional, etc.
    public string WeightAtFight { get; set; } = ""; // Ej. 65kg
}

public class AthleteProfile
{
    public string Name { get; set; } = "Pedro";
    public double WeightKg { get; set; } = 66.0;
    public int Age { get; set; } = 17;
    public double HeightCm { get; set; } = 170.0;
    public string ProfileImagePath { get; set; } = "";
    public int GenderIndex { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int Draws { get; set; } = 0;
    public int Knockouts { get; set; } = 0;
    public List<PastFight> FightHistory { get; set; } = new();

    public string BoxingCategory => WeightKg switch
    {
        <= 60 => "Superpluma",
        <= 64 => "Ligero",
        <= 67 => "Wélter",
        <= 71 => "Superwélter",
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