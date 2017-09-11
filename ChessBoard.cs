using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ChessEnginePort
{
    public static class ChessBoard
    {
        public enum Piece : byte
        {
            Pawn = 0,
            Knight,
            Bishop,
            Rook,
            Queen,
            King,
            Empty
        }

        public enum PieceColor : byte
        {
            White = 0,
            Black,
            Null
        }

        public enum Ranks : ulong
        {
            One = 0xff,
            Two = 0xff00,
            Three = 0xff0000,
            Four = 0xff000000,
            Five = 0xff00000000,
            Six = 0xff0000000000,
            Seven = 0xff000000000000,
            Eight = 0xff00000000000000
        }

        public enum Files : ulong
        {
            A = 0xfefefefefefefefe,
            B = 0xfdfdfdfdfdfdfdfd,
            C = 0xfbfbfbfbfbfbfbfb,
            D = 0xf7f7f7f7f7f7f7f7,
            E = 0xefefefefefefefef,
            F = 0xdfdfdfdfdfdfdfdf,
            G = 0xbfbfbfbfbfbfbfbf,
            H = 0x7f7f7f7f7f7f7f7f
        }

        public static int[] pieceValue = { 1, 3, 3, 5, 9, 0 };

        // Probably should use char int values
        public static char[] fileLetters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g' };

        public static ulong[,] vectors = new ulong[13, 64];

        public static ulong[] hashNumbers = new ulong[781];

        private static int[] _positions =
	    {
	        0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
	        31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
	    };

        private static int[] MultiplyDeBruijnBitPosition = 
        {
            0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
            8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
        };

        //Static optomizations
        public static List<AttackMask> masks = new List<AttackMask>(300);

        public static void init()
        {
            // Zobrist Hash Numbers
            Random r = new Random(104395301);

            // Castle Vectors
            vectors[12, 0] = ((ulong)0x1 << 1) + ((ulong)0x1 << 2) + ((ulong)0x1 << 3);
            vectors[12, 1] = ((ulong)0x1 << 5) + ((ulong)0x1 << 6);
            vectors[12, 2] = ((ulong)0x1 << 57) + ((ulong)0x1 << 58) + ((ulong)0x1 << 59);
            vectors[12, 3] = ((ulong)0x1 << 61) + ((ulong)0x1 << 62);

            vectors[12, 4] = ((ulong)0x1 << 4) + ((ulong)0x1 << 2);
            vectors[12, 5] = ((ulong)0x1 << 4) + ((ulong)0x1 << 6);
            vectors[12, 6] = ((ulong)0x1 << 60) + ((ulong)0x1 << 58);
            vectors[12, 7] = ((ulong)0x1 << 60) + ((ulong)0x1 << 62);

            vectors[12, 8] = ((ulong)0x1) + ((ulong)0x1 << 3);
            vectors[12, 9] = ((ulong)0x1 << 7) + ((ulong)0x1 << 5);
            vectors[12, 10] = ((ulong)0x1 << 56) + ((ulong)0x1 << 59);
            vectors[12, 11] = ((ulong)0x1 << 63) + ((ulong)0x1 << 61);




            for (int i = 0; i < 781; i++)
            {
                byte[] next = new byte[64];
                r.NextBytes(next);
                ulong number = (ulong)BitConverter.ToUInt64(next, 0);
                hashNumbers[i] = number;
            }

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    int deltaX = 0;
                    int deltaY = 1;

                    switch (i)
                    {
                        case 1:
                            deltaX = 1;
                            deltaY = 1;
                            break;

                        case 2:
                            deltaX = 1;
                            deltaY = 0;
                            break;

                        case 3:
                            deltaX = 1;
                            deltaY = -1;
                            break;

                        case 4:
                            deltaX = 0;
                            deltaY = -1;
                            break;

                        case 5:
                            deltaX = -1;
                            deltaY = -1;
                            break;

                        case 6:
                            deltaX = -1;
                            deltaY = 0;
                            break;

                        case 7:
                            deltaX = -1;
                            deltaY = 1;
                            break;
                    }

                    ulong vector = 0x0;

                    int x = j % 8;
                    int y = j / 8;

                    y += deltaY;
                    x += deltaX;

                    while (x >= 0 && x < 8 && y >= 0 && y < 8)
                    {
                        vector += ((ulong)0x1 << (y * 8 + x));

                        y += deltaY;
                        x += deltaX;
                    }

                    vectors[i, j] = vector;
                }
            }

            for (int j = 0; j < 64; j++)
            {
                ulong mask = 0x0;

                mask += (((ulong)0x1 << j)  << 17) & (ulong)Files.A;
                mask += (((ulong)0x1 << j) << 10) & (ulong)Files.A & (ulong)Files.B;
                mask += (((ulong)0x1 << j) >> 6) & (ulong)Files.A & (ulong)Files.B;
                mask += (((ulong)0x1 << j) >> 15) & (ulong)Files.A;
                mask += (((ulong)0x1 << j) >> 17) & (ulong)Files.H;
                mask += (((ulong)0x1 << j) >> 10) & (ulong)Files.H & (ulong)Files.G;
                mask += (((ulong)0x1 << j) << 6) & (ulong)Files.H & (ulong)Files.G;
                mask += (((ulong)0x1 << j) << 15) & (ulong)Files.H;

                vectors[8, j] = mask;
            }

            // Pawns
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    ulong position = (ulong)0x1 << j;
                    ulong mask = 0x0;
                    
                    if (i == 0)
                    {
                        mask += position << 7 & (ulong)Files.H;
                        mask += position << 8;
                        mask += position << 9 & (ulong)Files.A;
                        
                        if ((position & (ulong)Ranks.Two) > 0)
                            mask += position << 16;
                        
                        vectors[9, j] = mask;
                    }
                    else
                    {
                        mask += position >> 7 & (ulong)Files.A;
                        mask += position >> 8;
                        mask += position >> 9 & (ulong)Files.H;
                        
                        if ((position & (ulong)Ranks.Seven) > 0)
                            mask += position >> 16;

                        vectors[10, j] = mask;
                    }
                }
            }

            for (int i = 0; i < 64; i++)
            {
                ulong position = (ulong)0x1 << i;

                ulong mask = position << 8;
                mask += position >> 8 ;
                
                mask += (position >> 1) & (ulong)Files.H;
                mask += (position << 1) & (ulong)Files.A;

                mask += (position >> 9) & (ulong)Files.H;
                mask += (position << 9) & (ulong)Files.A;

                mask += (position >> 7) & (ulong)Files.A;
                mask += (position << 7) & (ulong)Files.H;

                vectors[11, i] = mask;
            }
        }

        // BOARD
        // 0 -> Pawn
        // 1 -> Knight
        // 2 -> Bishop
        // 3 -> Rook
        // 4 -> Queen
        // 5 -> King
        // 6 -> White pieces
        // 7 -> Black pieces

        public struct Position
        {
            public ulong[] boards;
            public ushort flags;
            public bool whiteToMove;
            public ulong hash;
            
            public ulong previousCapture;

            public Position(long[] boards, byte flags, bool whiteToMove)
            {
                this.boards = new ulong[8];

                if (boards != null)
                    Buffer.BlockCopy(boards, 0, this.boards, 0, boards.Length * 8);

                this.flags = flags;
                this.whiteToMove = whiteToMove;
                this.hash = 0x0;
                this.previousCapture = 0x0;
            }

            public Piece getPiece(Point position)
            {
                for (int i = 0; i < boards.Length - 2; i++)
                {
                    if (((boards[i] >> (position.Y * 8 + position.X)) & 0x1) == 1)
                        return (Piece)i;
                }

                return Piece.Empty;
            }

            public void setPiece(Piece piece, PieceColor color, Point position)
            {
                ulong mask = (ulong)0x1 << (position.Y * 8 + position.X);

                if (piece != Piece.Empty)
                {
                    this.boards[(int)piece] = this.boards[(int)piece] | mask;
                    this.boards[6] = (color == PieceColor.White ? this.boards[6] | mask : (this.boards[6] | mask) ^ mask);
                    this.boards[7] = (color == PieceColor.White ? (this.boards[7] | mask) ^ mask : this.boards[7] | mask);
                }
                else
                {
                    for (int i = 0; i < this.boards.Length; i++)
                        this.boards[i] = this.boards[i] & ~mask;
                }
            }

            public PieceColor getColor(Point position)
            {
                if (((this.boards[6] >> (position.Y * 8 + position.X)) & 0x1) == 1)
                    return PieceColor.White;
                else if (((this.boards[7] >> (position.Y * 8 + position.X)) & 0x1) == 1)
                    return PieceColor.Black;
                else
                    return PieceColor.Null;
            }
        }

        public struct AttackMask
        {
            public ulong mask;
            public ulong position;
            public byte pieceIndex;

            public AttackMask(ulong mask, ulong position, byte pieceIndex)
            {
                this.mask = mask;
                this.position = position;
                this.pieceIndex = pieceIndex;
            }
        }

        public struct Move
        {
            public byte pieceIndex;
            public ulong mask;
            public int deltaValue;
            public int promotion;

            public Move(ulong mask, int deltaValue, byte pieceIndex)
            {
                this.mask = mask;
                this.deltaValue = deltaValue;
                this.pieceIndex = pieceIndex;
                this.promotion = 0;
            }

            public override bool Equals(object obj)
            {
                Move other = (Move)obj;

                return this.mask == other.mask && this.promotion == other.promotion &&
                    this.pieceIndex == other.pieceIndex;
            }

            public static bool operator == (Move c1, Move c2)
            {
                return c1.Equals(c2);
            }

            public static bool operator != (Move c1, Move c2)
            {
                return !c1.Equals(c2);
            }
        }

        public static string movesToString(Crawler crawler, Move[] moves)
        {
            StringBuilder sb = new StringBuilder();
            int sideIndex = 7;

            if (crawler.currentPosition.whiteToMove)
                sideIndex = 6;

            for (int i = 0; i < moves.Length; i++)
            {
                ulong sideMask = crawler.currentPosition.boards[sideIndex];
                ulong fromPos = moves[i].mask & sideMask;
                ulong toPos = fromPos ^ moves[i].mask;

                int index = (int)Math.Log(fromPos, 2);

                sb.Append((char)(index % 8 + 97));
                sb.Append(index / 8 + 1);

                index = (int)Math.Log(toPos, 2);
                sb.Append((char)(index % 8 + 97));
                sb.Append(index / 8 + 1);

                if (moves[i].promotion > 0)
                    sb.Append('q');

                sb.Append(' ');

                crawler.move(moves[i]);

                if (crawler.currentPosition.whiteToMove)
                    sideIndex = 6;
                else
                    sideIndex = 7;
            }

            for (int i = 0; i < moves.Length; i++)
                crawler.undoMove();

                return sb.ToString();
        }

        private static ulong getPieceSquareHashNum(int square, byte pieceIndex, PieceColor color) 
        {
            // 0 - WP
            // 1 - WN
            // 2 - WB
            // 3 - WR
            // 4 - WQ
            // 5 - WK
            // 6 - BP
            // 7 - BN
            // 8 - BB
            // 9 - BR
            // 10 - BQ
            // 11 - BK

            int offset = 0;

            if (color == PieceColor.Black)
                offset = 6;

            return hashNumbers[(pieceIndex + offset) * 64 + square];
        }

        public static Position parseMoveString(string moveString)
        {
            Position pos = new Position(null, 0, true);

            createInitialPosition(pos);

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
                    pos = advancePosition(pos, stringToMove(pos, move), pos.hash);

                    move = "";
                }
            }

            return pos;
        }

        public static Move stringToMove(Position pos, string move)
        {
            int x = (int)move[0] - 97;
            int y = ((int)move[1] - 1) * 8;
            int x2 = (int)move[2] - 97;
            int y2 = ((int)move[3] - 1) * 8;

            ulong from = (ulong)0x1 << (x + y);
            ulong to = (ulong)0x1 << (x2 + y2);
            int index = 0;

            for (int i = 0; i < 6; i++)
            {
                if ((pos.boards[i] & from) > 0)
                    index = i;
            }

            Move m = new Move(from + to, 0, (byte)index);

            if (move.Length == 5)
                m.promotion = 4;

            return m;
        }

        public static Position advancePosition(Position pos, Move move, ulong hash)
        {
            // Hash Table Stuff
            PieceColor color = PieceColor.Black;
            PieceColor otherColor = PieceColor.White;

            Position newPos = pos;
            newPos.boards = new ulong[8];
            Buffer.BlockCopy(pos.boards, 0, newPos.boards, 0, 64);

            // Clear flag
            if ((newPos.flags & (ushort)15) > 0)
            {
                newPos.hash ^= hashNumbers[769 + newPos.flags & (ushort)7];
                newPos.flags &= (ushort)(0xFFF0);
            }
            
            int sameIndex = 7;
            int opponentIndex = 6;
            int enpassantSquare = 16;

            if (pos.whiteToMove)
            {
                sameIndex = 6;
                opponentIndex = 7;
                color = PieceColor.White;
                otherColor = PieceColor.Black;
                enpassantSquare = 40;
            }

            ulong sameColor = pos.boards[sameIndex];
            ulong opponentColor = pos.boards[opponentIndex];

            ulong toMask = move.mask & ~sameColor;
            ulong fromMask = move.mask & sameColor;

            // Enpassant
            if (move.pieceIndex == 0)
            {
                if (((move.mask & ~((ulong)Ranks.Two) & ~((ulong)Ranks.Four)) == 0 || (move.mask & ~((ulong)Ranks.Five) & ~((ulong)Ranks.Seven)) == 0))
                {
                    newPos.flags |= (ushort)(getLSBIndex(move.mask) % 8 + 8);
                    newPos.hash ^= hashNumbers[769 + newPos.flags & (ushort)7];
                }

                else
                {
                    int file = pos.flags & (ushort)7;

                    if ((pos.flags & (ushort)8) > 0)
                    {
                        if ((toMask & (ulong)0x1 << (enpassantSquare + file)) > 0)
                        {
                            newPos.boards[0] ^= move.mask;
                            newPos.boards[sameIndex] ^= move.mask;

                            if (pos.whiteToMove)
                            {
                                newPos.boards[opponentIndex] -= toMask >> 8;
                                newPos.boards[0] -= toMask >> 8;
                                hash ^= getPieceSquareHashNum(getLSBIndex(toMask >> 8), move.pieceIndex, otherColor);
                            }
                            else
                            {
                                newPos.boards[opponentIndex] -= toMask << 8;
                                newPos.boards[0] -= toMask << 8;
                                hash ^= getPieceSquareHashNum(getLSBIndex(toMask << 8), move.pieceIndex, otherColor);
                            }

                            // Clean up
                            hash ^= getPieceSquareHashNum(getLSBIndex(fromMask), move.pieceIndex, color);
                            hash ^= getPieceSquareHashNum(getLSBIndex(toMask), move.pieceIndex, color);
                            hash ^= hashNumbers[768];

                            newPos.whiteToMove = !newPos.whiteToMove;
                            newPos.hash = hash;

                            return newPos;
                        }
                    }
                }
            }

            // Castle Check
            if (move.pieceIndex == 5)
            {
                bool castled = false;

                // Set the flags
                if (newPos.whiteToMove)
                {
                    if ((move.mask & ~vectors[12, 4]) == 0)
                    {
                        hash ^= getPieceSquareHashNum(getLSBIndex(fromMask), 5, color);
                        hash ^= getPieceSquareHashNum(getLSBIndex(toMask), 5, color);

                        hash ^= getPieceSquareHashNum(getLSBIndex(vectors[12, 8]), 3, color);
                        hash ^= getPieceSquareHashNum(getMSBIndex(vectors[12, 8]), 3, color);

                        // Move rook and king
                        newPos.boards[5] ^= move.mask;
                        newPos.boards[3] ^= vectors[12, 8];
                        newPos.boards[sameIndex] ^= move.mask;
                        newPos.boards[sameIndex] ^= vectors[12, 8];
                        
                        castled = true;
                    }

                    else if ((move.mask & ~vectors[12, 5]) == 0)
                    {
                        hash ^= getPieceSquareHashNum(getLSBIndex(fromMask), 5, color);
                        hash ^= getPieceSquareHashNum(getLSBIndex(toMask), 5, color);

                        hash ^= getPieceSquareHashNum(getLSBIndex(vectors[12, 9]), 3, color);
                        hash ^= getPieceSquareHashNum(getMSBIndex(vectors[12, 9]), 3, color);

                        // Move rook and king
                        newPos.boards[5] ^= move.mask;
                        newPos.boards[3] ^= vectors[12, 9];
                        newPos.boards[sameIndex] ^= move.mask;
                        newPos.boards[sameIndex] ^= vectors[12, 9];

                        castled = true;
                    }

                    if ((newPos.flags & 0x10) == 0)
                    {
                        newPos.flags |= 0x10;
                        hash ^= hashNumbers[777];
                    }

                    if ((newPos.flags & 0x20) == 0)
                    {
                        newPos.flags |= 0x20;
                        hash ^= hashNumbers[778];
                    }
                }

                else
                {
                    if ((move.mask & ~vectors[12, 6]) == 0)
                    {
                        hash ^= getPieceSquareHashNum(getLSBIndex(fromMask), 5, color);
                        hash ^= getPieceSquareHashNum(getLSBIndex(toMask), 5, color);

                        hash ^= getPieceSquareHashNum(getLSBIndex(vectors[12, 10]), 3, color);
                        hash ^= getPieceSquareHashNum(getMSBIndex(vectors[12, 10]), 3, color);

                        // Move rook and king
                        newPos.boards[5] ^= move.mask;
                        newPos.boards[3] ^= vectors[12, 10];
                        newPos.boards[sameIndex] ^= move.mask;
                        newPos.boards[sameIndex] ^= vectors[12, 10];

                        castled = true;
                    }

                    else if ((move.mask & ~vectors[12, 7]) == 0)
                    {
                        hash ^= getPieceSquareHashNum(getLSBIndex(fromMask), 5, color);
                        hash ^= getPieceSquareHashNum(getLSBIndex(toMask), 5, color);

                        hash ^= getPieceSquareHashNum(getLSBIndex(vectors[12, 11]), 3, color);
                        hash ^= getPieceSquareHashNum(getMSBIndex(vectors[12, 11]), 3, color);

                        // Move rook and king
                        newPos.boards[5] ^= move.mask;
                        newPos.boards[3] ^= vectors[12, 11];
                        newPos.boards[sameIndex] ^= move.mask;
                        newPos.boards[sameIndex] ^= vectors[12, 11];

                        castled = true;
                    }

                    if ((newPos.flags & 0x40) == 0)
                    {
                        newPos.flags |= 0x40;
                        hash ^= hashNumbers[779];
                    }

                    if ((newPos.flags & 0x80) == 0)
                    {
                        newPos.flags |= 0x80;
                        hash ^= hashNumbers[780];
                    }
                }

                if (castled)
                {
                    hash ^= hashNumbers[768];
                    newPos.whiteToMove = !newPos.whiteToMove;
                    newPos.hash = hash;

                    return newPos;
                }
            }

            else if (move.pieceIndex == 3)
            {
                if (fromMask == (ulong)1)
                {
                    if ((newPos.flags & 0x10) == 0)
                    {
                        newPos.flags |= 0x10;
                        hash ^= hashNumbers[777];
                    }
                }

                else if (fromMask == (ulong)128)
                {
                    if ((newPos.flags & 0x20) == 0)
                    {
                        newPos.flags |= 0x20;
                        hash ^= hashNumbers[778];
                    }
                }
                else if (fromMask == ((ulong)0x1 << 56))
                {
                    if ((newPos.flags & 0x40) == 0)
                    {
                        newPos.flags |= 0x40;
                        hash ^= hashNumbers[779];
                    }
                }
                else if (fromMask == ((ulong)0x1 << 63))
                {
                    if ((newPos.flags & 0x80) == 0)
                    {
                        newPos.flags |= 0x80;
                        hash ^= hashNumbers[780];
                    }
                }
            }

            // Recapture
            newPos.previousCapture = toMask & opponentColor;

            hash ^= getPieceSquareHashNum(getLSBIndex(fromMask), move.pieceIndex, color);
            hash ^= getPieceSquareHashNum(getLSBIndex(toMask), move.pieceIndex, color);

            //Check if capture of same piece
            if ((toMask & newPos.boards[move.pieceIndex]) > 0)
            {   
                newPos.boards[opponentIndex] -= toMask;
                hash ^= getPieceSquareHashNum(getLSBIndex(toMask), move.pieceIndex, otherColor);
            }
            // Check if capture
            else if ((opponentColor & toMask) > 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    if ((newPos.boards[i] & toMask) > 0)
                    {
                        hash ^= getPieceSquareHashNum(getLSBIndex(newPos.boards[i] & toMask), (byte)i, otherColor);
                        newPos.boards[i] -= toMask;
                        break;
                    }
                }
                newPos.boards[opponentIndex] -= toMask;
            }

            //Finishing logic
            newPos.boards[move.pieceIndex] -= fromMask;
            newPos.boards[move.pieceIndex] |= toMask;
            newPos.boards[sameIndex] ^= move.mask;

            // Promotion logic
            if (move.promotion > 0)
            {
                newPos.boards[0] -= toMask;
                newPos.boards[move.promotion] += toMask;
                int pawnIndex = getLSBIndex(toMask);

                hash ^= getPieceSquareHashNum(pawnIndex, 0, color);
                hash ^= getPieceSquareHashNum(pawnIndex, (byte)move.promotion, color);
            }


            hash ^= hashNumbers[768];
            newPos.whiteToMove = !newPos.whiteToMove;
            newPos.hash = hash;

            return newPos;
        }

        // Overload if masks haven't yet been generated
        public static List<Move> getMoves(Position pos)
        {
            PieceColor color = PieceColor.Black;

            if (pos.whiteToMove)
                color = PieceColor.White;

            getPawnAttackMask(pos, color, masks);
            getKnightAttackMask(pos, color, masks);
            getBishopAttackMask(pos, color, masks);
            getRookAttackMask(pos, color, masks);
            getQueenAttackMask(pos, color, masks);
            getKingAttackMask(pos, color, masks);

            return getMoves(pos, masks);
        }

        public static List<Move> getMoves(Position pos, List<AttackMask> masks)
        {
            List<Move> moves = new List<Move>(250);

            ulong toMovePieces = pos.boards[7];
            ulong opponentPieces = pos.boards[6];

            if (pos.whiteToMove)
            {
                toMovePieces = pos.boards[6];
                opponentPieces= pos.boards[7];

            }

            int kingPos = getLSBIndex(pos.boards[5] & toMovePieces);

            // DEBUG
            //if (kingPos > 63 || kingPos < 0)
            //{
            //    ChessBoard.printPosition(pos);
            //    ChessBoard.printBitboard(pos.boards[6]);
            //    ChessBoard.printBitboard(pos.boards[7]);
            //    ChessBoard.printBitboard(pos.boards[5]);
            //    Console.WriteLine(pos.whiteToMove);
            //}

            foreach (AttackMask mask in masks)
            {
                moves.AddRange(getMovesFromMask(pos, mask, kingPos, toMovePieces));
            }

            masks.Clear();

            return moves; 
        }

        // Does not check for castling
        private static List<Move> getMovesFromMask(Position pos, AttackMask attackMask, int kingPos, ulong moveColorPieces)
        {
            bool king = attackMask.pieceIndex == 5;

            ulong mask = attackMask.mask;
            List<Move> moves = new List<Move>(35);

            while (mask > 0)
            {
                ulong toPos = (ulong)0x1 << getLSBIndex(mask);
                ulong moveMask = toPos + attackMask.position;
                mask &= ~toPos;
                int delta;
                
                if (king)
                {
                    delta = moveCheck(pos, new Move(moveMask, 0, attackMask.pieceIndex), getLSBIndex(toPos));
                }
                else if (attackMask.pieceIndex == 0)
                {
                    if ((pos.whiteToMove && (attackMask.position & (ulong)Ranks.Seven) > 0) || 
                        (!pos.whiteToMove && (attackMask.position & (ulong)Ranks.Two) > 0))
                    {
                        delta = moveCheck(pos, new Move(moveMask, 0, attackMask.pieceIndex), kingPos);
                        
                        if (delta != -100)
                        {
                            for (int i = 1; i < 5; i++)
                            {
                                Move promotionMove = new Move(moveMask, delta + pieceValue[i], 0);
                                promotionMove.promotion = i;
                                moves.Add(promotionMove);
                            }
                        }

                        continue;
                    }
                    else
                        delta = moveCheck(pos, new Move(moveMask, 0, attackMask.pieceIndex), kingPos);
                }
                else
                    delta = moveCheck(pos, new Move(moveMask, 0, attackMask.pieceIndex), kingPos);

                if (delta != -100)
                {
                    moves.Add(new Move(moveMask, delta, attackMask.pieceIndex));
                }
            }

            // Castle Case
            if (king)
            {
                if (pos.whiteToMove)
                {
                    if (canCastle(pos, 0))
                        moves.Add(new Move(vectors[12, 4], 0, 5));
                    if (canCastle(pos, 1))
                        moves.Add(new Move(vectors[12, 5], 0, 5));
                }
                else
                {
                    if (canCastle(pos, 2))
                        moves.Add(new Move(vectors[12, 6], 0, 5));
                    if (canCastle(pos, 3))
                        moves.Add(new Move(vectors[12, 7], 0, 5));
                }
            }

            return moves;
        }

        private static bool canCastle(Position pos, int side)
        {
            if (side == 0)
            {
                if ((pos.flags & 0x10) == 0 && ((pos.boards[6] | pos.boards[7]) & vectors[12, 0]) == 0 && (pos.boards[3] & pos.boards[6] & (ulong)0x1) > 0)
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        if (squareThreatened(pos, PieceColor.White, i))
                            return false;
                    }

                    return true;
                }
                  
            }

            else if (side == 1)
            {
                if ((pos.flags & 0x20) == 0 && ((pos.boards[6] | pos.boards[7]) & vectors[12, 1]) == 0 && (pos.boards[3] & pos.boards[6] & (ulong)0x1 << 7) > 0)
                {
                    for (int i = 4; i <= 6; i++)
                    {
                        if (squareThreatened(pos, PieceColor.White, i))
                            return false;
                    }

                    return true;
                }
            }

            else if (side == 2)
            {
                if ((pos.flags & 0x40) == 0 && ((pos.boards[6] | pos.boards[7]) & vectors[12, 2]) == 0 && (pos.boards[3] & pos.boards[7] & (ulong)0x1 << 56) > 0)
                {
                    for (int i = 57; i <= 60; i++)
                    {
                        if (squareThreatened(pos, PieceColor.Black, i))
                            return false;
                    }

                    return true;
                }
            }

            else if (side == 3)
            {
                if ((pos.flags & 0x80) == 0 && ((pos.boards[6] | pos.boards[7]) & vectors[12, 3]) == 0 && (pos.boards[3] & pos.boards[7] & (ulong)0x1 << 63) > 0)
                {
                    for (int i = 60; i <= 62; i++)
                    {
                        if (squareThreatened(pos, PieceColor.Black, i))
                            return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool kingInDanger(Position pos, ChessBoard.PieceColor color)
        {
            ulong sameColor = pos.boards[7];
            
            if (color == PieceColor.White)
                sameColor = pos.boards[6];

            return squareThreatened(pos, color, getLSBIndex(pos.boards[5] & sameColor));
        }

        // Returns 20 for illegal moves. Add different score for even trade.
        public static int moveCheck(Position pos, Move move, int kingPos)
        {
            int delta = 0;

            PieceColor color = PieceColor.Black;
            
            int enpassantSquare = 16;

            int sameIndex = 7;
            int opponentIndex = 6;

            if (pos.whiteToMove)
            {
                color = PieceColor.White;
                sameIndex = 6;
                opponentIndex = 7;
                enpassantSquare = 40;
            }

            ulong sameColor = pos.boards[sameIndex];
            ulong opponentColor = pos.boards[opponentIndex];
            ulong pieceBackup = pos.boards[move.pieceIndex];
            ulong otherPieceBackup = 0x0;
            int otherPieceIndex = -1;

            ulong toMask = move.mask & ~sameColor;
            ulong fromMask = move.mask & sameColor;

            // Enpassant
            if (move.pieceIndex == 0)
            {
                int file = pos.flags & (ushort)7;

                if ((pos.flags & (ushort)8) > 0)
                {
                    if ((toMask & (ulong)0x1 << (enpassantSquare + file)) > 0)
                    {
                        pos.boards[0] ^= move.mask;
                        pos.boards[sameIndex] ^= move.mask;

                        if (pos.whiteToMove)
                        {
                            pos.boards[opponentIndex] -= toMask >> 8;
                            pos.boards[0] -= toMask >> 8;
                        }
                        else
                        {
                            pos.boards[opponentIndex] -= toMask << 8;
                            pos.boards[0] -= toMask << 8;
                        }

                        bool hit = squareThreatened(pos, color, kingPos);

                        pos.boards[sameIndex] = sameColor;
                        pos.boards[opponentIndex] = opponentColor;
                        pos.boards[move.pieceIndex] = pieceBackup;

                        if (hit)
                            return -100;

                        return 0;
                    }
                }
            }

            // Check if capture of same piece
            if ((toMask & pos.boards[move.pieceIndex]) > 0)
            {
                delta = 1;
                pos.boards[opponentIndex] -= toMask;
            }
            // Check if capture
            else if ((opponentColor & toMask) > 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    if ((pos.boards[i] & toMask) > 0)
                    {
                        otherPieceIndex = i;
                        otherPieceBackup = pos.boards[i];

                        pos.boards[i] -= toMask;
                        
                        // Offset
                        delta = pieceValue[move.pieceIndex] - pieceValue[i] + 20;
                        break;
                    }
                }
                pos.boards[opponentIndex] -= toMask;
            }

            //Finishing logic
            pos.boards[move.pieceIndex] -= fromMask;
            pos.boards[move.pieceIndex] |= toMask;
            pos.boards[sameIndex] ^= move.mask;

            bool illegal = squareThreatened(pos, color, kingPos);

            pos.boards[sameIndex] = sameColor;
            pos.boards[opponentIndex] = opponentColor;
            pos.boards[move.pieceIndex] = pieceBackup;

            if (otherPieceIndex != -1)
                pos.boards[otherPieceIndex] = otherPieceBackup;

            if (illegal)
                return -100;

            // Recapture
            if ((pos.previousCapture & toMask) > 0)
                delta = 750 - pieceValue[move.pieceIndex];

            return delta;
        }

        public static void printPosition(Position position)
        {
            for (int y = 0; y < 8; y++)
            {
                Console.Write(" " + (8 - y) + " ");

                for (int x = 0; x < 8; x++)
                {
                    if (position.getPiece(new Point(x, 7 - y)) != Piece.Empty)
                        Console.Write(" " + pieceToString(position.getColor(new Point(x, 7 - y)), position.getPiece(new Point(x, 7 - y))) + " ");
                    else
                        Console.Write(" - ");
                }

                Console.WriteLine();
            }

            Console.WriteLine("--------------------------");
            Console.WriteLine("    A  B  C  D  E  F  G  H");
        }

        public static void getKnightAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
        {
            ulong boardMask = position.boards[6];

            if (color == PieceColor.Black)
                boardMask = position.boards[7];
            
            ulong knights = position.boards[1] & boardMask;

            if (knights == 0)
                return;

            while (knights > 0)
            {
                int index = getLSBIndex(knights);
                ulong posMask = (ulong)0x1 << index;
                attacks.Add(new AttackMask(vectors[8, index] & ~boardMask, posMask, 1));
                knights &= ~posMask;
            }
        }

        public static void getBishopAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
        {
            getRayPieceAttackMask(position, color, attacks, 1, 2, 2);
        }

        public static void getRookAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
        {
            getRayPieceAttackMask(position, color, attacks, 0, 2, 3);
        }

        public static void getQueenAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
        {
            getRayPieceAttackMask(position, color, attacks, 0, 1, 4);
        }

        public static void getKingAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
        {
            ulong boardMask = position.boards[6];

            if (color == PieceColor.Black)
                boardMask = position.boards[7];

            ulong king = position.boards[5] & boardMask;

            attacks.Add(new AttackMask(vectors[11, getLSBIndex(king)] & ~boardMask, king, 5));
        }

        public static void getPawnAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
        {
            if (color == PieceColor.White)
                getWhitePawnAttackMask(position, color, attacks);
            else
                getBlackPawnAttackMask(position, color, attacks);
        }

        private static void getBlackPawnAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
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
                int i = getLSBIndex(pawns);

                ulong positionMask = (ulong)0x1 << i;
                ulong moveMask = 0x0;

                ulong ahead = positionMask >> 8;

                if ((ahead & bothColors) == 0)
                {
                    moveMask += ahead;
                    ahead >>= 8;

                    if (((ulong)Ranks.Seven & positionMask) > 0 && (ahead & bothColors) == 0)
                        moveMask += ahead;
                }

                ulong capture = positionMask >> 7 & (ulong)Files.A;
                moveMask += capture & opponentColor;

                capture = positionMask >> 9 & (ulong)Files.H;
                moveMask += capture & opponentColor;

                if (enp)
                {
                    if ((i % 8 == file + 1 || i % 8 == file - 1) && i / 8 == 3)
                        moveMask += enpassant;
                }

                attacks.Add(new AttackMask(moveMask, positionMask, 0));

                pawns -= (ulong)0x1 << i;
            }
        }

        private static void getWhitePawnAttackMask(Position position, PieceColor color, List<AttackMask> attacks)
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
                int i = getLSBIndex(pawns);

                ulong positionMask = (ulong)0x1 << i;
                ulong moveMask = 0x0;
                    
                ulong ahead = positionMask << 8;
                    
                if ((ahead & bothColors) == 0)
                {
                    moveMask += ahead;
                    ahead <<=  8;

                    if (((ulong)Ranks.Two & positionMask) > 0 && (ahead & bothColors) == 0)
                        moveMask += ahead;
                }

                ulong capture = positionMask << 7 & (ulong)Files.H;
                moveMask += capture & opponentColor;

                capture = positionMask << 9 & (ulong)Files.A;
                moveMask += capture & opponentColor;

                if (enp)
                {
                    if ((i % 8 == file + 1 || i % 8 == file - 1) && i / 8 == 4)
                        moveMask += enpassant;
                }

                attacks.Add(new AttackMask(moveMask, positionMask, 0));

                pawns -= (ulong)0x1 << i;
            }
        }

        public static bool squareThreatened(Position position, PieceColor color, int boardOffset)
        {
            ulong dangerPieces = position.boards[6];

            if (color == PieceColor.White)
                dangerPieces = position.boards[7];

            ulong bothColor = position.boards[6] + position.boards[7];
            
            // Check knights
            if ((vectors[8, boardOffset] & position.boards[1] & dangerPieces) != 0)
                return true;

            // King
            if ((vectors[11, boardOffset] & dangerPieces & position.boards[5]) > 0)
                return true;

            for (int j = 0; j < 8; j++)
            {
                ulong ray = vectors[j, boardOffset];
                ulong blocker = (ray & ~bothColor) ^ ray;

                if (blocker > 0x0)
                {
                    int index;

                    if (j < 3 || j > 6)
                    {
                        index = getLSBIndex(blocker);

                        ulong piece = ((ulong)0x1 << index);

                        // Deal with queen
                        if ((piece & position.boards[4] & dangerPieces) > 0)
                            return true;

                        // Bishop/pawns
                        if ((j == 7 || j == 1))
                        {
                            if ((piece & (position.boards[2] & dangerPieces)) > 0)
                                return true;
                            else if ((piece & (position.boards[0] & dangerPieces)) > 0 && ((color == PieceColor.White && (index - boardOffset == 7 || index - boardOffset == 9)) ||
                                (color == PieceColor.Black && (index - boardOffset == -7 || index - boardOffset == -9))))
                                return true;
                        }
                        // Rook
                        else if ((j == 0 || j == 2) && (piece & (position.boards[3] & dangerPieces)) > 0)
                            return true;
                    }
                    else
                    {
                        index = getMSBIndex(blocker);

                        ulong piece = ((ulong)0x1 << index);

                        // Deal with queen
                        if ((piece & position.boards[4] & dangerPieces) > 0)
                            return true;

                        // Bishop/pawns
                        if ((j == 3 || j == 5))
                        {
                            if ((piece & (position.boards[2] & dangerPieces)) > 0)
                                return true;
                            else if ((piece & (position.boards[0] & dangerPieces)) > 0 && ((color == PieceColor.White && (index - boardOffset == 7 || index - boardOffset == 9)) ||
                                (color == PieceColor.Black && (index - boardOffset == -7 || index - boardOffset == -9))))
                                return true;
                        }
                        // Rook
                        else if ((j == 4 || j == 6) && (piece & (position.boards[3] & dangerPieces)) > 0)
                            return true;
                    }
                }
            }
            
            return false;
        }

        private static void getRayPieceAttackMask(Position position, PieceColor color, List<AttackMask> attacks, int start, int increment, int typeIndex)
        {
            ulong sameColor = position.boards[6];
            ulong opponentColor = position.boards[7];
            ulong bothColor = sameColor + opponentColor;

            if (color == PieceColor.Black)
            {
                sameColor = position.boards[7];
                opponentColor = position.boards[6];
            }

            ulong pieceType = position.boards[typeIndex] & sameColor;

            if (pieceType == 0)
                return;

            while (pieceType > 0x0)
            {
                int i = getLSBIndex(pieceType);

                ulong attackMask = 0;

                for (int j = start; j < 8; j += increment)
                {
                    ulong ray = vectors[j, i];
                    ulong blocker = (ray & ~bothColor) ^ ray;

                    if (blocker > 0)
                    {
                        int index;

                        if (j < 3 || j > 6)
                        {
                            index = getLSBIndex(blocker);
                            ray &= ~vectors[j, index];
                        }
                        else
                        {
                            index = getMSBIndex(blocker);
                            ray &= ~vectors[j, index];
                        }

                        blocker = (ulong)0x1 << index;

                        if ((blocker & sameColor) > 0)
                            ray = ray & ~blocker;
                    }

                    attackMask += ray;
                }

                attacks.Add(new AttackMask(attackMask, (ulong)0x1 << i, (byte)typeIndex));
                pieceType -= (ulong)0x1 << i;
            }
        }

        private static string pieceToString(PieceColor color, Piece piece)
        {
            string final = "";

            switch (piece)
            {
                case Piece.King:
                    final = "k";
                    break;
                case Piece.Queen:
                    final = "q";
                    break;
                case Piece.Rook:
                    final = "r";
                    break;
                case Piece.Bishop:
                    final = "b";
                    break;
                case Piece.Knight:
                    final = "n";
                    break;
                case Piece.Pawn:
                    final = "p";
                    break;
            }

            if (color == PieceColor.White)
            {
                return final.ToUpper();
            }
            else if (color == PieceColor.Black)
                return final;
            else
                return "E";
        }

        public static void createInitialPosition(Position board)
        {
            // Pawns

            ulong hash = 0;

            hash ^= getPieceSquareHashNum(0, 3, PieceColor.White);
            hash ^= getPieceSquareHashNum(1, 1, PieceColor.White);
            hash ^= getPieceSquareHashNum(2, 2, PieceColor.White);
            hash ^= getPieceSquareHashNum(3, 4, PieceColor.White);
            hash ^= getPieceSquareHashNum(4, 5, PieceColor.White);
            hash ^= getPieceSquareHashNum(5, 2, PieceColor.White);
            hash ^= getPieceSquareHashNum(6, 1, PieceColor.White);
            hash ^= getPieceSquareHashNum(7, 3, PieceColor.White);

            for (int i = 8; i <= 15; i++ )
            {
                hash ^= getPieceSquareHashNum(i, 0, PieceColor.White);
            }

            hash ^= getPieceSquareHashNum(56, 3, PieceColor.Black);
            hash ^= getPieceSquareHashNum(57, 1, PieceColor.Black);
            hash ^= getPieceSquareHashNum(58, 2, PieceColor.Black);
            hash ^= getPieceSquareHashNum(59, 4, PieceColor.Black);
            hash ^= getPieceSquareHashNum(60, 5, PieceColor.Black);
            hash ^= getPieceSquareHashNum(61, 2, PieceColor.Black);
            hash ^= getPieceSquareHashNum(62, 1, PieceColor.Black);
            hash ^= getPieceSquareHashNum(62, 3, PieceColor.Black);

            for (int i = 48; i <= 55; i++)
            {
                hash ^= getPieceSquareHashNum(i, 0, PieceColor.Black);
            }


            for (int i = 0; i < 8; i++)
            {
                board.setPiece(Piece.Pawn, PieceColor.White, new Point(i, 1));
                board.setPiece(Piece.Pawn, PieceColor.Black, new Point(i, 6));
            }

            board.setPiece(Piece.Rook, PieceColor.White, new Point(0, 0));
            board.setPiece(Piece.Rook, PieceColor.White, new Point(7, 0));
            board.setPiece(Piece.Rook, PieceColor.Black, new Point(0, 7));
            board.setPiece(Piece.Rook, PieceColor.Black, new Point(7, 7));

            board.setPiece(Piece.Knight, PieceColor.White, new Point(1, 0));
            board.setPiece(Piece.Knight, PieceColor.White, new Point(6, 0));
            board.setPiece(Piece.Knight, PieceColor.Black, new Point(1, 7));
            board.setPiece(Piece.Knight, PieceColor.Black, new Point(6, 7));

            board.setPiece(Piece.Bishop, PieceColor.White, new Point(2, 0));
            board.setPiece(Piece.Bishop, PieceColor.White, new Point(5, 0));
            board.setPiece(Piece.Bishop, PieceColor.Black, new Point(2, 7));
            board.setPiece(Piece.Bishop, PieceColor.Black, new Point(5, 7));

            board.setPiece(Piece.Queen, PieceColor.White, new Point(3, 0));
            board.setPiece(Piece.Queen, PieceColor.Black, new Point(3, 7));

            board.setPiece(Piece.King, PieceColor.White, new Point(4, 0));
            board.setPiece(Piece.King, PieceColor.Black, new Point(4, 7));
        }

        public static void printBitboard(ulong bitboard)
        {
            StringBuilder b = new StringBuilder();
            StringBuilder l = new StringBuilder();

            for (int i = 0; i < 64; i++)
            {
                if (i % 8 == 0)
                {
                    b.Insert(0, l.ToString() + Environment.NewLine);
                    l.Clear();
                }

                if ((bitboard & 1) == 1)
                    l.Append(" x ");
                else 
                    l.Append(" - ");

                bitboard >>= 1;
            }

            b.Insert(0, l.ToString() + Environment.NewLine);

            Console.WriteLine(b.ToString());
        }

        public static int oldGetLSBIndex(ulong mask)
        {
            int index = 32;
            int incriment = 16;

            for (int i = 1; i <= 6; i++)
            {
                if (mask << (index - 1) == 0x8000000000000000)
                    return (64 - index);

                if (mask << index == 0)
                    index = index - incriment;
                else
                    index = index + incriment;

                incriment /= 2;
            }

            return (63 - index);
        }
        
        private static ulong lowCut(ulong bitboard, int index)
        {
            bitboard >>= index;
            bitboard <<= index;
            return bitboard;
        }

        private static ulong highCut(ulong bitboard, int index)
        {
            index = 63 - index;
            bitboard <<= index;
            bitboard >>= index;
            return bitboard;
        }

        public static int getLSBIndex(ulong number)
        {
            if (number == 0)
                return 0;

            uint bottom = (uint)(number & 0xffffffff);

            if (bottom != 0)
                return bitPosition(bottom);
            else
            {
                uint top = (uint)((number >> 32) & 0xffffffff);
                return bitPosition(top) + 32;
            }
        }
	    /// <summary>
	    /// Returns the first set bit (FFS), or 0 if no bits are set.
	    /// </summary>
	    private static int bitPosition(uint number)
	    {
	        uint res = unchecked((uint)(number & -number) * 0x077CB531U) >> 27;
	        return _positions[res];
	    }

        //public static int getMSBIndex(ulong bitBoard)
        //{
        //    return (int)Math.Log(bitBoard, 2);
        //}

        private static int dMSB(uint v)
        {
            v |= v >> 1; // first round down to one less than a power of 2 
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;

            return MultiplyDeBruijnBitPosition[(v * 0x07C4ACDDU) >> 27];
        }

        public static int getMSBIndex(ulong number)
        {
            if (number == 0)
                return 0;

            uint top = (uint)((number >> 32) & 0xffffffff);

            if (top != 0)
                return dMSB(top) + 32;
            else
            {
                uint bottom = (uint)(number & 0xffffffff);
                return dMSB(bottom);
            }
        }
    }
}
