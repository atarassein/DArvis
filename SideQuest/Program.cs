using System.Threading;
    using SideQuest.Tasks;
    
    namespace SideQuest;
    
    class Program
    {
        private const string MutexName = "Global\\SideQuestSingleInstance";
    
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Running...");
            using var mutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                Console.WriteLine("Another instance of the application is already running.");
                return;
            }
    
            var cancellationTokenSource = new CancellationTokenSource();
    
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Termination requested...");
                eventArgs.Cancel = true;
                cancellationTokenSource.Cancel();
            };
    
            try
            {
                ToastListener.ListenForMessagesAsync(cancellationTokenSource.Token).Wait();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application terminated.");
            }
        }
    }