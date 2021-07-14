using m3u8_downloader_avalonia.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace m3u8_downloader_avalonia.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private string selectedMedium;
        private ObservableCollection<HeaderView> items = new ObservableCollection<HeaderView>();
        public ObservableCollection<HeaderView> Items
        {
            get => items;
            set => this.RaiseAndSetIfChanged(ref items, value);
        }

        private ObservableCollection<HeaderView> allItems;
        private ObservableCollection<string> mediums;
        private HeaderView selectedHeaderView;
        private const string AllMediums = "All";

        private List<HeaderView> headerViews = new List<HeaderView>();
        public ReactiveCommand<Unit, Unit> AddCommand { get; set; }
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; set; }

        public MainViewModel()
        {
            //PopulateData();

            AddCommand = ReactiveCommand.Create(AddCommand_ExecuteRequested);

            DeleteCommand = ReactiveCommand.Create(DeleteCommand_ExecuteRequested);
        }

        private void AddCommand_ExecuteRequested()
        {
            Items.Add(new HeaderView());
        }

        private void DeleteCommand_ExecuteRequested()
        {
            Items.Add(new HeaderView());
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

        public HeaderView SelectedMediaItem
        {
            get => selectedHeaderView;
            set => this.RaiseAndSetIfChanged(ref selectedHeaderView, value);
        }

        public ObservableCollection<string> Mediums
        {
            get => mediums;
            set => this.RaiseAndSetIfChanged(ref mediums, value);
        }

        public string SelectedMedium
        {
            get
            {
                return selectedMedium;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref selectedMedium, value);

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
            set => this.RaiseAndSetIfChanged(ref selectedHeaderView, value);
        }

        private void DeleteItem()
        {
            allItems.Remove(SelectedHeaderView);
            Items.Remove(SelectedHeaderView);
        }

        private bool CanDeleteItem() => SelectedHeaderView != null;
    }
}