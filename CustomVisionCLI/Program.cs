namespace CustomVisionCLI
{
    using PowerArgs;
    using System.Threading.Tasks;

    public class Program
    {
        static async Task Main(string[] args)
        {
            await Args.InvokeMainAsync<CustomVision>(args);
        }
    }
}