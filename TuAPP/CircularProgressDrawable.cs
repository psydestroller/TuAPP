using Microsoft.Maui.Graphics;

namespace TuAPP;

public class CircularProgressDrawable : IDrawable
{
    // Iniciamos en 1.0 (100%) para que el círculo de color aparezca INSTANTÁNEAMENTE al abrir la pantalla
    public double Progress { get; set; } = 1.0;
    public Color ProgressColor { get; set; } = Colors.White;

    // Este es el color del carril de fondo (El mismo gris elegante de tus botones)
    public Color BackgroundTrackColor { get; set; } = Color.FromArgb("#18181B");

    // Grosor del círculo (Puedes subirlo a 20f si lo quieres más grueso)
    public float Thickness { get; set; } = 15f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // 1. Calculamos el centro exacto del cuadrado (320x320)
        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;

        // 2. Calculamos el radio restando la mitad del grosor para que no se ampute en los bordes de la pantalla
        float radius = (Math.Min(dirtyRect.Width, dirtyRect.Height) / 2) - (Thickness / 2);

        // ==========================================
        // CAPA 1: EL ANILLO DE FONDO GRIS (Siempre visible)
        // ==========================================
        canvas.StrokeColor = BackgroundTrackColor;
        canvas.StrokeSize = Thickness;
        canvas.StrokeDashPattern = null;
        canvas.DrawCircle(centerX, centerY, radius);

        // ==========================================
        // CAPA 2: EL ARCO DE COLOR QUE SE VA VACIANDO
        // ==========================================
        if (Progress > 0)
        {
            canvas.StrokeColor = ProgressColor;
            canvas.StrokeSize = Thickness;
            canvas.StrokeLineCap = LineCap.Round; // Esto hace que las puntas del arco sean redondas y suaves

            // En .NET MAUI Graphics, 90 grados es "Arriba" (las 12 en punto)
            float startAngle = 90f;

            // Calculamos hacia dónde debe detenerse el arco basado en el progreso
            float endAngle = startAngle - (float)(Progress * 360f);

            // Definimos el área exacta donde el arco se va a dibujar
            float x = centerX - radius;
            float y = centerY - radius;
            float width = radius * 2;
            float height = radius * 2;

            // Dibujamos el arco (clockwise = true para que el reloj avance hacia la derecha)
            canvas.DrawArc(x, y, width, height, startAngle, endAngle, true, false);
        }
    }
}