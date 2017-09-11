using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ChessEnginePort
{
    static class Eval
    {
        private const float CASTLE_BONUS = .3f;
        private const float OPEN_KING_SAFETY = 1f;
        private const float MID_KING_SAFETY = 1.5f;
        private const float KING_AGGRESSION = .5f;
        private const float PAWN_CENTER = .25f;
        private const float DOUBLED_PAWNS = .15f;
        private const float PAWN_DISTANCE = .3f;
        private const float MINOR_DEVELOPMENT = .5f;
        private const float FLIGHT_SQUARE = .3f;
        private const float COVERAGE = .2f;

        private const int OUTER_SQUARE_VALUE = 1;
        private const int MID_SQUARE_VALUE = 2;
        private const int INNER_SQUARE_VALUE = 3;

        private const int MID_GAME_MATERIAL_MIN = 40;

        private const float MID_GAME_POSITION_WEIGHT = .2f;

        public static Dictionary<char, int> materialKey = new Dictionary<char, int>() { { 'k', 1 },  { 'q', 9 }, { 'r', 5 }, { 'b', 3 }, { 'n', 3 }, { 'p', 1 } };
        public static int[,] squareValueKey = new int[8, 8];

        // DEBUG/TRANSPOSITIONS
        public static int evaluations = 0;

        public static void init()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    squareValueKey[i, j] = OUTER_SQUARE_VALUE;
                }
            }
            
            for (int i = 2; i < 6; i++)
            {
                for (int j = 2; j < 6; j++)
                {
                    squareValueKey[i, j] = MID_SQUARE_VALUE;
                }
            }

            for (int i = 3; i < 5; i++)
            {
                for (int j = 3; j < 5; j++)
                {
                    squareValueKey[i, j] = INNER_SQUARE_VALUE;
                }
            }
        }

        // TEST

        public static decimal evauluate(Board board, char side, decimal rootScore, bool enableCut)
        {
            // TRANSPOSITIONS/DEBUG
            evaluations++;

            int[] points = materialCount(board);
            int multiplier = side == 'w' ? 1 : -1;

            int totalMaterial = points[0] + points[1];

            if (enableCut && Math.Abs(rootScore - (points[0] - points[1])) >= 3)
                return (decimal)((points[0] - points[1]) * multiplier);

            if (board.moves.Length / 4 < 20)
            {
                // Opening
                float doubledPawns = stackedPawns(board) * DOUBLED_PAWNS;
                float castle = castleBonus(board) * CASTLE_BONUS;
                float coverageScore = coverage(board, .3f, 0) * COVERAGE;
                float king = kingSafety(board, points) * OPEN_KING_SAFETY;
                float center = pawnCenter(board) * PAWN_CENTER;

                //Console.WriteLine("Opening");
                //Console.WriteLine("Doubled Pawns: {0}, King Safety: {1}, Center Pawns: {2}, Castle Bonus: {3} Coverage: {4}", -1 * doubledPawns, -1 * king, center, castle, coverageScore);

                return Math.Round((decimal)((points[0] - points[1] + coverageScore + castle - doubledPawns - king + center) * multiplier), 3, MidpointRounding.AwayFromZero);
            }

            else if (totalMaterial >= 36)
            {
                // Mid game
                float doubledPawns = stackedPawns(board) * DOUBLED_PAWNS;
                float castle = castleBonus(board) * CASTLE_BONUS;
                float coverageScore = coverage(board, 1f, 0) * COVERAGE;
                float king = kingSafety(board, points) * MID_KING_SAFETY;
                float kingHunt = kingAggression(board, points) * KING_AGGRESSION;
                float flight = flightSquares(board, points) * FLIGHT_SQUARE;

                //Console.WriteLine("Midgame");
                //Console.WriteLine("Doubled Pawns: {0}, King Safety: {1}, Flight Square: {2}, Castle Bonus: {3}, King Hunt: {4}", doubledPawns, -1 * king, flight, castle, kingHunt);

                return Math.Round((decimal)((points[0] - points[1] + coverageScore + castle - doubledPawns - king + flight) * multiplier), 3, MidpointRounding.AwayFromZero);
            }
            
            else
            {
                // Check for king/rook/queen endgame
                if (points[0] == 1 && (points[1] == 6 || points[1] == 10))
                    return RookOrQueenEndgame(board) * multiplier;

                if (points[1] == 1 && (points[0] == 6 || points[0] == 10))
                    return RookOrQueenEndgame(board) * multiplier;

                float coverageScore = coverage(board, 1f, 1f) * COVERAGE;
                float kingHunt = kingAggression(board, points) * KING_AGGRESSION;
                float flight = flightSquares(board, points) * FLIGHT_SQUARE;
                float pawns = pawnBonus(board) * PAWN_DISTANCE;

                return Math.Round((decimal)((points[0] - points[1] + coverageScore + flight + pawns) * multiplier), 3, MidpointRounding.AwayFromZero);
            }
        }

        private static int RookOrQueenEndgame(Board board)
        {
            Point piecePos = new Point();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] != null && board.board[i, j][1] != 'k')
                    {
                        piecePos = new Point(i, j);
                        break;
                    }
                }
            }

            Point kingPos = board.board[piecePos.X, piecePos.Y][0] == 'w' ? board.blackKingPos : board.whiteKingPos;

            int yDst = 8;

            if (piecePos.Y > kingPos.Y)
                yDst = piecePos.Y;
            else if (piecePos.Y < kingPos.Y)
                yDst = 7 - piecePos.Y;

            int xDst = 8;

            if (board.board[piecePos.X, piecePos.Y][1] == 'q')
            {
                if (piecePos.X > kingPos.X)
                    xDst = piecePos.X;
                else if (piecePos.X < kingPos.X)
                    xDst = 7 - piecePos.X;
            }

            Point otherKing = kingPos == board.whiteKingPos ? board.blackKingPos : board.whiteKingPos;

            int kingDistance = Math.Abs(kingPos.X - otherKing.X) + Math.Abs(kingPos.Y - otherKing.Y);

            // Console.WriteLine(xDst + ", " + yDst + ", " + kingDistance);

            return (100 - yDst * xDst - kingDistance) * (kingPos == board.blackKingPos ? 1 : -1);
        }

        private static float pawnBonus(Board board)
        {
            int whiteScore = 0;
            int blackScore = 0;
            int totalWhitePawns = 0;
            int totalBlackPawns = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] != null && board.board[i, j][1] == 'p')
                    {
                        if (board.board[i, j][0] == 'w')
                        {
                            whiteScore += j + 1;
                            totalWhitePawns++;
                        }
                        
                        else
                        {
                            blackScore += 8 - j;
                            totalBlackPawns++;
                        }
                    }
                }
            }

            return (float)whiteScore / (totalWhitePawns == 0 ? 1 : totalWhitePawns) - (float)blackScore / (totalBlackPawns == 0 ? 1 : totalBlackPawns);
        }

        private static float coverage(Board board, float heavyCorrection, float kingCorrection)
        {
            float whiteScore = 0f;
            float blackScore = 0f;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] != null)
                    {
                        if (board.board[i, j][0] == 'w')
                            whiteScore += board.getPieceCoverage(i, j, heavyCorrection, kingCorrection);

                        else
                            blackScore += board.getPieceCoverage(i, j, heavyCorrection, kingCorrection);
                    }
                }
            }

            return whiteScore - blackScore;
        }

        private static float flightSquares(Board board, int[] material)
        {
            float whiteScore = 0f;
            float blackScore = 0f;

            foreach (Board.Move move in board.getThreatenedSquares(board.whiteKingPos))
            {
                if (move.toPos.Y != board.whiteKingPos.Y)
                    whiteScore = 1f;
            }

            foreach (Board.Move move in board.getThreatenedSquares(board.blackKingPos))
            {
                if (move.toPos.Y != board.blackKingPos.Y)
                    blackScore = 1f;
            }

            return whiteScore - blackScore;
        }

        private static float kingSafety(Board board, int[] material)
        {
            return (float)kingInroads(board, 'w') / material[1] - (float)kingInroads(board, 'b') / material[0];
        }

        private static int kingInroads(Board board, char color)
        {
            Point kingPos = color == 'w' ? board.whiteKingPos : board.blackKingPos;
            int squares = 0;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    if (i == 0 && j == 0)
                        continue;

                    int xPos = kingPos.X + i;
                    int yPos = kingPos.Y + j;

                    while (xPos >= 0 && xPos < 8 && yPos >= 0 && yPos < 8)
                    {
                        if (board.board[xPos, yPos] != null)
                        {
                            if (board.board[xPos, yPos][0] == color)
                                break;
                        }

                        squares++;

                        xPos += i;
                        yPos += j;
                    }
                }
            }

            return squares;
        }

        public static bool deepSearch(Board board)
        {
            int whiteScore = 0;
            int blackScore = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] != null)
                    {
                        int pieceValue = materialKey[board.board[i, j][1]];

                        if (board.board[i, j][0] == 'w' && Math.Abs(board.blackKingPos.X - i) <= 2 && Math.Abs(board.blackKingPos.Y - j) <= 2)
                            whiteScore += pieceValue;

                        if (board.board[i, j][0] == 'b' && Math.Abs(board.whiteKingPos.X - i) <= 2 && Math.Abs(board.whiteKingPos.Y - j) <= 2)
                            blackScore += pieceValue;
                    }
                }
            }

            return whiteScore >= 9 || blackScore >= 9;
        }

        private static float kingAggression(Board board, int[] material)
        {
            int whiteScore = 0;
            int blackScore = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] != null)
                    {
                        int pieceValue = materialKey[board.board[i, j][1]];

                        if (board.board[i, j][0] == 'w' && Math.Abs(board.blackKingPos.X - i) <= 2 && Math.Abs(board.blackKingPos.Y - j) <= 4)
                            whiteScore += pieceValue;

                        if (board.board[i, j][0] == 'b' && Math.Abs(board.whiteKingPos.X - i) <= 2 && Math.Abs(board.whiteKingPos.Y - j) <= 4)
                            blackScore += pieceValue;
                    }
                }
            }

            return (float)whiteScore / material[0] - (float)blackScore / material[1];
        }

        public static float aggression(Board board, int[] material)
        {
            int whiteScore = 0;
            int blackScore = 0;
            
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] != null && board.board[i, j][0] == 'w' && j > 3)
                    {
                        whiteScore += materialKey[board.board[i, j][1]];
                    }
                    if (board.board[i, j] != null && board.board[i, j][0] == 'b' && j < 4)
                    {
                        blackScore += materialKey[board.board[i, j][1]];
                    }
                }
            }

            return (float)whiteScore / material[0] - (float)blackScore / material[1];
        }

        private static bool pointIsValid(Point p)
        {
            return (p.X >= 0 && p.X < 8 && p.Y >= 0 && p.Y < 8);
        }

        private static float homeSquares(Board board)
        {   
            int whiteSquares = 0;
            int blackSquares = 0;

            for (int x = 0; x < 8; x++)
            {
                if (board.board[x, 0] != null && (board.board[x, 0] == "wb" || board.board[x, 0] == "wn"))
                    whiteSquares++;
                if (board.board[x, 7] != null && (board.board[x, 7] == "bb" || board.board[x, 7] == "bn"))
                    blackSquares++;
            }

            return (float)whiteSquares - blackSquares;
        }

        private static float pawnCenter(Board board)
        {
            int whitePawns = 0;
            int blackPawns = 0;

            for (int i = 3; i < 5; i++)
            {
                for (int j = 3; j < 5; j++)
                {
                    if (board.board[i, j] != null)
                    {
                        if (board.board[i, j][0] == 'w' && whitePawns <= 2)
                            whitePawns++;
                        else if (board.board[i, j][0] == 'b' && blackPawns <= 2)
                            blackPawns++;
                    }
                }
            }

            return (float)whitePawns - blackPawns;
        }

        private static float stackedPawns(Board board)
        {
            int stackedWhite = 0;
            int stackedBlack = 0;
            int totalWhite = 0;
            int totalBlack = 0;

            for (int i = 0; i < 8; i++)
            {
                int blackPawns = 0;
                int whitePawns = 0;

                for (int j = 0; j < 8; j++)
                {
                    if (board.board[i, j] == "wp")
                    {
                        whitePawns++;
                        totalWhite++;
                    }
                    else if (board.board[i, j] == "bp")
                    {
                        blackPawns++;
                        totalBlack++;
                    }
                }

                stackedWhite += whitePawns > 0 ? whitePawns - 1 : 0;
                stackedBlack += blackPawns > 0 ? blackPawns - 1 : 0;
            }

            return (totalWhite > 0 ? (float)stackedWhite / totalWhite : 0) - (totalBlack > 0 ? (float)stackedBlack / totalBlack : 0);
        }

        private static float castleBonus(Board board)
        {
            return (board.moves.Contains("e1h1") || board.moves.Contains("e1a1") ? 1f : 0f) - (board.moves.Contains("e8h8") || board.moves.Contains("e8a8") ? 1f : 0f);
        }

        private static int[] materialCount(Board board)
        {
            int[] totals = new int[] { 0, 0 };

            foreach (string piece in board.board)
            {
                if (piece != null && piece[0] == 'w')
                    totals[0] += materialKey[piece[1]];
                else if (piece != null)
                    totals[1] += materialKey[piece[1]];
            }

            return totals;
        }

        private static int sumList(List<int> list)
        {
            int sum = 0;

            foreach (int n in list)
                sum += n;
            
            return sum;
        }
    }

    public struct Evaluation
    {
        public int score;

        public ChessBoard.Move[] line;

        public Evaluation(int score, ChessBoard.Move[] line)
        {
            this.score = score;
            this.line = line;
        }
    }
}
