using System.Text.Json;
using TuAPP.Models;

namespace TuAPP.Services;

public static class StorageService
{
    // --- HISTORIAL DE ENTRENAMIENTOS ---
    public static List<WorkoutSession> LoadWorkoutHistory()
    {
        var json = Preferences.Get("workout_history", "");
        if (string.IsNullOrEmpty(json)) return new List<WorkoutSession>();
        try { return JsonSerializer.Deserialize<List<WorkoutSession>>(json) ?? new List<WorkoutSession>(); }
        catch { return new List<WorkoutSession>(); }
    }

    public static void AddWorkoutSession(WorkoutSession session)
    {
        var list = LoadWorkoutHistory();
        list.Insert(0, session);
        Preferences.Set("workout_history", JsonSerializer.Serialize(list.Take(100).ToList()));
    }

    // EL NUEVO MÉTODO CORREGIDO (misma llave "workout_history")
    public static void SaveWorkoutHistory(List<WorkoutSession> history)
    {
        string json = JsonSerializer.Serialize(history);
        Preferences.Set("workout_history", json);
    }

    // --- PERFIL Y PESO ---
    public static AthleteProfile LoadProfile()
    {
        var json = Preferences.Get("athlete_profile", "");
        if (string.IsNullOrEmpty(json)) return new AthleteProfile();
        try { return JsonSerializer.Deserialize<AthleteProfile>(json) ?? new AthleteProfile(); }
        catch { return new AthleteProfile(); }
    }

    public static void SaveProfile(AthleteProfile profile) =>
        Preferences.Set("athlete_profile", JsonSerializer.Serialize(profile));

    // --- EVENTO DE PELEA ---
    public static FightEvent LoadFightEvent()
    {
        var json = Preferences.Get("fight_event", "");
        if (string.IsNullOrEmpty(json)) return new FightEvent();
        try { return JsonSerializer.Deserialize<FightEvent>(json) ?? new FightEvent(); }
        catch { return new FightEvent(); }
    }

    public static void SaveFightEvent(FightEvent fightEvent)
    {
        Preferences.Set("fight_event", JsonSerializer.Serialize(fightEvent));
    }
}