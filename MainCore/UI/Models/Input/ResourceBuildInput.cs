using Humanizer;
using MainCore.UI.Models.Output;
using MainCore.UI.ViewModels.Abstract;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace MainCore.UI.Models.Input
{
    public class ResourceBuildInput : ViewModelBase
    {
        public ResourceBuildInput()
        {
            Plans = new ObservableCollection<ComboBoxItem<ResourcePlanEnums>>();
            foreach (ResourcePlanEnums plan in Enum.GetValues(typeof(ResourcePlanEnums)))
            {
                var displayValue = Enum.GetName(typeof(ResourcePlanEnumsTW), (int)plan);
                Plans.Add(new ComboBoxItem<ResourcePlanEnums>(plan, displayValue));
            }

            SelectedPlan = Plans[0];
            Level = 10;
        }

        public (ResourcePlanEnums, int) Get()
        {
            return (SelectedPlan.Content, Level);
        }

        public ObservableCollection<ComboBoxItem<ResourcePlanEnums>> Plans { get; set; }

        private ComboBoxItem<ResourcePlanEnums> _selectedPlan;

        public ComboBoxItem<ResourcePlanEnums> SelectedPlan
        {
            get => _selectedPlan;
            set => this.RaiseAndSetIfChanged(ref _selectedPlan, value);
        }

        private int _level;

        public int Level
        {
            get => _level;
            set => this.RaiseAndSetIfChanged(ref _level, value);
        }
    }
}