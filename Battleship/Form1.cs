using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Battleship
{
	public partial class Form1 : Form
	{
		// Grid variables
		private int[,] playerGrid;
		private int[,] playerHits;
		private PictureBox[,] playerGridImages;

		private int[,] aiGrid;
		private int[,] aiHits;
		private PictureBox[,] aiGridImages;

		private int[] playerGridStart = new int[] { 38, 48 };  // The coordinates of where the PictureBoxes should start being placed
		private int[] aiGridStart = new int[] { 592, 48 };
		private int gridDelta = 36;  // How far away the PictureBoxes should be from each other

		private Image[,] imageGrid;
		private string letters = "ABCDEFGHIJ";

		// Place phase variables
		private bool isHorizontal = true;
		private bool isPlacePhase = true;
		private int playerShipID = 1;

		// Gameplay variables
		private BattleshipAI ai;
		private bool isGameOver = false;

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// Initialize AI and reset game
			ai = new BattleshipAI();
			ResetGame();
		}

		private void CreateGrid(string prefix, int[] gridStart, bool isPlayer, EventHandler hoverMethod, EventHandler clickMethod)
		{
			for(int x = 0; x < 10; x++)
			{
				for(int y = 0; y < 10; y++)
				{
					PictureBox square = new PictureBox();
					square.Name = prefix + letters[x] + y.ToString();

					square.Image = Properties.Resources.WaterTile;
					square.SizeMode = PictureBoxSizeMode.StretchImage;

					square.MouseHover += hoverMethod;
					square.Click += clickMethod;

					square.Size = new Size(30, 30);
					square.Location = new Point(gridStart[0] + (x * gridDelta), gridStart[1] + (y * gridDelta));

					Controls.Add(square);

					if (isPlayer)
						playerGridImages[x, y] = square;
					else
						aiGridImages[x, y] = square;
				}
			}
		}

		private void PlayerSquareClick(object sender, EventArgs e)
		{
			// To place ships, not active while game is running
			if (isPlacePhase)
			{
				DisplayShip((sender as PictureBox).Name, true);  // Player squares are named P[Column][Row]
			}
		}

		private void PlayerSquareHover(object sender, EventArgs e)
		{
			if (isPlacePhase)
			{
				DisplayShip((sender as PictureBox).Name);  // Player squares are named P[Column][Row]
			}
		}

		private void ResetGridImages(PictureBox[,] pictureBoxes, Image[,] images)
		{
			for (int x = 0; x < 10; x++)
			{
				for (int y = 0; y < 10; y++)
				{
					pictureBoxes[x, y].Image = images[x, y] ?? Properties.Resources.WaterTile;
				}
			}
		}

		private void DisplayShip(string squareName, bool applyShip = false)
		{
			// Reset everything
			ResetGridImages(playerGridImages, imageGrid);

			// Check if ship can be placed
			int squareX = letters.IndexOf(squareName[1]);
			int squareY = int.Parse(squareName[2].ToString());
			int shipLength = playerShipID >= 3 ? playerShipID : playerShipID + 1;

			for (int i = 0; i < shipLength; i++)
			{
				// If the ship is horizontal, move to the right. If vertical, move upwards
				int checkX = isHorizontal ? squareX + i : squareX;
				int checkY = isHorizontal ? squareY : squareY + i;

				if (checkX == playerGrid.GetLength(0) || checkY == playerGrid.GetLength(1))  // If it is going off the board, do not display
					return;

				if (playerGrid[checkX, checkY] != 0)  // If square is not empty, do not display
					return;
			}

			// If ship can be placed, place ship
			for (int i = 0; i < shipLength; i++)
			{
				// If the ship is horizontal, move to the right. If vertical, move upwards
				int shipX = isHorizontal ? squareX + i : squareX;
				int shipY = isHorizontal ? squareY : squareY + i;

				if (i == 0)
					playerGridImages[shipX, shipY].Image = isHorizontal ? Properties.Resources.ShipHorizLeft : Properties.Resources.ShipVertTop;
				else if (i == shipLength - 1)
					playerGridImages[shipX, shipY].Image = isHorizontal ? Properties.Resources.ShipHorizRight : Properties.Resources.ShipVertBottom;
				else
					playerGridImages[shipX, shipY].Image = isHorizontal ? Properties.Resources.ShipHorizMiddle : Properties.Resources.ShipVertMiddle;

				if (applyShip)
				{
					playerGrid[shipX, shipY] = playerShipID;
					imageGrid[shipX, shipY] = playerGridImages[shipX, shipY].Image;
				}
			}

			if (applyShip)
			{
				playerShipID += 1;

				if (playerShipID == 6)
					ChangeToGameplayPhase();
			}
		}

		private void ToggleHoriztonalVertical(object sender, EventArgs e)
		{
			isHorizontal = !isHorizontal;
		}

		private void ChangeToGameplayPhase()
		{
			isPlacePhase = false;
			imageGrid = new Image[10, 10];
			PhaseLabel.Text = "Gameplay Phase";
			ToggleButton.Enabled = false;
		}

		private void AISquareClick(object sender, EventArgs e)
		{
			if (!isPlacePhase && !isGameOver)
			{
				ResetGridImages(aiGridImages, imageGrid);

				// Find if the square has already been hit
				string squareName = (sender as PictureBox).Name;
				int squareX = letters.IndexOf(squareName[1]);
				int squareY = int.Parse(squareName[2].ToString());

				if (aiHits[squareX, squareY] != 0)
					return;

				// Hit the AI square
				aiHits[squareX, squareY] = aiGrid[squareX, squareY] != 0 ? -1 : 1;
				aiGridImages[squareX, squareY].Image = aiGrid[squareX, squareY] != 0 ? Properties.Resources.ShipHit : Properties.Resources.Miss;
				imageGrid[squareX, squareY] = aiGridImages[squareX, squareY].Image;

				if (aiGrid[squareX, squareY] != 0)
				{
					// Check if ship is sunk
					bool isSunk = true;
					for (int x = 0; x < 10; x++)
					{
						for (int y = 0; y < 10; y++)
						{
							if (aiGrid[x, y] == aiGrid[squareX, squareY] && aiHits[x, y] == 0)
								isSunk = false;
						}
					}

					if (isSunk)
						PlayerStatus.Text = "Player: Sunk";
					else
						PlayerStatus.Text = "Player: Hit";
				}
				else
				{
					PlayerStatus.Text = "Player: Miss";
				}

				// Check if player won
				CheckWin(aiGrid, aiHits, "Player Won", playerGrid);

				if (isGameOver)
					return;

				// Hit the player square
				int[] attackCoords = ai.Attack(playerHits);
				playerHits[attackCoords[0], attackCoords[1]] = playerGrid[attackCoords[0], attackCoords[1]] != 0 ? -1 : 1;
				playerGridImages[attackCoords[0], attackCoords[1]].Image = playerGrid[attackCoords[0], attackCoords[1]] != 0 ? Properties.Resources.ShipHit : Properties.Resources.Miss;

				if (playerGrid[attackCoords[0], attackCoords[1]] != 0)
				{
					// Check if ship is sunk
					bool isSunk = true;
					for (int x = 0; x < 10; x++)
					{
						for (int y = 0; y < 10; y++)
						{
							if (playerGrid[x, y] == playerGrid[attackCoords[0], attackCoords[1]] && playerHits[x, y] == 0)
								isSunk = false;
						}
					}

					if (isSunk)
					{
						AIStatus.Text = "AI: Sunk";
						ai.lastHitCoord = null;
					}
					else
					{
						AIStatus.Text = "AI: Hit";
						ai.lastHitCoord = attackCoords;
					}
				}
				else
				{
					AIStatus.Text = "AI: Miss";
				}

				// Check if AI won
				CheckWin(playerGrid, playerHits, "AI Won", aiGrid);
			}
		}

		private void AISquareHover(object sender, EventArgs e)
		{
			if (isGameOver || isPlacePhase)
				return;

			// Reset grid and add target to the square
			ResetGridImages(aiGridImages, imageGrid);

			string squareName = (sender as PictureBox).Name;
			int squareX = letters.IndexOf(squareName[1]);
			int squareY = int.Parse(squareName[2].ToString());

			if (aiHits[squareX, squareY] == 0)
				aiGridImages[squareX, squareY].Image = Properties.Resources.Target;
		}

		private void CheckWin(int[,] grid, int[,] hits, string winText, int[,] opposingGrid)
		{
			bool isWin = true;
			for (int x = 0; x < 10; x++)
			{
				for (int y = 0; y < 10; y++)
				{
					if (grid[x, y] != 0)
					{
						// If there is a ship at the location, check if it has been hit. If not, then they haven't won.
						if (hits[x, y] == 0)
							isWin = false;
					}
				}
			}
			
			if (isWin)
			{
				isGameOver = true;
				PhaseLabel.Text = winText;
				ResetButton.Visible = true;

				// AI will save the winning grid
				ai.SaveGrid(opposingGrid);
			}
		}

		private void ResetClick(object sender, EventArgs e)
		{
			ResetButton.Visible = false;
			ResetGame();
		}

		private void ResetGame()
		{
			// Delete PictureBoxes if they exist
			if (playerGridImages != null)
			{
				for (int x = 0; x < 10; x++)
				{
					for (int y = 0; y < 10; y++)
					{
						if (playerGridImages[x, y] != null)
							Controls.Remove(playerGridImages[x, y]);
					}
				}
			}

			if (aiGridImages != null)
			{
				for (int x = 0; x < 10; x++)
				{
					for (int y = 0; y < 10; y++)
					{
						if (aiGridImages[x, y] != null)
							Controls.Remove(aiGridImages[x, y]);
					}
				}
			}

			// Reset variables
			playerGrid = new int[10, 10];
			playerHits = new int[10, 10];
			playerGridImages = new PictureBox[10, 10];

			aiGrid = new int[10, 10];
			aiHits = new int[10, 10];
			aiGridImages = new PictureBox[10, 10];

			imageGrid = new Image[10, 10];

			isHorizontal = true;
			isPlacePhase = true;
			isGameOver = false;
			playerShipID = 1;

			// Reset labels
			PhaseLabel.Text = "Place Phase";
			PlayerStatus.Text = "";
			AIStatus.Text = "";

			// Create game grids
			ai.PlaceShips(ref aiGrid);
			CreateGrid("P", playerGridStart, true, PlayerSquareHover, PlayerSquareClick);
			CreateGrid("A", aiGridStart, false, AISquareHover, AISquareClick);
		}
	}
}
