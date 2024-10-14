using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using SimplexNoise;

namespace SanguigoreRPG
{
    public class WorldGenForm : Form
    {
        private Button generateButton;
        private ProgressBar worldGenProgressBar;
        private Label statusLabel;
        private System.Windows.Forms.Timer worldGenTimer;
        private int progress;

        public Color[,] BiomeColors { get; private set; }
        private int mapWidth;
        private int mapHeight;
        private const int tileSize = 2;
        private Random random;
        private GameSettings gameSettings;
        private float[,] heightMap;
        private float[,] moistureMap;
        private float[,] temperatureMap;



        public WorldGenForm(GameSettings settings)
        {
            gameSettings = settings;
            InitializeComponent();
            random = new Random();
            worldGenTimer = new System.Windows.Forms.Timer();
            worldGenTimer.Tick += WorldGenTimer_Tick;
        }

        private void InitializeComponent()
        {
            worldGenProgressBar = new ProgressBar
            {
                Size = new Size(500, 30),
                Minimum = 0,
                Maximum = 100
            };

            statusLabel = new Label
            {
                Text = "World Generation Status",
                Font = new Font("Arial", 14, FontStyle.Bold),
                Size = new Size(500, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            generateButton = new Button
            {
                Text = "Generate World",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Size = new Size(200, 50),
            };
            generateButton.Click += GenerateButton_Click;

            this.Controls.Add(worldGenProgressBar);
            this.Controls.Add(statusLabel);
            this.Controls.Add(generateButton);
            CenterControls();

            this.Resize += (s, e) => CenterControls();
            this.Text = "Sanguigore - World Generation";
            ApplyWindowSettings();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

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
                this.ClientSize = new Size(gameSettings.TerrainWindowWidth, gameSettings.TerrainWindowHeight);
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
        }

        private void CenterControls()
        {
            statusLabel.Location = new Point((this.ClientSize.Width - statusLabel.Width) / 2, 100);
            worldGenProgressBar.Location = new Point((this.ClientSize.Width - worldGenProgressBar.Width) / 2, 150);
            generateButton.Location = new Point((this.ClientSize.Width - generateButton.Width) / 2, 250);
        }

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            if (CheckForSavedGame())
                return;

            generateButton.Enabled = false;
            InitializeGenerationProgress();
            CalculateMapSize();

            worldGenTimer.Start();
        }

        private bool CheckForSavedGame()
        {
            string saveFilePath = "saved_game.json";
            if (File.Exists(saveFilePath))
            {
                GameState loadedGameState = LoadGame(saveFilePath);
                if (loadedGameState != null)
                {
                    LoadSavedWorld(loadedGameState);
                    return true;
                }
                else
                {
                    MessageBox.Show("Error loading saved game. Generating a new world instead.");
                }
            }
            return false;
        }


        private void LoadSavedWorld(GameState loadedGameState)
        {
            DisplayForm displayForm = new DisplayForm(loadedGameState.BiomeColors, loadedGameState.TileSize, loadedGameState);
            displayForm.FormClosed += (s, args) => this.Close();
            displayForm.Show();
            this.Hide();
        }

        private void InitializeGenerationProgress()
        {
            progress = 0;
            worldGenProgressBar.Value = 0;
            statusLabel.Text = "Generating world...";
        }

        private void CalculateMapSize()
{
    mapWidth = (int)(this.ClientSize.Width / tileSize);
    mapHeight = (int)(this.ClientSize.Height / tileSize);
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

        private Color[,] GenerateWorld()
        {
            Color[,] worldColors = new Color[mapWidth, mapHeight];

            float[,] heightMap = GenerateHeightMap();
            float[,] moistureMap = GenerateMoistureMap();
            float[,] temperatureMap = GenerateTemperatureMap(); // New temperature map

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float height = heightMap[x, y];
                    float moisture = moistureMap[x, y];
                    float temperature = temperatureMap[x, y];

                    // Biome generation based on height, moisture, and temperature
                    worldColors[x, y] = GetBiomeColor(height, moisture, temperature);
                }
            }

            // Now, add villages
            AddVillages(worldColors, heightMap);

            return worldColors;

        }

        private Color GetBiomeColor(float height, float moisture, float temperature)
        {
            // Define transition range for smoother biome blending
            float transitionRange = 0.05f;

            // Adjustable blend strength for more pronounced or subtle transitions
            float blendStrength = 10.0f; // Increase this value to make blending stronger
            //30, 70, 30
            //80, 120, 60
            //
            // Define colors for different regions
            Color deepWaterColor = Color.FromArgb(10, 30, 50);   // Deep water
            Color shallowWaterColor = Color.FromArgb(20, 50, 70); // Shallow water
            Color wetBeachColor = Color.FromArgb(194, 178, 128);  // Wet beach
            Color dryBeachColor = Color.FromArgb(210, 185, 140);  // Dry beach
            Color dryGrassColor = Color.FromArgb(30, 70, 30);    // Dry grasslands
            Color lushGrassColor = Color.FromArgb(50, 100, 30);   // Lush grasslands
            Color forestColor = Color.FromArgb(80, 120, 60);       // Dense forest green
            Color rockyMountainLow = Color.FromArgb(100, 100, 100);  // Low-altitude rocky mountains
            Color rockyMountainHigh = Color.FromArgb(160, 160, 160); // Higher rocky mountains
            Color snowyMountainColor = Color.FromArgb(240, 240, 240); // Snow-capped peaks

            // Deep Water to Shallow Water Gradient
            if (height < 0.35f)
            {
                // Interpolate between deep and shallow water based on height
                float waterFactor = (height - 0.25f) / (0.35f - 0.25f);
                return InterpolateColor(deepWaterColor, shallowWaterColor, Math.Clamp(waterFactor, 0, 1));
            }

            // Beach biome: Transition between water and land
            if (height >= 0.35f && height < 0.38f)
            {
                return wetBeachColor; // Wet beach closer to water
            }
            else if (height >= 0.38f && height < 0.40f)
            {
                return dryBeachColor; // Dry beach farther from water
            }


            // Green biomes (grasslands, forests, etc.) with smooth transitions
            if (height < 0.70f)  // Lower the threshold to make the mountains start earlier
            {
                if (height < 0.5f + transitionRange)  // Dry grasslands
                {
                    float blendFactor = (height - (0.5f - transitionRange)) / (2 * transitionRange);
                    return InterpolateColor(dryGrassColor, lushGrassColor, Math.Clamp(blendFactor * blendStrength, 0, 1));
                }
                else if (height < 0.60f + transitionRange)  // Lush grasslands to forest
                {
                    float blendFactor = (height - (0.60f - transitionRange)) / (2 * transitionRange);
                    return InterpolateColor(lushGrassColor, forestColor, Math.Clamp(blendFactor * blendStrength, 0, 1));
                }
                else if (height < 0.70f + transitionRange)  // Forest to hills
                {
                    return forestColor;
                }
            }

            // Increase range for rocky mountains
            if (height >= 0.70f && height < 0.85f)  // Start grey areas earlier
            {
                // Blend between green (forest) and grey (rocky) to simulate trees growing on rocky slopes
                float mountainTransitionFactor = (height - 0.70f) / (0.85f - 0.70f);
                return InterpolateColor(forestColor, rockyMountainLow, Math.Clamp(mountainTransitionFactor * blendStrength, 0, 1));
            }

            // Rocky Mountains (extend the range)
            if (height >= 0.85f && height < 0.95f)
            {
                // Blend between lower rocky mountains and higher rocky mountains
                float mountainFactor = (height - 0.85f) / (0.95f - 0.85f);
                return InterpolateColor(rockyMountainLow, rockyMountainHigh, Math.Clamp(mountainFactor * blendStrength, 0, 1));
            }

            // Snowy peaks (after 0.95f to the top)
            if (height >= 0.95f)
            {
                float snowFactor = (height - 0.95f) / (1.0f - 0.95f);
                return InterpolateColor(rockyMountainHigh, snowyMountainColor, Math.Clamp(snowFactor * blendStrength, 0, 1));
            }

            // Default for high peaks
            return snowyMountainColor;
        }










        // Method to get forest color based on moisture
        private Color GetForestColor(float moisture)
{
    if (moisture > 0.65f)
        return Color.FromArgb(30, 70, 30); // Dense forest green
    else if (moisture < 0.4f)
        return Color.FromArgb(40, 70, 30); // Drier forest
    else
        return Color.FromArgb(60, 90, 40); // Mixed forest
}

        

        private float[,] GenerateTemperatureMap()
{
    float[,] temperatureMap = new float[mapWidth, mapHeight];

    // Base frequencies and amplitudes for multiple layers of noise
    float[] frequencies = { 0.01f, 0.02f, 0.05f };
    float[] amplitudes = { 0.5f, 0.3f, 0.2f };

    for (int x = 0; x < mapWidth; x++)
    {
        for (int y = 0; y < mapHeight; y++)
        {
            float temperatureValue = 0;
            for (int i = 0; i < frequencies.Length; i++)
            {
                // Accumulate multiple noise layers
                temperatureValue += Noise.CalcPixel2D(x, y, frequencies[i]) / 255.0f * amplitudes[i];
            }
            // Clamp the final value to ensure it stays within [0,1] range
            temperatureMap[x, y] = Math.Clamp(temperatureValue, 0, 1);
        }
    }

    return temperatureMap;
}


        private void AddVillages(Color[,] worldColors, float[,] heightMap)
        {
            int numberOfVillages = random.Next(15, 25);
            List<(int, int)> villageCenters = new List<(int, int)>();

            for (int i = 0; i < numberOfVillages; i++)
            {
                int villageX = random.Next(1, mapWidth - 1);
                int villageY = random.Next(1, mapHeight - 1);

                if (IsSuitableForVillage(heightMap, villageX, villageY))
                {
                    CreateVillage(worldColors, villageX, villageY);
                    villageCenters.Add((villageX, villageY));
                }
            }
        }

        private bool IsSuitableForVillage(float[,] heightMap, int villageX, int villageY)
        {
            return heightMap[villageX, villageY] >= 0.35f && heightMap[villageX, villageY] < 0.6f; // Plains or lower hills
        }

        private void CreateVillage(Color[,] worldColors, int villageX, int villageY)
        {
            // Place a village in a 3x3 grid
            for (int x = villageX - 1; x <= villageX + 1; x++)
            {
                for (int y = villageY - 1; y <= villageY + 1; y++)
                {
                    // Ensure we stay within bounds
                    if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                    {
                        worldColors[x, y] = Color.FromArgb(180, 100, 50); // Light brown for village
                    }
                }
            }

           
        }

        

        // Height map generation method with improved features
        private float[,] GenerateHeightMap()
        {
            float[,] heightMap = new float[mapWidth, mapHeight];

            // Base frequencies and amplitudes for multiple layers of noise
            float[] frequencies = { 0.005f, 0.01f, 0.02f, 0.04f };
            float[] amplitudes = { 0.6f, 0.3f, 0.2f, 0.1f };

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float heightValue = 0;
                    for (int i = 0; i < frequencies.Length; i++)
                    {
                        // Accumulate multiple noise layers
                        heightValue += Noise.CalcPixel2D(x, y, frequencies[i]) / 255.0f * amplitudes[i];
                    }
                    // Clamp the final value to ensure it stays within [0,1] range
                    heightMap[x, y] = Math.Clamp(heightValue, 0, 1);
                }
            }

            return heightMap;
        }


        // Moisture map generation with smoother transitions
        private float[,] GenerateMoistureMap()
        {
            float[,] moistureMap = new float[mapWidth, mapHeight];

            // Base frequencies and amplitudes for multiple layers of noise
            float[] frequencies = { 0.02f, 0.04f, 0.08f };
            float[] amplitudes = { 0.5f, 0.3f, 0.2f };

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float moistureValue = 0;
                    for (int i = 0; i < frequencies.Length; i++)
                    {
                        // Accumulate multiple noise layers
                        moistureValue += Noise.CalcPixel2D(x, y, frequencies[i]) / 255.0f * amplitudes[i];
                    }
                    // Clamp the final value to ensure it stays within [0,1] range
                    moistureMap[x, y] = Math.Clamp(moistureValue, 0, 1);
                }
            }

            return moistureMap;
        }
        
        private Color InterpolateColor(Color color1, Color color2, float factor)
        {
            int r = (int)(color1.R + (color2.R - color1.R) * factor);
            int g = (int)(color1.G + (color2.G - color1.G) * factor);
            int b = (int)(color1.B + (color2.B - color1.B) * factor);
            return Color.FromArgb(r, g, b);
        }


        // Timer tick event to handle the generation progress
        private void WorldGenTimer_Tick(object sender, EventArgs e)
        {
            progress += 5;  // Increase progress by 5%

            if (progress <= 100)
            {
                worldGenProgressBar.Value = progress;
            }
            else
            {
                // Stop the timer when generation is complete
                worldGenTimer.Stop();
                statusLabel.Text = "World generation complete!";

                // Generate the world
                float[,] heightMap = GenerateHeightMap();
                float[,] moistureMap = GenerateMoistureMap();
                BiomeColors = GenerateWorld();

                // Create MapData instance
                MapData mapData = new MapData(heightMap, moistureMap, BiomeColors);

                // Now open the DisplayForm and pass mapData if needed
                DisplayForm displayForm = new DisplayForm(mapData.BiomeColors, tileSize); // Pass the tile size
                displayForm.FormClosed += (s, args) => this.Close();  // Close WorldGenForm after DisplayForm is closed
                displayForm.Show();

                // Close this form (WorldGenForm)
                this.Hide();
            }
        }

       
    }
}

