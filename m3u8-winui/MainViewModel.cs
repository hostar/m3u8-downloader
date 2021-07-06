using m3u8_winui.Models;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace m3u8_winui
{
    // source: https://github.com/PacktPublishing/-Learn-WinUI-3.0
    public class MainViewModel : BindableBase
    {
        private string selectedMedium;
        private ObservableCollection<HeaderView> items = new ObservableCollection<HeaderView>();
        private ObservableCollection<HeaderView> allItems;
        private ObservableCollection<string> mediums;
        private HeaderView selectedHeaderView;
        private const string AllMediums = "All";

        private List<HeaderView> headerViews = new List<HeaderView>();
        public XamlUICommand AddCommand { get; set; }
        public XamlUICommand DeleteCommand { get; set; }

        public MainViewModel()
        {
            //PopulateData();

            AddCommand = new StandardUICommand();
            AddCommand.ExecuteRequested += AddCommand_ExecuteRequested;

            DeleteCommand = new StandardUICommand();
            DeleteCommand.ExecuteRequested += DeleteCommand_ExecuteRequested;
        }

        private void AddCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            items.Add(new HeaderView());
        }

        private void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            items.Add(new HeaderView());
        }

        public void PopulateData()
        {
            items.Clear();

            foreach (var item in headerViews)
            {
                items.Add(item);
            }

            allItems = new ObservableCollection<HeaderView>(Items);

            mediums = new ObservableCollection<string>
            {
                AllMediums
            };

            /*
            foreach (var itemType in _dataService.GetItemTypes())
            {
                mediums.Add(itemType.ToString());
            }
            */

            selectedMedium = Mediums[0];
        }

        public ObservableCollection<HeaderView> Items
        {
            get
            {
                return items;
            }
            set
            {
                SetProperty(ref items, value);
            }
        }

        public HeaderView SelectedMediaItem
        {
            get => selectedHeaderView;
            set
            {
                SetProperty(ref selectedHeaderView, value);
                //((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> Mediums
        {
            get
            {
                return mediums;
            }
            set
            {
                SetProperty(ref mediums, value);
            }
        }

        public string SelectedMedium
        {
            get
            {
                return selectedMedium;
            }
            set
            {
                SetProperty(ref selectedMedium, value);

                Items.Clear();
                foreach (var item in from item in allItems
                                     where string.IsNullOrWhiteSpace(selectedMedium) ||
                                           selectedMedium == "All" ||
                                           selectedMedium == item.Name.ToString()
                                     select item)
                {
                    Items.Add(item);
                }
            }
        }

        public HeaderView SelectedHeaderView
        {
            get => selectedHeaderView;
            set
            {
                SetProperty(ref selectedHeaderView, value);
                //((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            }
        }

        private void DeleteItem()
        {
            allItems.Remove(SelectedHeaderView);
            Items.Remove(SelectedHeaderView);
        }

        private bool CanDeleteItem() => SelectedHeaderView != null;
    }
}
