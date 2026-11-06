using Akka.Actor;
public record Cat(string Name);
public record Dog(string Name);
public union Pet(Cat, Dog);
public record Adopted(string Owner, Pet Pet);

public sealed class PetActor : ReceiveActor
{
    public PetActor()
    {
        Receive<Cat>(c => Sender.Tell($"Receive<Cat> got {c.Name}"));
        Receive<Pet>(p => Sender.Tell(p switch { Cat c => $"Receive<Pet> cat {c.Name}", Dog d => $"Receive<Pet> dog {d.Name}", null => "null" }));
    }
}
public static class P {
  public static async Task Main(){
    using var sys = ActorSystem.Create("u");
    var a = sys.ActorOf<PetActor>();
    Pet pet = new Cat("Tom");
    Console.WriteLine(await a.Ask<string>(pet, TimeSpan.FromSeconds(3)));
    Console.WriteLine(await a.Ask<string>(pet.Value!, TimeSpan.FromSeconds(3)));
    foreach (object msg in new object[]{ pet, new Adopted("ann", new Dog("Rex")) }) {
      try {
        var ser = sys.Serialization.FindSerializerFor(msg);
        var bytes = ser.ToBinary(msg);
        Console.WriteLine($"{ser.GetType().Name}: {System.Text.Encoding.UTF8.GetString(bytes)}");
        var back = ser.FromBinary(bytes, msg.GetType());
        Console.WriteLine($"  back: {back} equal={Equals(back,msg)} inner={(back is Adopted ad ? ad.Pet.Value : back is Pet pp ? pp.Value : null)}");
      } catch (Exception ex) { Console.WriteLine("  FAIL " + ex.GetType().Name + ": " + ex.Message); }
    }
  }
}
