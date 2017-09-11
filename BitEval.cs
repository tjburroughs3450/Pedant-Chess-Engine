using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessEnginePort
{
    static class BitEval
    {
        public const int CUT_BUFFER = 20;

        public static int evaluations = 0;

        private static int[] pieceValues = { 1, 3, 3, 5, 9, 20};

        private static List<ChessBoard.AttackMask> whiteAttackMasks = new List<ChessBoard.AttackMask>(250);
        private static List<ChessBoard.AttackMask> blackAttackMasks = new List<ChessBoard.AttackMask>(250);

        private static List<CriticalSquare> criticalSquares = new List<CriticalSquare>(16);

        // Masks
        private static ulong bottomMask;
        private static ulong topMask;

        // List for building threats
        private static List<ChessBoard.Move> moves = new List<ChessBoard.Move>(16);

        private struct CriticalSquare
        {
            public ChessBoard.Move[] samePieces;
            public ChessBoard.Move[] opponentPieces;
            public int initialResident;

            public CriticalSquare(int initialResident)
            {
                this.samePieces = null;
                this.opponentPieces = null;
                this.initialResident = initialResident;
            }

            //public CriticalSquare()
            //{
            //    this.samePieces = null;
            //    this.opponentPieces = null;
            //    this.initialResident = 0;
            //}
        }

        public static void init()
        {
            for (int i = 0; i <= 31; i++)
                bottomMask += (ulong)0x1 << i;

            for (int i = 32; i < 64; i++)
                topMask += (ulong)0x1 << i;
        }

        private static int aggressionScore(ChessBoard.Position pos)
        {
            int score = 0;

            for (int i = 1; i < 5; i++)
                score += populationCount(pos.boards[i] & pos.boards[6] & topMask) - populationCount(pos.boards[i] & pos.boards[7] & bottomMask);

            return score;
        }

        public static int oldPopulationCount(ulong bitBoard)
        {
            int count = 0;

            while (bitBoard > 0)
            {
                bitBoard = bitBoard - ((ulong)0x1 << ChessBoard.getMSBIndex(bitBoard));
                count++;
            }

            return count;
        }

        public static int populationCount(ulong value)
        {
            const ulong NibbleLowBitsMask = 0x7777777777777777UL;
            const ulong SummingMask = 0x0F0F0F0F0F0F0F0FUL;
            var uvalue = (ulong)value;
            var temp = uvalue;
            uvalue = (uvalue >> 1) & NibbleLowBitsMask;
            temp -= uvalue;
            uvalue = (uvalue >> 1) & NibbleLowBitsMask;
            temp -= uvalue;
            uvalue = (uvalue >> 1) & NibbleLowBitsMask;
            temp -= uvalue;
            
            return (int)(((temp + (temp >> 4)) & SummingMask) % 255); // casting out 255's
        }

        public static int evaluate(Crawler crawler, ChessBoard.PieceColor color, int alpha, int beta)
        {
            ChessBoard.Position position = crawler.currentPosition;
            getMasks(position);

            //DEBUG
            evaluations++;

            int score = 0;
            int multiplier = 1;

            ulong whitePieces = position.boards[6];
            ulong blackPieces = position.boards[7];

            if (color == ChessBoard.PieceColor.Black)
                multiplier = -1;
            
            // Material Count
            for (int i = 0; i < 5; i++)
            {
                int material = populationCount(position.boards[i] & whitePieces) - populationCount(position.boards[i] & blackPieces);
                score += (material * pieceValues[i]) * 10;
            }
            
            int offset = staticMaterialOffset(crawler) * 10;

            score += offset;

            // Lazy eval
            if (score * multiplier + CUT_BUFFER < alpha)
            {
                whiteAttackMasks.Clear();
                blackAttackMasks.Clear();
                criticalSquares.Clear();

                return score * multiplier;
            }

            else if (score * multiplier - CUT_BUFFER > beta)
            {
                whiteAttackMasks.Clear();
                blackAttackMasks.Clear();
                criticalSquares.Clear();

                return score * multiplier;
            }

            score += getCoverageScore(position);
            score += aggressionScore(position);

            whiteAttackMasks.Clear();
            blackAttackMasks.Clear();
            criticalSquares.Clear();

            return score * multiplier;
        }

        private static void getMasks(ChessBoard.Position pos)
        {
            getPawnAttackMask(pos, ChessBoard.PieceColor.White, whiteAttackMasks);
            getKnightAttackMask(pos, ChessBoard.PieceColor.White, whiteAttackMasks);
            getKingAttackMask(pos, ChessBoard.PieceColor.White, whiteAttackMasks);
            getBishopAttackMask(pos, ChessBoard.PieceColor.White, whiteAttackMasks);
            getRookAttackMask(pos, ChessBoard.PieceColor.White, whiteAttackMasks);
            getQueenAttackMask(pos, ChessBoard.PieceColor.White, whiteAttackMasks);

            getPawnAttackMask(pos, ChessBoard.PieceColor.Black, blackAttackMasks);
            getKnightAttackMask(pos, ChessBoard.PieceColor.Black, blackAttackMasks);
            getKingAttackMask(pos, ChessBoard.PieceColor.Black, blackAttackMasks);
            getBishopAttackMask(pos, ChessBoard.PieceColor.Black, blackAttackMasks);
            getRookAttackMask(pos, ChessBoard.PieceColor.Black, blackAttackMasks);
            getQueenAttackMask(pos, ChessBoard.PieceColor.Black, blackAttackMasks);

            // Typical setup bullshit

            List<ChessBoard.AttackMask> opponentAttacks = blackAttackMasks;
            List<ChessBoard.AttackMask> sameAttacks = whiteAttackMasks;

            ulong samePieces = pos.boards[6];
            ulong opponentPieces = pos.boards[7];

            ulong attackedSquares = 0x0;

            if (pos.whiteToMove)
            {
                opponentAttacks = whiteAttackMasks;
                sameAttacks = blackAttackMasks;
                samePieces = pos.boards[7];
                opponentPieces = pos.boards[6];
            }


            // Preparing list
            foreach (ChessBoard.AttackMask mask in opponentAttacks)
            {
                attackedSquares |= mask.mask & samePieces;
            }

            while (attackedSquares > 0x0)
            {
                CriticalSquare currentSquare = new CriticalSquare(0);
                int index = ChessBoard.getLSBIndex(attackedSquares);
                ulong squareMask = (ulong)0x1 << index;

                // Set the current resident
                for (int i = 0; i < 5; i++)
                {
                    if ((samePieces & pos.boards[i] & squareMask) > 0)
                    {
                        currentSquare.initialResident = i;
                        break;
                    }
                }

                moves.Clear();

                foreach (ChessBoard.AttackMask mask in opponentAttacks)
                {
                    if ((mask.mask & squareMask) > 0)
                    {
                        moves.Add(new ChessBoard.Move(squareMask + mask.position, 0, mask.pieceIndex));
                    }
                }

                currentSquare.opponentPieces = moves.ToArray();
                moves.Clear();

                foreach (ChessBoard.AttackMask mask in sameAttacks)
                {
                    if ((mask.mask & squareMask) > 0)
                    {
                        moves.Add(new ChessBoard.Move(squareMask + mask.position, 0, mask.pieceIndex));
                    }
                }

                currentSquare.samePieces = moves.ToArray();
                criticalSquares.Add(currentSquare);

                attackedSquares -= squareMask;
            }
        }

        // Array/List shit must be fixed
        public static int staticMaterialOffset(Crawler crawler)
        {
            int maxLoss = 0;
            int multiplier = -1;

            if (crawler.currentPosition.whiteToMove)
                multiplier = 1;

            foreach (CriticalSquare square in criticalSquares)
            {
                int loss = miniAlphaBeta(crawler, square.samePieces.ToList(), square.opponentPieces.ToList(), 0, false, pieceValues[square.initialResident], -100, 100);

                if (loss > maxLoss)
                    maxLoss = loss;

                // Might not really help
                if (maxLoss == 9)
                {
                    break;
                }
            }

            return maxLoss * multiplier;
        }

        private static int miniAlphaBeta(Crawler crawler, List<ChessBoard.Move> sameMoves, List<ChessBoard.Move> opponentMoves, int delta, bool sameToMove, int chopValue, int alpha, int beta)
        {
            List<ChessBoard.Move> moves = opponentMoves;
            int negate = 1;

            if (sameToMove)
            {
                moves = sameMoves;
                negate = -1;
            }

            ChessBoard.PieceColor color = ChessBoard.PieceColor.White;

            if (crawler.currentPosition.whiteToMove)
                color = ChessBoard.PieceColor.Black;

            int max = delta;

            for (int i = 0; i < moves.Count; i++)
            {
                ChessBoard.Move move = moves[i];
                
                crawler.move(move);

                // Check for illegal moves -- probably horribly optimized -- This needs to check whose move it is
                if (ChessBoard.kingInDanger(crawler.currentPosition, color))
                {
                    crawler.undoMove();
                    continue;
                }

                // TODO: Find a better way

                moves.RemoveAt(i);
                int score = miniAlphaBeta(crawler, sameMoves, opponentMoves, delta + chopValue * negate, !sameToMove, pieceValues[move.pieceIndex], alpha, beta);
                moves.Insert(i, move);
                crawler.undoMove();

                if (score * negate > max * negate)
                {
                    max = score;

                    if (negate == 1 && score > alpha)
                        alpha = max;

                    else if (negate == -1 && score < beta)
                        beta = max;

                    if (alpha >= beta)
                        return max;
                }
            }

            return max;
        }

        // Pawn and king mask consideration may cause odd results
        public static int getCoverageScore(ChessBoard.Position pos)
        {
            float score = 0f;

            foreach (ChessBoard.AttackMask mask in whiteAttackMasks)
            {
                if (mask.pieceIndex == 0 || mask.pieceIndex == 5)
                    continue;

                score += populationCount(mask.mask) / (float)pieceValues[mask.pieceIndex];
            }
            
            foreach (ChessBoard.AttackMask mask in blackAttackMasks)
            {
                if (mask.pieceIndex == 0 || mask.pieceIndex == 5)
                    continue;

                score -= populationCount(mask.mask) / (float)pieceValues[mask.pieceIndex];
            }

            return (int)score;
        }

        // Attack mask stuffs

        public static void getKnightAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            ulong boardMask = position.boards[6];

            if (color == ChessBoard.PieceColor.Black)
                boardMask = position.boards[7];

            ulong knights = position.boards[1] & boardMask;

            if (knights == 0)
                return;

            while (knights > 0)
            {
                int index = ChessBoard.getLSBIndex(knights);
                ulong posMask = (ulong)0x1 << index;
                attacks.Add(new ChessBoard.AttackMask(ChessBoard.vectors[8, index], posMask, 1));
                knights &= ~posMask;
            }
        }

        public static void getBishopAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            getRayPieceAttackMask(position, color, attacks, 1, 2, 2);
        }

        public static void getRookAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            getRayPieceAttackMask(position, color, attacks, 0, 2, 3);
        }

        public static void getQueenAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            getRayPieceAttackMask(position, color, attacks, 0, 1, 4);
        }

        public static void getKingAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            ulong boardMask = position.boards[6];

            if (color == ChessBoard.PieceColor.Black)
                boardMask = position.boards[7];

            ulong king = position.boards[5] & boardMask;

            attacks.Add(new ChessBoard.AttackMask(ChessBoard.vectors[11, ChessBoard.getLSBIndex(king)], king, 5));
        }

        public static void getPawnAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            if (color == ChessBoard.PieceColor.White)
                getWhitePawnAttackMask(position, color, attacks);
            else
                getBlackPawnAttackMask(position, color, attacks);
        }

        private static void getBlackPawnAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            ulong sameColor = position.boards[7];
            ulong opponentColor = position.boards[6];

            ulong enpassant = 0;

            ushort flag = position.flags;
            bool enp = (flag & (ushort)8) > 0;
            int file = flag & (ushort)7;

            if (enp)
            {
                enpassant = (ulong)0x1 << (file + 16);
            }

            ulong bothColors = sameColor + opponentColor;
            ulong pawns = position.boards[0] & sameColor;

            while (pawns > 0)
            {
                int i = ChessBoard.getLSBIndex(pawns);

                ulong positionMask = (ulong)0x1 << i;
                ulong moveMask = 0x0;

                ulong ahead = positionMask >> 8;

                if ((ahead & bothColors) == 0)
                {
                    moveMask += ahead;
                    ahead >>= 8;

                    if (((ulong)ChessBoard.Ranks.Seven & positionMask) > 0 && (ahead & bothColors) == 0)
                        moveMask += ahead;
                }

                ulong capture = positionMask >> 7 & (ulong)ChessBoard.Files.A;
                moveMask += capture & bothColors;

                capture = positionMask >> 9 & (ulong)ChessBoard.Files.H;
                moveMask += capture & bothColors;

                if (enp)
                {
                    if ((i % 8 == file + 1 || i % 8 == file - 1) && i / 8 == 3)
                        moveMask += enpassant;
                }

                attacks.Add(new ChessBoard.AttackMask(moveMask, positionMask, 0));

                pawns -= (ulong)0x1 << i;
            }
        }

        private static void getWhitePawnAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks)
        {
            ulong sameColor = position.boards[6];
            ulong opponentColor = position.boards[7];

            ulong enpassant = 0;
            ushort flag = position.flags;
            bool enp = (flag & (ushort)8) > 0;
            int file = flag & (ushort)7;

            if (enp)
            {
                enpassant = (ulong)0x1 << (file + 40);
            }

            ulong bothColors = sameColor + opponentColor;
            ulong pawns = position.boards[0] & sameColor;

            while (pawns > 0)
            {
                int i = ChessBoard.getLSBIndex(pawns);

                ulong positionMask = (ulong)0x1 << i;
                ulong moveMask = 0x0;

                ulong ahead = positionMask << 8;

                if ((ahead & bothColors) == 0)
                {
                    moveMask += ahead;
                    ahead <<= 8;

                    if (((ulong)ChessBoard.Ranks.Two & positionMask) > 0 && (ahead & bothColors) == 0)
                        moveMask += ahead;
                }

                ulong capture = positionMask << 7 & (ulong)ChessBoard.Files.H;
                moveMask += capture & bothColors;

                capture = positionMask << 9 & (ulong)ChessBoard.Files.A;
                moveMask += capture & bothColors;

                if (enp)
                {
                    if ((i % 8 == file + 1 || i % 8 == file - 1) && i / 8 == 4)
                        moveMask += enpassant;
                }

                attacks.Add(new ChessBoard.AttackMask(moveMask, positionMask, 0));

                pawns -= (ulong)0x1 << i;
            }
        }

        private static void getRayPieceAttackMask(ChessBoard.Position position, ChessBoard.PieceColor color, List<ChessBoard.AttackMask> attacks, int start, int increment, int typeIndex)
        {
            ulong sameColor = position.boards[6];
            ulong opponentColor = position.boards[7];
            ulong bothColor = sameColor + opponentColor;

            if (color == ChessBoard.PieceColor.Black)
            {
                sameColor = position.boards[7];
                opponentColor = position.boards[6];
            }

            ulong pieceType = position.boards[typeIndex] & sameColor;

            if (pieceType == 0)
                return;

            while (pieceType > 0x0)
            {
                int i = ChessBoard.getLSBIndex(pieceType);

                ulong attackMask = 0;

                for (int j = start; j < 8; j += increment)
                {
                    ulong ray = ChessBoard.vectors[j, i];
                    ulong blocker = (ray & ~bothColor) ^ ray;

                    if (blocker > 0)
                    {
                        int index;

                        if (j < 3 || j > 6)
                        {
                            index = ChessBoard.getLSBIndex(blocker);
                            ray &= ~ChessBoard.vectors[j, index];
                        }
                        else
                        {
                            index = ChessBoard.getMSBIndex(blocker);
                            ray &= ~ChessBoard.vectors[j, index];
                        }
                    }

                    attackMask += ray;
                }

                attacks.Add(new ChessBoard.AttackMask(attackMask, (ulong)0x1 << i, (byte)typeIndex));
                pieceType -= (ulong)0x1 << i;
            }
        }
    }
}
