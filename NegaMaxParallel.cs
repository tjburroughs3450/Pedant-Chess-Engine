using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace ChessEnginePort
{
    static class NegaMaxParallel
    {
        public static Dictionary<ulong, Transposition> transpositions = new Dictionary<ulong, Transposition>();
        public static Dictionary<ulong, Transposition> writeTranspositions = new Dictionary<ulong, Transposition>();
        private static Dictionary<int, ChessBoard.Move[]> killerMoves = new Dictionary<int, ChessBoard.Move[]>();
        
        private static Thread[] threads = new Thread[3];//Environment.ProcessorCount - 1];
        private static object resultLock = new object();
        private static Evaluation threadEval = new Evaluation();

        public static Evaluation previousScore = new Evaluation(0, new ChessBoard.Move[1]);

        private static int timeCut = 0;

        private static Stopwatch sw = new Stopwatch();

        public static Evaluation iterativeDeepening(Crawler crawler, ChessBoard.PieceColor color, List<ChessBoard.Move> moves, int depth, int time)
        {
            timeCut = time;
            Evaluation bestEval = search(crawler, color, moves, 1, 0, -10000, 10000);
            Console.Write("Depth SCOUT: ");
            sw.Start();
            for (int i = 2; i <= depth; i++)
            {
                Evaluation testEval = search(crawler, color, moves, i, 0, -10000, 10000);

                if (sw.ElapsedMilliseconds < timeCut)
                    bestEval = testEval;
                else
                    break;

                Console.Write(i + " ");
            }
            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("Eval/s : " + BitEval.evaluations / (sw.ElapsedMilliseconds / 1000f));
            Console.WriteLine("Transposition: " + transpositions.Count);
            sw.Reset();
            BitEval.evaluations = 0;

            transpositions.Clear();
            killerMoves.Clear();

            previousScore = bestEval;

            return bestEval;
        }

        public static Evaluation iterativeDeepeningMTDF(Crawler crawler, ChessBoard.PieceColor color, List<ChessBoard.Move> moves, int depth, int time)
        {
            timeCut = time;
            Evaluation bestEval = mtdf(crawler, color, moves, 1, previousScore.score);
            Console.Write("Depth MTDF : ");

            Crawler[] crawlers = new Crawler[threads.Length];
            crawlers[0] = crawler;

            for (int i = 1; i < crawlers.Length; i++)
                crawlers[i] = crawler.duplicate();

            sw.Start();
            
            for (int i = 2; i <= depth; i++)
            {
                ManualResetEvent mre = new ManualResetEvent(false);

                for (int j = 0; j < threads.Length; j++)
                {
                    threads[j] = new Thread(() =>
                        {
                            Evaluation testEval = mtdf(crawlers[j], color, moves, i, bestEval.score);

                            if (sw.ElapsedMilliseconds < timeCut)
                            {
                                lock (resultLock)
                                {
                                    threadEval = testEval;
                                }
                            }

                            mre.Set();
                        });

                    threads[j].Start();
                }

                mre.WaitOne();

                foreach (Thread thread in threads)
                    thread.Abort();

                Console.Write(i + " ");

                bestEval = threadEval;
            }

            sw.Stop();
            Console.WriteLine();
            Console.WriteLine("Eval/s : " + BitEval.evaluations / (sw.ElapsedMilliseconds / 1000f));
            Console.WriteLine("Transposition: " + transpositions.Count);
            sw.Reset();
            BitEval.evaluations = 0;

            transpositions.Clear();
            killerMoves.Clear();

            previousScore = bestEval;

            return bestEval;
        }

        public static Evaluation mtdf(Crawler crawler, ChessBoard.PieceColor color, List<ChessBoard.Move> moves, int depth, int guess)
        {
            Evaluation g = new Evaluation(guess, moves.ToArray());

            int lowerBound = -10000;
            int upperBound = 10000;
            int beta;

            while (upperBound > lowerBound)
            {
                // Time cut
                if (timeCut < sw.ElapsedMilliseconds)
                    return g;

                if (g.score == lowerBound)
                    beta = g.score + 1;
                else
                    beta = g.score;

                g = alphaBetaWithMemory(crawler, color, moves, depth, 0, beta - 1, beta);

                if (g.score < beta)
                    upperBound = g.score;
                else
                    lowerBound = g.score;
            }

            return g;
        }


        public static Evaluation alphaBetaWithMemory(Crawler crawler, ChessBoard.PieceColor color, List<ChessBoard.Move> moves, int depth, int ply, int alpha, int beta)
        {
            //Time
            if (sw.ElapsedMilliseconds > timeCut)
                return new Evaluation(0, moves.ToArray());

            ulong hash = crawler.hash;
            ChessBoard.Move pvMove = new ChessBoard.Move(0, 0, 0);

            // Transpositions
            lock (transpositions)
            {
                if (transpositions.ContainsKey(hash))
                {
                    Transposition transposition = transpositions[hash];

                    if (transposition.depth >= depth)
                    {
                        if (transposition.upperBound <= alpha)
                            return new Evaluation(transposition.upperBound, moves.ToArray());

                        else if (transposition.lowerBound >= beta)
                            return new Evaluation(transposition.lowerBound, moves.ToArray());
                        else
                        {
                            if (transposition.lowerBound > alpha)
                                alpha = transposition.lowerBound;

                            if (transposition.upperBound < beta)
                                beta = transposition.upperBound;

                            if (alpha == beta)
                                return new Evaluation(transposition.lowerBound, moves.ToArray());
                        }

                        pvMove = transposition.pvMove;
                    }
                }
            }

            int initialAlpha = alpha;
            int initialBeta = beta;

            // Base Case
            if (depth == 0)
            {
                Evaluation score = new Evaluation(BitEval.evaluate(crawler, color, alpha, beta), moves.ToArray());
                return score;
            }

            // NegaMax
            int negate = -1;

            if ((crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.White) || (!crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.Black))
                negate = 1;

            Evaluation max = new Evaluation(-1000 * negate, moves.ToArray());

            ChessBoard.Move newPVMove;

            List<ChessBoard.Move> nextMoves = ChessBoard.getMoves(crawler.currentPosition);

            //if (pvMove.mask > 0)
            //{
            //    for (int i = 0; i < nextMoves.Count; i++)
            //    {
            //        if (nextMoves[i].mask == pvMove.mask)
            //        {
            //            ChessBoard.Move move = nextMoves[i];
            //            move.deltaValue = 1000;
            //            nextMoves[i] = move;
            //            break;
            //        }
            //    }
            //}

            bool getKillers = killerMoves.ContainsKey(ply);

            ChessBoard.Move[] killers = null;

            if (getKillers)
                killers = killerMoves[ply];

            if (getKillers || pvMove.mask > 0)
            {
                for (int i = 0; i < nextMoves.Count; i++)
                {
                    if (nextMoves[i].mask == pvMove.mask)
                    {
                        ChessBoard.Move move = nextMoves[i];
                        move.deltaValue = 1000;
                        nextMoves[i] = move;
                    }

                    else if (getKillers)
                    {
                        if (killers[0] == nextMoves[i])
                        {
                            ChessBoard.Move move = nextMoves[i];
                            move.deltaValue = 500;
                            nextMoves[i] = move;
                        }

                        else if (killers[1] == nextMoves[i])
                        {
                            ChessBoard.Move move = nextMoves[i];
                            move.deltaValue = 499;
                            nextMoves[i] = move;
                        }
                    }
                }
            }
            // END OF KILLER MOVE

            // Checkmate check
            if (nextMoves.Count == 0)
            {
                ChessBoard.PieceColor kingColor = ChessBoard.PieceColor.Black;

                if (crawler.currentPosition.whiteToMove)
                    kingColor = ChessBoard.PieceColor.White;

                if (ChessBoard.kingInDanger(crawler.currentPosition, kingColor))
                    return new Evaluation((-10000 - depth) * negate, moves.ToArray());
                else
                    return new Evaluation(0, moves.ToArray());
            }

            nextMoves.Sort((s2, s1) => s1.deltaValue.CompareTo(s2.deltaValue));

            foreach (ChessBoard.Move move in nextMoves)
            {
                crawler.move(move);
                moves.Add(move);
                Evaluation moveScore = alphaBetaWithMemory(crawler, color, moves, depth - 1, ply + 1, alpha, beta);
                crawler.undoMove();
                moves.RemoveAt(moves.Count - 1);

                if (moveScore.score * negate > max.score * negate)
                {
                    max = moveScore;
                    newPVMove = move;

                    if (negate == 1)
                    {
                        if (max.score > alpha)
                            alpha = max.score;
                    }
                    else
                    {
                        if (max.score < beta)
                            beta = max.score;
                    }

                    if (alpha >= beta)
                    {
                        if (!killerMoves.ContainsKey(ply))
                            killerMoves.Add(ply, new ChessBoard.Move[2]);

                        if (!killerMoves[ply].Contains(move))
                        {
                            killerMoves[ply][1] = killerMoves[ply][0];
                            killerMoves[ply][0] = move;
                        }

                        break;
                    }
                }
            }

            // Transpositions
            Transposition oldTransposition = new Transposition(-10000, 10000, (byte)depth, (byte)0, pvMove);

            lock (transpositions)
            {
                if (transpositions.ContainsKey(hash))
                {
                    //oldTransposition = transpositions[hash];
                    //oldTransposition.depth = (byte)depth;
                    //oldTransposition.pvMove = pvMove;
                    transpositions.Remove(hash);
                }

                if (max.score <= initialAlpha)
                {
                    oldTransposition.upperBound = max.score;
                }

                else if (max.score >= initialBeta)
                    oldTransposition.lowerBound = max.score;

                else
                {
                    oldTransposition.upperBound = max.score;
                    oldTransposition.lowerBound = max.score;
                }

                transpositions.Add(hash, oldTransposition);
            }

            return max;
        }

        public static Evaluation search(Crawler crawler, ChessBoard.PieceColor color, List<ChessBoard.Move> moves, int depth, int ply, int alpha, int beta)
        {
            //Time
            if (sw.ElapsedMilliseconds > timeCut)
                return new Evaluation(0, moves.ToArray());

            ulong hash = crawler.hash;
            ChessBoard.Move pvMove = new ChessBoard.Move(0, 0, 0);

            // Transpositions
            if (transpositions.ContainsKey(hash))
            {
                Transposition transposition = transpositions[hash];

                if (transposition.depth >= depth)
                {
                    if (transposition.upperBound <= alpha)
                        return new Evaluation(transposition.upperBound, moves.ToArray());

                    else if (transposition.lowerBound >= beta)
                        return new Evaluation(transposition.lowerBound, moves.ToArray());
                    else
                    {
                        if (transposition.lowerBound > alpha)
                            alpha = transposition.lowerBound;

                        if (transposition.upperBound < beta)
                            beta = transposition.upperBound;

                        if (alpha == beta)
                            return new Evaluation(transposition.lowerBound, moves.ToArray());
                    }

                    pvMove = transposition.pvMove;
                }
            }

            int initialAlpha = alpha;
            int initialBeta = beta;

            // Base Case
            if (depth == 0)
            {
                Evaluation score = new Evaluation(BitEval.evaluate(crawler, color, alpha, beta), moves.ToArray());

                return score;
            }

            // NegaMax
            int negate = -1;

            if ((crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.White) || (!crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.Black))
                negate = 1;

            Evaluation max = new Evaluation(-1000 * negate, moves.ToArray());

            List<ChessBoard.Move> nextMoves = ChessBoard.getMoves(crawler.currentPosition);

            // Checkmate check
            if (nextMoves.Count == 0)
            {
                ChessBoard.PieceColor kingColor = ChessBoard.PieceColor.Black;

                if (crawler.currentPosition.whiteToMove)
                    kingColor = ChessBoard.PieceColor.White;

                if (ChessBoard.kingInDanger(crawler.currentPosition, kingColor))
                    return new Evaluation((-10000 - depth) * negate, moves.ToArray());
                else
                    return new Evaluation(0, moves.ToArray());
            }

            // Optimization
            bool getKillers = killerMoves.ContainsKey(ply);

            ChessBoard.Move[] killers = null;

            if (getKillers)
                killers = killerMoves[ply];

            if (getKillers || pvMove.mask > 0)
            {
                for (int i = 0; i < nextMoves.Count; i++)
                {
                    if (nextMoves[i].mask == pvMove.mask)
                    {
                        ChessBoard.Move move = nextMoves[i];
                        move.deltaValue = 1000;
                        nextMoves[i] = move;
                    }

                    else if (getKillers)
                    {
                        if (killers[0] == nextMoves[i])
                        {
                            ChessBoard.Move move = nextMoves[i];
                            move.deltaValue = 500;
                            nextMoves[i] = move;
                        }

                        else if (killers[1] == nextMoves[i])
                        {
                            ChessBoard.Move move = nextMoves[i];
                            move.deltaValue = 499;
                            nextMoves[i] = move;
                        }
                    }
                }
            }

            nextMoves.Sort((s2, s1) => s1.deltaValue.CompareTo(s2.deltaValue));

            ChessBoard.Move newPVMove;
            bool zeroWindow = true;

            for (int i = 0; i < nextMoves.Count; i++)
            {
                ChessBoard.Move move = nextMoves[i];
                if (i != 0 && zeroWindow)
                {
                    crawler.move(move);
                    int bound = zeroWindowSearch(crawler, color, depth - 1, max.score);
                    crawler.undoMove();

                    if (negate == 1)
                    {
                        if (bound >= max.score)
                            zeroWindow = false;
                        else
                            continue;
                    }
                    else
                    {
                        if (bound < max.score)
                            zeroWindow = false;
                        else
                            continue;
                    }
                }

                crawler.move(move);
                moves.Add(move);
                Evaluation moveScore = search(crawler, color, moves, depth - 1, ply + 1, alpha, beta);
                crawler.undoMove();
                moves.RemoveAt(moves.Count - 1);

                if (moveScore.score * negate > max.score * negate)
                {
                    max = moveScore;
                    newPVMove = move;

                    if (negate == 1)
                    {
                        if (max.score > alpha)
                            alpha = max.score;
                    }
                    else
                    {
                        if (max.score < beta)
                            beta = max.score;
                    }

                    if (alpha >= beta)
                    {
                        if (!killerMoves.ContainsKey(ply))
                            killerMoves.Add(ply, new ChessBoard.Move[2]);

                        if (!killerMoves[ply].Contains(move))
                        {
                            killerMoves[ply][1] = killerMoves[ply][0];
                            killerMoves[ply][0] = move;
                        }

                        break;
                    }
                }
            }

            // Transpositions
            Transposition oldTransposition = new Transposition(-10000, 10000, (byte)depth, 0, pvMove);

            if (transpositions.ContainsKey(hash))
            {
                //oldTransposition = transpositions[hash];
                //oldTransposition.depth = (byte)depth;
                //oldTransposition.pvMove = pvMove;
                transpositions.Remove(hash);
            }

            if (max.score <= initialAlpha)
            {
                oldTransposition.upperBound = max.score;
            }

            else if (max.score >= initialBeta)
                oldTransposition.lowerBound = max.score;

            else
            {
                oldTransposition.upperBound = max.score;
                oldTransposition.lowerBound = max.score;
            }

            transpositions.Add(hash, oldTransposition);

            return max;
        }

        public static int zeroWindowSearch(Crawler crawler, ChessBoard.PieceColor color, int depth, int beta)
        {
            int alpha = beta - 1;

            ulong hash = crawler.hash;

            ChessBoard.Move pvMove = new ChessBoard.Move(0, 0, 0);

            // Transpositions
            if (transpositions.ContainsKey(hash))
            {
                Transposition transposition = transpositions[hash];

                if (transposition.depth >= depth)
                {
                    if (transposition.upperBound <= alpha)
                        return transposition.upperBound;

                    else if (transposition.lowerBound >= beta)
                        return transposition.lowerBound;
                    else
                    {
                        if (transposition.lowerBound > alpha)
                            alpha = transposition.lowerBound;

                        if (transposition.upperBound < beta)
                            beta = transposition.upperBound;

                        if (alpha == beta)
                            return transposition.lowerBound;
                    }

                    pvMove = transposition.pvMove;
                }
            }

            int initialAlpha = alpha;
            int initialBeta = beta;

            // Base Case
            if (depth == 0)
            {
                int score = BitEval.evaluate(crawler, color, alpha, beta);

                return score;
            }

            // NegaMax
            int negate = -1;

            if ((crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.White) || (!crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.Black))
                negate = 1;

            int max = -10000 * negate;

            List<ChessBoard.Move> nextMoves = ChessBoard.getMoves(crawler.currentPosition);

            if (pvMove.mask > 0)
            {
                for (int i = 0; i < nextMoves.Count; i++)
                {
                    if (nextMoves[i].mask == pvMove.mask)
                    {
                        ChessBoard.Move move = nextMoves[i];
                        move.deltaValue = 1000;
                        nextMoves[i] = move;
                        break;
                    }
                }
            }

            // Checkmate check
            if (nextMoves.Count == 0)
            {
                ChessBoard.PieceColor kingColor = ChessBoard.PieceColor.Black;
                if (crawler.currentPosition.whiteToMove)
                    kingColor = ChessBoard.PieceColor.White;

                if (ChessBoard.kingInDanger(crawler.currentPosition, kingColor))
                    return (-1000 - depth) * negate;
                else
                    return 0;
            }

            nextMoves.Sort((s2, s1) => s1.deltaValue.CompareTo(s2.deltaValue));

            foreach (ChessBoard.Move move in nextMoves)
            {
                crawler.move(move);
                int moveScore = zeroWindowSearch(crawler, color, depth - 1, beta);
                crawler.undoMove();

                if (moveScore * negate > max * negate)
                {
                    max = moveScore;

                    if (negate == 1)
                    {
                        if (max > alpha)
                            alpha = max;
                    }
                    else
                    {
                        if (max < beta)
                            beta = max;
                    }

                    if (alpha >= beta)
                    {
                        break;
                    }
                }
            }

            // Transpositions
            Transposition oldTransposition = new Transposition(-10000, 10000, (byte)depth, (byte)0, pvMove);

            if (transpositions.ContainsKey(hash))
            {
                //oldTransposition = transpositions[hash];
                //oldTransposition.depth = (byte)depth;
                //oldTransposition.pvMove = pvMove;
                transpositions.Remove(hash);
            }

            if (max <= initialAlpha)
            {
                oldTransposition.upperBound = max;
            }

            else if (max >= initialBeta)
                oldTransposition.lowerBound = max;

            transpositions.Add(hash, oldTransposition);
            return max;
        }

        public struct Transposition
        {
            public byte depth;
            public byte quiescence;
            public int lowerBound;
            public int upperBound;
            public ChessBoard.Move pvMove;

            public Transposition(int lowerBound, int upperBound, byte depth, byte quiescence, ChessBoard.Move pvMove)
            {
                this.lowerBound = lowerBound;
                this.upperBound = upperBound;
                this.depth = depth;
                this.quiescence = quiescence;
                this.pvMove = pvMove;
            }
        }
    }
}
