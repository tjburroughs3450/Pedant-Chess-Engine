//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Drawing;
//using System.Threading;

//namespace ChessEnginePort
//{
//    class SearchTree
//    {
//        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        
//        private static int cutoffTime = 0;

//        // Save some time
//        private static TreeNode lastSearchTree = new TreeNode(new ChessBoard.Move());

//        // TRANSPOSITIONS
//        private static Dictionary<string, Transposition> transpositions = new Dictionary<string, Transposition>();
//        private static int calls = 0;
//        private static bool enabledTranspositions = false;

//        // Zero Window Search
//        private static bool enabledZeroWindow = true;

//        private static int betaZero = 0;
//        private static int alphaZero = 0;

//        // Optimization
//        private static decimal rootScore = 0;
//        private static bool enableMaterialCut = false;

//        public static Evaluation getMoveTree(Crawler boardCrawler, int depth, int initialOverflow, int maxOverflow, ChessBoard.PieceColor color, int cutoff)
//        {
//            // Openings
//            //string[] openingLines = Properties.Resources.Openings.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

//            //if (board.moves.Length == 0)
//            //    return new Evaluation(0, openingLines[new Random().Next(0, openingLines.Length)]);

//            //foreach (string opening in openingLines)
//            //{
//            //    if (opening.StartsWith(board.moves) && opening.Length > board.moves.Length)
//            //        return new Evaluation(0, opening.Substring(board.moves.Length));
//            //}

//            cutoffTime = cutoff;

//            TreeNode root = new TreeNode(new ChessBoard.Move());

//            //Optimiztion
//            //rootScore = root.evaluation.score;

//            List<ChessBoard.Move> moves = new List<ChessBoard.Move>(20);
//            TreeNode bestNode = negaMaxTree(boardCrawler, 1, 1, moves, color, -1000, 1000, root);

//            Evaluation bestEval = new Evaluation(bestNode.evaluation.score, bestNode.evaluation.line);

//            // TRANSPOSITIONS
//            //calls = 0;
//            //transpositions.Clear();
//            //Eval.evaluations = 0;

//            Console.Write("Searched depth: ");

//            sw.Start();

//            for (int i = 2; i <= depth; i++)
//            {
//                bestNode = negaMaxTree(boardCrawler, i, (i >= 4 ? maxOverflow : initialOverflow), moves, color, -1000, 1000, root);
                
//                if (sw.ElapsedMilliseconds < cutoffTime)
//                {
//                    bestEval = new Evaluation(bestNode.evaluation.score, bestNode.evaluation.line);
//                    Console.Write(i + " ");
//                }

//                else
//                    break;
//            }

//            return bestEval;
//        }

//        //private static TreeNode newGetLastNode(TreeNode root, Board board)
//        //{
//        //    string lastMove = board.moves.Substring(board.moves.Length - 4);
//        //    Point[] move = board.stringToFullMove(lastMove);

//        //    foreach (TreeNode node in root.moveNodes)
//        //    {
//        //        if (node.moveTo.fromPos == move[0] && node.moveTo.toPos == move[1])
//        //        {
//        //            return node;
//        //        }
//        //    }
//        //    return new TreeNode(new Board.Move());
//        //}

//        //public static void ponder(object board)
//        //{
//        //    char color = ((Board)board).whiteToMove ? 'b' : 'w';
//        //    getMoveTree((Board)board, 100, 2, 3, color, 1000000000, false);
//        //}

//        //public static void abortPonder()
//        //{
//        //    cutoffTime = 0;
//        //}

//        //private static TreeNode getTreeNode(TreeNode currentNode, string targetMoveString)
//        //{
//        //    if (currentNode.position == null || currentNode.position.Length >= targetMoveString.Length)
//        //        return currentNode;
//        //    else
//        //    {
//        //        if (currentNode.moveNodes != null)
//        //        {
//        //            foreach (TreeNode child in currentNode.moveNodes)
//        //            {
//        //                if (targetMoveString.StartsWith(child.position))
//        //                    return getTreeNode(child, targetMoveString);
//        //            }
//        //        }

//        //        return new TreeNode(new Board.Move());
//        //    }
//        //}

//        private static TreeNode negaMaxTree(Crawler crawler, int depth, int overflowDepth, List<ChessBoard.Move> moves, ChessBoard.PieceColor color, int alpha, int beta, TreeNode currentNode)
//        {
//            // TRANSPOSITIONS
//            //string currentPos = board.getPositionString();
            
//            //Transposition transposition = new Transposition(null, -1, -1);

//            //if (transpositions.ContainsKey(currentPos) && enabledTranspositions)
//            //{
//            //    transposition = transpositions[currentPos];
//            //    transposition.position.evaluation.line = moveString;

//            //    if ((transposition.depth >= depth && transposition.overflowDepth >= overflowDepth) || transposition.overflowDepth > overflowDepth)
//            //    {
//            //        calls++;
//            //        return transposition.position;
//            //    }
//            //}

//            //currentNode.position = board.moves;

//            // TIME CUTOFF
//            if (sw.ElapsedMilliseconds >= cutoffTime)
//            {
//                currentNode.evaluation = new Evaluation(BitEval.evaluate(crawler.currentPosition, color, new List<ChessBoard.AttackMask>(250), alpha, beta), moves.ToArray());
//                return currentNode;
//            }
            
//            // BASE CASE
//            if (depth <= 0)
//            {
//                //if (overflowDepth > 0 && ((board.uncertainPosition()))
//                //    return negaMaxTree(board, 1, overflowDepth - 1, moveString, color, alpha, beta, currentNode);

//                if (currentNode.evaluated)
//                {
//                    currentNode.evaluation.line = moves.ToArray();
//                    return currentNode;
//                }

//                // TRANSPOSITIONS (check logic)
//                //if (enabledTranspositions && transposition.depth != -1)
//                //{
//                //    transpositions.Remove(currentPos);
//                //    transpositions.Add(currentPos, new Transposition(currentNode, depth, overflowDepth));
//                //}
//                currentNode.masks = new List<ChessBoard.AttackMask>(250);

//                currentNode.evaluation = new Evaluation(BitEval.evaluate(crawler.currentPosition, color, currentNode.masks, alpha, beta), moves.ToArray());
//                currentNode.evaluated = true;
//                return currentNode;
//            }
             
//            int negate = (crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.White) || (!crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.Black) ? 1 : -1;
            
//            TreeNode max = new TreeNode(new ChessBoard.Move());
//            max.evaluation = new Evaluation(-1000 * negate, moves.ToArray());
            
//            if (currentNode.moveNodes.Count <= 0)
//            {
//                List<ChessBoard.Move> nextMoves;

//                if (currentNode.masks.Count == 0)
//                    nextMoves = ChessBoard.getMoves(crawler.currentPosition, crawler.flags);
//                else
//                    nextMoves = ChessBoard.getMoves(crawler.currentPosition, currentNode.masks);

//                if (nextMoves.Count == 0)
//                {
                    
//                    if (ChessBoard.kingInDanger(crawler.currentPosition))
//                    {
//                        currentNode.evaluation.score = (500 + depth) * -1 * negate;
//                    }
                        
//                    else
//                        currentNode.evaluation.score = 0;

//                    currentNode.evaluation.line = moves.ToArray();
                    
//                    // TRANSPOSITIONS
//                    //if (enabledTranspositions)
//                    //{
//                    //    transpositions.Remove(currentPos);
//                    //    transpositions.Add(currentPos, new Transposition(currentNode, depth, overflowDepth));
//                    //}

//                    return currentNode;
//                }

//                foreach (ChessBoard.Move move in nextMoves)
//                {
//                    TreeNode newPos = new TreeNode(move);
//                    currentNode.moveNodes.Add(newPos);
//                }
//            }
            
//            //Zero Window
//            bool pvNode = true;

//            foreach (TreeNode node in currentNode.moveNodes)
//            {
//                crawler.move(node.moveTo);

//                TreeNode selectedNode = null;

//                if (!enabledZeroWindow)
//                    selectedNode = negaMaxTree(crawler, depth - 1, overflowDepth, moves, color, alpha, beta, node);

//                else if (pvNode)
//                {
//                    selectedNode = negaMaxTree(crawler, depth - 1, overflowDepth, moves, color, alpha, beta, node);
//                }
//                else
//                {
//                    if (negate == 1)
//                    {
//                        selectedNode = zeroWindowSearch(crawler, depth - 1, overflowDepth, moves, color, alpha, node);
//                        alphaZero++;
//                        if (selectedNode.evaluation.score >= alpha)
//                        {
//                            alphaZero--;
//                            selectedNode = negaMaxTree(crawler, depth - 1, overflowDepth, moves, color, alpha, beta, node);
//                        }
//                    }
//                    else
//                    {
//                        selectedNode = zeroWindowSearch(crawler, depth - 1, overflowDepth, moves, color, beta, node);
//                        betaZero++;
//                        if (selectedNode.evaluation.score <= beta)
//                        {
//                            selectedNode = negaMaxTree(crawler, depth - 1, overflowDepth, moves, color, alpha, beta, node);
//                            betaZero--;
//                        }
//                    }
//                }

//                pvNode = false;
//                crawler.undoMove();


//                if (selectedNode.evaluation.score * negate > max.evaluation.score * negate)
//                {
//                    max = selectedNode;

//                    currentNode.evaluation = max.evaluation;

//                    if (negate == 1)
//                        alpha = max.evaluation.score;
//                    else
//                        beta = max.evaluation.score;
//                }

//                if (alpha >= beta)
//                {
//                    currentNode.moveNodes = sortOptions(currentNode.moveNodes, negate);
//                    return max;
//                }
//            }

//            currentNode.moveNodes = sortOptions(currentNode.moveNodes, negate);

//            //if (transpositions.ContainsKey(currentPos) && enabledTranspositions)
//            //{
//            //    transpositions.Remove(currentPos);
//            //    transpositions.Add(currentPos, new Transposition(currentNode, depth, overflowDepth));
//            //}

//            return max;
//        }

//        private static TreeNode zeroWindowSearch(Crawler crawler, int depth, int overflowDepth, List<ChessBoard.Move> moves, ChessBoard.PieceColor color, int alpha, TreeNode currentNode)
//        {
//            int beta = alpha + 1;
//            int initialAlpha = alpha;

//            // TRANSPOSITIONS
//            //string currentPos = board.getPositionString();
//            //Transposition transposition = new Transposition(null, -1, -1);

//            //if (transpositions.ContainsKey(currentPos) && enabledTranspositions)
//            //{
//            //    transposition = transpositions[currentPos];

//            //    if ((transposition.depth >= depth && transposition.overflowDepth >= overflowDepth) || transposition.overflowDepth > overflowDepth)
//            //    {
//            //        calls++;
//            //        return transposition.position;
//            //    }
//            //}

//            //currentNode.position = board.moves;

//            // TIME CUTOFF
//            if (sw.ElapsedMilliseconds >= cutoffTime)
//            {
//                // todo could set this
//                currentNode.evaluation = new Evaluation(BitEval.evaluate(crawler.currentPosition, color, new List<ChessBoard.AttackMask>(250), alpha, beta), moves.ToArray());
//                return currentNode;
//            }

//            // BASE CASE
//            if (depth <= 0)
//            {
//                //if (overflowDepth > 0 && (board.uncertainPosition() || Eval.deepSearch(board)))
//                //    return zeroWindowSearch(board, 1, overflowDepth - 1, moveString, color, alpha, currentNode);

//                //// TRANSPOSITIONS (check logic)
//                //if (transposition.depth != -1 && enabledTranspositions)
//                //    transpositions.Remove(currentPos);

//                //if (enabledTranspositions)
//                //{
//                //    if (currentNode.moveNodes.Count != 0)
//                //        return currentNode;

//                //    transpositions.Add(currentPos, new Transposition(currentNode, depth, overflowDepth));
//                //}
//                currentNode.masks = new List<ChessBoard.AttackMask>(250);

//                currentNode.evaluation = new Evaluation(BitEval.evaluate(crawler.currentPosition, color, currentNode.masks, alpha, beta), moves.ToArray());
//                return currentNode;
//            }

//            int negate = (crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.White) || (!crawler.currentPosition.whiteToMove && color == ChessBoard.PieceColor.Black) ? 1 : -1;

//            TreeNode max = new TreeNode(new ChessBoard.Move());
//            max.evaluation = new Evaluation(-1000 * negate, moves.ToArray());

//            if (currentNode.moveNodes.Count <= 0)
//            {
//                List<ChessBoard.Move> nextMoves;

//                if (currentNode.masks.Count == 0)
//                    nextMoves = ChessBoard.getMoves(crawler.currentPosition, crawler.flags);
//                else
//                    nextMoves = ChessBoard.getMoves(crawler.currentPosition, currentNode.masks);

//                if (nextMoves.Count == 0)
//                {

//                    if (ChessBoard.kingInDanger(crawler.currentPosition))
//                    {
//                        currentNode.evaluation.score = (500 + depth) * -1 * negate;
//                    }

//                    else
//                        currentNode.evaluation.score = 0;

//                    currentNode.evaluation.line = moves.ToArray();

//                    // TRANSPOSITIONS
//                    //if (enabledTranspositions)
//                    //{
//                    //    transpositions.Remove(currentPos);
//                    //    transpositions.Add(currentPos, new Transposition(currentNode, depth, overflowDepth));
//                    //}

//                    //return currentNode;
//                }

//                foreach (ChessBoard.Move move in nextMoves)
//                {
//                    TreeNode newPos = new TreeNode(move);
//                    currentNode.moveNodes.Add(newPos);
//                }
//            }

//            foreach (TreeNode node in currentNode.moveNodes)
//            {
//                crawler.move(node.moveTo);
//                TreeNode selectedNode = zeroWindowSearch(crawler, depth - 1, overflowDepth, moves, color, initialAlpha, node);
//                crawler.undoMove();

//                if (selectedNode.evaluation.score * negate > max.evaluation.score * negate)
//                {
//                    max = selectedNode;

//                    currentNode.evaluation = max.evaluation;

//                    if (negate == 1)
//                        alpha = max.evaluation.score;
//                    else
//                        beta = max.evaluation.score;
//                }

//                if (alpha >= beta)
//                {
//                    currentNode.moveNodes = sortOptions(currentNode.moveNodes, negate);
//                    return max;
//                }
//            }

//            currentNode.moveNodes = sortOptions(currentNode.moveNodes, negate);

//            return max;
//        }

//        private static List<TreeNode> sortOptions(List<TreeNode> unsorted, int negate)
//        {
//            var sorted = from element in unsorted orderby element.evaluation.score * negate descending select element;
//            return sorted.ToList();
//        }

//        private struct Transposition
//        {
//            public int depth;
//            public int overflowDepth;
//            public TreeNode position;

//            public Transposition(TreeNode position, int depth, int overflowDepth)
//            {
//                this.position = position;
//                this.depth = depth;
//                this.overflowDepth = overflowDepth;
//            }
//        }
//    }

//    class TreeNode
//    {
//        public Evaluation evaluation;
//        public List<TreeNode> moveNodes;
//        public ChessBoard.Move moveTo;
//        public bool evaluated;
//        public List<ChessBoard.AttackMask> masks;

//        public TreeNode(ChessBoard.Move moveTo)
//        {
//            this.moveTo = moveTo;
//            this.moveNodes = new List<TreeNode>();
//            this.evaluated = false;
//            this.masks = new List<ChessBoard.AttackMask>(25);
//        }
//    }
//}
