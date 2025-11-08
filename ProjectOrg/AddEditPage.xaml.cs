using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectOrg
{
    
    public partial class AddEditPage : Page
    {
        private PROJECT _currentProject = new PROJECT();
        private ProjectOrganizationEntities _context;

        public AddEditPage(PROJECT SelectedProject)
        {
            InitializeComponent();
            _context = new ProjectOrganizationEntities(); // Создаем один контекст для всей страницы

            // Подписываемся на событие Unloaded для освобождения ресурсов
            this.Unloaded += AddEditPage_Unloaded;

            if (SelectedProject != null)
            {
                // Если редактируем существующий проект, загружаем его из базы с включенными связанными данными
                _currentProject = _context.PROJECT
                    .Include(p => p.CONTRACT)
                    .Include(p => p.CONTRACT.ORGANIZATION)
                    .Include(p => p.DEPARTAMENT)
                    .FirstOrDefault(p => p.Project_ID == SelectedProject.Project_ID) ?? new PROJECT();
            }
            else
            {
                _currentProject = new PROJECT();
            }

            // Инициализация DatePicker
            startd.SelectedDate = DateTime.Today;
            if (_currentProject.Start_Date != null && _currentProject.Start_Date > Convert.ToDateTime("01.01.1990"))
                startd.SelectedDate = _currentProject.Start_Date;

            endd.SelectedDate = DateTime.Today;
            if (_currentProject.End_Date != null && _currentProject.End_Date > Convert.ToDateTime("01.01.1990"))
                endd.SelectedDate = _currentProject.End_Date;

            LoadComboBoxData();
            DataContext = _currentProject;
        }

        private void AddEditPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose(); // Освобождаем контекст при выгрузке страницы
        }

        private void LoadComboBoxData()
        {
            try
            {
                // Загрузка контрактов
                var contracts = _context.CONTRACT.ToList();
                ContractComboBox.ItemsSource = contracts;

                // Загрузка организаций
                var organizations = _context.ORGANIZATION.ToList();
                OrganizationComboBox.ItemsSource = organizations;

                // Загрузка отделов
                var departaments = _context.DEPARTAMENT.ToList();
                DepartamentComboBox.ItemsSource = departaments;

                // Установка выбранных значений, если редактируем существующий проект
                if (_currentProject.Project_ID != 0)
                {
                    ContractComboBox.SelectedValue = _currentProject.Contract_ID;
                    OrganizationComboBox.SelectedValue = _currentProject.CONTRACT?.Organization_ID;
                    DepartamentComboBox.SelectedValue = _currentProject.Departament_ID;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            // Проверка названия проекта
            if (String.IsNullOrWhiteSpace(_currentProject.ProjectName))
                errors.AppendLine("Введите название проекта");
            else
            {
                if (!IsMatchesPattern(_currentProject.ProjectName))
                    errors.AppendLine("Название содержит недопустимые символы");
                if (_currentProject.ProjectName.Length > 50)
                    errors.AppendLine("Длина названия больше 50");
            }

            // Проверка стоимости проекта
            if (_currentProject.ProjectCost <= 0)
            {
                errors.AppendLine("Укажите корректно стоимость проекта");
            }

            // Проверка даты начала
            if (startd.SelectedDate == null)
                errors.AppendLine("Введите дату начала");
            else
            {
                _currentProject.Start_Date = (DateTime)startd.SelectedDate;
            }

            // Проверка отдела
            if (DepartamentComboBox.SelectedItem == null)
                errors.AppendLine("Выберите отдел");

            // Проверка контракта
            if (ContractComboBox.SelectedItem == null)
                errors.AppendLine("Выберите контракт");

            // Проверка даты окончания
            if (endd.SelectedDate == null)
            {
                errors.AppendLine("Введите дату окончания");
            }
            else if (endd.SelectedDate <= startd.SelectedDate)
            {
                errors.AppendLine("Дата окончания должна быть позже даты начала");
            }
            else
            {
                _currentProject.End_Date = (DateTime)endd.SelectedDate;
            }

            // Проверка выбора и названия организации
            if (OrganizationComboBox.SelectedItem == null)
            {
                errors.AppendLine("Выберите организацию");
            }

            // Если есть ошибки, показываем их
            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            try
            {
                // Обновляем свойства из ComboBox
                if (ContractComboBox.SelectedValue != null)
                {
                    _currentProject.Contract_ID = (int)ContractComboBox.SelectedValue;
                }

                if (DepartamentComboBox.SelectedValue != null)
                {
                    _currentProject.Departament_ID = (int)DepartamentComboBox.SelectedValue;
                }

                // Если это новый проект, добавляем его в контекст
                if (_currentProject.Project_ID == 0)
                {
                    _context.PROJECT.Add(_currentProject);
                }
                else
                {
                    // Если проект уже существует, помечаем как измененный
                    _context.Entry(_currentProject).State = EntityState.Modified;
                }

                _context.SaveChanges();
                MessageBox.Show("Информация сохранена");
                Manager.MainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool IsMatchesPattern(string str)
        {
            str = str.Replace("-", "").Replace(" ", "");
            bool isOK = true;
            foreach (char character in str)
            {
                if (!(character >= 'а' && character <= 'я' || character >= 'А' && character <= 'Я' || character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z'))
                {
                    isOK = false;
                    return isOK;
                }

            }

            return isOK;
        }

        private void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            string projectDirectory = GetProjectRootDirectory();
            string clientsFolderPath = System.IO.Path.Combine(projectDirectory, "проекты");

            if (!Directory.Exists(clientsFolderPath))
            {
                Directory.CreateDirectory(clientsFolderPath);
            }

            OpenFileDialog myOpenFileDialog = new OpenFileDialog
            {
                InitialDirectory = clientsFolderPath
            };

            if (myOpenFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = myOpenFileDialog.FileName;

                // Сохраняем относительный путь ОТНОСИТЕЛЬНО КОРНЯ ПРОЕКТА
                _currentProject.ProjectPhoto = System.IO.Path.Combine("проекты", System.IO.Path.GetFileName(selectedFilePath));

                // Загружаем изображение по полному пути
                LogoImage.Source = new BitmapImage(new Uri(selectedFilePath));
            }
        }

        private string GetProjectRootDirectory()
        {
            // Путь к исполняемому файлу (bin/Debug)
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(exePath)));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}