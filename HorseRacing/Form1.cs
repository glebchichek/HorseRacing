
namespace HorseRacing
{
    public partial class Form1 : Form
    {
        private List<Horse> horses;
        private List<ProgressBar> progressBars;
        private List<Label> nameLabels;
        private List<Label> numberLabels;
        private List<Thread> horseThreads;
        private Button btnStart;
        private Button btnReset;
        private DataGridView dgvResults;
        private bool isRacing;
        private object lockObject = new object();
        private System.Windows.Forms.Timer updateTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            InitializeHorses();
        }

        private void InitializeCustomComponents()
        {
            Text = "Конные скачки";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;

            Label lblTitle = new Label
            {
                Text = "КОННЫЕ СКАЧКИ",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(250, 10),
                AutoSize = true,
                ForeColor = Color.DarkGreen
            };
            Controls.Add(lblTitle);

            Panel horsePanel = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(740, 300),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(horsePanel);

            progressBars = new List<ProgressBar>();
            nameLabels = new List<Label>();
            numberLabels = new List<Label>();

            for (int i = 0; i < 5; i++)
            {
                int yPos = 10 + i * 55;

                Label lblNumber = new Label
                {
                    Text = $"#{i + 1}",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Location = new Point(10, yPos + 5),
                    AutoSize = true,
                    ForeColor = Color.Blue
                };
                horsePanel.Controls.Add(lblNumber);
                numberLabels.Add(lblNumber);

                Label lblName = new Label
                {
                    Text = GetHorseName(i),
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Location = new Point(50, yPos + 5),
                    AutoSize = true,
                    ForeColor = Color.DarkRed
                };
                horsePanel.Controls.Add(lblName);
                nameLabels.Add(lblName);

                ProgressBar pb = new ProgressBar
                {
                    Location = new Point(170, yPos + 5),
                    Size = new Size(500, 25),
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    Style = ProgressBarStyle.Continuous,
                    ForeColor = GetHorseColor(i)
                };
                horsePanel.Controls.Add(pb);
                progressBars.Add(pb);


                Label lblEmoji = new Label
                {
                    Text = "🐴",
                    Font = new Font("Segoe UI Emoji", 16),
                    Location = new Point(140, yPos),
                    AutoSize = true
                };
                horsePanel.Controls.Add(lblEmoji);
            }

            btnStart = new Button
            {
                Text = "СТАРТ",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(200, 380),
                Size = new Size(150, 40),
                BackColor = Color.LimeGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);

            btnReset = new Button
            {
                Text = "СБРОС",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(450, 380),
                Size = new Size(150, 40),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnReset.Click += BtnReset_Click;
            Controls.Add(btnReset);

            Label lblResults = new Label
            {
                Text = "ТАБЛИЦА РЕЗУЛЬТАТОВ",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(300, 440),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };
            Controls.Add(lblResults);

            dgvResults = new DataGridView
            {
                Location = new Point(50, 470),
                Size = new Size(700, 100),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Arial", 10)
            };
            dgvResults.Columns.Add("Place", "Место");
            dgvResults.Columns.Add("Number", "№");
            dgvResults.Columns.Add("Name", "Имя лошади");
            dgvResults.Columns.Add("Time", "Время (сек)");
            dgvResults.Columns.Add("Speed", "Скорость");

            dgvResults.Columns[0].Width = 60;
            dgvResults.Columns[1].Width = 50;
            dgvResults.Columns[2].Width = 200;
            dgvResults.Columns[3].Width = 100;
            dgvResults.Columns[4].Width = 100;

            Controls.Add(dgvResults);

            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 100;
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void InitializeHorses()
        {
            horses = new List<Horse>();
            string[] names = { "Молния", "Ветер", "Гром", "Стрела", "Титан" };
            string[] emojis = { "🐴", "🐎", "🏇", "🐴", "🐎" };

            for (int i = 0; i < 5; i++)
            {
                horses.Add(new Horse
                {
                    Id = i + 1,
                    Name = names[i],
                    Emoji = emojis[i],
                    Progress = 0,
                    Speed = 0,
                    Time = 0,
                    IsFinished = false,
                    Place = 0
                });
            }

            horseThreads = new List<Thread>();
            isRacing = false;
        }

        private string GetHorseName(int index)
        {
            string[] names = { "Молния", "Ветер", "Гром", "Стрела", "Титан" };
            return names[index];
        }

        private Color GetHorseColor(int index)
        {
            Color[] colors = { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Purple };
            return colors[index];
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (isRacing)
                return;

            ResetHorses();
            dgvResults.Rows.Clear();

            btnStart.Enabled = false;
            btnReset.Enabled = false;
            isRacing = true;

            horseThreads.Clear();
            foreach (var horse in horses)
            {
                Thread thread = new Thread(() => RaceHorse(horse));
                thread.IsBackground = true;
                thread.Start();
                horseThreads.Add(thread);
            }

            updateTimer.Start();
        }

        private void RaceHorse(Horse horse)
        {
            Random rand = new Random(horse.Id * DateTime.Now.Millisecond);
            DateTime startTime = DateTime.Now;

            while (horse.Progress < 100)
            {
                int speed = rand.Next(1, 6);
                horse.Speed = speed;

                lock (lockObject)
                {
                    horse.Progress += speed;
                    if (horse.Progress > 100)
                        horse.Progress = 100;
                }

                horse.Time = (DateTime.Now - startTime).TotalSeconds;

                Thread.Sleep(rand.Next(100, 300));

                if (horse.Progress >= 100 && !horse.IsFinished)
                {
                    horse.IsFinished = true;
                    horse.Time = (DateTime.Now - startTime).TotalSeconds;
                }
            }
            CheckAllFinished();
        }

        private void CheckAllFinished()
        {
            bool allFinished = true;
            foreach (var horse in horses)
            {
                if (!horse.IsFinished)
                {
                    allFinished = false;
                    break;
                }
            }

            if (allFinished)
            {
                updateTimer.Stop();

                var sorted = horses.OrderBy(h => h.Time).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].Place = i + 1;
                }

                Invoke((MethodInvoker)delegate
                {
                    ShowResults();
                    btnStart.Enabled = true;
                    btnReset.Enabled = true;
                    isRacing = false;
                });
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < horses.Count; i++)
            {
                progressBars[i].Value = horses[i].Progress;

                if (horses[i].IsFinished)
                {
                    progressBars[i].ForeColor = Color.Gold;
                }
                else
                {
                    progressBars[i].ForeColor = GetHorseColor(i);
                }
            }
        }

        private void ShowResults()
        {
            dgvResults.Rows.Clear();

            var sorted = horses.OrderBy(h => h.Place).ToList();

            foreach (var horse in sorted)
            {
                string place;
                switch (horse.Place)
                {
                    case 1: place = "🥇 1"; break;
                    case 2: place = "🥈 2"; break;
                    case 3: place = "🥉 3"; break;
                    default: place = $"{horse.Place}"; break;
                }

                string speedText = horse.Speed > 0 ? $"{horse.Speed:F1}" : "-";
                dgvResults.Rows.Add(place, horse.Id, horse.Name, horse.Time.ToString("F2"), speedText);

                if (horse.Place == 1)
                {
                    dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightGreen;
                }
                else if (horse.Place == 2)
                {
                    dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightBlue;
                }
                else if (horse.Place == 3)
                {
                    dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightSalmon;
                }
            }

            var winner = sorted.First();
            Text = $"ПОБЕДИТЕЛЬ: {winner.Name} (Время: {winner.Time:F2} сек)";
        }

        private void ResetHorses()
        {
            foreach (var horse in horses)
            {
                horse.Progress = 0;
                horse.Speed = 0;
                horse.Time = 0;
                horse.IsFinished = false;
                horse.Place = 0;
            }

            foreach (var pb in progressBars)
            {
                pb.Value = 0;
            }

            Text = "Конные скачки";
            dgvResults.Rows.Clear();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (isRacing)
                return;

            ResetHorses();
            btnStart.Enabled = true;
            btnReset.Enabled = false;

            foreach (var thread in horseThreads)
            {
                if (thread != null && thread.IsAlive)
                {
                    try
                    {
                        thread.Abort();
                    }
                    catch { }
                }
            }
            horseThreads.Clear();

            updateTimer.Stop();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (horseThreads != null)
            {
                foreach (var thread in horseThreads)
                {
                    if (thread != null && thread.IsAlive)
                    {
                        try
                        {
                            thread.Abort();
                        }
                        catch { }
                    }
                }
            }
            base.OnFormClosing(e);
        }
    }

    public class Horse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Emoji { get; set; }
        public int Progress { get; set; }
        public int Speed { get; set; }
        public double Time { get; set; }
        public bool IsFinished { get; set; }
        public int Place { get; set; }
    }
}
