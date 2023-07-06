using Grpc.Core;
using Grpc.Net.Client;
using GrpcProto;
using Microsoft.UI.Xaml;
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
            channel = GrpcChannel.ForAddress("https://localhost:7228"); //createChannel
            commandHandler = new CommandHandler((msg) => myText.Text = msg, new Events.EventsClient(channel));
			_ = ListenToGrpcCommands();

			Closed += (_, __) => channel?.Dispose();
		}

		private void myButton_Click(object sender, RoutedEventArgs e)
		{
			myButton.Content = "Clicked";
			//Grpc();
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
	}
}
