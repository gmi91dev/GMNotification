using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using GMNotification;
using NotificationSample.ViewModels;

namespace NotificationSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private bool _isClosed;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainWindowViewModel(NotificationCenter.DefaultCenter(), Dispatcher);
            DataContext = _viewModel;
            _viewModel.LogEntries.CollectionChanged += OnLogEntriesChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosed = true;
            _viewModel.LogEntries.CollectionChanged -= OnLogEntriesChanged;
            _viewModel.Dispose();
            base.OnClosed(e);
        }

        private void OnLogEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add)
            {
                return;
            }

            // 延迟到当前集合变更完成后再滚动，避免在 CollectionChanged 中触发布局重入
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ScrollToLatestLog));
        }

        private void ScrollToLatestLog()
        {
            if (_isClosed || LogList.Items.Count == 0)
            {
                return;
            }

            LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
        }

        private async void OnSuccessRequestClick(object sender, RoutedEventArgs e)
            => await _viewModel.RunSuccessRequestAsync();

        private async void OnFailureRequestClick(object sender, RoutedEventArgs e)
            => await _viewModel.RunFailureRequestAsync();

        private async void OnBackgroundRequestClick(object sender, RoutedEventArgs e)
            => await _viewModel.RunBackgroundRequestAsync();

        private void OnToggleSuccessSubscriptionClick(object sender, RoutedEventArgs e)
            => _viewModel.ToggleSuccessSubscription();

        private void OnThrowCallbackErrorClick(object sender, RoutedEventArgs e)
            => _viewModel.TriggerCallbackError();

        private void OnDemoOverrideClick(object sender, RoutedEventArgs e)
            => _viewModel.TriggerOverrideDemo();

        private void OnClearLogClick(object sender, RoutedEventArgs e)
            => _viewModel.ClearLog();
    }
}
