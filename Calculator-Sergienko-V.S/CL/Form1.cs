using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CL // Убедитесь, что имя пространства имен совпадает с вашим проектом
{
    // ВАЖНО: Добавлено слово 'partial', чтобы связать этот файл с формой Windows
    public partial class Form1 : Form
    {
        // Элементы управления (объявляем их здесь, так как дизайнера больше нет)
        private TextBox txtDisplay;
        private ListBox lstHistory;
        private Dictionary<string, Button> buttons = new Dictionary<string, Button>();

        // Переменные состояния калькулятора
        private double previousValue = 0;
        private string currentOperator = "";
        private bool isNewOperation = true;
        private List<string> historyLog = new List<string>();
        private string historyFilePath = "calculator_history.txt";
        private bool isDarkTheme = false;

        public Form1()
        {
            // Инициализация базовых свойств формы
            this.Text = "Калькулятор";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Создаем интерфейс и применяем тему
            CreateInterface();
            ApplyTheme();
        }

        // Метод для создания всех кнопок и полей программно
        private void CreateInterface()
        {
            // 1. Дисплей
            txtDisplay = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(370, 40),
                Font = new Font("Microsoft Sans Serif", 18),
                TextAlign = HorizontalAlignment.Right,
                ReadOnly = true,
                Text = "0"
            };
            this.Controls.Add(txtDisplay);

            // 2. Список истории
            lstHistory = new ListBox
            {
                Location = new Point(390, 10),
                Size = new Size(90, 450),
                Font = new Font("Microsoft Sans Serif", 8)
            };
            this.Controls.Add(lstHistory);

            // 3. Сетка кнопок (цифры и операции)
            string[,] layout = new string[,]
            {
                {"7","8","9","/"},
                {"4","5","6","*"},
                {"1","2","3","-"},
                {"0",".","=","+"}
            };

            int startX = 10, startY = 60, btnW = 85, btnH = 40, gap = 10;

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    string text = layout[r, c];
                    Button btn = new Button
                    {
                        Text = text,
                        Location = new Point(startX + c * (btnW + gap), startY + r * (btnH + gap)),
                        Size = new Size(btnW, btnH),
                        Font = new Font("Microsoft Sans Serif", 12),
                        FlatStyle = FlatStyle.Flat
                    };
                    // Все эти кнопки используют один обработчик Btn_Click
                    btn.Click += Btn_Click;
                    this.Controls.Add(btn);
                    buttons[text] = btn;
                }
            }

            // 4. Кнопка очистки (C)
            Button btnClear = new Button
            {
                Text = "C",
                Location = new Point(startX + 3 * (btnW + gap), startY + 4 * (btnH + gap)),
                Size = new Size(btnW, btnH),
                Font = new Font("Microsoft Sans Serif", 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightCoral
            };
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);

            // 5. Кнопки управления (Сохранить, Загрузить, Тема)
            Button btnSave = new Button { Text = "Сохранить", Location = new Point(10, 420), Size = new Size(115, 35), FlatStyle = FlatStyle.Flat };
            Button btnLoad = new Button { Text = "Загрузить", Location = new Point(135, 420), Size = new Size(115, 35), FlatStyle = FlatStyle.Flat };
            Button btnTheme = new Button { Text = "Тема", Location = new Point(260, 420), Size = new Size(115, 35), FlatStyle = FlatStyle.Flat };

            btnSave.Click += BtnSave_Click;
            btnLoad.Click += BtnLoad_Click;
            btnTheme.Click += BtnTheme_Click;

            this.Controls.AddRange(new Control[] { btnSave, btnLoad, btnTheme });
        }

        // --- ЛОГИКА КАЛЬКУЛЯТОРА ---

        // Универсальный обработчик для цифр, точек и знаков операций
        private void Btn_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string text = btn.Text;

            if ("0123456789".Contains(text))
            {
                // Логика ввода цифр
                if (isNewOperation || txtDisplay.Text == "0" || txtDisplay.Text.Contains("Ошибка"))
                {
                    txtDisplay.Text = text;
                    isNewOperation = false;
                }
                else
                {
                    txtDisplay.Text += text;
                }
            }
            else if (text == ".")
            {
                // Логика десятичной точки
                if (isNewOperation || txtDisplay.Text.Contains("Ошибка"))
                {
                    txtDisplay.Text = "0.";
                    isNewOperation = false;
                }
                else if (!txtDisplay.Text.Contains("."))
                {
                    txtDisplay.Text += ".";
                }
            }
            else if ("+-*/".Contains(text))
            {
                // Логика операторов
                if (!isNewOperation && currentOperator != "")
                {
                    PerformCalculation();
                }
                else
                {
                    if (double.TryParse(txtDisplay.Text, out double val))
                        previousValue = val;
                }
                currentOperator = text;
                isNewOperation = true;
            }
            else if (text == "=")
            {
                // Логика равно
                if (currentOperator != "" && !isNewOperation)
                {
                    PerformCalculation();
                    historyLog.Add($"{txtDisplay.Text} (результат)");
                    currentOperator = "";
                }
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtDisplay.Text = "0";
            previousValue = 0;
            currentOperator = "";
            isNewOperation = true;
        }

        private void PerformCalculation()
        {
            if (!double.TryParse(txtDisplay.Text, out double currentValue))
            {
                txtDisplay.Text = "Ошибка ввода";
                isNewOperation = true;
                return;
            }

            double result = 0;
            string entry = $"{previousValue} {currentOperator} {currentValue}";

            switch (currentOperator)
            {
                case "+": result = previousValue + currentValue; break;
                case "-": result = previousValue - currentValue; break;
                case "*": result = previousValue * currentValue; break;
                case "/":
                    if (currentValue == 0)
                    {
                        txtDisplay.Text = "Ошибка: деление на 0!";
                        isNewOperation = true;
                        currentOperator = "";
                        return;
                    }
                    result = previousValue / currentValue;
                    break;
            }

            txtDisplay.Text = result % 1 == 0 ? result.ToString("F0") : result.ToString();
            previousValue = result;
            isNewOperation = true;
            historyLog.Add($"{entry} = {txtDisplay.Text}");
        }

        // --- ФУНКЦИИ СОХРАНЕНИЯ И ТЕМЫ ---

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllLines(historyFilePath, historyLog);
                MessageBox.Show("История сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                if (File.Exists(historyFilePath))
                {
                    lstHistory.Items.Clear();
                    foreach (var line in File.ReadAllLines(historyFilePath))
                        lstHistory.Items.Add(line);
                }
                else
                    MessageBox.Show("Файл истории не найден.");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color bg = isDarkTheme ? Color.FromArgb(30, 30, 30) : Color.White;
            Color fg = isDarkTheme ? Color.White : Color.Black;
            Color btnBg = isDarkTheme ? Color.FromArgb(60, 60, 60) : Color.LightGray;

            this.BackColor = bg;
            txtDisplay.BackColor = bg;
            txtDisplay.ForeColor = fg;
            lstHistory.BackColor = bg;
            lstHistory.ForeColor = fg;

            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Button b)
                {
                    b.BackColor = btnBg;
                    b.ForeColor = fg;
                }
            }
        }
    }
}