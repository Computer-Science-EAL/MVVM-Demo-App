using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using DemoApp.ViewModel;
using DevZest.Windows.Docking;

namespace DemoApp
{
    public class MyDockControl : DockControl
    {
        public static readonly DependencyProperty WorkspacesProperty = DependencyProperty.Register("Workspaces", typeof(ObservableCollection<WorkspaceViewModel>), typeof(MyDockControl),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(_OnWorkspacesChanged)));

        ICollectionView _workspacesView;
        Dictionary<WorkspaceViewModel, DockItem> _dockItems = new Dictionary<WorkspaceViewModel,DockItem>();

        public ObservableCollection<WorkspaceViewModel> Workspaces
        {
            get { return (ObservableCollection<WorkspaceViewModel>)GetValue(WorkspacesProperty); }
            set { SetValue(WorkspacesProperty, value); }
        }

        static void _OnWorkspacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MyDockControl)d).OnWorkspacesChanged((ObservableCollection<WorkspaceViewModel>)e.OldValue, (ObservableCollection<WorkspaceViewModel>)e.NewValue);
        }

        void OnWorkspacesChanged(ObservableCollection<WorkspaceViewModel> oldValue, ObservableCollection<WorkspaceViewModel> newValue)
        {
            // For simplicity, we assume Workspaces property changed only once
            _workspacesView = CollectionViewSource.GetDefaultView(this.Workspaces);
            _workspacesView.CollectionChanged += this.OnWorkspacesChanged;
            _workspacesView.CurrentChanged += this.OnActiveWorkspaceChanged;
        }

        void OnWorkspacesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.NewItems)
                {
                    DockItem dockItem = new DockItem();
                    dockItem.HideOnPerformClose = false;
                    dockItem.Content = workspace;
                    dockItem.AllowedDockTreePositions = AllowedDockTreePositions.Document;
                    dockItem.TabText = workspace.DisplayName;
                    dockItem.Show(this, DockPosition.Document);
                    _dockItems.Add(workspace, dockItem);
                }

            if (e.OldItems != null && e.OldItems.Count != 0)
                foreach (WorkspaceViewModel workspace in e.OldItems)
                {
                    _dockItems.Remove(workspace);
                }
        }

        void OnActiveWorkspaceChanged(object sender, EventArgs e)
        {
            WorkspaceViewModel activeWorkspace = (WorkspaceViewModel)_workspacesView.CurrentItem;
            if (activeWorkspace != null && _dockItems.ContainsKey(activeWorkspace))
                _dockItems[activeWorkspace].Activate();
        }

        protected override void OnDockItemStateChanged(DockItemStateEventArgs e)
        {
            base.OnDockItemStateChanged(e);
            WorkspaceViewModel workspace = e.DockItem.Content as WorkspaceViewModel;
            if (workspace != null)
            {
                if (e.NewDockPosition == DockPosition.Unknown)  // DockItem closed
                    workspace.CloseCommand.Execute(null);
                else if (e.NewDockPosition == DockPosition.Document && e.DockItem.IsSelected)
                    _workspacesView.MoveCurrentTo(workspace);
            }
        }
    }
}
