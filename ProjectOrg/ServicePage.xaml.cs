using System;
using System.Collections.Generic;
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
using System.Data.Entity;

namespace ProjectOrg
{
    /// <summary>
    /// Логика взаимодействия для ServicePage.xaml
    /// </summary>
    public partial class ServicePage : Page
    {
        int CountRecords;
        int CountPage;
        int CurrentPage = 0;
        List<PROJECT> CurrentPageList = new List<PROJECT>();
        List<PROJECT> TableList;
        List<ORGANIZATION> Organizations = new List<ORGANIZATION>();

        public ServicePage()
        {
            InitializeComponent();
            LoadOrganizations();
            UpdateProject();
        }

        private void LoadOrganizations()
        {
            try
            {
                using (var context = new ProjectOrganizationEntities())
                {
                    // Добавляем элемент "Все организации" в начало списка
                    Organizations = context.ORGANIZATION.ToList();
                    var allOrganizations = new List<ORGANIZATION>
                    {
                        new ORGANIZATION { Organization_ID = 0, Organization_Name = "Все организации" }
                    };
                    allOrganizations.AddRange(Organizations);

                    ComboOrganization.ItemsSource = allOrganizations;
                    ComboOrganization.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки организаций: {ex.Message}");
            }
        }


        private void UpdateProject()
        {
            try
            {
                using (var context = new ProjectOrganizationEntities())
                {
                    var currentProject = context.PROJECT
                        .Include(p => p.CONTRACT)
                        .Include(p => p.CONTRACT.ORGANIZATION)
                        .Include(p => p.DEPARTAMENT)
                        .ToList();

                    // Фильтрация по организации (пропускаем фильтрацию если выбрано "Все организации")
                    if (ComboOrganization.SelectedItem is ORGANIZATION selectedOrg && selectedOrg.Organization_ID != 0)
                    {
                        currentProject = currentProject
                            .Where(p => p.CONTRACT != null &&
                                       p.CONTRACT.ORGANIZATION != null &&
                                       p.CONTRACT.ORGANIZATION.Organization_ID == selectedOrg.Organization_ID)
                            .ToList();
                    }

                    // Фильтрация по поиску
                    currentProject = currentProject.Where(p =>
                        p.ProjectName.ToLower().Contains(TBoxSearch.Text.ToLower())).ToList();

                    // Сортировка по стоимости
                    if (RButtonDown.IsChecked == true)
                    {
                        currentProject = currentProject.OrderByDescending(p => p.ProjectCost).ToList();
                    }
                    else if (RButtonUp.IsChecked == true)
                    {
                        currentProject = currentProject.OrderBy(p => p.ProjectCost).ToList();
                    }

                    TableList = currentProject;
                    ChangePage(0, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления проектов: {ex.Message}");
            }
        }


        private void ChangePage(int direction, int? selectedPage)
        {

            if (TableList == null) return;

            CurrentPageList.Clear();
            CountRecords = TableList.Count;
            if (CountRecords % 10 > 0)
            {
                CountPage = CountRecords / 10 + 1;
            }
            else
            {
                CountPage = CountRecords / 10;
            }

            Boolean Ifupdate = true;

            int min;

            if (selectedPage.HasValue)
            {
                if (selectedPage >= 0 && selectedPage <= CountPage)
                {
                    CurrentPage = (int)selectedPage;
                    min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                    for (int i = CurrentPage * 10; i < min; i++)
                    {
                        CurrentPageList.Add(TableList[i]);
                    }
                }
            }
            else
            {
                switch (direction)
                {
                    case 1:
                        if (CurrentPage > 0)
                        {
                            CurrentPage--;
                            min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                            for (int i = CurrentPage * 10; i < min; i++)
                            {
                                CurrentPageList.Add(TableList[i]);
                            }
                        }
                        else
                        {
                            Ifupdate = false;
                        }
                        break;
                    case 2:
                        if (CurrentPage < CountPage - 1)
                        {
                            CurrentPage++;
                            min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                            for (int i = CurrentPage * 10; i < min; i++)
                            {
                                CurrentPageList.Add(TableList[i]);
                            }
                        }
                        else
                        {
                            Ifupdate = false;
                        }
                        break;
                }
            }
            if (Ifupdate)
            {
                PageListBox.Items.Clear();
                for (int i = 1; i <= CountPage; i++)
                {
                    PageListBox.Items.Add(i);
                }
                PageListBox.SelectedIndex = CurrentPage;

                min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                TBCount.Text = min.ToString();
                TBALLRecords.Text = " из " + CountRecords.ToString();
                ServiceListView.ItemsSource = CurrentPageList;
                ServiceListView.Items.Refresh();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var projectToDelete = (sender as Button).DataContext as PROJECT;

            // Проверяем, завершен ли проект (дата окончания должна быть в прошлом)
            if (projectToDelete.End_Date > DateTime.Now)
            {
                MessageBox.Show("Невозможно выполнить удаление, так как проект еще не завершен (дата окончания в будущем)");
                return;
            }

            if (MessageBox.Show("Вы точно хотите выполнить удаление?", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new ProjectOrganizationEntities())
                    {
                        // Находим проект в базе данных по ID
                        var projectInDb = context.PROJECT
                            .FirstOrDefault(p => p.Project_ID == projectToDelete.Project_ID);

                        if (projectInDb != null)
                        {
                            // Удаляем проект из контекста
                            context.PROJECT.Remove(projectInDb);
                            context.SaveChanges();

                            MessageBox.Show("Проект успешно удален");
                            UpdateProject(); // Обновляем список
                        }
                        else
                        {
                            MessageBox.Show("Проект не найден в базе данных");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage((sender as Button).DataContext as PROJECT));
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProject();
        }

        

        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProject();
        }

        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            UpdateProject();
        }
        private void ComboOrganization_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProject();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditPage(null));
        }

        private void LeftDirButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(1, null);
        }

        private void PageListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangePage(0, Convert.ToInt32(PageListBox.SelectedItem.ToString()) - 1);
        }

        private void RightDirButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(2, null);
        }

        private void Page_IsVisibleChanged_1(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                UpdateProject();
            }
        }
    }
}