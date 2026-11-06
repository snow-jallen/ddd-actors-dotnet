using Akka.Actor;
using System.Collections.Concurrent;
var log = new ConcurrentQueue<string>();
using var sys = ActorSystem.Create("a");
var a = sys.ActorOf(Props.Create(() => new AsyncActor(log)));
var tasks = Enumerable.Range(1, 3).Select(i => a.Ask<string>(i, TimeSpan.FromSeconds(5))).ToArray();
var replies = await Task.WhenAll(tasks);
Console.WriteLine("log: " + string.Join(" | ", log));
Console.WriteLine("replies: " + string.Join(", ", replies));

sealed class AsyncActor : ReceiveActor
{
    public AsyncActor(ConcurrentQueue<string> log)
    {
        ReceiveAsync<int>(async i =>
        {
            log.Enqueue($"start {i}");
            var before = Sender;
            await Task.Delay(100);
            log.Enqueue($"end {i} senderStable={ReferenceEquals(before, Sender)}");
            Sender.Tell($"reply {i}");
        });
    }
}
