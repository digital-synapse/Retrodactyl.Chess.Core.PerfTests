using MessagePack;
using Retrodactyl.Extensions.DotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MessagePack")]
[assembly: InternalsVisibleTo("MessagePack.Resolvers.DynamicObjectResolver")]
[assembly: InternalsVisibleTo("MessagePack.Resolvers.DynamicUnionResolver")]

namespace Retrodactyl.Chess.Core.PerfTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //initPerfTest();
            //evaluationPerfTest();
            aiPerfTest();
        }

        static void aiPerfTest(int TESTS = 6)
        {
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var board = new Board(true);
                    var rnd = new Random();
                    var times = new List<TimeSpan>();
                    var sw = new Stopwatch();
                    var ai = new GameAI();

                    drawBoardAscii(board, 60, i*9);
                    for (var test = 0; test < TESTS; test++)
                    {
                        sw.Restart();
                        var bestMove = ai.Search(board);
                        board.Move(bestMove);
                        sw.Stop();
                        times.Add(sw.Elapsed);
                        Console.WriteLine($"Test {test + 1}: {sw.Elapsed.TotalSeconds}");
                        drawBoardAscii(board, 60, i*9);
                        drawStringAt(bestMove.ToString(), 85, (i*9) + test);
                    }
                    var total = times.Sum(t => t.TotalSeconds);
                    var avg = times.Sum(t => t.TotalSeconds) / TESTS;
                    Console.WriteLine($"Total time for {TESTS} turns (Seconds): " + total);
                    Console.WriteLine($"Average per turn (Seconds): " + avg);
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }

        }

        private static void drawStringAt(string str, int left, int top)
        {
            var cl = Console.CursorLeft;
            var ct = Console.CursorTop;
            Console.SetCursorPosition(left, top);
            Console.Write(str);
            Console.SetCursorPosition(cl, ct);
        }
        private static void drawBoardAscii(Board board, int left = 60, int top=0)
        {
            var cl = Console.CursorLeft;
            var ct = Console.CursorTop;
            int y = top;
            foreach (var line in board.ToAscii().Split(Environment.NewLine))
            {
                Console.SetCursorPosition(left, y++);
                Console.Write(line);
            }
            Console.SetCursorPosition(cl, ct);
        }

        static void initPerfTest(int depth = 6)
        {
            var sw = new Stopwatch();
            sw.Start();
            var ai = new GameAI();
            var board = new Board();
            var cache = ai.InitMoveCache(depth, board);
            sw.Stop();
            Console.WriteLine($"Init Time: {sw.Elapsed.TotalSeconds}");

            var data = dto(cache.Floor);
            var lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
            var bytes = MessagePackSerializer.Serialize(data, lz4Options);
            File.WriteAllBytes("starting_moves.pak", bytes); // Requires System.IO
        }

        static Move[] dto(List<Forest<Core.Move>.Node> nodes)
        {
            var moves = new List<Move>();
            foreach (var node in nodes)
            {
                var move = new Move();
                move.from = (byte)node.Value.from.ToByte();
                move.to = (byte)node.Value.to.ToByte();
                move.children = dto(node.Children);
                moves.Add(move);
            }
            return moves.ToArray();

        }

        [MessagePackObject]        
        public class Move
        {
            [Key(0)]
            public byte from;
            [Key(1)]
            public byte to;
            [Key(2)]
            public Move[] children;
        }
        
        static void evaluationPerfTest(int TESTS = 5)
        {
            try
            {
                var board = new Board(true);
                var rnd = new Random();

                //Debug.WriteLine(Convert.ToString((long)board.bitsNotOccupied, 2).PadLeft(64, '0'));
                //Debug.WriteLine(Convert.ToString((long)board.bitsBlack, 2).PadLeft(64, '0'));
                //Debug.WriteLine(Convert.ToString((long)board.bitsWhite, 2).PadLeft(64,'0'));

                var times = new List<TimeSpan>();
                var sw = new Stopwatch();
                for (var test = 0; test < TESTS; test++)
                {
                    sw.Restart();
                    for (var i = 0; i < 1000000; i++)
                    {
                        var moves = board.GetMoves().ToArray();
                        var move = moves[rnd.Next(0, moves.Length)];
                        board.Move(move);
                    }
                    sw.Stop();
                    times.Add(sw.Elapsed);
                    Console.WriteLine($"Test {test + 1}: {sw.Elapsed.TotalSeconds}");
                }
                var total = times.Sum(t => t.TotalSeconds);
                var avg = times.Sum(t => t.TotalSeconds) / TESTS;
                //Debug.WriteLine(board);
                Console.WriteLine($"Total time for {1 * TESTS} million evaluations and moves (Seconds): " + total);
                Console.WriteLine($"Average time for 1 million evaluations and moves per test (Seconds): " + avg);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }

        }
    }
}
