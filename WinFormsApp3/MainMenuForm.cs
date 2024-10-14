using System;
using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace SanguigoreRPG
{
    // Class for storing game settings like fullscreen/windowed mode
    public class GameSettings
    {
        public bool IsFullscreen { get; set; } = false;  // Default to windowed mode
        public int TerrainWindowWidth { get; set; } = 1920;  // Default width
        public int TerrainWindowHeight { get; set; } = 1080;  // Default height
        private static string settingsFilePath = "game_settings.json";

        // Method to load settings from file
        public static GameSettings LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                return JsonConvert.DeserializeObject<GameSettings>(json);
            }
            return new GameSettings();  // Return default settings if no file found
        }

        // Method to save settings to file
        public void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(settingsFilePath, json);
        }
    }

    public class MainMenuForm : Form
    {
        private Button startButton;
        private Button loadButton; // Added Load Game button
        private Button settingsButton;
        private Button exitButton;
        private RadioButton windowedModeButton;
        private RadioButton fullscreenModeButton;
        private Button settings2Button; // New settings button

        private System.Windows.Forms.Timer titleColorTimer;


        private Color startColor = Color.DarkRed;
        private Color endColor = Color.DarkSlateGray;

        private string gameTitle = "Sanguigore";
        private Color[] letterColors;
        private float fadeStep = 0.01f;
        private float[] colorPositions;

        private GameSettings gameSettings;


        public MainMenuForm()
        {
            // Load settings at startup
            gameSettings = GameSettings.LoadSettings();

            // Set up the form (window)
            this.Text = "Sanguigore - Main Menu";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            // Apply settings (fullscreen/windowed mode)
            if (gameSettings.IsFullscreen)
            {
                SetFullscreenMode();
            }
            else
            {
                SetWindowedMode();
            }

            // Initialize the colors array for each letter and position in the fade
            letterColors = new Color[gameTitle.Length];
            colorPositions = new float[gameTitle.Length];

            for (int i = 0; i < letterColors.Length; i++)
            {
                letterColors[i] = startColor;
                colorPositions[i] = 0.0f;
            }

            // Initialize buttons
            MainMenuForm_Initialize();

            // Set up a timer to gradually fade the color of the game title
            titleColorTimer = new System.Windows.Forms.Timer();
            titleColorTimer.Interval = 50;
            titleColorTimer.Tick += TitleColorTimer_Tick;
            titleColorTimer.Start();

            // Handle resizing to ensure buttons are centered
            this.Resize += MainMenuForm_Resize;
            CenterElements();
        }

        private void MainMenuForm_Initialize()
        {
            // Clear existing controls
            this.Controls.Clear();

            // Rebuild the New Game button
            startButton = new Button
            {
                Text = "New Game",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width - 200) / 2, 150) // Adjusted Y position
            };
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);

            // Rebuild the Load Game button
            loadButton = new Button
            {
                Text = "Load Game",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width - 200) / 2, 230) // Adjusted Y position
            };
            loadButton.Click += LoadButton_Click;
            this.Controls.Add(loadButton);

            // Rebuild the Settings button
            settingsButton = new Button
            {
                Text = "Settings",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width - 200) / 2, 310) // Adjusted Y position
            };
            settingsButton.Click += SettingsButton_Click;
            this.Controls.Add(settingsButton);
            settingsButton.BringToFront();



            Console.WriteLine($"Settings button location: {settingsButton.Location}");

            // Rebuild the Settings2 button
            settings2Button = new Button
            {
                Text = "Settings",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width - 200) / 2, 470) // Adjusted Y position
            };
            settings2Button.Click += SettingsButton_Click; // Link to the same event handler as settingsButton
            this.Controls.Add(settings2Button);



            // Rebuild the Exit button
            exitButton = new Button
            {
                Text = "Exit",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width - 200) / 2, 390) // Adjusted Y position
            };
            exitButton.Click += ExitButton_Click;
            this.Controls.Add(exitButton);
            this.Controls.Add(settingsButton);
            this.Invalidate(); // Forces the control to redraw
            this.Update();     // Refreshes the control
            




            // Ensure the controls are centered and not hidden
            CenterElements();

            // Debug logging to ensure the buttons are being added
            Console.WriteLine("Buttons added: New Game, Load Game, Settings, Exit");
        }




        // Event handler for the Start button
        private void StartButton_Click(object sender, EventArgs e)
        {
            string saveFilePath = "saved_game.json";
            if (File.Exists(saveFilePath))
            {
                GameState loadedGameState = LoadGame(saveFilePath);

                if (loadedGameState != null)
                {
                    this.Hide();  // Hide the main menu
                    DisplayForm displayForm = new DisplayForm(loadedGameState.BiomeColors, loadedGameState.TileSize, loadedGameState);
                    displayForm.FormClosed += (s, args) => this.Close();  // Close the main menu when the display form is done
                    displayForm.Show();
                }
                else
                {
                    MessageBox.Show("Error loading saved game.");
                }
            }
            else
            {
                this.Hide();
                WorldGenForm worldGenForm = new WorldGenForm(gameSettings);
                worldGenForm.FormClosed += (s, args) => this.Close();
                worldGenForm.Show();
            }
        }

        // Event handler for the Load Game button
        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON Files|*.json";
                openFileDialog.Title = "Load Game";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFilePath = openFileDialog.FileName;

                    GameState loadedGameState = LoadGame(selectedFilePath); // Load the game state
                    if (loadedGameState != null)
                    {
                        // Add 1 second to the game time
                        loadedGameState.GameTime += TimeSpan.FromSeconds(1);
                        // Hide the main menu and show DisplayForm with the loaded game state
                        this.Hide();
                        DisplayForm displayForm = new DisplayForm(loadedGameState.BiomeColors, loadedGameState.TileSize, loadedGameState);

                        // Initialize the clock directly after the form loads
                        displayForm.Shown += (s, args) => displayForm.UpdateGameTime(loadedGameState.GameTime);

                        displayForm.FormClosed += (s, args) => this.Close();
                        displayForm.Show();
                    }
                    else
                    {
                        MessageBox.Show("Failed to load game.");
                    }
                }
            }
        }


        private GameState LoadGame(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Save file not found.");
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<GameState>(json);
        }

        // Event handler for the Settings button to open settings in the same form
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            // Clear the current controls
            this.Controls.Clear();

            // Add a back button to return to the main menu
            Button backButton = new Button
            {
                Text = "Back",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
                Location = new Point((this.ClientSize.Width - 200) / 2, 400)
            };
            backButton.Click += BackButton_Click;
            this.Controls.Add(backButton);

            // Add radio buttons for windowed and fullscreen options
            windowedModeButton = new RadioButton
            {
                Text = "Windowed",
                Font = new Font("Arial", 14),
                Size = new Size(250, 30),  // Increased width to avoid clipping
                Location = new Point((this.ClientSize.Width - 250) / 2, 200),  // Center based on button width
                Checked = !gameSettings.IsFullscreen
            };
            windowedModeButton.CheckedChanged += WindowedModeButton_CheckedChanged;
            this.Controls.Add(windowedModeButton);

            fullscreenModeButton = new RadioButton
            {
                Text = "Fullscreen",
                Font = new Font("Arial", 14),
                Size = new Size(250, 30),  // Increased width to avoid clipping
                Location = new Point((this.ClientSize.Width - 250) / 2, 250),  // Center based on button width
                Checked = gameSettings.IsFullscreen
            };
            fullscreenModeButton.CheckedChanged += FullscreenModeButton_CheckedChanged;
            this.Controls.Add(fullscreenModeButton);

            CenterElements();  // Center everything on Settings menu load
        }

        // Event handler for the Back button to return to the main menu
        private void BackButton_Click(object sender, EventArgs e)
        {
            this.Controls.Clear();  // Clear the current controls
            MainMenuForm_Initialize();  // Rebuild the main menu
        }

        // Event handler for Windowed mode
        private void WindowedModeButton_CheckedChanged(object sender, EventArgs e)
        {
            if (windowedModeButton.Checked)
            {
                SetWindowedMode();
                gameSettings.IsFullscreen = false;  // Update the settings
                gameSettings.SaveSettings();  // Save to file
            }
        }

        // Event handler for Fullscreen mode
        private void FullscreenModeButton_CheckedChanged(object sender, EventArgs e)
        {
            if (fullscreenModeButton.Checked)
            {
                SetFullscreenMode();
                gameSettings.IsFullscreen = true;  // Update the settings
                gameSettings.SaveSettings();  // Save to file
            }
        }

        private void SetWindowedMode()
        {
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Normal;
            this.TopMost = false;
            CenterElements();  // Ensure controls are centered when switching modes
        }

        private void SetFullscreenMode()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = false;  // Keep the window on top
            CenterElements();  // Ensure controls are centered when switching modes
        }

        // Event handler for the Exit button
        private void ExitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();  // Exit the application
        }

        // Handle dynamic centering when resizing the window
        private void MainMenuForm_Resize(object sender, EventArgs e)
        {
            CenterElements(); // Center the elements again when the form resizes
        }

        // Function to center the buttons and other elements
        // Ensure the CenterElements function works for positioning the buttons
        private void CenterElements()
        {
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;

            // Ensure the controls are centered based on the client size
            if (startButton != null)
                startButton.Location = new Point((formWidth - startButton.Width) / 2, formHeight / 4);

            if (loadButton != null)
                loadButton.Location = new Point((formWidth - loadButton.Width) / 2, formHeight / 3);

            if (settingsButton != null)
                settingsButton.Location = new Point((formWidth - settingsButton.Width) / 2, (formHeight * 4) / 12);

            if (exitButton != null)
                exitButton.Location = new Point((formWidth - exitButton.Width) / 2, (formHeight * 6) / 12);
            if (settings2Button != null)
                settings2Button.Location = new Point((formWidth - settings2Button.Width) / 2, (formHeight * 5) / 12); // Adjust Y position as needed

        }

        // Event handler to gradually fade the color of each letter
        private void TitleColorTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < letterColors.Length; i++)
            {
                letterColors[i] = InterpolateColor(startColor, endColor, colorPositions[i]);
                colorPositions[i] += fadeStep;

                if (colorPositions[i] >= 1.0f || colorPositions[i] <= 0.0f)
                {
                    fadeStep = -fadeStep;  // Reverse the fade direction
                }
            }

            Invalidate();  // Force the form to repaint and apply the new colors
        }

        // Function to interpolate between two colors
        private Color InterpolateColor(Color startColor, Color endColor, float position)
        {
            // Clamp the position to be between 0 and 1
            position = Math.Max(0, Math.Min(1, position));

            // Interpolate the color components
            int red = (int)(startColor.R + (endColor.R - startColor.R) * position);
            int green = (int)(startColor.G + (endColor.G - startColor.G) * position);
            int blue = (int)(startColor.B + (endColor.B - startColor.B) * position);

            // Clamp each component to the valid range [0, 255]
            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));

            // Return the interpolated color
            return Color.FromArgb(red, green, blue);
        }

        // Override OnPaint to draw the title with gradually fading colors for each letter
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Define font and position
            Font titleFont = new Font("Arial", 48, FontStyle.Bold);
            SizeF totalSize = e.Graphics.MeasureString(gameTitle, titleFont);

            // Center the text horizontally
            float x = (this.ClientSize.Width - totalSize.Width) / 2;
            float y = 50;

            // Draw each letter individually with its assigned color
            for (int i = 0; i < gameTitle.Length; i++)
            {
                string letter = gameTitle[i].ToString();
                SizeF letterSize = e.Graphics.MeasureString(letter, titleFont);

                using (Brush brush = new SolidBrush(letterColors[i]))
                {
                    e.Graphics.DrawString(letter, titleFont, brush, x, y);
                }

                x += letterSize.Width - 10;  // Adjust letter spacing slightly
            }
        }
    }
}