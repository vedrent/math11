using NAudio.Wave;

namespace math11
{
    public class LoopingWaveStream : WaveStream
    {
        private readonly WaveStream _sourceStream;
        private long _position;

        public LoopingWaveStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

        public override long Length => _sourceStream.Length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _sourceStream.Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                // Если достигнут конец потока, сбрасываем позицию для зацикливания
                _sourceStream.Position = 0;
                bytesRead = _sourceStream.Read(buffer, offset, count);
            }

            _position += bytesRead;
            return bytesRead;
        }
    }


    public partial class RaceForm : Form
    {
        private System.Windows.Forms.Timer timer;
        private Cockroach[] cockroaches;
        private int screenWidth;
        private int screenHeight;
        private int lapsToWin = 1; // Количество кругов для победы
        private Image[][] cockroachImages;
        private int[] animationFrame;
        private int[] animationFrameCounter;
        private IWavePlayer waveOutDevice;
        private AudioFileReader stepReader;
        private AudioFileReader winReader;



        public RaceForm()
        {
            this.DoubleBuffered = true;
            this.Width = 1000;
            this.Height = 600;

            screenWidth = this.ClientSize.Width;
            screenHeight = this.ClientSize.Height;
            InitializeSounds();
            InitializeRace();

            //this.Paint += OnPaint;
            //InitializeComponent();
        }

        private void InitializeSounds()
        {
            try
            {
                // Загрузка звуков
                stepReader = new AudioFileReader("C:\\_coding\\progmath\\math11\\Resources\\cockroach-run.mp3");
                winReader = new AudioFileReader("C:\\_coding\\progmath\\math11\\Resources\\game-won.mp3");

                waveOutDevice = new WaveOutEvent();

                var loopingStream = new LoopingWaveStream(stepReader);

                waveOutDevice.Init(loopingStream);
                waveOutDevice.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки звуков: {ex.Message}");
            }
        }

        private void InitializeRace()
        {
            cockroachImages = new Image[3][]
            {
                new Image[] {
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach1-1.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach1-2.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach1-3.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach1-4.png")
                },
                new Image[] {
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach2-1.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach2-2.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach2-3.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach2-4.png")
                },
                new Image[] {
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach3-1.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach3-2.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach3-3.png"),
                    Image.FromFile("C:\\_coding\\progmath\\math11\\Resources\\cockroach3-4.png")
                },
            };

            animationFrame = new int[3];
            animationFrameCounter = new int[3];

            cockroaches = new Cockroach[3];
            Random rnd = new Random();

            for (int i = 0; i < cockroaches.Length; i++)
            {
                int speed = rnd.Next(2, 6); // Случайная начальная скорость
                cockroaches[i] = new Cockroach(new Point(0, 50 + i * 150), speed, rnd);
            }

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 10; // Интервал таймера
            timer.Tick += Timer_Tick;
            timer.Start();

            //this.Paint += OnPaint;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {

            for (int i = 0; i < cockroaches.Length; i++)
            {
                if (cockroaches[i].Finished != true)
                {
                    cockroaches[i].Move(screenWidth);

                    animationFrameCounter[i]++;
                    animationFrame[i] = (animationFrameCounter[i]/3) % cockroachImages[i].Length;

                    if (cockroaches[i].Laps >= lapsToWin)
                    {
                        //timer.Stop();

                        if (waveOutDevice != null && winReader != null)
                        {
                            waveOutDevice.Stop();
                            waveOutDevice.Init(winReader);
                            waveOutDevice.Play();
                        }

                        //MessageBox.Show($"{cockroaches[i].Name} победил!");
                        //cockroaches[i].CurrentSpeedCounter = -1;
                        //cockroaches[i].Speed = 0;
                        cockroaches[i].Finished = true;
                    }
                }
            }
            this.Invalidate(); // Перерисовка формы

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            for (int i = 0; i < cockroaches.Length; i++)
            {
                var cockroach = cockroaches[i];
                e.Graphics.DrawImage(cockroachImages[i][animationFrame[i]], cockroach.Position.X, cockroach.Position.Y, 100, 100);
                e.Graphics.DrawString($"Круги: {cockroach.Laps}", this.Font, Brushes.Red, cockroach.Position.X, cockroach.Position.Y - 15);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            stepReader?.Dispose();
            winReader?.Dispose();
            waveOutDevice?.Dispose();
        }

    }

    public class Cockroach
    {
        private Random random;
        public Point Position { get; private set; }
        public int Speed { get; set; }
        public int CurrentSpeedCounter { get; set; }
        public int Laps { get; private set; }
        public string Name { get; }
        public bool Finished { get; set; }

        public Cockroach(Point startPosition, int speed, Random rnd)
        {
            Position = startPosition;
            Speed = speed;
            CurrentSpeedCounter = 10;
            random = rnd;
            Name = "Таракан " + rnd.Next(1000, 9999);
            Finished = false;
        }

        public void Move(int screenWidth)
        {
            Position = new Point(Position.X + Speed, Position.Y);
            if (CurrentSpeedCounter != 0)
            {
                CurrentSpeedCounter--;
            }
            else {
                CurrentSpeedCounter = random.Next(5, 15);
                Speed = random.Next(-2, 6);
            }

            // Обновляем круги, если таракан выходит за границы экрана
            if (Position.X > screenWidth)
            {
                Laps++;
                Position = new Point(0, Position.Y);
            }
        }
    }
}
