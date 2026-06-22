using Microsoft.Maui.Graphics;

namespace TuAPP;

public class CircularProgressDrawable : IDrawable
{
    public double Progress { get; set; } = 1.0;
    public Color ProgressColor { get; set; } = Color.FromArgb("#10B981");
    public Color BackgroundColor { get; set; } = Color.FromArgb("#18181B");
    public int Thickness { get; set; } = 15;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float centerX = dirtyRect.Center.X;
        float centerY = dirtyRect.Center.Y;
        float radius = (Math.Min(dirtyRect.Width, dirtyRect.Height) / 2) - (Thickness / 2f);

        canvas.StrokeColor = BackgroundColor;
        canvas.StrokeSize = Thickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 0, 360, true, false);

        if (Progress > 0)
        {
            canvas.StrokeColor = ProgressColor;

            // Inicia arriba (90 grados en MAUI) y dibuja solo la fracción que queda
            float startAngle = 90f;
            float endAngle = 90f - (float)(Progress * 360f);

            canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, startAngle, endAngle, true, false);
        }
    }
}