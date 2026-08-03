using Microsoft.Maui.Graphics;

namespace TuAPP;

public class CircularProgressDrawable : IDrawable
{
    public double Progress { get; set; } = 1.0;
    public Color ProgressColor { get; set; } = Colors.White;
    public Color BackgroundTrackColor { get; set; } = Color.FromArgb("#18181B");

    // ¡Aro súper grueso para verlo desde la otra esquina del gimnasio!
    public float Thickness { get; set; } = 35f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;
        float radius = (Math.Min(dirtyRect.Width, dirtyRect.Height) / 2) - (Thickness / 2) - 2f;

        // ==========================================
        // CAPA 1: DECORACIÓN DEL RELOJ (Rayitas interiores)
        // ==========================================
        // Calculamos el borde interno del aro grueso, y le damos 8 píxeles de espacio hacia adentro
        float innerEdge = radius - (Thickness / 2) - 8f;

        // Dibujamos 60 marcas alrededor del círculo
        for (int i = 0; i < 60; i++)
        {
            float angle = i * 6f; // 360 grados / 60 marcas = 6 grados por marca
            double angleRad = (angle - 90) * Math.PI / 180.0;

            bool isMajorTick = (i % 5 == 0); // Cada 5 marcas hacemos una principal (ej. 12, 1, 2, 3...)

            float tickLength = isMajorTick ? 14f : 6f; // Las principales son más largas
            canvas.StrokeSize = isMajorTick ? 3f : 1.5f; // Las principales son más gruesas
            canvas.StrokeColor = isMajorTick ? Color.FromArgb("#A1A1AA") : Color.FromArgb("#27272A");

            // Calculamos punto de inicio y fin de cada rayita
            float x1 = centerX + (float)(innerEdge * Math.Cos(angleRad));
            float y1 = centerY + (float)(innerEdge * Math.Sin(angleRad));

            float x2 = centerX + (float)((innerEdge - tickLength) * Math.Cos(angleRad));
            float y2 = centerY + (float)((innerEdge - tickLength) * Math.Sin(angleRad));

            canvas.DrawLine(x1, y1, x2, y2);
        }

        // ==========================================
        // CAPA 2: EL ANILLO DE FONDO GRIS
        // ==========================================
        canvas.StrokeColor = BackgroundTrackColor;
        canvas.StrokeSize = Thickness;
        canvas.StrokeDashPattern = null;
        canvas.DrawCircle(centerX, centerY, radius);

        // ==========================================
        // CAPA 3: EL ARCO DE COLOR (Progreso)
        // ==========================================
        if (Progress > 0)
        {
            canvas.StrokeColor = ProgressColor;
            canvas.StrokeSize = Thickness;
            canvas.StrokeLineCap = LineCap.Round;

            float startAngle = 90f;
            float endAngle = startAngle - (float)(Progress * 360f);

            float x = centerX - radius;
            float y = centerY - radius;
            float width = radius * 2;
            float height = radius * 2;

            canvas.DrawArc(x, y, width, height, startAngle, endAngle, true, false);
        }
    }
}