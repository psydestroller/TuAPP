using Microsoft.Maui.Graphics;

namespace TuAPP;

public class CircularProgressDrawable : IDrawable
{
    public double Progress { get; set; } = 1.0;
    public Color ProgressColor { get; set; } = Colors.White;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var centerX = dirtyRect.Width / 2;
        var centerY = dirtyRect.Height / 2;
        var radius = Math.Min(centerX, centerY) - 10; // 10px de margen

        // 1. Dibuja el fondo del anillo (gris muy oscuro)
        canvas.StrokeColor = Color.FromArgb("#1A1A1A");
        canvas.StrokeSize = 12;
        canvas.DrawCircle(centerX, centerY, radius);

        // Si el progreso es 0 o menor, no dibujamos color
        if (Progress <= 0) return;

        // 2. Dibuja el progreso actual
        canvas.StrokeColor = ProgressColor;
        canvas.StrokeSize = 12;
        canvas.StrokeLineCap = LineCap.Round;

        // MAUI calcula los ángulos: 90 grados es arriba al centro.
        float startAngle = 90;
        // Calculamos cuánto arco dibujar en base al progreso (0.0 a 1.0)
        float endAngle = startAngle - (float)(360 * Progress);

        // true = se dibuja en el sentido de las agujas del reloj
        canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, startAngle, endAngle, true, false);
    }
}