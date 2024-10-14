using System;
using System.Drawing;
using System.Windows.Forms;

namespace SanguigoreRPG
{
    public class DetailedMapForm : Form
    {
        private Color[,] detailedBiomeColors;
        private int tileSize;
        private int segmentWidth;
        private int segmentHeight;
        private GameSettings gameSettings;

        // BufferedGraphics for smoother rendering
        private BufferedGraphicsContext bufferedContext;
        private BufferedGraphics bufferedGraphics;

        // Console Panel
        private Panel consolePanel;
        private TextBox consoleTextBox;

        // Track if plus and minus keys are pressed
        private bool isPlusPressed = false;
        private bool isMinusPressed = false;

        public DetailedMapForm(Color[,] mapSegment, int tileSize, GameSettings settings)
        {
            this.detailedBiomeColors = mapSegment;
            this.tileSize = tileSize;
            this.segmentWidth = mapSegment.GetLength(0);
            this.segmentHeight = mapSegment.GetLength(1);
            this.gameSettings = settings;

            // Set up the form size based on the detailed map size and tile size
            this.ClientSize = new Size(segmentWidth * tileSize, segmentHeight * tileSize);

            // Apply window settings (fullscreen/windowed)
            ApplyWindowSettings();

            // Initialize BufferedGraphics
            InitializeBufferedGraphics();

            // Enable double-buffering to reduce flicker during rendering
            this.DoubleBuffered = true;

            // Add a "Back to Large Map" button
            AddBackToMapButton();

            // Ask the user to confirm before proceeding
            ShowGoToConfirmation();

            // Initialize console (hidden by default)
            InitializeConsole();

            // Enable key preview for key events
            this.KeyPreview = true;
        }

        private void InitializeBufferedGraphics()
        {
            // Set up BufferedGraphics for smoother rendering
            bufferedContext = BufferedGraphicsManager.Current;
            bufferedGraphics = bufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
        }

        private void ApplyWindowSettings()
        {
            if (gameSettings.IsFullscreen)
            {
                SetFullscreenMode();
            }
            else
            {
                SetWindowedMode();
            }
        }

        private void SetFullscreenMode()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
        }

        private void SetWindowedMode()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Normal;
            this.TopMost = false;
            this.ClientSize = new Size(gameSettings.TerrainWindowWidth, gameSettings.TerrainWindowHeight);
        }

        private void ShowGoToConfirmation()
        {
            var result = MessageBox.Show("Go to Detailed Map?", "Confirm", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
            {
                this.Close(); // Close the detailed map form if they choose "No"
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int screenWidth = this.ClientSize.Width;
            int screenHeight = this.ClientSize.Height;

            float scaleX = (float)screenWidth / (segmentWidth * tileSize);
            float scaleY = (float)screenHeight / (segmentHeight * tileSize);
            float scale = Math.Min(scaleX, scaleY);

            int scaledWidth = (int)(segmentWidth * tileSize * scale);
            int scaledHeight = (int)(segmentHeight * tileSize * scale);
            int offsetX = (screenWidth - scaledWidth) / 2;
            int offsetY = (screenHeight - scaledHeight) / 2;

            e.Graphics.Clear(Color.Black);

            for (int x = 0; x < segmentWidth; x++)
            {
                for (int y = 0; y < segmentHeight; y++)
                {
                    Brush brush = new SolidBrush(detailedBiomeColors[x, y]);
                    e.Graphics.FillRectangle(brush,
                                             offsetX + (x * tileSize * scale),
                                             offsetY + (y * tileSize * scale),
                                             tileSize * scale,
                                             tileSize * scale);
                }
            }
        }

        // Set the highlighted area based on selection
        

        // Dispose BufferedGraphics when the form is closed to avoid resource leaks
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bufferedGraphics?.Dispose();
                bufferedContext?.Dispose();
            }
            base.Dispose(disposing);
        }

        // Adds a button to allow the player to return to the large map (DisplayForm)
        private void AddBackToMapButton()
        {
            Button backButton = new Button
            {
                Text = "Back to Large Map",
                Location = new Point(10, 10),
                Size = new Size(120, 30)
            };

            backButton.Click += (sender, e) =>
            {
                foreach (Form openForm in Application.OpenForms)
                {
                    if (openForm is DisplayForm)
                    {
                        openForm.BringToFront();
                        break;
                    }
                }
                this.Close();  // Close the detailed map
            };

            this.Controls.Add(backButton);
        }

        // Console Implementation
        private void InitializeConsole()
        {
            // Create a panel for the console
            consolePanel = new Panel
            {
                Size = new Size(this.ClientSize.Width, 200),
                BackColor = Color.Black,
                Visible = false, // Hidden by default
                Dock = DockStyle.Bottom
            };

            // Create a text box for user input
            consoleTextBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Font = new Font("Consolas", 12),
                BorderStyle = BorderStyle.None
            };

            consolePanel.Controls.Add(consoleTextBox);
            this.Controls.Add(consolePanel);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                isPlusPressed = true;
            }

            if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
            {
                isMinusPressed = true;
            }

            // Check if both keys are pressed
            if (isPlusPressed && isMinusPressed)
            {
                ToggleConsole();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                isPlusPressed = false;
            }

            if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
            {
                isMinusPressed = false;
            }
        }

        private void ToggleConsole()
        {
            consolePanel.Visible = !consolePanel.Visible;

            // Set focus on the console when opened
            if (consolePanel.Visible)
            {
                consoleTextBox.Focus();
            }
        }

        // Handle commands typed in the console (optional)
        private void HandleConsoleCommand(string command)
        {
            // Example: Close the console when "exit" is typed
            if (command.ToLower() == "exit")
            {
                consolePanel.Visible = false;
            }
            else
            {
                // Add feedback or custom commands
                consoleTextBox.AppendText($"Command '{command}' not recognized.\n");
            }
        }
    }
}
