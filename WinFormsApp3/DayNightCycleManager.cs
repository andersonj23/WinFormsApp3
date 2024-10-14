using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SanguigoreRPG
{
    public class ShaderManager
    {
        private readonly DisplayForm displayForm;
        private readonly System.Windows.Forms.Timer shaderTimer;
        private readonly Bitmap shaderLayer;
        private TimeSpan lastShaderUpdateTime = TimeSpan.Zero;

        public ShaderManager(DisplayForm form)
        {
            displayForm = form;

            // Create shader layer with the same dimensions as the display
            shaderLayer = new Bitmap(displayForm.ClientSize.Width, displayForm.ClientSize.Height);

            // Set up the shader timer
            shaderTimer = new System.Windows.Forms.Timer
            {
                Interval = 100  // Adjust frequency if needed
            };
            shaderTimer.Tick += ShaderTimer_Tick;
            shaderTimer.Start();
        }

        private void ShaderTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan currentTime = displayForm.GetElapsedTime();

            if (ShouldUpdateShader(currentTime))
            {
                ApplyShaderEffect(currentTime);
                lastShaderUpdateTime = currentTime;
            }
        }

        private bool ShouldUpdateShader(TimeSpan currentTime)
        {
            // Ensure updates happen every minute for performance
            return Math.Abs((currentTime - lastShaderUpdateTime).TotalMinutes) >= 1;
        }

        private void ApplyShaderEffect(TimeSpan time)
        {
            double progress = 0.0;

            // Calculate transition progress based on the time of day
            if (time.Hours >= 6 && time.Hours < 18)
            {
                // Morning transition: 4 AM to 6 AM (smooth fade-out)
                progress = time.Hours < 8
                    ? 1.0 - Math.Min((time.TotalHours - 6) / 2.0, 1.0)  // 6 AM to 8 AM
                    : 0.0;  // Fully day, no shader

                ClearShaderLayer();  // Reset shader layer after morning fade-out
            }
            else
            {
                // Evening transition: 6 PM to 8 PM (smooth fade-in)
                progress = time.Hours >= 18
                    ? Math.Min((time.TotalHours - 18) / 2.0, 1.0)  // 6 PM to 8 PM
                    : Math.Min((time.TotalHours + 6) / 2.0, 1.0);  // Midnight to 2 AM
            }

            DrawShaderLayer((float)progress);
            displayForm.Invalidate();  // Request redraw with the updated shader layer
        }

        private void ClearShaderLayer()
        {
            using (Graphics g = Graphics.FromImage(shaderLayer))
            {
                g.Clear(Color.Transparent); // Clear the shader layer
            }
        }

        private void DrawShaderLayer(float intensity)
        {
            using (Graphics g = Graphics.FromImage(shaderLayer))
            {
                g.Clear(Color.Transparent);  // Clear previous shader
                g.CompositingMode = CompositingMode.SourceOver;

                // Adjust alpha to cap intensity at 180
                int alpha = (int)(intensity * 180);  // Max alpha at 180 for visibility

                using (Brush brush = new SolidBrush(Color.FromArgb(alpha, 10, 10, 40)))
                {
                    g.FillRectangle(brush, 0, 0, shaderLayer.Width, shaderLayer.Height);
                }
            }
        }

        // Render the shader layer on top of the form's graphics
        public void RenderShader(Graphics g)
        {
            g.DrawImage(shaderLayer, Point.Empty);
        }
    }
}
