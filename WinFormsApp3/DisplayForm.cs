    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.IO;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Threading.Tasks;
using System.Numerics;
using System.Diagnostics;

    namespace SanguigoreRPG
    {
    public class GameState
    {
        public Color[,] BiomeColors { get; set; }
        public TimeSpan GameTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }  // New property for elapsed time
        public int TileSize { get; set; }
    }


    public class DisplayForm : Form
        {
            private Color[,] biomeColors;
            private int mapWidth;
            private int mapHeight;
            private int tileSize;
            private GameSettings gameSettings;
            private Bitmap bufferedMap;
            private Bitmap gridBuffer;

            private TimeSpan gameTime;
            private Label clockLabel;
            private System.Windows.Forms.Timer clockTimer;

            private Rectangle[,] sections;

            private bool isDraggingClock;
            private Point dragStartPoint;

            private Button saveButton;
            private Button loadButton;
            private List<(int x, int y)> clickedCoordinates = new List<(int x, int y)>();
        private double timeMultiplier = 1.0; // 1 means normal speed, 2 means double speed, 0.5 means half speed, etc.

        private Label threadTimerLabel;           // To display the timer
        private System.Timers.Timer threadTimer; // For updating the timer on the UI thread
        private double vibeMultiplier = 1.0;     // Speed multiplier
                // Track elapsed time\

        

        private Stopwatch stopwatch;  // High precision timer
        private TimeSpan previousElapsed;  // Track previous elapsed time

        private ShaderManager shaderManager;  // Add this field
        private TimeSpan elapsedTime = new TimeSpan(12, 0, 0); // Start at 12:00:00


        private CancellationTokenSource cancellationTokenSource;
        private readonly object cancellationLock = new(); // Lock for thread-safety
        private bool isGeneratingTerrain = false; // Track if terrain is being generated

        private bool isLoading = false; // New flag to track loading state.

        private bool timersInitialized = false; // Ensure timers start only once.

        private bool terrainGenerated = false; // Prevent multiple generations.

        













        public DisplayForm(Color[,] biomeColors, int tileSize, GameState loadedGameState = null)
        {
            // Initialize game state and set map dimensions
            if (loadedGameState != null)
            {
                this.biomeColors = loadedGameState.BiomeColors;
                this.tileSize = loadedGameState.TileSize;
                this.gameTime = loadedGameState.GameTime;
                this.mapWidth = biomeColors.GetLength(0);
                this.mapHeight = biomeColors.GetLength(1);
            }
            else
            {
                this.biomeColors = biomeColors;
                this.tileSize = tileSize;
                this.mapWidth = biomeColors.GetLength(0);
                this.mapHeight = biomeColors.GetLength(1);
                this.gameTime = new TimeSpan(12, 0, 0); // Default start time
            }

            // Configure the form properties
            this.KeyPreview = true;
            gameSettings = GameSettings.LoadSettings();
            DoubleBuffered = true;
            ClientSize = new Size(mapWidth * tileSize, mapHeight * tileSize);
            shaderManager = new ShaderManager(this);  // Initialize ShaderManager here
            cancellationTokenSource = new CancellationTokenSource(); // Initialize here


            if (gameSettings.IsFullscreen)
                SetFullscreenMode();
            else
                SetWindowedMode();

            Text = "Sanguigore - Generated World";
            StartPosition = FormStartPosition.CenterScreen;

            // Create the map buffers only once
            bufferedMap = new Bitmap(mapWidth * tileSize, mapHeight * tileSize);
            gridBuffer = new Bitmap(mapWidth * tileSize, mapHeight * tileSize);

            // *** Add these two lines to draw the map and grid only once ***
            DrawMapToBuffer();  // Draw the map once during initialization
            DrawGridToBuffer(); // Draw the grid once during initialization

            InitializeSections();
            InitializeGameClock();
            InitializeSaveLoadButtons();
            InitializeThreadedTimer();
            InitializeCancellationTokenSource();

            // Start the clock timer
            clockTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000  // Update every second
            };
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
            

        }

        private void InitializeCancellationTokenSource()
        {
            lock (cancellationLock) // Ensure thread-safe access
            {
                cancellationTokenSource = new CancellationTokenSource();
            }
        }
        protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                    return cp;
                }
            }

            protected override void OnPaintBackground(PaintEventArgs e) { }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.Escape)
                {
                    OpenMainMenu();
                }
            }

            private void SetFullscreenMode()
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                TopMost = false;
            }

            private void SetWindowedMode()
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                Size = new Size(gameSettings.TerrainWindowWidth, gameSettings.TerrainWindowHeight);
                WindowState = FormWindowState.Normal;
                TopMost = false;
            }

        private void InitializeTimers()
        {
            if (timersInitialized) return; // Prevent re-initialization.

            // Clock Timer: Update game time every second.
            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += ClockTimer_Tick;

            // Threaded Timer: Update elapsed time smoothly.
            threadTimer = new System.Timers.Timer(10); // ~100 FPS
            threadTimer.Elapsed += ThreadTimer_Tick;

            timersInitialized = true; // Mark as initialized.
        }
        private void DrawMapToBuffer()
        {
            using (Graphics g = Graphics.FromImage(bufferedMap))
            {
                if (biomeColors != null)
                {
                    Brush brush = new SolidBrush(Color.Black);  // Default brush

                    for (int x = 0; x < mapWidth; x++)
                    {
                        for (int y = 0; y < mapHeight; y++)
                        {
                            brush = new SolidBrush(biomeColors[x, y]);
                            g.FillRectangle(brush, x * tileSize, y * tileSize, tileSize, tileSize);
                        }
                    }

                    brush.Dispose();
                }
            }
        }

        private async Task GenerateTerrainOnce()
        {
            if (terrainGenerated || isGeneratingTerrain) return; // Ensure it runs only once.

            isGeneratingTerrain = true;

            try
            {
                await GenerateTerrainDynamically();
                terrainGenerated = true; // Mark as completed.
            }
            finally
            {
                isGeneratingTerrain = false;
            }
        }







        private void DrawGridToBuffer()
        {
            using (Graphics g = Graphics.FromImage(gridBuffer))
            {
                // Adjust grid thickness and color
                using (Pen gridPen = new Pen(Color.FromArgb(100, 0, 0, 0), 3)) // Make the grid lines thicker (3px)
                {
                    int gridSpacing = 50; // Space grid every 50 pixels, adjust to your needs

                    // Draw vertical grid lines
                    for (int x = 0; x < gridBuffer.Width; x += gridSpacing)
                    {
                        g.DrawLine(gridPen, x, 0, x, gridBuffer.Height);
                    }

                    // Draw horizontal grid lines
                    for (int y = 0; y < gridBuffer.Height; y += gridSpacing)
                    {
                        g.DrawLine(gridPen, 0, y, gridBuffer.Width, y);
                    }
                }
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawImage(bufferedMap, 0, 0);  // Render the map once.
            e.Graphics.DrawImage(gridBuffer, 0, 0);   // Render the grid once.

            // Render shader layer if active.
            shaderManager.RenderShader(e.Graphics);
        }




        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            gameTime = gameTime.Add(TimeSpan.FromSeconds(1 * timeMultiplier));
            if (gameTime.TotalHours >= 24)
                gameTime = TimeSpan.FromHours(gameTime.TotalHours % 24);

            // Only update the label if it changes.
            UpdateClockLabel();
        }




        private void InvalidateDirtyRegions()
        {
            // Invalidate only a specific portion of the screen instead of the entire map
            var dirtyRect = new Rectangle(0, 0, mapWidth * tileSize / 4, mapHeight * tileSize / 4);
            Invalidate(dirtyRect);
        }



        // Add a condition to determine if the map actually needs to be redrawn
        private bool ShouldRedrawMap()
        {
            // Example: Only redraw if it's a new hour or significant time interval (e.g., every 5 minutes)
            return gameTime.Minutes % 5 == 0 && gameTime.Seconds == 0;
        }



        



        private void InitializeGameClock()
        {
            gameTime = new TimeSpan(12, 0, 0);

            // Remove or comment out the clock label initialization and event handling
            clockLabel = new Label
            {
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(this.ClientSize.Width - 150, 10),
                AutoSize = true
            };

            UpdateClockLabel();

            // Don't add the clockLabel to the Controls so it won't be displayed
            // this.Controls.Add(clockLabel);

            // Optionally, keep the mouse events for clockLabel if you want to add it back later
            // clockLabel.MouseDown += ClockLabel_MouseDown;
            // clockLabel.MouseMove += ClockLabel_MouseMove;
            // clockLabel.MouseUp += ClockLabel_MouseUp;
        }

        private void StartTimers()
        {
            if (!timersInitialized) InitializeTimers();
            clockTimer?.Start();
            threadTimer?.Start();
        }

        private void StopTimers()
        {
            clockTimer?.Stop();
            threadTimer?.Stop();
        }

        private void ClockLabel_MouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDraggingClock = true;
                    dragStartPoint = new Point(e.X, e.Y);
                }
            }

            private void ClockLabel_MouseMove(object sender, MouseEventArgs e)
            {
                if (isDraggingClock)
                {
                    int newLeft = clockLabel.Left + (e.X - dragStartPoint.X);
                    int newTop = clockLabel.Top + (e.Y - dragStartPoint.Y);
                    newLeft = Math.Max(0, Math.Min(newLeft, this.ClientSize.Width - clockLabel.Width));
                    newTop = Math.Max(0, Math.Min(newTop, this.ClientSize.Height - clockLabel.Height));
                    clockLabel.Left = newLeft;
                    clockLabel.Top = newTop;
                }
            }

            private void ClockLabel_MouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) isDraggingClock = false;
            }

        private void UpdateClockLabel()
        {
            // Update the clock label to show hours, minutes, seconds, and milliseconds
            clockLabel.Text = gameTime.ToString(@"hh\:mm\:ss\.fff");
        }


        private void InitializeSections()
            {
                int sectionWidth = 25;
                int sectionHeight = 25;

                int sectionCountX = mapWidth / sectionWidth;
                int sectionCountY = mapHeight / sectionHeight;

                sections = new Rectangle[sectionCountX, sectionCountY];

                for (int x = 0; x < sectionCountX; x++)
                {
                    for (int y = 0; y < sectionCountY; y++)
                    {
                        sections[x, y] = new Rectangle(x * sectionWidth * tileSize, y * sectionHeight * tileSize, sectionWidth * tileSize, sectionHeight * tileSize);
                    }
                }
            }

            private void OpenMainMenu()
            {
                this.Hide();
                MainMenuForm mainMenu = new MainMenuForm();
                mainMenu.FormClosed += (s, args) => this.Close();
                mainMenu.Show();
            }

        private void SaveGame(string filePath)
        {
            GameState gameState = new GameState
            {
                BiomeColors = biomeColors,
                GameTime = gameTime,
                ElapsedTime = elapsedTime,  // Save the elapsed time
                TileSize = tileSize
            };

            string json = JsonConvert.SerializeObject(gameState, Formatting.Indented);
            File.WriteAllText(filePath, json);
            MessageBox.Show("Game saved successfully!");
        }



        public GameState LoadGame(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Save file not found.");
                return null;
            }

            string json = File.ReadAllText(filePath);
            GameState loadedGameState = JsonConvert.DeserializeObject<GameState>(json);

            if (loadedGameState != null)
            {
                elapsedTime = loadedGameState.ElapsedTime;  // Restore the elapsed time
            }

            return loadedGameState;
        }



        private void InitializeSaveLoadButtons()
            {
                saveButton = new Button
                {
                    Text = "Save Game",
                    Location = new Point(10, 10),
                    Size = new Size(100, 30)
                };
                saveButton.Click += SaveButton_Click;

                loadButton = new Button
                {
                    Text = "Load Game",
                    Location = new Point(120, 10),
                    Size = new Size(100, 30)
                };
                loadButton.Click += LoadButton_Click;

                this.Controls.Add(saveButton);
                this.Controls.Add(loadButton);
            }

            private void SaveButton_Click(object sender, EventArgs e)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "JSON Files|*.json";
                    saveFileDialog.Title = "Save Game";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SaveGame(saveFileDialog.FileName);
                        MessageBox.Show("Game saved successfully!");
                    }
                }
            }

        private async void LoadButton_Click(object sender, EventArgs e)
        {
            StopTimers(); // Stop timers during loading.

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON Files|*.json";
                openFileDialog.Title = "Load Game";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    GameState loadedGameState = await Task.Run(() => LoadGame(openFileDialog.FileName));
                    ApplyLoadedGameState(loadedGameState); // Apply the loaded state.
                    await GenerateTerrainOnce(); // Ensure terrain only generates once.
                }
            }

            StartTimers(); // Resume timers after loading.
        }
        private void ApplyLoadedGameState(GameState state)
        {
            if (state == null) return;

            biomeColors = state.BiomeColors;
            gameTime = state.GameTime;
            elapsedTime = state.ElapsedTime;
            tileSize = state.TileSize;

            DrawMapToBuffer(); // Redraw the map only once after loading.
            Invalidate(); // Refresh the screen.
        }


        private void PauseTimers()
        {
            if (threadTimer != null) threadTimer.Stop();
            if (clockTimer != null) clockTimer.Stop();
        }

        private void ResumeTimers()
        {
            if (threadTimer != null) threadTimer.Start();
            if (clockTimer != null) clockTimer.Start();
        }



        private void RestartStopwatchFromElapsedTime()
        {
            if (stopwatch.IsRunning) stopwatch.Stop();

            // Ensure stopwatch restarts without drift.
            stopwatch = Stopwatch.StartNew();
            previousElapsed = TimeSpan.Zero; // Reset previous elapsed time.
        }




        private void UpdateElapsedTimeLabel()
        {
            threadTimerLabel.Text = $"Timer: {elapsedTime:hh\\:mm\\:ss\\.fff}";
        }


        private async Task GenerateTerrainDynamically()
        {
            if (isGeneratingTerrain) return;

            isLoading = true; // Set the loading flag.

            CancellationToken token;
            lock (cancellationLock)
            {
                if (cancellationTokenSource == null)
                    InitializeCancellationTokenSource();

                token = cancellationTokenSource.Token;
            }

            try
            {
                isGeneratingTerrain = true;

                await Task.Run(async () =>
                {
                    int centerX = mapWidth / 2;
                    int centerY = mapHeight / 2;

                    var tileQueue = new Queue<(int, int)>();
                    tileQueue.Enqueue((centerX, centerY));
                    var generatedTiles = new HashSet<(int, int)> { (centerX, centerY) };

                    while (tileQueue.Count > 0)
                    {
                        if (token.IsCancellationRequested) break;

                        var (x, y) = tileQueue.Dequeue();
                        AddNeighborsToQueue(tileQueue, generatedTiles, x, y);

                        // Batch updates every 200 tiles for smoother performance.
                        if (generatedTiles.Count % 200 == 0 && !IsDisposed && IsHandleCreated)
                        {
                            this.Invoke(() =>
                            {
                                DrawMapToBuffer();
                                Invalidate(new Rectangle(0, 0, mapWidth * tileSize, mapHeight * tileSize));
                            });

                            await Task.Delay(30); // Avoid blocking the thread completely.
                        }
                    }
                }, token);
            }
            finally
            {
                isGeneratingTerrain = false;
                isLoading = false; // Reset the loading flag.
            }
        }





        private void AddNeighborsToQueue(Queue<(int, int)> queue, HashSet<(int, int)> set, int x, int y)
        {
            if (x - 1 >= 0 && set.Add((x - 1, y))) queue.Enqueue((x - 1, y));
            if (x + 1 < mapWidth && set.Add((x + 1, y))) queue.Enqueue((x + 1, y));
            if (y - 1 >= 0 && set.Add((x, y - 1))) queue.Enqueue((x, y - 1));
            if (y + 1 < mapHeight && set.Add((x, y + 1))) queue.Enqueue((x, y + 1));
        }


        public void UpdateGameTime(TimeSpan loadedGameTime)
            {
                gameTime = loadedGameTime;
                UpdateClockLabel();  // Update the clock label with the loaded game time
            }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            int sectionWidth = 25 * tileSize; // Each grid section is 25 tiles wide
            int sectionHeight = 25 * tileSize; // Each grid section is 25 tiles high

            // Determine the clicked grid section
            int clickedSectionX = e.X / sectionWidth; // Mouse X-coordinate divided by grid section width
            int clickedSectionY = e.Y / sectionHeight; // Mouse Y-coordinate divided by grid section height


            // Ensure clicked section is within bounds
            if (clickedSectionX >= 0 && clickedSectionX < sections.GetLength(0) &&
                clickedSectionY >= 0 && clickedSectionY < sections.GetLength(1))
            {
                // Open the detailed map for the clicked grid section
                OpenDetailedMap(clickedSectionX, clickedSectionY);
            }
        }


        private void OpenDetailedMap(int clickedSectionX, int clickedSectionY)
        {
            // Define the size of the detailed map segment to match 25x25
            int segmentWidth = 25;
            int segmentHeight = 25;

            // Get the map segment for the clicked section
            Color[,] mapSegment = GetMapSegment(clickedSectionX, clickedSectionY, segmentWidth, segmentHeight);

            // Initialize and show the DetailedMapForm with the processed segment
            DetailedMapForm detailedMapForm = new DetailedMapForm(mapSegment, tileSize, gameSettings);
            detailedMapForm.Show();  // Show the form without closing the current one
        }


        private Color[,] GetMapSegment(int clickedSectionX, int clickedSectionY, int segmentWidth, int segmentHeight)
        {
            Color[,] mapSegment = new Color[segmentWidth, segmentHeight];

            // Calculate the starting position based on the clicked grid section
            int startX = clickedSectionX * segmentWidth; // Start from the top-left corner of the selected section
            int startY = clickedSectionY * segmentHeight;

            // Adjust if the segment goes out of bounds
            if (startX + segmentWidth > mapWidth)
            {
                startX = mapWidth - segmentWidth;
            }

            if (startY + segmentHeight > mapHeight)
            {
                startY = mapHeight - segmentHeight;
            }

            // Loop through the segment and copy the colors
            for (int x = 0; x < segmentWidth; x++)
            {
                for (int y = 0; y < segmentHeight; y++)
                {
                    if (startX + x < mapWidth && startY + y < mapHeight)
                    {
                        mapSegment[x, y] = biomeColors[startX + x, startY + y];
                    }
                    else
                    {
                        // Assign a default color if out of bounds
                        mapSegment[x, y] = Color.Black;
                    }
                }
            }

            return mapSegment;
        }














        private void InitializeThreadedTimer()
        {
            threadTimerLabel = new Label
            {
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(10, 50),
                AutoSize = true
            };

            Controls.Add(threadTimerLabel);

            // Initialize high-precision stopwatch
            stopwatch = new Stopwatch();
            stopwatch.Start();
            previousElapsed = TimeSpan.Zero;

            // Use System.Timers.Timer for better precision
            threadTimer = new System.Timers.Timer(10);  // Update every 10ms
            threadTimer.Elapsed += ThreadTimer_Tick;
            threadTimer.AutoReset = true;
            threadTimer.Start();
        }

        private void ThreadTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan currentElapsed = stopwatch.Elapsed;
            TimeSpan delta = currentElapsed - previousElapsed;
            previousElapsed = currentElapsed;

            elapsedTime = elapsedTime.Add(delta * vibeMultiplier);
            if (elapsedTime.TotalHours >= 24)
                elapsedTime = elapsedTime.Subtract(TimeSpan.FromHours(24));

            // Throttle UI updates to every 500ms to avoid overload.
            if (previousElapsed.TotalMilliseconds % 10 < 10)
            {
                this.Invoke((Action)(() =>
                {
                    threadTimerLabel.Text = $"Timer: {elapsedTime:hh\\:mm\\:ss\\.fff}";
                }));
            }
        }








        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelTerrainGeneration(); // Safely cancel terrain generation
            DisposeTimers();
            base.OnFormClosing(e);
        }

        private void CancelTerrainGeneration()
        {
            lock (cancellationLock)
            {
                if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel(); // Trigger cancellation.
                    cancellationTokenSource.Dispose(); // Dispose safely.
                    cancellationTokenSource = null; // Reset to avoid reuse.
                }
            }
        }




        private void DisposeTimers()
        {
            // Stop and dispose of the threaded timer
            if (threadTimer != null)
            {
                threadTimer.Stop();
                threadTimer.Dispose();
                threadTimer = null;
            }

            // Stop and dispose of the clock timer
            if (clockTimer != null)
            {
                clockTimer.Stop();
                clockTimer.Dispose();
                clockTimer = null;
            }
        }

        public TimeSpan GetElapsedTime()
        {
            return elapsedTime;  // Return the current elapsed time
        }

        public void ApplyShaderEffect(Color? shaderColor, int alpha)
        {
            if (shaderColor == null)
            {
                // No shader (daytime) – redraw the original map without overlay
                DrawMapToBuffer();
            }
            else
            {
                // Apply the shader effect with the given alpha (transparency)
                using (Graphics g = Graphics.FromImage(bufferedMap))
                {
                    using (Brush overlay = new SolidBrush(Color.FromArgb(alpha, shaderColor.Value.R, shaderColor.Value.G, shaderColor.Value.B)))
                    {
                        g.FillRectangle(overlay, 0, 0, bufferedMap.Width, bufferedMap.Height);
                    }
                }
            }

            // Trigger a redraw of the form
            Invalidate();
        }









    }

}
