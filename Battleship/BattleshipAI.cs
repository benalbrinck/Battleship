using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Battleship
{
	public class BattleshipAI
	{
		// Gampleay variables
		private int[] lastAttemptCoord;
		public int[] lastHitCoord;  // Reset when the ship is sunk
		private int[] shipStartCoord;

		private bool isVertical = true;
		private bool isForward = true;

		public void PlaceShips(ref int[,] grid)
		{
			// Randomly choose whether or not to load a grid
			var random = new Random();

			if (random.Next(2) == 0)
			{
				var loadedGrid = LoadGrid();

				if (loadedGrid != null)
				{
					grid = loadedGrid;
					return;
				}
			}

			// If it decided not to load a grid or if there were no grids to load, create a random grid
			int shipID = 1;

			while (shipID != 6)
			{
				int coordX = random.Next(10);
				int coordY = random.Next(10);

				bool isHorizontal = Convert.ToBoolean(random.Next(0, 2));
				int shipLength = shipID >= 3 ? shipID : shipID + 1;

				bool canPlace = true;

				for (int i = 0; i < shipLength; i++)
				{
					// If the ship is horizontal, check to the right. If vertical, check upwards
					int checkX = isHorizontal ? coordX + i : coordX;
					int checkY = isHorizontal ? coordY : coordY + i;

					if (checkX == grid.GetLength(0) || checkY == grid.GetLength(1))  // If it is going off the board, try a different square
					{
						canPlace = false;
						break;
					}

					if (grid[checkX, checkY] != 0)  // If square is not empty, try a different square
					{
						canPlace = false;
						break;
					}
				}

				if (!canPlace)
					continue;

				// If the placement of the ship is valid, place the ship and increment shipID
				for (int i = 0; i < shipLength; i++)
				{
					int shipX = isHorizontal ? coordX + i : coordX;
					int shipY = isHorizontal ? coordY : coordY + i;

					grid[shipX, shipY] = shipID;
				}

				shipID += 1;
			}
		}

		public void SaveGrid(int[,] grid)
		{
			// Flatten array then convert to byte array
			int[] flattenedGrid = grid.Cast<int>().ToArray();  // https://stackoverflow.com/questions/641499/convert-2-dimensional-array

			byte[] byteGrid = new byte[flattenedGrid.Length * sizeof(int)];  // https://stackoverflow.com/questions/5896680/converting-an-int-to-byte-in-c-sharp
			Buffer.BlockCopy(flattenedGrid, 0, byteGrid, 0, byteGrid.Length);

			// Hash grid for file name
			var md5 = new MD5CryptoServiceProvider();  // https://stackoverflow.com/questions/4181198/how-to-hash-a-password
			var md5Data = md5.ComputeHash(byteGrid);

			var fileName = BitConverter.ToString(md5Data).Replace("-", string.Empty);  // https://stackoverflow.com/questions/760166/how-to-convert-an-md5-hash-to-a-string-and-use-it-as-a-file-name

			// Save the byte array to file
			using (var writer = new BinaryWriter(File.Open($"../../Grids/{fileName}.gr", FileMode.Create)))  // https://docs.microsoft.com/en-us/dotnet/api/system.io.binarywriter?view=net-5.0
			{
				writer.Write(byteGrid);
			}
		}

		private int[,] LoadGrid()
		{
			// Open directory and pick random file (if there are no files, return null)
			var fileNames = Directory.GetFiles("../../Grids/");

			if (fileNames.Length == 0)
				return null;

			var random = new Random();
			var fileName = fileNames[random.Next(fileNames.Length)];

			// Read from file and convert to grid
			byte[] byteGrid = new byte[100 * sizeof(int)];
			using (var reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
			{
				reader.Read(byteGrid, 0, byteGrid.Length);
			}
			
			var flattenedGrid = new int[100];
			Buffer.BlockCopy(byteGrid, 0, flattenedGrid, 0, byteGrid.Length);

			var grid = new int[10, 10];
			for (int x = 0; x < 10; x++)
			{
				for (int y = 0; y < 10; y++)
				{
					grid[x, y] = flattenedGrid[(x * 10) + y];
				}
			}

			// Delete grid file (if this grid loses to the player, then it won't be used again)
			File.Delete(fileName);

			return grid;
		}

		public int[] Attack(int[,] grid)
		{
			int[] coords = new int[2] { 0, 0 };

			if (lastHitCoord == null)
			{
				// If it hasn't hit a ship or has just sunk a ship, randomly attack
				isVertical = true;
				isForward = true;
				Random random = new Random();

				while (true)
				{
					coords[0] = random.Next(10);
					coords[1] = random.Next(10);

					if (grid[coords[0], coords[1]] == 0)
						break;
				}

				lastAttemptCoord = new int[2];
				shipStartCoord = new int[2];

				coords.CopyTo(lastAttemptCoord, 0);
				coords.CopyTo(shipStartCoord, 0);
				return coords;
			}
			
			if (isVertical)
			{
				if (isForward)
				{
					if (Enumerable.SequenceEqual(lastAttemptCoord, lastHitCoord))  // https://stackoverflow.com/questions/3232744/easiest-way-to-compare-arrays-in-c-sharp/26798136
					{
						// Try to hit upwards. If the space is already hit/miss, then try downwards
						lastHitCoord.CopyTo(coords, 0);  // https://stackoverflow.com/questions/46070530/copy-arrays-to-array
						coords[1] -= 1;

						if (coords[1] >= 0 && grid[coords[0], coords[1]] == 0)
						{
							coords.CopyTo(lastAttemptCoord, 0);
							return coords;
						}
					}

					isForward = false;
					shipStartCoord.CopyTo(lastHitCoord, 0);
				}

				// Try to hit downwards. If not, switch to horizontal
				lastHitCoord.CopyTo(coords, 0);
				coords[1] += 1;

				if (coords[1] < 10 && grid[coords[0], coords[1]] == 0)
				{
					coords.CopyTo(lastAttemptCoord, 0);
					return coords;
				}

				isVertical = false;
				isForward = true;

				shipStartCoord.CopyTo(lastAttemptCoord, 0);
				shipStartCoord.CopyTo(lastHitCoord, 0);
			}

			if (isForward)
			{
				if (Enumerable.SequenceEqual(lastAttemptCoord, lastHitCoord))
				{
					// Try to hit to the left. If the space is already hit/miss, then try to the right
					lastHitCoord.CopyTo(coords, 0);
					coords[0] -= 1;

					if (coords[0] >= 0 && grid[coords[0], coords[1]] == 0)
					{
						coords.CopyTo(lastAttemptCoord, 0);
						return coords;
					}
				}

				isForward = false;
				shipStartCoord.CopyTo(lastHitCoord, 0);
			}

			// Try to hit to the right. If not, go back to random hits
			lastHitCoord.CopyTo(coords, 0);
			coords[0] += 1;

			if (coords[0] < 10 && grid[coords[0], coords[1]] == 0)
			{
				coords.CopyTo(lastAttemptCoord, 0);
				return coords;
			}

			lastHitCoord = null;
			return Attack(grid);
		}
	}
}
