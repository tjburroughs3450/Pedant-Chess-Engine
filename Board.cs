using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ChessEnginePort
{
    class Board
    {
        private static string[] promotions = new string[] { "q", "r", "b", "n" };

        public Point whiteKingPos;

        public Point blackKingPos;

        private struct BoardSquare
        {
            public Point position;
            public string piece;
            public string promotion;

            public BoardSquare(Point position, string piece, string promotion)
            {
                this.position = position;
                this.piece = piece;
                this.promotion = promotion;
            }
        }

        public struct Move
        {
            public Point toPos;
            public Point fromPos;
            public string promotion;

            public Move(Point fromPos, Point toPos)
            {
                this.fromPos = fromPos;
                this.toPos = toPos;
                this.promotion = "";
            }

            public Move(Point fromPos, Point toPos, string promotion)
            {
                this.fromPos = fromPos;
                this.toPos = toPos;
                this.promotion = promotion;
            }

            public override string ToString()
            {
                return Board.moveToString(fromPos) + Board.moveToString(toPos) + this.promotion;
            }
        }

        public string[,] board;

        public string moves { get; set; }

        public bool whiteToMove { get; set; }

        public bool evaluationState { get; set; }

        private Stack<BoardSquare> toStack;

        private Stack<BoardSquare> fromStack;

        public Board()
        {
            board = new string[8,8];
            moves = "";
            whiteToMove = true;
            toStack = new Stack<BoardSquare>();
            fromStack = new Stack<BoardSquare>();
            evaluationState = false;
        }

        private void addPieceWithSymmetry(int x, string piece)
        {
            this.board[x, 0] = "w" + piece;
            this.board[7 - x, 0] = "w" + piece;

            this.board[x, 7] = "b" + piece;
            this.board[7 - x, 7] = "b" + piece;
        }

        public static Point stringToMove(string moveString)
        {
            return new Point((int)moveString[0] - 97, (int)moveString[1] - 49);
        }
        
        public Point[] stringToFullMove(string moveString)
        {
            return new Point[] {stringToMove(moveString.Substring(0,2)), stringToMove(moveString.Substring(2))};
        }

        public static string moveToString(Point move)
        {
            return ((char)(move.X + 97)).ToString() + ((char)(move.Y + 49)).ToString();
        }

        public void createInitialPosition()
        {
            for (int i = 0; i < 8; i++)
            {
                this.board[i, 1] = "wp";
                this.board[i, 6] = "bp";
            }

            this.addPieceWithSymmetry(0, "r");
            this.addPieceWithSymmetry(1, "n");
            this.addPieceWithSymmetry(2, "b");

            this.board[3, 0] = "wq";
            this.board[3, 7] = "bq";

            this.board[4, 0] = "wk";
            this.board[4, 7] = "bk";

            this.whiteKingPos = new Point(4, 0);
            this.blackKingPos = new Point(4, 7);
        }

        public void parseMoveString(string moveString)
        {
            List<int> promotionIndices = new List<int>();

            for (int i = 0; i < moveString.Length; i++)
            {
                if ((int)moveString[i] > 56 && (i + 1 >= moveString.Length || (int)moveString[i + 1] > 56))
                    promotionIndices.Add(i);
            }

            string move = "";

            for (int i = 0; i < moveString.Length; i++)
            {
                move += moveString[i];

                if (move.Length >= 4 && !promotionIndices.Contains(i + 1))
                {
                    this.moveS(move);

                    move = "";
                }
            }
        }

        public bool specialCase()
        {
            BoardSquare toMove = toStack.Peek();

            return toMove.promotion != "";
        }

        private void addWithPromotionCheck(List<Move> moveList, Point fromPos, Point toPos)
        {
                        // Promotion
            if ((toPos.Y == 7 && this.board[fromPos.X, fromPos.Y] == "wp") || (toPos.Y == 0 && this.board[fromPos.X, fromPos.Y] == "bp"))
            {
                foreach (string promo in promotions)
                    moveList.Add(new Move(fromPos, toPos, promo));
            }

            else
                moveList.Add(new Move(fromPos, toPos));
        }

        // Improve performance with a dictionary
        public List<Move> getMoves()
        {
            char toMove = whiteToMove ? 'w' : 'b';

            List<Move> recaptureSquares = new List<Move>();

            if (toStack.Count > 0 && toStack.Peek().piece != null)
            {
                foreach (Point piece in squareWatchingPoints(toStack.Peek().position))
                {
                    if (this.board[piece.X, piece.Y][0] == toMove && moveIsValid(piece, toStack.Peek().position))
                    {
                        addWithPromotionCheck(recaptureSquares, piece, toStack.Peek().position);
                    }
                }
            }

            var sorted = from element in recaptureSquares orderby Eval.materialKey[this.board[element.fromPos.X, element.fromPos.Y][1]] ascending select element;

            recaptureSquares = sorted.ToList();

            List<Move> moves = new List<Move>();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8;  j++)
                {
                    if (board[i, j] != null && board[i, j][0] == toMove)
                    {
                        foreach (Move move in this.getPieceMoves(new Point(i, j)))
                        {   
                            if (recaptureSquares.FindIndex((p) => p.fromPos == move.fromPos && p.toPos == move.toPos) == -1)
                                moves.Add(move);
                        }
                    }
                }
            }

            recaptureSquares.AddRange(moves);

            return recaptureSquares;
        }

        private bool moveDoesNotExist(List<Point[]> moves, Point[] checkMove)
        {
            foreach (Point[] move in moves)
            {
                if (movesMatch(move, checkMove))
                    return false;
            }

            return true;
        }

        public bool movesMatch(Point[] pointOne, Point[] pointTwo)
        {
            for (int i = 0; i < pointOne.Length; i++)
            {
                if (pointOne[i] != pointTwo[i])
                    return false;
            }

            return true;
        }

        public List<Point[]> getSortedMoves()
        {
            char toMove = whiteToMove ? 'w' : 'b';

            List<Point> pieces = new List<Point>();
            List<Point[]> moves = new List<Point[]>();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] != null && board[i, j][0] == toMove)
                    {
                        pieces.Add(new Point(i, j));
                    }
                }
            }

            var sorted = from element in pieces orderby squareInDanger(element) ? 1 : 0 descending select element;

            Console.WriteLine(pieces.Count + " " + sorted.ToList().Count);

            foreach (Point p in sorted)
                Console.WriteLine(p);

            return moves;
        }

        public List<Move> getPieceMoves(Point piecePos)
        {
            char pieceType = this.board[piecePos.X, piecePos.Y][1];
            char pieceColor = this.board[piecePos.X, piecePos.Y][0];

            // Move list to return
            List<Move> moves = new List<Move>();

            if (pieceType == 'p')
            {
                int direction = -1;
                int emPassantRank = 3;

                if (pieceColor == 'w')
                {
                    direction = 1;
                    emPassantRank = 4;
                }

                int movesForward = (piecePos.Y == 1 && pieceColor == 'w') || (piecePos.Y == 6 && pieceColor == 'b') ? 2 : 1;

                for (int i = 1; i <= movesForward; i++)
                {
                    Point forward = new Point(piecePos.X, piecePos.Y + direction * i);

                    if (forward.X >= 0 && forward.X < 8 && forward.Y >= 0 && forward.Y < 8 && this.board[forward.X, forward.Y] == null && moveIsValid(piecePos, forward))
                        addWithPromotionCheck(moves, piecePos, forward);
                    else
                        break;
                }

                // Capture
                for (int i = -1; i <= 1; i += 2)
                {
                    Point capturePos = new Point(piecePos.X + i, piecePos.Y + direction);

                    if (capturePos.X >= 0 && capturePos.X < 8 && capturePos.Y >= 0 && capturePos.Y < 8 && this.board[capturePos.X, capturePos.Y] != null && this.board[capturePos.X, capturePos.Y][0] != pieceColor && moveIsValid(piecePos, capturePos))
                        addWithPromotionCheck(moves, piecePos, capturePos);

                    // Em passant
                    if (piecePos.Y == emPassantRank && this.moves.Length >= 4
                        && moveToString(new Point(piecePos.X + i, piecePos.Y + direction * 2)) +
                        moveToString(new Point(piecePos.X + i, piecePos.Y)) == this.moves.Substring(this.moves.Length - 4))
                    {
                        if (moveIsValid(piecePos, new Point(piecePos.X + i, piecePos.Y + direction)))
                            moves.Add(new Move(piecePos, new Point(piecePos.X + i, piecePos.Y + direction)));
                    }
                }
            }

            else if (pieceType == 'r')
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    int incriment = piecePos.X + i;

                    // Horrizontal
                    while (incriment >= 0 && incriment < 8)
                    {
                        Point next = new Point(incriment, piecePos.Y);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (this.board[next.X, next.Y][0] != pieceColor && moveIsValid(piecePos, next))
                                moves.Add(new Move(piecePos, next));
                            break;
                        }

                        else if (moveIsValid(piecePos, next))
                            moves.Add(new Move(piecePos, next));

                        incriment += i;
                    }

                    incriment = piecePos.Y + i;

                    // Vertical
                    while (incriment >= 0 && incriment < 8)
                    {
                        Point next = new Point(piecePos.X, incriment);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (this.board[next.X, next.Y][0] != pieceColor && moveIsValid(piecePos, next))
                                moves.Add(new Move(piecePos, next));
                            break;
                        }

                        else if (moveIsValid(piecePos, next))
                            moves.Add(new Move(piecePos, next));

                        incriment += i;
                    }
                }
            }

            else if (pieceType == 'b')
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        int xPos = piecePos.X + i;
                        int yPos = piecePos.Y + j;

                        while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                        {
                            Point next = new Point(xPos, yPos);

                            xPos += i;
                            yPos += j;

                            if (this.board[next.X, next.Y] != null)
                            {
                                if (this.board[next.X, next.Y][0] != pieceColor && moveIsValid(piecePos, next))
                                    moves.Add(new Move(piecePos, next));
                                break;
                            }

                            else if (moveIsValid(piecePos, next))
                                moves.Add(new Move(piecePos, next));
                        }
                    }
                }
            }

            else if (pieceType == 'q')
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        int xPos = piecePos.X + i;
                        int yPos = piecePos.Y + j;

                        while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                        {
                            Point next = new Point(xPos, yPos);

                            xPos += i;
                            yPos += j;

                            if (this.board[next.X, next.Y] != null)
                            {
                                if (this.board[next.X, next.Y][0] != pieceColor && moveIsValid(piecePos, next))
                                    moves.Add(new Move(piecePos, next));
                                break;
                            }

                            else if (moveIsValid(piecePos, next))
                                moves.Add(new Move(piecePos, next));
                        }
                    }
                }
            }

            else if (pieceType == 'n')
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Point[] options = new Point[2];
                        options[0] = new Point(i * 2 + piecePos.X, j + piecePos.Y);
                        options[1] = new Point(i + piecePos.X, j * 2 + piecePos.Y);

                        foreach (Point move in options)
                        {
                            if (move.X >= 0 && move.X < 8 && move.Y >= 0 && move.Y < 8 && (this.board[move.X, move.Y] == null || this.board[move.X, move.Y][0] != pieceColor) && moveIsValid(piecePos, move))
                                moves.Add(new Move(piecePos, move));
                        }
                    }
                }
            }

            else if (pieceType == 'k')
            {
                string king = "e8";
                string left = "c8";
                string right = "g8";

                if (pieceColor == 'w')
                {
                    king = "e1";
                    left = "c1";
                    right = "g1";
                }

                if (!this.moves.Contains(king))
                {
                    if (!this.moves.Contains(right) && this.clearX(4, 7, piecePos.Y, getBoardPositions()))
                    {
                        bool canCastle = true;

                        for (int i = piecePos.X; i < piecePos.X + 4; i++)
                        {
                            if (squareInDanger(new Point(i, piecePos.Y)))
                            {
                                canCastle = false;
                                break;
                            }

                        }

                        if (canCastle)
                            moves.Add(new Move(piecePos, new Point(6, piecePos.Y)));
                    }

                    if (!this.moves.Contains(left) && this.clearX(0, 4, piecePos.Y, getBoardPositions()))
                    {
                        bool canCastle = true;
                        
                        for (int i = piecePos.X - 3; i <= piecePos.X; i++)
                        {
                            if (squareInDanger(new Point(i, piecePos.Y)))
                            {
                                canCastle = false;
                                break;
                            }

                        }

                        if (canCastle)
                            moves.Add(new Move(piecePos, new Point(2, piecePos.Y)));

                    }
                }

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        Point threat = new Point(piecePos.X + i, piecePos.Y + j);

                        if (threat.X >= 0 && threat.X < 8 && threat.Y >= 0 && threat.Y < 8 && (this.board[threat.X, threat.Y] == null || this.board[threat.X, threat.Y][0] != pieceColor) && moveIsValid(piecePos, threat))
                            moves.Add(new Move(piecePos, threat));
                    }
                }
            }

            return moves;
        }

        private Point[] getBoardPositions()
        {
            List<Point> final = new List<Point>();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (this.board[i, j] != null)
                        final.Add(new Point(i, j));
                }
            }

            return final.ToArray();
        }

        public bool specialMoveCheck(Point piecePos)
        {
            char type = this.board[piecePos.X, piecePos.Y][1];
            char pieceColor = this.board[piecePos.X, piecePos.Y][0];

            if (type == 'p')
            {
                int direction = -1;
                int emPassantRank = 3;

                if (pieceColor == 'w')
                {
                    direction = 1;
                    emPassantRank = 4;
                }

                // Capture
                for (int i = -1; i <= 1; i += 2)
                {
                    // Em passant
                    if (piecePos.Y == emPassantRank && this.moves.Length >= 4
                        && moveToString(new Point(piecePos.X + i, piecePos.Y + direction * 2)) +
                        moveToString(new Point(piecePos.X + i, piecePos.Y)) == this.moves.Substring(this.moves.Length - 4))
                    {
                        if (moveIsValid(piecePos, new Point(piecePos.X + i, piecePos.Y + direction)))
                            return true;
                    }
                }
            }
            else if (type == 'k')
            {
                string king = "e8";
                string left = "a8";
                string right = "h8";

                if (pieceColor == 'w')
                {
                    king = "e1";
                    left = "a1";
                    right = "h1";
                }

                if (!this.moves.Contains(king))
                {
                    if (!this.moves.Contains(right) && this.clearX(4, 7, piecePos.Y, getBoardPositions()))
                    {
                        for (int i = piecePos.X; i < piecePos.X + 4; i++)
                        {
                            if (squareInDanger(new Point(i, piecePos.Y)))
                            {
                                return false;
                            }

                        }

                        return true;
                    }

                    if (!this.moves.Contains(left) && this.clearX(0, 4, piecePos.Y, getBoardPositions()))
                    {
                        for (int i = piecePos.X - 3; i <= piecePos.X; i++)
                        {
                            if (squareInDanger(new Point(i, piecePos.Y)))
                            {
                                return false;
                            }

                        }
                        
                        return true;
                    }
                }
            }
            
            return false;
        }

        public string getPositionString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.whiteToMove ? 'w' : 'b');

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (this.board[x, y] != null)
                    {
                        if (specialMoveCheck(new Point(x, y)))
                        {
                            sb.Append(this.board[x, y].ToUpper());
                        }

                        else
                        {
                            sb.Append(this.board[x, y]);
                        }
                    }

                    else
                        sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        public bool kingInDanger()
        {
            return (squareInDanger(this.whiteToMove ? this.whiteKingPos : this.blackKingPos));
        }

        public bool moveIsValid(Point fromPos, Point toPos)
        {
            if (evaluationState)
                return true;

            Point lastWhiteKingPos = this.whiteKingPos;
            Point lastBlackKingPos = this.blackKingPos;

            // Update king
            if (this.board[fromPos.X, fromPos.Y] == "wk")
                this.whiteKingPos = toPos;
            else if (this.board[fromPos.X, fromPos.Y] == "bk")
                this.blackKingPos = toPos;

            string beforeFrom = this.board[fromPos.X, fromPos.Y];
            string beforeTo = this.board[toPos.X, toPos.Y] != null ? this.board[toPos.X, toPos.Y] : null;

            this.board[toPos.X, toPos.Y] = this.board[fromPos.X, fromPos.Y];
            this.board[fromPos.X, fromPos.Y] = null;

            bool valid = !this.kingInDanger();

            this.board[fromPos.X, fromPos.Y] = beforeFrom;

            if (beforeTo != null)
                this.board[toPos.X, toPos.Y] = beforeTo;
            else
                this.board[toPos.X, toPos.Y] = null;

            this.whiteKingPos = lastWhiteKingPos;
            this.blackKingPos = lastBlackKingPos;

            return valid;
        }

        //private List<Point> threatList(char pieceColor) 
        //{
        //    List<Point> threats = new List<Point>();

        //    for (int y = 0; y < 8; y++)
        //    {
        //        for (int x = 0; x < 8; x++)
        //        {
        //            if (this.board[x, y] != null && this.board[x, y][0] != pieceColor)
        //                threats.AddRange(this.getThreatenedSquares(new Point(x, y)));
        //        }
        //    }
            
        //    return threats;
        //}

        private bool clearX(int x1, int x2, int y, Point[] list)
        {
            for (int i = x1 + 1; i < x2; i++)
            {
                Point p = new Point(i, y);
                if (list.Contains(p))
                    return false;
            }
           
            return true;
        }

        public bool uncertainPosition()
        {
            if (toStack.Peek().piece != null)
                return true;

            //Point whiteQueen = new Point(-1, -1);
            //Point blackQueen = new Point(-1, -1);

            //for (int i = 0; i < 8; i++)
            //{
            //    for (int j = 0; j < 8; j++)
            //    {
            //        if (this.board[i, j] == "wq")
            //            whiteQueen = new Point(i, j);
            //        else if (this.board[i, j] == "bq")
            //            blackQueen = new Point(i, j);
            //    }
            //}

            //if (blackQueen.X != -1 && this.whiteToMove)
            //{
            //    this.whiteToMove = !this.whiteToMove;
            //    if (squareInDanger(blackQueen))
            //    {
            //        this.whiteToMove = !this.whiteToMove;
            //        return true;
            //    }
            //    this.whiteToMove = !this.whiteToMove;
            //}

            //else if (whiteQueen.X != -1 && !this.whiteToMove)
            //{
            //    this.whiteToMove = !this.whiteToMove;
            //    if (squareInDanger(whiteQueen))
            //    {
            //        this.whiteToMove = !this.whiteToMove;
            //        return true;
            //    }
            //    this.whiteToMove = !this.whiteToMove;
            //}

            return false;
        }

        private bool squareInDanger(Point square)
        {
            char color = this.whiteToMove ? 'w' : 'b';

            int direction = this.whiteToMove ? 1 : -1;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;

                    int xPos = square.X + i;
                    int yPos = square.Y + j;

                    while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                    {
                        Point next = new Point(xPos, yPos);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (this.board[next.X, next.Y][0] == color)
                                break;
                            else if (i == 0 || j == 0)
                            {
                                if (this.board[next.X, next.Y][1] == 'q' || this.board[next.X, next.Y][1] == 'r' || (this.board[next.X, next.Y][1] == 'k' && Math.Abs((square.X - xPos) + (square.Y - yPos)) == 1))
                                    return true;
                                else
                                    break;
                            }
                            else
                            {
                                if (this.board[next.X, next.Y][1] == 'q' || this.board[next.X, next.Y][1] == 'b')
                                    return true;
                                else if ((((Math.Abs(square.X - xPos) + Math.Abs(square.Y - yPos)) == 2) && (this.board[next.X, next.Y][1] == 'k' || (this.board[next.X, next.Y][1] == 'p' && j == direction))))
                                    return true;
                                else
                                    break;
                            }
                        }

                        xPos += i;
                        yPos += j;
                    }

                    // Knight
                    if (i != 0 && j != 0)
                    {
                        Point[] options = new Point[2];
                        options[0] = new Point(i * 2 + square.X, j + square.Y);
                        options[1] = new Point(i + square.X, j * 2 + square.Y);

                        foreach (Point move in options)
                        {
                            if (move.X >= 0 && move.X < 8 && move.Y >= 0 && move.Y < 8 && this.board[move.X, move.Y] != null && this.board[move.X, move.Y][1] == 'n' && this.board[move.X, move.Y][0] != color)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        public List<string> squareWatchingPieces(Point square)
        {
            List<string> results = new List<string>();

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;

                    int xPos = square.X + i;
                    int yPos = square.Y + j;

                    while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                    {
                        Point next = new Point(xPos, yPos);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (i == 0 || j == 0)
                            {
                                if (this.board[next.X, next.Y][1] == 'q' || this.board[next.X, next.Y][1] == 'r' || (this.board[next.X, next.Y][1] == 'k' && Math.Abs((square.X - xPos) + (square.Y - yPos)) == 1))
                                    results.Add(this.board[next.X, next.Y]);
                                break;
                            }
                            else
                            {
                                if (this.board[next.X, next.Y][1] == 'q' || this.board[next.X, next.Y][1] == 'b')
                                    results.Add(this.board[next.X, next.Y]);
                                else if ((Math.Abs(square.X - xPos) + Math.Abs(square.Y - yPos)) == 2)
                                {
                                    if (this.board[next.X, next.Y][1] == 'k')
                                        results.Add(this.board[next.X, next.Y]);
                                    else if (this.board[next.X, next.Y][1] == 'p' && ((this.board[next.X, next.Y][0] == 'w' && j == -1) || (this.board[next.X, next.Y][0] == 'b' && j == 1)))
                                        results.Add(this.board[next.X, next.Y]);
                                }

                                break;
                            }
                        }

                        xPos += i;
                        yPos += j;
                    }

                    // Knight
                    if (i != 0 && j != 0)
                    {
                        Point[] options = new Point[2];
                        options[0] = new Point(i * 2 + square.X, j + square.Y);
                        options[1] = new Point(i + square.X, j * 2 + square.Y);

                        foreach (Point move in options)
                        {
                            if (move.X >= 0 && move.X < 8 && move.Y >= 0 && move.Y < 8 && this.board[move.X, move.Y] != null && this.board[move.X, move.Y][1] == 'n')
                                results.Add(this.board[move.X, move.Y]);
                        }
                    }
                }
            }

            return results;
        }

        public List<Point> squareWatchingPoints(Point square)
        {
            List<Point> results = new List<Point>();

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;

                    int xPos = square.X + i;
                    int yPos = square.Y + j;

                    while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                    {
                        Point next = new Point(xPos, yPos);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (i == 0 || j == 0)
                            {
                                if (this.board[next.X, next.Y][1] == 'q' || this.board[next.X, next.Y][1] == 'r' || (this.board[next.X, next.Y][1] == 'k' && Math.Abs((square.X - xPos) + (square.Y - yPos)) == 1))
                                    results.Add(next);
                                break;
                            }
                            else
                            {
                                if (this.board[next.X, next.Y][1] == 'q' || this.board[next.X, next.Y][1] == 'b')
                                    results.Add(next);
                                else if ((Math.Abs(square.X - xPos) + Math.Abs(square.Y - yPos)) == 2)
                                {
                                    if (this.board[next.X, next.Y][1] == 'k')
                                        results.Add(next);
                                    else if (this.board[next.X, next.Y][1] == 'p' && ((this.board[next.X, next.Y][0] == 'w' && j == -1) || (this.board[next.X, next.Y][0] == 'b' && j == 1)))
                                        results.Add(next);
                                }

                                break;
                            }
                        }

                        xPos += i;
                        yPos += j;
                    }

                    // Knight
                    if (i != 0 && j != 0)
                    {
                        Point[] options = new Point[2];
                        options[0] = new Point(i * 2 + square.X, j + square.Y);
                        options[1] = new Point(i + square.X, j * 2 + square.Y);

                        foreach (Point move in options)
                        {
                            if (move.X >= 0 && move.X < 8 && move.Y >= 0 && move.Y < 8 && this.board[move.X, move.Y] != null && this.board[move.X, move.Y][1] == 'n')
                                results.Add(move);
                        }
                    }
                }
            }

            return results;
        }

        public List<Move> getThreatenedSquares(Point piecePos)
        {
            if (this.board[piecePos.X, piecePos.Y][1] == 'p')
            {
                char pieceColor = this.board[piecePos.X, piecePos.Y][0];

                List<Move> moves = new List<Move>();

                int direction = pieceColor == 'w' ? 1 : -1;

                // Capture
                for (int i = -1; i <= 1; i += 2)
                {
                    Point capturePos = new Point(piecePos.X + i, piecePos.Y + direction);

                    if (capturePos.X >= 0 && capturePos.X < 8 && capturePos.Y >= 0 && capturePos.Y < 8 &&
                        (this.board[capturePos.X, capturePos.Y] == null || this.board[capturePos.X, capturePos.Y][0] != pieceColor))
                        moves.Add(new Move(piecePos, capturePos));
                }

                return moves;
            }

            else if (this.board[piecePos.X, piecePos.Y][1] == 'k')
            {
                char pieceColor = this.board[piecePos.X, piecePos.Y][0];

                List<Move> moves = new List<Move>();

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        Point threat = new Point(piecePos.X + i, piecePos.Y + j);

                        if (threat.X >= 0 && threat.X < 8 && threat.Y >= 0 && threat.Y < 8 && (this.board[threat.X, threat.Y] == null || this.board[threat.X, threat.Y][0] != pieceColor))
                            moves.Add(new Move(piecePos, threat));
                    }
                }

                return moves;
            }

            else
                return this.getPieceMoves(piecePos);
        }

        public float getPieceCoverage(int x, int y, float heavyCorrection, float kingCorrection)
        {
            float score = 0f;

            char pieceColor = this.board[x, y][0];

            char pieceType = this.board[x, y][1];

            if (pieceType == 'k')
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        Point threat = new Point(x + i, y + j);

                        if (threat.X >= 0 && threat.X < 8 && threat.Y >= 0 && threat.Y < 8 && (this.board[threat.X, threat.Y] == null || this.board[threat.X, threat.Y][0] != pieceColor))
                            score += kingCorrection;
                    }
                }
            }

            else if (pieceType == 'r')
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    int incriment = x + i;

                    // Horrizontal
                    while (incriment >= 0 && incriment < 8)
                    {
                        Point next = new Point(incriment, y);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (this.board[next.X, next.Y][0] != pieceColor)
                                score += heavyCorrection / 5;
                            break;
                        }

                        else
                            score += heavyCorrection / 5;

                        incriment += i;
                    }

                    incriment = y + i;

                    // Vertical
                    while (incriment >= 0 && incriment < 8)
                    {
                        Point next = new Point(x, incriment);

                        if (this.board[next.X, next.Y] != null)
                        {
                            if (this.board[next.X, next.Y][0] != pieceColor)
                                score++;
                            break;
                        }

                        else
                            score++;

                        incriment += i;
                    }
                }
            }

            else if (pieceType == 'b')
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        int xPos = x + i;
                        int yPos = y + j;

                        while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                        {
                            Point next = new Point(xPos, yPos);

                            xPos += i;
                            yPos += j;

                            if (this.board[next.X, next.Y] != null)
                            {
                                if (this.board[next.X, next.Y][0] != pieceColor)
                                    score += .33f;
                                break;
                            }

                            else
                                score += .33f;
                        }
                    }
                }
            }

            else if (pieceType == 'q')
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        int xPos = x + i;
                        int yPos = y + j;

                        while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                        {
                            Point next = new Point(xPos, yPos);

                            xPos += i;
                            yPos += j;

                            if (this.board[next.X, next.Y] != null)
                            {
                                if (this.board[next.X, next.Y][0] != pieceColor)
                                    score += heavyCorrection / 9;
                                break;
                            }

                            else
                                score += heavyCorrection / 9;
                        }
                    }
                }
            }

            else if (pieceType == 'n')
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Point[] options = new Point[2];
                        options[0] = new Point(i * 2 + x, j + y);
                        options[1] = new Point(i + x, j * 2 + y);

                        foreach (Point move in options)
                        {
                            if (move.X >= 0 && move.X < 8 && move.Y >= 0 && move.Y < 8 && (this.board[move.X, move.Y] == null || this.board[move.X, move.Y][0] != pieceColor))
                                score += .33f;
                        }
                    }
                }
            }

            return score;
        }

        public void move(Move m)
        {
            move(m.fromPos, m.toPos, m.promotion);
        }

        public void move(Point fromPos, Point toPos, string promotion)
        {
            this.fromStack.Push(new BoardSquare(fromPos, this.board[fromPos.X, fromPos.Y], null));

            // Castle code
            if (this.board[fromPos.X, fromPos.Y][1] == 'k' && Math.Abs(toPos.X - fromPos.X) > 1)
            {
                int sign = toPos.X < fromPos.X ? -1 : 1;

                // Update king position
                if (this.board[fromPos.X, fromPos.Y][0] == 'w')
                    this.whiteKingPos = new Point(fromPos.X + sign * 2, fromPos.Y);
                else
                    this.blackKingPos = new Point(fromPos.X + sign * 2, fromPos.Y);

                this.toStack.Push(new BoardSquare(new Point(fromPos.X + sign * 2, fromPos.Y), null, promotion));
                this.fromStack.Push(new BoardSquare(new Point(sign == 1 ? 7 : 0, toPos.Y), this.board[fromPos.X, fromPos.Y][0] + "r", ""));
                this.toStack.Push(new BoardSquare(new Point(fromPos.X + sign, fromPos.Y), null, "c"));

                // Add king in new position
                this.board[fromPos.X + 2 * sign, fromPos.Y] = this.board[fromPos.X, fromPos.Y];

                // Add rook in new position
                this.board[fromPos.X + sign, fromPos.Y] = this.board[fromPos.X, fromPos.Y][0] + "r";

                // Remove the king
                this.board[fromPos.X, fromPos.Y] = null;

                // Remove rook
                this.board[sign == 1 ? 7 : 0, toPos.Y] = null;
            }

            else if (this.board[fromPos.X, fromPos.Y][1] == 'p' && toPos.X != fromPos.X && this.board[toPos.X, toPos.Y] == null)
            {
                int side = toPos.X - fromPos.X;
                string capturingPiece = this.board[fromPos.X, fromPos.Y];
                string capturedPiece = this.board[fromPos.X + side, fromPos.Y];
                this.board[fromPos.X, fromPos.Y] = null;
                this.board[toPos.X, toPos.Y] = capturingPiece;
                this.board[fromPos.X + side, fromPos.Y] = null;

                toStack.Push(new BoardSquare(new Point(toPos.X, toPos.Y), null, "e"));
            }

            else
            {
                if (this.board[fromPos.X, fromPos.Y] == "wk")
                    this.whiteKingPos = toPos;
                else if (this.board[fromPos.X, fromPos.Y] == "bk")
                    this.blackKingPos = toPos;

                this.toStack.Push(new BoardSquare(toPos, this.board[toPos.X, toPos.Y], promotion));
                this.board[toPos.X, toPos.Y] = (promotion == "" ? this.board[fromPos.X, fromPos.Y] : this.board[fromPos.X, fromPos.Y][0] + promotion);
                this.board[fromPos.X, fromPos.Y] = null;
            }

            this.moves += moveToString(fromPos) + moveToString(toPos) + promotion;
            this.whiteToMove = !this.whiteToMove;
        }

        public void moveS(string moveString)
        {
            Point fromPos = stringToMove(moveString.Substring(0, 2));
            Point toPos = stringToMove(moveString.Substring(2));

            this.move(fromPos, toPos, moveString.Length == 5 ? moveString[4].ToString() : "");
        }

        public void undoMove()
        {
            BoardSquare toInfo = this.toStack.Pop();
            BoardSquare fromInfo = this.fromStack.Pop();

            // King tracking
            if (fromInfo.piece == "wk")
                this.whiteKingPos = fromInfo.position;
            else if (fromInfo.piece == "bk")
                this.blackKingPos = fromInfo.position;

            if (toInfo.promotion == "e")
                board[toInfo.position.X, toInfo.position.Y + (fromInfo.piece[0] == 'w' ? -1 : 1)] = (fromInfo.piece[0] == 'w' ? 'b' : 'w') + "p";

            if (toInfo.piece != null)
                this.board[toInfo.position.X, toInfo.position.Y] = toInfo.piece;
            else
                this.board[toInfo.position.X, toInfo.position.Y] = null;

            this.board[fromInfo.position.X, fromInfo.position.Y] = fromInfo.piece;

            if (toInfo.promotion == "c")
                this.undoMove();
            else
            {
                this.moves = this.moves.Substring(0, this.moves.Length - (toInfo.promotion == null || toInfo.promotion == "" || toInfo.promotion == "c" || toInfo.promotion == "e" ? 4 : 5));
                this.whiteToMove = !this.whiteToMove;
            }
        }

        public void printBoard()
        {
            for (int y = 0; y < 8; y++)
            {
                Console.Write(" " + (8 - y) + " ");

                for (int x = 0; x < 8; x++)
                {
                    if (this.board[x, 7 - y] != null)
                        Console.Write(" " + (this.board[x, 7 - y][0] == 'w' ? this.board[x, 7 - y][1].ToString().ToUpper() : this.board[x, 7 - y][1].ToString()) + " ");
                    else
                        Console.Write(" - ");
                }

                Console.WriteLine();
            }

            Console.WriteLine("--------------------------");
            Console.WriteLine("    A  B  C  D  E  F  G  H");
        }
    }
}
