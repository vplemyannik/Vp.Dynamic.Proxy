# Vp.Dynamic.Proxy

Suppose you have some interfaces or abstract classes, and you want added additional functionality for them. You can do this used Decorator for each kind of interface, something like this 
```csharp
 public interface ISomeWorker
  {
      void DoWork(string message);
  }
  
  public class Worker: ISomeWorker
  {
      void DoWork(string message)
      {
        Console.WriteLine(message)
      }
  }
  
  public class ProxyWorker: ISomeWorker worker
 {
    private readonly ISomeWorker _worker;
    
    public ProxyWorker(ISomeWorker worker)
    {
      _worker = worker;
    }
    
    void DoWork(string message)
    {
      //... Some usefull work before
      _worker.DoWork(message)
      // ... Some usefull work after
    }
  }
```
but you can create this proxy dynamically just use ```DynamicProxyBuilder```

```csharp
public class Program
{
    public interface ISomeWorker
    {
        void DoWork(string message);
    }

    public class WorkerTest : ISomeWorker
    { 
        public void DoWork(string message)
        {                
            Console.WriteLine(message);
        }
    }

    static void Main(string[] args)
    {
        var worker = new WorkerTest();
        var proxy = new DynamicProxyBuilder<ISomeWorker>()
            .AddPreAction(() =>
            {
                Console.WriteLine("Pre Action Started");
            })
            .AddPostAction(() =>
            {
                Console.WriteLine("Post Action Started");
            })
            .Build(worker);

        proxy.DoWork("some workkkkkkkkkkkkkkkk");
        Console.ReadLine();
    }
}
```
