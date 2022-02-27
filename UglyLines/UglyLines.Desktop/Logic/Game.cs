using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using UglyLines.Desktop.Views;

namespace UglyLines.Desktop.Logic;

public class Game
{
    public Game(FieldSettings fieldSettings, int fieldWidth, int fieldHeight)
    {
        FieldSettings = fieldSettings;
        FieldWidth = fieldWidth;
        FieldHeight = fieldHeight;
        
        _field = new Shape? [FieldWidth, FieldHeight];
    }
    
    public int FieldWidth { get; }
    public int FieldHeight { get; }

    public Shape?[,] Field => _field;

    public GameState State { get; private set; } = GameState.WaitingForSelection;
    
    public bool IsWithinField(int x, int y) => x >= 0 && x < FieldWidth && y >= 0 && y < FieldHeight;

    public bool CanMoveTo(int x, int y)
    {
        if (!IsWithinField(x, y))
        {
            return false;
        }

        if (_field[x,y] != null)
        {
            return false;
        }
        
        //todo check if path is possible
        return true;
    }
    
    
    
    private Shape?[,] _field;
    public FieldSettings FieldSettings { get; }


    public Shape? SelectedBall =>
        SelectedBallCell != null ? _field[SelectedBallCell.Value.x, SelectedBallCell.Value.y] : null;


    private readonly List<(int x, int y)> _ballsToClear = new List<(int x, int y)>();
    public IReadOnlyList<(int x, int y)> BallsToClear => _ballsToClear;
    
    public (int x, int y)? SelectedBallCell { get; private set; }

    public Shape? MovingBall { get; private set; }
    public (int x, int y)? MovingBallDestination { get; private set; }

    private readonly List<(int x, int y, Shape ball)> _ballsToShoot = new List<(int x, int y, Shape ball)>();
    public IReadOnlyList<(int x, int y, Shape ball)> BallsToShoot => _ballsToShoot;
    
    
    public Shape SelectBall(int x, int y)
    {
        if (!IsWithinField(x, y))
        {
            throw new ArgumentOutOfRangeException();
        }

        var ball = _field[x, y]; 
         
        if (ball == null)
        {
            throw new ArgumentException();
        }
        
        SelectedBallCell = (x, y);
        State = GameState.BallSelected;
        return ball;
    }

    public bool StartMakingMove(int x, int y)
    {
        if (State != GameState.BallSelected || SelectedBall == null || !SelectedBallCell.HasValue)
        {
            return false;
        }

        if (!CanMoveTo(x, y))
        {
            return false;
        }

        MovingBall = SelectedBall;
        MovingBallDestination = (x, y);
        
        _field[SelectedBallCell.Value.x, SelectedBallCell.Value.y] = null;

        State = GameState.BallMoving;
        return true;
    }

    public bool EndMakingMove()
    {
        if (State != GameState.BallMoving || MovingBall == null || !MovingBallDestination.HasValue)
        {
            return false;
        }

        var ballDest = MovingBallDestination.Value;
        
        _field[ballDest.x, ballDest.y] = MovingBall;
        MovingBall = null;
        MovingBallDestination = null;

        var ballsToClear = CheckBallsToClear(ballDest.x, ballDest.y, _field[ballDest.x, ballDest.y]);
        _ballsToClear.Clear();
        _ballsToClear.AddRange(ballsToClear);
        
        State = GameState.ClearLines;
        return true;
    }

    public bool ClearLinesAndShootNewBalls(IEnumerable<Shape> newBalls)
    {
        foreach (var ballXY in BallsToClear)
        {
            _field[ballXY.x, ballXY.y] = null;
        }
        
        _ballsToClear.Clear();

        var emptyCells = new List<(int x, int y)>();

        for (var x = 0; x < FieldSettings.Width; x++)
        for (var y = 0; y < FieldSettings.Height; y++)
        {
            if (_field[x, y] == null)
            {
                emptyCells.Add((x, y));
            }
        }

        if (!emptyCells.Any())
        {
            return false;
        }

        _ballsToShoot.Clear();
        
        var rnd = new Random();
        
        foreach (var newBall in newBalls)
        {
            var cellIndex = rnd.Next(0, emptyCells.Count - 1);
                
            _ballsToShoot.Add((emptyCells[cellIndex].x,emptyCells[cellIndex].y, newBall));
            
            emptyCells.RemoveAt(cellIndex);

            if (!emptyCells.Any())
                break;
        }

        State = GameState.ShootNewBalls;
        return true;
    }

    public bool ApplyNewBallsAndProceedToNewMoveOrEndGame()
    {
        foreach (var newBall in BallsToShoot)
        {
            _field[newBall.x, newBall.y] = newBall.ball;
        }
        
        //todo clear lines if they appear after adding the balls
        
        _ballsToShoot.Clear();

        bool noEmptyCells = true;
        
        for (var x = 0; x < FieldSettings.Width; x++)
        for (var y = 0; y < FieldSettings.Height; y++)
        {
            if (_field[x, y] == null)
            {
                noEmptyCells = false;
                goto EndOfFor;
            }
        }
        
        EndOfFor:
        if (noEmptyCells)
        {
            State = GameState.GameOver;
            return true;
        }

        State = GameState.WaitingForSelection;
        return true;
    }

    private List<(int x, int y)> CheckBallsToClear(int x, int y, Shape ballToSet)
    {
        var ballBrush = ballToSet.Fill as SolidColorBrush;
        if (ballBrush == null)
        {
            throw new Exception("Bad ball");
        }

        var result = new List<(int x, int y)>();
        
        var color = ballBrush.Color;
     
        // check horizontal line
        var leftPos = x;
        var rightPos = x;
        
        for (var xCheck = x-1; xCheck >= 0; xCheck--)
        {
            var brush = _field[xCheck, y]?.Fill as SolidColorBrush;

            if (brush == null || brush.Color != color)
            {
                break;
            }
            leftPos = xCheck;
        }
        
        for (var xCheck = x+1; xCheck < FieldSettings.Width; xCheck++)
        {
            var brush = _field[xCheck, y]?.Fill as SolidColorBrush;

            if (brush == null || brush.Color != color)
            {
                break;
            }
            rightPos = xCheck;
        }

        if (rightPos - leftPos >= 4)
        {
            for (var xPos = leftPos; xPos <= rightPos; xPos++)
            {
                var ball = _field[xPos, y];
                if (ball != null) result.Add((xPos, y));
            }
        }
        
        //todo check vertical
        var upPos = y;
        var downPos = y;
        
        for (var yCheck = y-1; yCheck >= 0; yCheck--)
        {
            var brush = _field[x, yCheck]?.Fill as SolidColorBrush;

            if (brush == null || brush.Color != color)
            {
                break;
            }
            upPos = yCheck;
        }
        
        for (var yCheck = y+1; yCheck < FieldSettings.Height; yCheck++)
        {
            var brush = _field[x, yCheck]?.Fill as SolidColorBrush;

            if (brush == null || brush.Color != color)
            {
                break;
            }
            downPos = yCheck;
        }
        
        if (downPos - upPos >= 4)
        {
            for (var yPos = upPos; yPos <= downPos; yPos++)
            {
                var ball = _field[x, yPos];
                if (ball != null) result.Add((x, yPos));
            }
        }
        
        //todo check diagonal tl->br
        result.AddRange(DiagonalCheckTLBR(x, y, color));

        //todo check diagonal bl->tr
        result.AddRange(DiagonalCheckBLTR(x, y, color));
        
        return result;
    }

    private List<(int x, int y)> DiagonalCheckBLTR(int x, int y, Color currentColor)
    {
        var tlXPos = x;
        var tlYPos = y;

        var brXPos = x;

        var xPosTemp = x-1;
        var yPosTemp = y+1;
        while (xPosTemp >= 0 && yPosTemp < FieldSettings.Height)
        {
            var brush = _field[xPosTemp, yPosTemp]?.Fill as SolidColorBrush;
            if (brush == null || brush.Color != currentColor)
            {
                break;
            }

            tlXPos = xPosTemp;
            tlYPos = yPosTemp;

            xPosTemp--;
            yPosTemp++;
        }
        
        xPosTemp = x+1;
        yPosTemp = y-1;
        while (xPosTemp < FieldSettings.Width && yPosTemp >= 0)
        {
            var brush = _field[xPosTemp, yPosTemp]?.Fill as SolidColorBrush;
            if (brush == null || brush.Color != currentColor)
            {
                break;
            }

            brXPos = xPosTemp;

            xPosTemp++;
            yPosTemp--;
        }
        
        var result = new List<(int x, int y)>();
        if (brXPos - tlXPos >= 4)
        {
            var tlbrDiagonalX = tlXPos;
            var tlbrDiagonalY = tlYPos;
            while (tlbrDiagonalX <= brXPos)
            {
                var ball = _field[tlbrDiagonalX, tlbrDiagonalY];
                if (ball != null) result.Add((tlbrDiagonalX, tlbrDiagonalY));

                tlbrDiagonalX++;
                tlbrDiagonalY--;
            }
        }

        return result;
    }

    private List<(int x, int y)> DiagonalCheckTLBR(int x, int y, Color currentColor)
    {
        var tlXPos = x;
        var tlYPos = y;

        var brXPos = x;

        var xPosTemp = x-1;
        var yPosTemp = y-1;
        while (xPosTemp >= 0 && yPosTemp >= 0)
        {
            var brush = _field[xPosTemp, yPosTemp]?.Fill as SolidColorBrush;
            if (brush == null || brush.Color != currentColor)
            {
                break;
            }

            tlXPos = xPosTemp;
            tlYPos = yPosTemp;

            xPosTemp--;
            yPosTemp--;
        }
        
        xPosTemp = x+1;
        yPosTemp = y+1;
        while (xPosTemp < FieldSettings.Width && yPosTemp < FieldSettings.Height)
        {
            var brush = _field[xPosTemp, yPosTemp]?.Fill as SolidColorBrush;
            if (brush == null || brush.Color != currentColor)
            {
                break;
            }

            brXPos = xPosTemp;

            xPosTemp++;
            yPosTemp++;
        }
        
        var result = new List<(int x, int y)>();
        if (brXPos - tlXPos >= 4)
        {
            var tlbrDiagonalX = tlXPos;
            var tlbrDiagonalY = tlYPos;
            while (tlbrDiagonalX <= brXPos)
            {
                var ball = _field[tlbrDiagonalX, tlbrDiagonalY];
                if (ball != null) result.Add((tlbrDiagonalX, tlbrDiagonalY));

                tlbrDiagonalX++;
                tlbrDiagonalY++;
            }
        }

        return result;
    }
}