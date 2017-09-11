using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessEnginePort
{
    public class Crawler
    {
        public Stack<ChessBoard.Position> previousPositions;
        public ChessBoard.Position currentPosition;
        public List<ChessBoard.AttackMask> masks;
        public Dictionary<ulong, int> positionCount;
        public ulong hash;

        public Crawler(ChessBoard.Position position)
        {
            this.previousPositions = new Stack<ChessBoard.Position>();
            this.currentPosition = position;
            this.hash = position.hash;
            this.masks = new List<ChessBoard.AttackMask>(300);
            this.positionCount = new Dictionary<ulong, int>();

            this.positionCount.Add(this.hash, 1);
        }

        public void flipPosition()
        {
            currentPosition.whiteToMove = !currentPosition.whiteToMove;
            hash ^= ChessBoard.hashNumbers[768];
        }

        // Needs fixin'
        public Crawler duplicate()
        {
            Crawler crawler = new Crawler(this.currentPosition);
            crawler.hash = this.hash;
            
            return crawler;
        }

        public void move(ChessBoard.Move move)
        {
            this.previousPositions.Push(currentPosition);
            this.currentPosition = ChessBoard.advancePosition(currentPosition, move, currentPosition.hash);
            this.hash = currentPosition.hash;

            if (!this.positionCount.ContainsKey(this.hash))
                this.positionCount.Add(this.hash, 1);
    
            this.positionCount[this.hash] += 1;
        }

        public void undoMove()
        {
            //if (positionCount.ContainsKey(this.hash))
            this.positionCount[this.hash] -= 1;

            this.currentPosition = previousPositions.Pop();
            this.hash = currentPosition.hash;
        }

        public bool isDraw()
        {
            //if (!this.positionCount.ContainsKey(this.hash))
            //    return false;

            return this.positionCount[this.hash] >= 3;
        }
    }
}
