using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace ChessEnginePort
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Pedant";
            Console.ForegroundColor = ConsoleColor.Green;

            Eval.init();

            ChessBoard.init();
            BitEval.init();

            ChessBoard.Position pos = new ChessBoard.Position(null, 0, true);
            ChessBoard.createInitialPosition(pos);

            Crawler crawler = new Crawler(pos);

            List<ChessBoard.Move> moves = new List<ChessBoard.Move>();

            //Console.WriteLine(crawler.currentPosition.whiteToMove);
            //ulong newMask = ((ulong)0x1 << 31) + ((ulong)0x1 << 38);
            //ChessBoard.Move nextMove = new ChessBoard.Move(newMask, 0, 0);
            //ChessBoard.printBitboard(crawler.currentPosition.boards[0]);
            //crawler.move(nextMove);       

            ChessBoard.PieceColor color = ChessBoard.PieceColor.White;

            Console.WriteLine("UCI mode debruijn Capture First");

            Crawler b = new Crawler(pos);

            while (true)
            {
                string input = Console.ReadLine();

                Console.WriteLine("UCI Command: {0}", input);

                if (input.StartsWith("position startpos"))
                {
                    // Cleanup move string
                    input = input.Replace("position startpos", "");
                    input = input.Replace(" moves ", "");
                    input = input.Replace(" ", "");

                    //Dictionary<ulong, int> positionCount = b.positionCount;
                    b = new Crawler(ChessBoard.parseMoveString(input));
                    //b.positionCount = positionCount;
                }

                else if (input.StartsWith("go movetime "))
                {
                    input = input.Replace("go movetime ", "");

                    Evaluation evaluation = NegaMax.iterativeDeepeningMTDF(b, b.currentPosition.whiteToMove ? ChessBoard.PieceColor.White : ChessBoard.PieceColor.Black, moves, 100, Convert.ToInt32(input));

                    Console.WriteLine("bestmove " + ChessBoard.movesToString(b, new ChessBoard.Move[] { evaluation.line[0] }) + " Line: " + ChessBoard.movesToString(b, evaluation.line) + " Evaluation: " + evaluation.score);
                }
            }

            //ulong population = pos.boards[0] & pos.boards[6];

            //Console.WriteLine(" BIT " + ChessBoard.getMSBIndex(population));

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //for (ulong i = 0; i < 1000000; i++)
            //{
            //    int oldPopCount = ChessBoard.getMSBIndex(population);
            //    //int newPopCount = BitEval.newPopulationCount(i);
            //}
            //sw.Stop();
            //Console.WriteLine("Time : " + sw.ElapsedMilliseconds);

            //pos.whiteToMove = false;

            //pos.setPiece(ChessBoard.Piece.Empty, ChessBoard.PieceColor.Null, new Point(4, 6));
            //pos.setPiece(ChessBoard.Piece.Rook, ChessBoard.PieceColor.White, new Point(4, 2));

            //Console.WriteLine(ChessBoard.kingInDanger(pos));

            //ChessBoard.printPosition(pos);
            //Console.ReadLine();

            ChessBoard.Move testMove = new ChessBoard.Move(((ulong)0x1 << 3) + ((ulong)0x1 << 27), 0, 4);
            crawler.move(testMove);

            testMove = new ChessBoard.Move(((ulong)0x1 << 57) + ((ulong)0x1 << 42), 0, 1);
            crawler.move(testMove);

            //testMove = new ChessBoard.Move(((ulong)0x1 << 35) + ((ulong)0x1 << 45), 0, 4);
            //crawler.move(testMove);

            testMove = new ChessBoard.Move(((ulong)0x1 << 2) + ((ulong)0x1 << 20), 0, 2);
            crawler.move(testMove);

            //testMove = new ChessBoard.Move(((ulong)0x1 << 14) + ((ulong)0x1 << 38), 0, 0);
            //crawler.move(testMove);

            //List<ChessBoard.AttackMask> testAttacks = new List<ChessBoard.AttackMask>();
            //List<ChessBoard.Move> testMoves;

            //testMove = new ChessBoard.Move(((ulong)0x1 << 2) + ((ulong)0x1 << 20), 0, 2);
            //crawler.move(testMove);

            ChessBoard.printPosition(crawler.currentPosition);
            Console.WriteLine("EVAL: " + BitEval.evaluate(crawler, ChessBoard.PieceColor.White, -1000, 1000));

            //testMove = new ChessBoard.Move(((ulong)0x1 << 47) + ((ulong)0x1 << 39), 0, 0);
            //crawler.move(testMove);

            //testMove = new ChessBoard.Move(((ulong)0x1 << 3) + ((ulong)0x1 << 35), 0, 4);
            //crawler.move(testMove);

            //testMove = new ChessBoard.Move(((ulong)0x1 << 39) + ((ulong)0x1 << 31), 0, 0);
            //crawler.move(testMove);

            //testMove = new ChessBoard.Move(((ulong)0x1 << 4) + ((ulong)0x1 << 3), 0, 5);
            //crawler.move(testMove);

            //testMove = new ChessBoard.Move(((ulong)0x1 << 55) + ((ulong)0x1 << 47), 0, 0);
            //crawler.move(testMove);

            //ChessBoard.printPosition(crawler.currentPosition);

            //ChessBoard.getKingAttackMask(crawler.currentPosition, ChessBoard.PieceColor.White, testAttacks);
            //testMoves = ChessBoard.getMoves(crawler.currentPosition, testAttacks);

            //foreach (ChessBoard.Move move in testMoves)
            //{
            //    Console.WriteLine(move.deltaValue);
            //    ChessBoard.printBitboard(move.mask);
            //}

            //Console.ReadLine();

            //Evaluation mtdf = NegaMax.mtdf(crawler, color, moves, 5, 2, 0);
            //Evaluation negaMax = NegaMax.iterativeDeepening(crawler, color, moves, 5, 2);
            //Console.WriteLine(mtdf.score + " " + ChessBoard.movesToString(crawler, mtdf.line));
            //Console.WriteLine(negaMax.score + " " + ChessBoard.movesToString(crawler, negaMax.line));
            //ChessBoard.printBitboard(ChessBoard.stringToMove(crawler.currentPosition, "a1a2").mask);

            Console.WriteLine(crawler.currentPosition.whiteToMove);

            while (true)
            {
                Console.Write("Move? ");
                string move = Console.ReadLine();
                crawler.move(ChessBoard.stringToMove(crawler.currentPosition, move.Trim()));

                Evaluation bestMove = NegaMax.iterativeDeepening(crawler, ChessBoard.PieceColor.Black, moves, 1000, 5000);
                //Evaluation bestMove = NegaMax.search(crawler, ChessBoard.PieceColor.White, moves, 6, 2, -10000, 10000);
                
                Console.WriteLine();
                string line = ChessBoard.movesToString(crawler, bestMove.line);
                crawler.move(bestMove.line[0]);
                ChessBoard.printPosition(crawler.currentPosition);

                Console.WriteLine("Line: " + line);
                Console.WriteLine("Forcast: " + bestMove.score);
                Console.WriteLine("Score: " + BitEval.evaluate(crawler, ChessBoard.PieceColor.White, 0, 0));
            }


            // MTD-F vs NegaScout
            ChessBoard.PieceColor side = ChessBoard.PieceColor.White;
            Evaluation nextMove;

            Random r = new Random();

            while (true)
            {
                if (side == ChessBoard.PieceColor.White)
                {
                    nextMove = NegaMax.iterativeDeepening(crawler, ChessBoard.PieceColor.White, moves, 20, r.Next(200, 500));
                    side = ChessBoard.PieceColor.Black;
                }
                else
                {
                    nextMove = NegaMax.iterativeDeepening(crawler, ChessBoard.PieceColor.Black, moves, 20, r.Next(500, 1000));
                    side = ChessBoard.PieceColor.White;
                }

                Console.WriteLine();
                string line = ChessBoard.movesToString(crawler, nextMove.line);

                if (nextMove.line.Count() == 0)
                    break;
                crawler.move(nextMove.line[0]);
                ChessBoard.printPosition(crawler.currentPosition);

                Console.WriteLine("Line: " + line);
                Console.WriteLine("Forcast: " + nextMove.score);
                Console.WriteLine("Score: " + BitEval.evaluate(crawler, ChessBoard.PieceColor.White, 0, 0));
            }

            Console.WriteLine("CHECKMATE");
            Console.ReadLine();

            // UCI mode
            //Console.WriteLine("UCI mode");

            //Thread ponderThread = null;

            //Board b = null;

            //while (true)
            //{
            //    string input = Console.ReadLine();

            //    Console.WriteLine("UCI Command: {0}", input);

            //    if (input.StartsWith("position startpos"))
            //    {
            //        b = new Board();
            //        b.createInitialPosition();

            //        // Cleanup move string
            //        input = input.Replace("position startpos", "");
            //        input = input.Replace(" moves ", "");
            //        input = input.Replace(" ", "");
            //        b.parseMoveString(input);
            //    }

            //    else if (input.StartsWith("go movetime "))
            //    {
            //        if (ponderThread != null && ponderThread.IsAlive)
            //        {
            //            SearchTree.abortPonder();
            //            ponderThread.Join();
            //        }

            //        input = input.Replace("go movetime ", "");
            //        Evaluation evaluation = SearchTree.getMoveTree(b, 20, 2, 4, b.whiteToMove ? 'w' : 'b', Convert.ToInt32(input), true);

            //        Console.WriteLine("bestmove " + evaluation.line.Substring(0, 4) + " Line: " + evaluation.line + " Evaluation: " + evaluation.score);

            //        // Duplicate board
            //        Board duplicate = new Board();
            //        duplicate.createInitialPosition();
            //        duplicate.parseMoveString(b.moves);
            //        duplicate.moveS(evaluation.line.Substring(0, 4));

            //        ponderThread = new Thread(SearchTree.ponder);
            //        ponderThread.Start(duplicate);
            //    }
            //}
             //End UCI mode

            int depth = 20;
            int time = 10000;

            //Thread ponderThread1 = null;

            //while (true)
            //{
            //    Console.WriteLine("Move? ");
            //    string response = Console.ReadLine();

            //    if (response.StartsWith("time:"))
            //    {
            //        time = Convert.ToInt32(response.Split(':')[1]);
            //        continue;
            //    }

            //    else if (response.StartsWith("depth:"))
            //    {
            //        depth = Convert.ToInt32(response.Split(':')[1]);
            //    }

            //    //Ponder
            //    if (ponderThread1 != null && ponderThread1.IsAlive)
            //    {
            //        SearchTree.abortPonder();
            //        ponderThread1.Join();
            //    }

            //    board.moveS(response);
            //    //Evaluation eval = SearchTree.getMove(board, depth, 0, 'b');
            //    //Evaluation eval = negaMax(board, depth, 3, "", 'b', -1000, 1000);
            //    Evaluation eval = SearchTree.getMoveTree(board, depth, 1, 3, 'b', time, true);
            //    board.moveS(eval.line.Substring(0, 4));
            //    Console.WriteLine("Move: " + eval.line.Substring(0, 4));
            //    board.printBoard();
            //    Console.WriteLine("Score: " + eval.score + " Line: " + eval.line);
            //    System.Windows.Forms.Clipboard.SetText(board.moves);

            //    // Ponder

            //    ponderThread1 = new Thread(SearchTree.ponder);
            //    ponderThread1.Start(board);
            //}

        //    while (true)
        //    {
        //        Evaluation eval = SearchTree.getMoveTree(crawler, depth, 1, 2, ChessBoard.PieceColor.White, time, true);
        //        crawler.moveS(eval.line.Substring(0, 4));
        //        Console.WriteLine("Move: " + eval.line.Substring(0, 4));
        //        board.printBoard();
        //        Console.WriteLine("Score: " + eval.score + " Line: " + eval.line);

        //        Console.WriteLine("Move? ");
        //        string response = Console.ReadLine();

        //        if (response.StartsWith("time:"))
        //        {
        //            time = Convert.ToInt32(response.Split(':')[1]);
        //            continue;
        //        }

        //        else if (response.StartsWith("depth:"))
        //        {
        //            depth = Convert.ToInt32(response.Split(':')[1]);
        //        }

        //        board.moveS(response);
        //        //Evaluation eval = SearchTree.getMove(board, depth, 0, 'b');
        //        //Evaluation eval = negaMax(board, depth, 3, "", 'b', -1000, 1000);
        //        System.Windows.Forms.Clipboard.SetText(board.moves);
        //    }
        }
    }
}
