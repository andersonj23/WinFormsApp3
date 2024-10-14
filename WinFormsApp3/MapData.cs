using System;
using System.Drawing;
using Newtonsoft.Json;
using System.IO;

namespace SanguigoreRPG
{
    // Class to hold all relevant map data
    public class MapData
    {
        public float[,] HeightMap { get; private set; }
        public float[,] MoistureMap { get; private set; }
        public string[,] BiomeMap { get; private set; }
        public Color[,] BiomeColors { get; private set; }

        // Additional properties for segment data
        public int SegmentX { get; private set; }
        public int SegmentY { get; private set; }
        public int SegmentWidth { get; private set; }
        public int SegmentHeight { get; private set; }

        // Constructor
        public MapData(float[,] heightMap, float[,] moistureMap, Color[,] biomeColors,
                       int segmentX = 0, int segmentY = 0, int segmentWidth = 0, int segmentHeight = 0)
        {
            HeightMap = heightMap;
            MoistureMap = moistureMap;
            BiomeColors = biomeColors;
            BiomeMap = CreateBiomeMap(heightMap, moistureMap); // Generate biome map from height and moisture
            SegmentX = segmentX;
            SegmentY = segmentY;
            SegmentWidth = segmentWidth;
            SegmentHeight = segmentHeight;
        }

        // Create a biome map based on height and moisture maps
        private string[,] CreateBiomeMap(float[,] heightMap, float[,] moistureMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            string[,] biomeMap = new string[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float heightValue = heightMap[x, y];
                    float moistureValue = moistureMap[x, y];

                    // Determine biome based on height and moisture
                    if (heightValue < 0.1f) // Water
                    {
                        biomeMap[x, y] = "Deep Water";
                    }
                    else if (heightValue < 0.35f) // Plains
                    {
                        biomeMap[x, y] = moistureValue > 0.75f ? "Lush Plains" : "Dry Plains";
                    }
                    else if (heightValue < 0.6f) // Forest
                    {
                        biomeMap[x, y] = moistureValue > 0.65f ? "Dense Forest" : "Drier Forest";
                    }
                    else if (heightValue < 0.75f) // Hills
                    {
                        biomeMap[x, y] = "Hills";
                    }
                    else // Mountains
                    {
                        biomeMap[x, y] = "Mountains";
                    }
                }
            }

            return biomeMap;
        }

        // Capture segment data for later refinement
        public void SetSegmentData(int x, int y, int width, int height)
        {
            SegmentX = x;
            SegmentY = y;
            SegmentWidth = width;
            SegmentHeight = height;
        }

        // Serialize the MapData to JSON for saving
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        // Deserialize from JSON to create MapData
        public static MapData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MapData>(json);
        }

        // Save MapData to a specified file
        public void SaveToFile(string filePath)
        {
            string json = ToJson();
            File.WriteAllText(filePath, json);
        }

        // Load MapData from a specified file
        public static MapData LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Save file not found.", filePath);
            }

            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        // Display map data information
        public void DisplayMapData()
        {
            Console.WriteLine("Map Data Overview:");
            Console.WriteLine($"Segment X: {SegmentX}");
            Console.WriteLine($"Segment Y: {SegmentY}");
            Console.WriteLine($"Segment Width: {SegmentWidth}");
            Console.WriteLine($"Segment Height: {SegmentHeight}");
            Console.WriteLine("\nHeight Map:");
            DisplayMap(HeightMap);
            Console.WriteLine("\nMoisture Map:");
            DisplayMap(MoistureMap);
            Console.WriteLine("\nBiome Map:");
            DisplayBiomeMap(BiomeMap);
        }

        // Helper method to display a 2D float array
        private void DisplayMap(float[,] map)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Console.Write($"{map[x, y]:F2}\t");
                }
                Console.WriteLine();
            }
        }

        // Helper method to display the biome map
        private void DisplayBiomeMap(string[,] biomeMap)
        {
            int width = biomeMap.GetLength(0);
            int height = biomeMap.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Console.Write($"{biomeMap[x, y]}\t");
                }
                Console.WriteLine();
            }
        }
    }
}
