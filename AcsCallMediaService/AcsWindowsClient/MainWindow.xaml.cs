using Grpc.Core;
using Grpc.Net.Client;
using GrpcProto;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AcsWindowsClient
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
	{
        private CommandHandler commandHandler;
        private GrpcChannel channel;

        public MainWindow()
		{
			this.InitializeComponent();
			Activated += (_, __) => MinimizeWindow();
			channel = GrpcChannel.ForAddress("https://localhost:7228"); //createChannel
			commandHandler = new CommandHandler((msg) => myText.Text = msg, new Events.EventsClient(channel), SetImage);
			_ = ListenToGrpcCommands();

			//Closed += (_, __) => channel?.Dispose();
		}

		private void SetImage(Bitmap bitmap)
		{
			DispatcherQueue.TryEnqueue(() => myImage.Source = ToBitmapImage(bitmap));
        }

		private async void myButton_Click(object sender, RoutedEventArgs e)
		{
			MinimizeWindow();
			//Grpc();
			//SetImage(await GetTestBitmap());
		}

		private async Task<Bitmap> GetTestBitmap()
		{
			string url = "https://cdn-dynmedia-1.microsoft.com/is/image/microsoftcorp/communication-services_hero?resMode=sharp2&op_usm=1.5,0.65,15,0&wid=4000&qlt=100&fmt=png-alpha&fit=constrain";
			using HttpClient client = new();
			Stream stream = await client.GetStreamAsync(url);
            return new Bitmap(stream);
		}

		private async Task ListenToGrpcCommands()
		{
			var client = new ServerSentCommands.ServerSentCommandsClient(channel);
			using var stream = client.GetCommands(new GetCommandsRequest());
			await foreach (var command in stream.ResponseStream.ReadAllAsync())
			{
				//myText.Text = $"Command: {command.Action} ({command.Args})";
				await commandHandler.Handle(command);
			}
		}

		private static GrpcChannel CreateChannel()
		{
			var connectionFactory = new NamedPipesConnectionFactory("GrpcPipe");
			var socketsHttpHandler = new SocketsHttpHandler
			{
				ConnectCallback = connectionFactory.ConnectAsync
			};

			return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
			{
				HttpHandler = socketsHttpHandler
			});
		}

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
			ms.Position = 0;
            BitmapImage image = new();
			var stream = ms.AsRandomAccessStream();
            image.SetSource(stream);
			return image;
        }

		private void MinimizeWindow()
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            PInvoke.User32.ShowWindow(windowHandle, PInvoke.User32.WindowShowStyle.SW_MINIMIZE);
        }
    }
}
