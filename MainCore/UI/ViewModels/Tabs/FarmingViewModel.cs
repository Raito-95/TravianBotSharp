﻿using MainCore.Commands.UI.AccountSettingViewModel;
using MainCore.Commands.UI.FarmingViewModel;
using MainCore.Commands.UI.Misc;
using MainCore.UI.Models.Input;
using MainCore.UI.Models.Output;
using MainCore.UI.ViewModels.Abstract;
using MainCore.UI.ViewModels.UserControls;
using Microsoft.Extensions.DependencyInjection;

namespace MainCore.UI.ViewModels.Tabs
{
    [RegisterSingleton<FarmingViewModel>]
    public partial class FarmingViewModel : AccountTabViewModelBase
    {
        public AccountSettingInput AccountSettingInput { get; } = new();
        public ListBoxItemViewModel FarmLists { get; } = new();

        private readonly IDialogService _dialogService;
        private readonly IValidator<AccountSettingInput> _accountsettingInputValidator;
        private readonly ICustomServiceScopeFactory _serviceScopeFactory;
        private readonly ITaskManager _taskManager;

        private static readonly Dictionary<SplatColor, string> _activeTexts = new()
        {
            { SplatColor.Green , "Deactive" },
            { SplatColor.Red , "Active" },
            { SplatColor.Black , "No farmlist selected" },
        };

        public FarmingViewModel(IDialogService dialogService, IValidator<AccountSettingInput> accountsettingInputValidator, ICustomServiceScopeFactory serviceScopeFactory, ITaskManager taskManager)
        {
            _accountsettingInputValidator = accountsettingInputValidator;
            _dialogService = dialogService;
            _serviceScopeFactory = serviceScopeFactory;
            _taskManager = taskManager;

            LoadFarmListCommand.Subscribe(items =>
            {
                FarmLists.Load(items);
                if (items.Count > 0)
                {
                    var color = FarmLists.SelectedItem?.Color ?? SplatColor.Black;
                    ActiveText = _activeTexts[color];
                }
            });
            LoadSettingCommand.Subscribe(AccountSettingInput.Set);
            ActiveFarmListCommand.Subscribe(x =>
            {
                var color = FarmLists.SelectedItem?.Color ?? SplatColor.Black;
                ActiveText = _activeTexts[color];
            });

            this.WhenAnyValue(x => x.FarmLists.SelectedItem)
                .WhereNotNull()
                .Subscribe(selectedItem =>
                {
                    var color = selectedItem.Color;
                    ActiveText = _activeTexts[color];
                });
        }

        public async Task FarmListRefresh(AccountId accountId)
        {
            if (!IsActive) return;
            if (accountId != AccountId) return;
            await LoadFarmListCommand.Execute(accountId);
        }

        protected override async Task Load(AccountId accountId)
        {
            await LoadFarmListCommand.Execute(accountId);
            await LoadSettingCommand.Execute(accountId);
        }

        [ReactiveCommand]
        private async Task UpdateFarmList()
        {
            _taskManager.AddOrUpdate<UpdateFarmListTask.Task>(new(AccountId));
            await _dialogService.MessageBox.Handle(new MessageBoxData("Information", "Added update farm list task"));
        }

        [ReactiveCommand]
        private async Task Start()
        {
            using var scope = _serviceScopeFactory.CreateScope(AccountId);
            var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();

            var useStartAllButton = settingService.BooleanByName(AccountId, AccountSettingEnums.UseStartAllButton);
            if (!useStartAllButton)
            {
                var count = CountActive(AccountId);
                if (count == 0)
                {
                    await _dialogService.MessageBox.Handle(new MessageBoxData("Information", "There is no active farm or use start all button is disable"));
                    return;
                }
            }
            _taskManager.AddOrUpdate<StartFarmListTask.Task>(new(AccountId));
            await _dialogService.MessageBox.Handle(new MessageBoxData("Information", "Added start farm list task"));
        }

        [ReactiveCommand]
        private async Task Stop()
        {
            _taskManager.Remove<StartFarmListTask.Task>(AccountId);
            await _dialogService.MessageBox.Handle(new MessageBoxData("Information", "Removed start farm list task"));
        }

        [ReactiveCommand]
        private async Task Save()
        {
            var result = await _accountsettingInputValidator.ValidateAsync(AccountSettingInput);
            if (!result.IsValid)
            {
                await _dialogService.MessageBox.Handle(new MessageBoxData("Error", result.ToString()));
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope(AccountId);
            var saveAccountSettingCommand = scope.ServiceProvider.GetRequiredService<SaveAccountSettingCommand.Handler>();
            await saveAccountSettingCommand.HandleAsync(new(AccountId, AccountSettingInput.Get()));
            await _dialogService.MessageBox.Handle(new MessageBoxData("Information", "Saved"));
        }

        [ReactiveCommand]
        private async Task ActiveFarmList()
        {
            if (FarmLists.SelectedItem is null)
            {
                await _dialogService.ConfirmBox.Handle(new MessageBoxData("Warning", "No farm list selected"));
                return;
            }

            var selectedFarmList = FarmLists.SelectedItem;
            if (selectedFarmList is null) return;

            using var scope = _serviceScopeFactory.CreateScope(AccountId);
            var activationCommand = scope.ServiceProvider.GetRequiredService<ActivationCommand.Handler>();
            await activationCommand.HandleAsync(new(AccountId, new FarmId(selectedFarmList.Id)));
            await _dialogService.MessageBox.Handle(new MessageBoxData("Information", "Activated farm list"));
        }

        [ReactiveCommand]
        private async Task<Dictionary<AccountSettingEnums, int>> LoadSetting(AccountId accountId)
        {
            using var scope = _serviceScopeFactory.CreateScope(AccountId);
            var getSettingQuery = scope.ServiceProvider.GetRequiredService<GetSettingQuery.Handler>();
            var items = await getSettingQuery.HandleAsync(new(accountId));
            return items;
        }

        [ReactiveCommand]
        private async Task<List<ListBoxItem>> LoadFarmList(AccountId accountId)
        {
            using var scope = _serviceScopeFactory.CreateScope(AccountId);
            var getFarmListItemsQuery = scope.ServiceProvider.GetRequiredService<GetFarmListItemsQuery.Handler>();
            var items = await getFarmListItemsQuery.HandleAsync(new(accountId));
            return items;
        }

        [Reactive]
        private string _activeText = "No farmlist selected";

        private int CountActive(AccountId accountId)
        {
            using var scope = _serviceScopeFactory.CreateScope(AccountId);
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var count = context.FarmLists
                .Where(x => x.AccountId == accountId.Value)
                .Where(x => x.IsActive)
                .Count();
            return count;
        }
    }
}