using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using UglyLines.Desktop.Logic;
using UglyLines.Desktop.Views;

namespace UglyLines.Desktop.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        public Game Game { get; private set; } = new Game(new FieldSettings
            {
                LeftMargin = 30,
                TopMargin = 80,
                CellSize = 40,
                Width = 10,
                Height = 10

            },
            10, 10);

        public void StartGame()
        {
            Game = new Game(new FieldSettings
                {
                    LeftMargin = 30,
                    TopMargin = 80,
                    CellSize = 40,
                    Width = 10,
                    Height = 10

                },
                10, 10);
        }
        
        public void FieldCellClick(int x, int y)
        {
            if (Game.State == GameState.WaitingForSelection && Game.IsWithinField(x, y))
            {
                try
                {
                    var ball = Game.SelectBall(x, y);
                    (ball as Ellipse).Stroke = new SolidColorBrush(Color.Parse("Black"));
                    (ball as Ellipse).StrokeThickness = 4;
                }
                catch
                {
                }
            }
            
            if (Game.State == GameState.BallSelected)
            {
                if (Game.CanMoveTo(x, y))
                {
                    Game.StartMakingMove(x, y);
                }
            }
        }
    }
}