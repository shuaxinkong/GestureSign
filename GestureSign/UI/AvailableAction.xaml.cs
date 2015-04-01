﻿using System;
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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

using GestureSign.Common.Applications;
using GestureSign.Common.Plugins;
using GestureSign.Common.Gestures;
using GestureSign.Common.Drawing;

using MahApps.Metro.Controls.Dialogs;


namespace GestureSign.UI
{
    /// <summary>
    /// AvailableAction.xaml 的交互逻辑
    /// </summary>
    public partial class AvailableAction : UserControl
    {
        // public static event EventHandler StartCapture;
        public AvailableAction()
        {
            InitializeComponent();
            var sourceView = new ListCollectionView(ActionInfos);//创建数据源的视图
            var groupDesctrption = new PropertyGroupDescription("ApplicationName");//设置分组列
            sourceView.GroupDescriptions.Add(groupDesctrption);//在图中添加分组
            lstAvailableActions.ItemsSource = sourceView;//绑定数据源

            ApplicationDialog.ActionsChanged += ActionDefinition_ActionsChanged;

            AvailableGestures.GestureChanged += ActionDefinition_ActionsChanged;
            GestureDefinition.GesturesChanged += ActionDefinition_ActionsChanged;
        }




        Size sizThumbSize = new Size(65, 65);
        ObservableCollection<ActionInfo> ActionInfos = new ObservableCollection<ActionInfo>();
        //  List<ActionInfo> ActionInfos = new List<ActionInfo>(5);


        public class ActionInfo : INotifyPropertyChanged
        {

            public ActionInfo(string actionName, string applicationName, string description, ImageSource gestureThumbnail, string gestureName, bool isEnabled)
            {
                IsEnabled = isEnabled;
                GestureThumbnail = gestureThumbnail;
                ApplicationName = applicationName;
                ActionName = actionName;
                Description = description;
                GestureName = gestureName;
            }
            private bool isEnabled;
            public bool IsEnabled
            {
                get
                {
                    return isEnabled;
                }

                set { SetProperty(ref isEnabled, value); }
            }
            public string GestureName { get; set; }
            public ImageSource GestureThumbnail { get; set; }
            public string ApplicationName { get; set; }
            public string ActionName { get; set; }

            public string Description { get; set; }


            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
            protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
            {
                if (object.Equals(storage, value)) return;

                storage = value;
                this.OnPropertyChanged(propertyName);
            }
        }

        void ActionDefinition_ActionsChanged(object sender, EventArgs e)
        {
            BindActions();
        }
        private async void cmdEditAction_Click(object sender, RoutedEventArgs e)
        {
            // Make sure at least one item is selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                var mySettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    // NegativeButtonText = "Go away!",
                    // FirstAuxiliaryButtonText = "Cancel",
                    ColorScheme = MetroDialogColorScheme.Accented //: MetroDialogColorScheme.Theme
                };

                MessageDialogResult result = await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("请选择", "编辑前需要先选择一项动作 ", MessageDialogStyle.Affirmative, mySettings);
                // MessageBox.Show("You must select an item before editing", "Please Select an Item", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Get first item selected, associated action, and selected application
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            IAction selectedAction = null;
            IApplication selectedApplication = null;
            string selectedGesture = null;

            // Store selected item group header for later use
            string strApplicationHeader = selectedItem.ApplicationName;

            if (strApplicationHeader != ApplicationManager.Instance.GetGlobalApplication().Name)
                selectedApplication = ApplicationManager.Instance.GetExistingUserApplication(strApplicationHeader);
            else
                selectedApplication = ApplicationManager.Instance.GetGlobalApplication();

            if (selectedApplication == null)
                // Select action from global application list
                selectedAction = ApplicationManager.Instance.GetGlobalApplication().Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            else
                // Select action from selected application list
                selectedAction = selectedApplication.Actions.FirstOrDefault(a => a.Name == selectedItem.ActionName);
            if (selectedAction == null) return;
            // Get currently assigned gesture
            selectedGesture = selectedAction.GestureName;

            // Set current application, current action, and current gestures
            ApplicationManager.Instance.CurrentApplication = selectedApplication;
            GestureManager.Instance.GestureName = selectedGesture;

            ApplicationDialog applicationDialog = new ApplicationDialog(this, selectedAction);
            applicationDialog.ShowDialog();
            SelectAction(strApplicationHeader, selectedItem.ActionName, true);
        }

        private void SelectAction(string applicationName, string actionName, bool scrollIntoView)
        {
            foreach (ActionInfo ai in lstAvailableActions.Items)
            {
                if (ai.ApplicationName.Equals(applicationName) && ai.ActionName.Equals(actionName))
                {
                    lstAvailableActions.SelectedItem = ai;
                    if (scrollIntoView)
                    {
                        lstAvailableActions.UpdateLayout();
                        lstAvailableActions.ScrollIntoView(ai);
                    }
                    return;
                }
            }
        }

        private async void cmdDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            // Verify that we have an item selected
            if (lstAvailableActions.SelectedItems.Count == 0)
            {
                await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("未选择项目", "删除前需要先选择一项 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                // MessageBox.Show("Please select and item before trying to delete.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Confirm user really wants to delete selected items
            if (await Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("删除确认", "确定要删除这个动作吗？", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
            {
                AffirmativeButtonText = "确定",
                NegativeButtonText = "取消",
                ColorScheme = MetroDialogColorScheme.Accented
            }) != MessageDialogResult.Affirmative) return;


            // Loop through selected actions
            for (int i = lstAvailableActions.SelectedItems.Count - 1; i >= 0; i--)
            {
                // Grab selected item
                ActionInfo selectedAction = lstAvailableActions.SelectedItems[i] as ActionInfo;

                // Get the name of the action
                string strActionName = selectedAction.ActionName;

                // Get name of application
                string strApplicationName = selectedAction.ApplicationName;

                // Is this a global action or application specific
                if (strApplicationName == ApplicationManager.Instance.GetGlobalApplication().Name)
                    // Delete action from global list
                    ApplicationManager.Instance.RemoveGlobalAction(strActionName);
                else
                    // Delete action from application
                    ApplicationManager.Instance.RemoveNonGlobalAction(strActionName);

            }
            BindActions();
            // Save entire list of applications
            ApplicationManager.Instance.SaveApplications();
        }

        private void lstAvailableActions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableRelevantButtons();
        }
        //如果有全选需求，再分别选择：界面+保存的数据
        private void ActionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo actionInfo = Common.UI.WindowsHelper.GetParentDependencyObject<ListBoxItem>(sender as CheckBox).Content as ActionInfo;
            if (actionInfo == null) return;
            ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, actionInfo.ApplicationName).IsEnabled = (sender as CheckBox).IsChecked.Value;
            ApplicationManager.Instance.SaveApplications();
        }





        private void btnAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (GestureManager.Instance.Gestures.Length == 0)
            {
                Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("无可用手势", "添加动作前需要先添加至少一项手势 ", MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确定",
                    ColorScheme = MetroDialogColorScheme.Accented
                });
                return;
            }
            ApplicationDialog applicationDialog = new ApplicationDialog(this);
            applicationDialog.Show();
        }



        public void BindActions()
        {
            ActionInfos.Clear();
            this.CopyActionMenuItem.Items.Clear();
            //Task task = new Task(() =>
            //{
            //    this.Dispatcher.BeginInvoke(new Action(() =>
            //     {
            // Add global actions to global applications group
            AddActionsToGroup(ApplicationManager.Instance.GetGlobalApplication().Name, ApplicationManager.Instance.GetGlobalApplication().Actions.OrderBy(a => a.Name));

            // Get all applications
            IApplication[] lstApplications = ApplicationManager.Instance.GetAvailableUserApplications();

            foreach (UserApplication App in lstApplications)
            {
                // Add this applications actions to applications group
                AddActionsToGroup(App.Name, App.Actions.OrderBy(a => a.Name));
            }

            EnableRelevantButtons();
            //     }));
            //});
            //task.Start();
        }
        private void AddActionsToGroup(string ApplicationName, IEnumerable<IAction> Actions)
        {
            MenuItem menuItem = new MenuItem() { Header = ApplicationName };
            menuItem.Click += CopyActionMenuItem_Click;
            this.CopyActionMenuItem.Items.Add(menuItem);

            string description = String.Empty;
            DrawingImage Thumb = null;
            string gestureName = String.Empty;
            string pluginName = String.Empty;
            // Loop through each global action
            foreach (Applications.Action currentAction in Actions)
            {
                // Ensure this action has a plugin
                if (PluginManager.Instance.PluginExists(currentAction.PluginClass, currentAction.PluginFilename))
                {

                    // Get plugin for this action
                    IPluginInfo pluginInfo = PluginManager.Instance.FindPluginByClassAndFilename(currentAction.PluginClass, currentAction.PluginFilename);

                    // Feed settings to plugin
                    if (!pluginInfo.Plugin.Deserialize(currentAction.ActionSettings))
                        currentAction.ActionSettings = pluginInfo.Plugin.Serialize();

                    pluginName = pluginInfo.Plugin.Name;
                    description = pluginInfo.Plugin.Description;
                }
                else
                {
                    pluginName = String.Empty;
                    description = "无关联动作";
                }
                // Get handle of action gesture
                IGesture actionGesture = GestureManager.Instance.GetNewestGestureSample(currentAction.GestureName);


                if (actionGesture == null)
                {
                    Thumb = null;
                    gestureName = String.Empty;

                }
                else
                {
                    var accent = MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current);
                    var brush = accent != null ? accent.Item2.Resources["HighlightBrush"] as Brush : SystemParameters.WindowGlassBrush;

                    Thumb = GestureImage.CreateImage(actionGesture.Points, sizThumbSize, brush);
                    gestureName = actionGesture.Name;
                }
                ActionInfo ai = new ActionInfo(
                   !String.IsNullOrEmpty(currentAction.Name) ? currentAction.Name : pluginName,
                    ApplicationName,
                    description,
                    Thumb,
                    gestureName,
                    currentAction.IsEnabled);
                ActionInfos.Add(ai);

            }
        }


        private void EnableRelevantButtons()
        {
            cmdEdit.IsEnabled = (lstAvailableActions.SelectedItems.Count == 1);
            cmdDelete.IsEnabled = (lstAvailableActions.SelectedItems.Count > 0);
        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            ActionInfo ai = (sender as ListBoxItem).Content as ActionInfo;
            if (ai != null)
            {
                // Getting the ContentPresenter of myListBoxItem
                ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(sender as ListBoxItem);
                // if (myContentPresenter.ContentTemplate == null) return;
                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                // DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                ComboBox comboBox = (myContentPresenter.ContentTemplate.FindName("availableGesturesComboBox", myContentPresenter)) as ComboBox;
                comboBox.Visibility = Visibility.Visible;
                ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Visibility = Visibility.Collapsed;

                Binding bind = new Binding();
                bind.Source = ((GestureSign.Common.UI.WindowsHelper.GetParentDependencyObject<TabControl>(this)).FindName("availableGestures") as AvailableGestures).lstAvailableGestures;
                bind.Mode = BindingMode.OneWay;
                bind.Path = new PropertyPath("Items");
                comboBox.SetBinding(ComboBox.ItemsSourceProperty, bind);

                foreach (GestureItem item in comboBox.Items)
                {
                    if (item.Name == ai.GestureName)
                        comboBox.SelectedIndex = comboBox.Items.IndexOf(item);
                }
            }
        }
        private void ListBoxItem_Unselected(object sender, RoutedEventArgs e)
        {
            if ((sender as ListBoxItem).Content is ActionInfo)
            {
                // Getting the ContentPresenter of myListBoxItem
                ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(sender as ListBoxItem);
                if (myContentPresenter.ContentTemplate == null) return;
                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                // DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                ((myContentPresenter.ContentTemplate.FindName("availableGesturesComboBox", myContentPresenter)) as ComboBox).Visibility = Visibility.Collapsed;
                ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Visibility = Visibility.Visible;
            }
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void availableGesturesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            ContentPresenter myContentPresenter = Common.UI.WindowsHelper.GetParentDependencyObject<ContentPresenter>(sender as ComboBox);
            ActionInfo actionInfo = Common.UI.WindowsHelper.GetParentDependencyObject<ListBoxItem>(sender as ComboBox).Content as ActionInfo;
            if (((GestureItem)e.AddedItems[0]).Name != actionInfo.GestureName)
            {
                IAction action = ApplicationManager.Instance.GetAnyDefinedAction(actionInfo.ActionName, actionInfo.ApplicationName);
                actionInfo.GestureName = action.GestureName = ((GestureItem)e.AddedItems[0]).Name;
                ((myContentPresenter.ContentTemplate.FindName("GestureImage", myContentPresenter)) as Image).Source = ((sender as ComboBox).SelectedItem as GestureItem).Image;
                ApplicationManager.Instance.SaveApplications();
            }
        }

        private void CopyActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            var targetApplication = ApplicationManager.Instance.Applications.Find(
                   a => !(a is IgnoredApplication) && a.Name == menuItem.Header.ToString().Trim());

            if (targetApplication.Actions.Exists(a => a.Name == selectedItem.ActionName))
            {
                Common.UI.WindowsHelper.GetParentWindow(this).ShowMessageAsync("此动作已存在", String.Format("在 {0} 中已经存在 {1} 动作", menuItem.Header, selectedItem.ActionName),
                    MessageDialogStyle.Affirmative, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "确定",
                        ColorScheme = MetroDialogColorScheme.Accented
                    });
                return;
            }
            IAction selectedAction = ApplicationManager.Instance.GetAnyDefinedAction(selectedItem.ActionName, selectedItem.ApplicationName);
            Applications.Action newAction = new Applications.Action()
            {
                ActionSettings = selectedAction.ActionSettings,
                GestureName = selectedAction.GestureName,
                IsEnabled = selectedAction.IsEnabled,
                Name = selectedAction.Name,
                PluginClass = selectedAction.PluginClass,
                PluginFilename = selectedAction.PluginFilename
            };
            targetApplication.AddAction(selectedAction);

            BindActions();
            SelectAction(targetApplication.Name, newAction.Name, true);
            ApplicationManager.Instance.SaveApplications();
        }

        private void ImportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofdApplications = new Microsoft.Win32.OpenFileDialog() { Filter = "动作文件|*.json", Title = "导入动作定义文件", CheckFileExists = true };
            if (ofdApplications.ShowDialog().Value)
            {
                int addcount = 0;
                List<IApplication> newApps = Common.Configuration.FileManager.LoadObject<List<IApplication>>(ofdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(CustomApplication), typeof(IgnoredApplication), typeof(Applications.Action) }, false);

                if (newApps != null)
                {
                    newApps = newApps.ConvertAll<IApplication>(new Converter<IApplication, IApplication>(app =>
                        {
                            if (app is UserApplication && !(app is CustomApplication))
                                return new CustomApplication()
                                {
                                    Actions = app.Actions,
                                    IsRegEx = app.IsRegEx,
                                    InterceptTouchInput = true,
                                    MatchString = app.MatchString,
                                    MatchUsing = app.MatchUsing,
                                    Name = app.Name
                                };
                            else return app;
                        }));
                    foreach (IApplication newApp in newApps)
                    {
                        if (newApp is IgnoredApplication) continue;
                        if (ApplicationManager.Instance.ApplicationExists(newApp.Name))
                        {
                            var existingApp = ApplicationManager.Instance.Applications.Find(a => a.Name == newApp.Name);
                            foreach (IAction newAction in newApp.Actions)
                            {
                                if (existingApp.Actions.Exists(action => action.Name.Equals(newAction.Name)))
                                {
                                    var result = MessageBox.Show(String.Format("在 \"{0}\" 中已经存在 \"{1}\" 动作，是否覆盖？", existingApp.Name, newAction.Name), "已存在同名动作", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        existingApp.Actions.RemoveAll(ac => ac.Name.Equals(newAction.Name));
                                        existingApp.AddAction(newAction);
                                        addcount++;
                                    }
                                    else if (result == MessageBoxResult.Cancel) goto End;
                                }
                                else
                                {
                                    existingApp.AddAction(newAction);
                                    addcount++;
                                }
                            }
                        }
                        else
                        {
                            ApplicationManager.Instance.AddApplication(newApp);
                        }
                    }
                }
            End:
                if (addcount != 0)
                {
                    ApplicationManager.Instance.SaveApplications();
                    BindActions();
                }
                MessageBox.Show(String.Format("已添加 {0} 个动作", addcount), "导入完成");
            }
        }

        private void ExportActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfdApplications = new Microsoft.Win32.SaveFileDialog() { Filter = "动作文件|*.json", Title = "导出动作定义文件", AddExtension = true, DefaultExt = "json", ValidateNames = true };
            if (sfdApplications.ShowDialog().Value)
            {
                Common.Configuration.FileManager.SaveObject<List<IApplication>>(ApplicationManager.Instance.Applications.Where(app => !(app is IgnoredApplication)).ToList(), sfdApplications.FileName, new Type[] { typeof(GlobalApplication), typeof(UserApplication), typeof(CustomApplication), typeof(IgnoredApplication), typeof(Applications.Action) });
            }
        }

        private void InterceptTouchInputMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return;
            if (selectedItem.ApplicationName.Equals(ApplicationManager.Instance.GetGlobalApplication().Name))
            {
                this.InterceptTouchInputMenuItem.IsEnabled = false;
            }
            else
            {
                this.InterceptTouchInputMenuItem.IsEnabled = true;
                this.InterceptTouchInputMenuItem.IsChecked =
                    ((CustomApplication)ApplicationManager.Instance.Applications.Find(app => app.Name.Equals(selectedItem.ApplicationName))).InterceptTouchInput;
            }
        }

        private void InterceptTouchInputMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ActionInfo selectedItem = (ActionInfo)lstAvailableActions.SelectedItem;
            if (selectedItem == null) return;
            var menuItem = (MenuItem)sender;
            ((CustomApplication)ApplicationManager.Instance.Applications.Find(app => app.Name.Equals(selectedItem.ApplicationName))).InterceptTouchInput = menuItem.IsChecked;

            ApplicationManager.Instance.SaveApplications();

        }

        private void AllActionsCheckBoxs_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = ((CheckBox)sender);
            bool isChecked = checkbox.IsChecked.Value;
            try
            {
                dynamic dc = checkbox.DataContext;
                foreach (ActionInfo ai in ActionInfos.Where(a => a.ApplicationName.Equals(dc.Name)))
                {
                    ai.IsEnabled = isChecked;
                }
            }
            catch { }
            ApplicationManager.Instance.SaveApplications();
        }
    }
    public class HeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = values[0] as string;
            int count = (int)values[1];
            if (name != null)
            {
                return String.Format("{0}  {1}个动作", name, count);

            }
            else return DependencyProperty.UnsetValue;
        }
        // 因为是只从数据源到目标的意向Binding，所以，这个函数永远也不会被调到
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return new object[3] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
        }
    }
}
