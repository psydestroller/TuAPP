using Microsoft.Maui.Graphics;

namespace TuAPP // <- REVISA QUE ESTE NAMESPACE COINCIDA CON TU PROYECTO
{
    public class CircularProgressDrawable : IDrawable
    {
        // Propiedades enlazables (Bindable) para controlar el dibujo
        public double Progress { get; set; } = 0.0; // Valor de 0.0 a 1.0 (para el 0 al 100%)
        public Color ProgressColor { get; set; } = Color.FromArgb("#10B981"); // Verde esmeralda (como tu diseño)
        public Color BackgroundColor { get; set; } = Color.FromArgb("#18181B"); // Gris oscuro del diseño
        public float Thickness { get; set; } = 15f; // Grosor de la línea

        // Variables precalculadas para mejorar el rendimiento (fuera de Draw)
        private float startAngle = -90f; // Empezar en la parte superior (12 en punto)

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // --- OPTIMIZACIÓN 1: Cálculo eficiente de medidas ---
            // Centramos el dibujo en el GraphicsView
            float centerX = dirtyRect.Center.X;
            float centerY = dirtyRect.Center.Y;

            // Calculamos el radio asegurándonos de que quepa todo el grosor
            float maxDimension = Math.Min(dirtyRect.Width, dirtyRect.Height);
            float radius = (maxDimension / 2f) - (Thickness / 2f);

            // Evitamos errores de renderizado si el radio es negativo o cero
            if (radius <= 0) return;


            // --- OPTIMIZACIÓN 2: Usar funciones nativas suaves ---

            // A) DIBUJAR EL ANILLO DE FONDO (Gris oscuro)
            // canvas.SetFillColor no sirve para anillos, necesitamos Stroke
            canvas.StrokeColor = BackgroundColor;
            canvas.StrokeSize = Thickness;
            canvas.StrokeLineCap = LineCap.Round; // Puntas redondeadas

            // Dibujamos un arco completo (360 grados) para el fondo
            canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, 0, 360, true, false);


            // B) DIBUJAR EL ANILLO DE PROGRESO (Verde)
            if (Progress > 0)
            {
                canvas.StrokeColor = ProgressColor;

                // --- CLAVE PARA LA FLUIDEZ: DrawArc ---
                // Calculamos el ángulo final basado en el progreso (0.0 - 1.0) * 360 grados
                float sweepAngle = (float)(Progress * 360.0);

                // MAUI maneja los ángulos en sentido horario.
                // DrawArc dibuja un arco continuo y suave, no por "pedacitos".
                canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2,
                               startAngle, startAngle + sweepAngle,
                               true, // En sentido de las agujas del reloj
                               false); // No rellenar el centro del arco
            }
        }
    }
}