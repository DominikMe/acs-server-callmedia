using Grpc.Core;
using Grpc.Net.Client;
using GrpcProto;
using Microsoft.UI.Xaml;
using System;
using System.Net.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AcsWindowsClient
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();
		}

		private void myButton_Click(object sender, RoutedEventArgs e)
		{
			myButton.Content = "Clicked";
			Grpc();
		}

		private async void Grpc()
		{
			using var channel = CreateChannel();
			//var client = new Greeter.GreeterClient(channel);
			//var reply = await client.SayHelloAsync(
			//				  new HelloRequest { Name = "GreeterClient" });
			//myText.Text = "Greeting: " + reply.Message;

			var client = new ServerSentCommands.ServerSentCommandsClient(channel);
			using var stream = client.GetCommands(new GetCommandsRequest());
			await foreach (var command in stream.ResponseStream.ReadAllAsync())
			{
				myText.Text = $"Command: {command.Action} ({command.Args})";
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
