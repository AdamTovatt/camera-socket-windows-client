using System.Net.WebSockets;
using System.Windows.Forms;

namespace CameraSocketWindowsClient
{
    public partial class Form1 : Form
    {
        private ClientWebSocket webSocket;
        private Thread imageThread;
        private bool isFetchingImages;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize the WebSocket and start the image fetching thread
            webSocket = new ClientWebSocket();
            isFetchingImages = true;
            imageThread = new Thread(FetchImages);
            imageThread.Start();
        }

        private async void FetchImages()
        {
            // Connect to the WebSocket server
            await webSocket.ConnectAsync(new Uri("wss://sakurapi.se/camera-server/video-output-stream"), CancellationToken.None);

            await webSocket.SendAsync(new ArraySegment<byte>(BitConverter.GetBytes(1)), WebSocketMessageType.Binary, true, CancellationToken.None);

            while (isFetchingImages)
            {
                // Continuously fetch images from the WebSocket connection
                var buffer = new byte[1024];
                var imageBuffer = new byte[1024 * 1024]; // Adjust the size based on your image requirements

                await webSocket.SendAsync(new ArraySegment<byte>(new byte[1] { 0 }), WebSocketMessageType.Binary, true, CancellationToken.None);

                WebSocketReceiveResult result;
                using (MemoryStream ms = new MemoryStream())
                {
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    ms.Position = 0;
                    ms.Read(imageBuffer, 0, (int)ms.Length);

                    if (ms.Length > 0)
                    {
                        Image image = Image.FromStream(ms);

                        try
                        {
                            // Update the UI on the main thread
                            Invoke(new Action(() =>
                            {
                                pictureBox.Image = image; // Assuming you have a PictureBox control named pictureBox
                            }));
                        }
                        catch { }
                    }
                }
            }
        }

        private void DisplayImage(byte[] imageBytes)
        {
            // Use the imageBytes to create an image object (e.g., using System.Drawing.Image.FromStream)
            // Once you have the image object, you can display it on the form using PictureBox or any other control

            // Example:
            using (var ms = new System.IO.MemoryStream(imageBytes))
            {
                var image = System.Drawing.Image.FromStream(ms);

                // Update the UI on the main thread
                Invoke(new Action(() =>
                {
                    pictureBox.Image = image; // Assuming you have a PictureBox control named pictureBox
                }));
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop the image fetching thread and close the WebSocket connection when the form is closing
            isFetchingImages = false;
            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Form closing", CancellationToken.None).Wait();

            base.OnFormClosing(e);
        }
    }
}