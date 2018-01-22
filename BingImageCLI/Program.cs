namespace BingImageCLI
{
    using PowerArgs;

    public class Program
    {
        static void Main(string[] args)
        {
            Args.InvokeMain<BingImageSearch>(args);
        }
    }
}