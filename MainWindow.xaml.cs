using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace FocusTimerDesk
{
    public partial class MainWindow : Window
    {
        // Enum para indicar si estamos en una sesión de trabajo o descanso
        private enum TimerMode
        {
            Work,
            Break
        }

        // Timer que se ejecuta cada X tiempo (1 segundo)
        private DispatcherTimer _timer;

        // Tiempo restante en el temporizador
        private TimeSpan _timeRemaining;

        // ¿El temporizador está en marcha?
        private bool _isRunning;

        // Número de sesiones de trabajo completadas
        private int _completedWorkSessions;

        // Minutos configurados para trabajo y descanso
        private int _workMinutes = 25;
        private int _breakMinutes = 5;

        // Modo actual (Trabajo o Descanso)
        private TimerMode _currentMode = TimerMode.Work;

        // Historial de descansos anteriores (en minutos)
        private List<int> _breakHistory = new List<int>();


        public MainWindow()
        {
            InitializeComponent();

            // Tiempo inicial según los minutos de trabajo configurados
            _timeRemaining = TimeSpan.FromMinutes(_workMinutes);
            UpdateTimerText();
            UpdateModeText();

            // Configuramos el DispatcherTimer (1 segundo)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
        }

        // Se ejecuta cada segundo cuando el timer está encendido
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_timeRemaining.TotalSeconds > 0)
            {
                // Restamos 1 segundo
                _timeRemaining = _timeRemaining.Add(TimeSpan.FromSeconds(-1));
                UpdateTimerText();
            }
            else
            {
                // Se acabó el tiempo
                _timer.Stop();
                _isRunning = false;

                if (_currentMode == TimerMode.Work)
                {
                    // Hemos terminado una sesión de TRABAJO
                    _completedWorkSessions++;
                    SessionsText.Text = $"Sesiones de trabajo completadas hoy: {_completedWorkSessions}";

                    // Cambiamos a modo descanso
                    _currentMode = TimerMode.Break;
                    UpdateModeText();

                    // Guardamos en historial este descanso (aún no ha pasado, pero mostramos la duración)
                    _breakHistory.Add(_breakMinutes);
                    UpdateBreakHistoryText();

                    // Configuramos el temporizador al tiempo de descanso
                    _timeRemaining = TimeSpan.FromMinutes(_breakMinutes);
                    UpdateTimerText();

                    // Preguntamos si quiere iniciar el descanso ya
                    var result = MessageBox.Show(
                        $"Sesión completada. ¿Empezamos un descanso de {_breakMinutes} minutos?",
                        "FocusTimer Desk",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        _isRunning = true;
                        _timer.Start();
                    }
                    else
                    {
                        // Se queda listo para descanso, pero parado
                    }
                }
                else
                {
                    // Hemos terminado una sesión de DESCANSO
                    MessageBox.Show("Descanso terminado. ¡Volvemos al trabajo!", "FocusTimer Desk");

                    // Volvemos a modo trabajo
                    _currentMode = TimerMode.Work;
                    UpdateModeText();

                    // Preparamos una nueva sesión de trabajo
                    _timeRemaining = TimeSpan.FromMinutes(_workMinutes);
                    UpdateTimerText();
                }
            }
        }

        // Actualiza el texto del contador (mm:ss)
        private void UpdateTimerText()
        {
            TimerText.Text = _timeRemaining.ToString(@"mm\:ss");
        }

        // Actualiza el texto indicando el modo actual (Trabajo / Descanso)
        private void UpdateModeText()
        {
            if (_currentMode == TimerMode.Work)
            {
                ModeText.Text = "Modo: Trabajo";
                ModeText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                ModeText.Text = "Modo: Descanso";
                ModeText.Foreground = System.Windows.Media.Brushes.SkyBlue;
            }
        }

        // Actualiza el texto con el historial de descansos
        private void UpdateBreakHistoryText()
        {
            if (_breakHistory.Count == 0)
            {
                BreakHistoryText.Text = "Descansos anteriores (min): -";
            }
            else
            {
                // Ejemplo: "Descansos anteriores (min): 5, 5, 10"
                string history = string.Join(", ", _breakHistory);
                BreakHistoryText.Text = $"Descansos anteriores (min): {history}";
            }
        }

        // Botón Iniciar
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            _isRunning = true;
            _timer.Start();
        }

        // Botón Pausar
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isRunning = false;
            _timer.Stop();
        }

        // Botón Reiniciar
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;

            // Volvemos siempre al inicio de una sesión de TRABAJO
            _currentMode = TimerMode.Work;
            UpdateModeText();

            _timeRemaining = TimeSpan.FromMinutes(_workMinutes);
            UpdateTimerText();
        }

        // Botón "Aplicar tiempos": lee los minutos desde los TextBox
        private void ApplyDurations_Click(object sender, RoutedEventArgs e)
        {
            // Intentamos convertir los textos a enteros
            bool workOk = int.TryParse(WorkMinutesInput.Text, out int newWorkMinutes);
            bool breakOk = int.TryParse(BreakMinutesInput.Text, out int newBreakMinutes);

            // Comprobamos que sean válidos
            if (!workOk || !breakOk || newWorkMinutes <= 0 || newBreakMinutes <= 0)
            {
                MessageBox.Show(
                    "Introduce valores válidos (números mayores que 0) para trabajo y descanso.",
                    "Error en configuración",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Guardamos los nuevos valores
            _workMinutes = newWorkMinutes;
            _breakMinutes = newBreakMinutes;

            // Si el temporizador no está corriendo y estamos en modo trabajo,
            // actualizamos el tiempo inicial para la próxima sesión
            if (!_isRunning && _currentMode == TimerMode.Work)
            {
                _timeRemaining = TimeSpan.FromMinutes(_workMinutes);
                UpdateTimerText();
            }

            MessageBox.Show(
                $"Tiempos actualizados:\nTrabajo: {_workMinutes} min\nDescanso: {_breakMinutes} min",
                "Configuración aplicada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
