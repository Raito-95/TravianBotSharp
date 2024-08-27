using FluentValidation;
using MainCore.Tasks;
using MainCore.UI.Models.Input;
using MainCore.UI.Models.Output;
using MainCore.UI.ViewModels.Abstract;
using MainCore.UI.ViewModels.UserControls;
using ReactiveUI;
using System.Drawing;
using System.Reactive.Linq;

namespace MainCore.UI.ViewModels.Tabs
{
    [RegisterSingleton(Registration = RegistrationStrategy.Self)]
    public class FarmingViewModel : AccountTabViewModelBase
    {
        private readonly IValidator<FarmListSettingInput> _farmListSettingInputValidator;

        public FarmListSettingInput FarmListSettingInput { get; } = new();
        public ListBoxItemViewModel FarmLists { get; } = new();

        private readonly IMediator _mediator;
        private readonly IDialogService _dialogService;
        private readonly ITaskManager _taskManager;

        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public ReactiveCommand<Unit, Unit> UpdateFarmList { get; }
        public ReactiveCommand<Unit, Unit> Save { get; }
        public ReactiveCommand<Unit, Unit> ActiveFarmList { get; }
        public ReactiveCommand<Unit, Unit> Start { get; }
        public ReactiveCommand<Unit, Unit> Stop { get; }
        public ReactiveCommand<AccountId, List<ListBoxItem>> LoadFarmList { get; }
        public ReactiveCommand<AccountId, Dictionary<AccountSettingEnums, int>> LoadSetting { get; }

        private static readonly Dictionary<Color, string> _activeTexts = new()
        {
            { Color.Green , "停用" },
            { Color.Red , "啟用" },
            { Color.Black , "未選擇羊單列表" },
        };

        public FarmingViewModel(IMediator mediator, IDialogService dialogService, ITaskManager taskManager, IDbContextFactory<AppDbContext> contextFactory, IValidator<FarmListSettingInput> farmListSettingInputValidator)
        {
            _mediator = mediator;
            _dialogService = dialogService;
            _taskManager = taskManager;
            _contextFactory = contextFactory;
            _farmListSettingInputValidator = farmListSettingInputValidator;

            UpdateFarmList = ReactiveCommand.CreateFromTask(UpdateFarmListHandler);
            Start = ReactiveCommand.CreateFromTask(StartHandler);
            Stop = ReactiveCommand.CreateFromTask(StopHandler);

            Save = ReactiveCommand.CreateFromTask(SaveHandler);
            ActiveFarmList = ReactiveCommand.CreateFromTask(ActiveFarmListHandler);

            LoadFarmList = ReactiveCommand.Create<AccountId, List<ListBoxItem>>(LoadFarmListHandler);
            LoadSetting = ReactiveCommand.Create<AccountId, Dictionary<AccountSettingEnums, int>>(LoadSettingHandler);

            LoadFarmList.Subscribe(items =>
            {
                FarmLists.Load(items);
                if (items.Count > 0)
                {
                    var color = FarmLists.SelectedItem?.Color ?? Color.Black;
                    ActiveText = _activeTexts[color];
                }
            });
            LoadSetting.Subscribe(items => FarmListSettingInput.Set(items));
            ActiveFarmList.Subscribe(x =>
            {
                var color = FarmLists.SelectedItem?.Color ?? Color.Black;
                ActiveText = _activeTexts[color];
            });
        }

        public async Task FarmListRefresh(AccountId accountId)
        {
            if (!IsActive) return;
            if (accountId != AccountId) return;
            await LoadFarmList.Execute(accountId);
        }

        protected override async Task Load(AccountId accountId)
        {
            await LoadFarmList.Execute(accountId);
            await LoadSetting.Execute(accountId);
        }

        private async Task UpdateFarmListHandler()
        {
            await _taskManager.AddOrUpdate<UpdateFarmListTask>(AccountId);
            _dialogService.ShowMessageBox("資訊", "已新增更新羊單列表任務");
        }

        private async Task StartHandler()
        {
            var useStartAllButton = new GetSetting().BooleanByName(AccountId, AccountSettingEnums.UseStartAllButton);
            if (!useStartAllButton)
            {
                var count = CountActive(AccountId);
                if (count == 0)
                {
                    _dialogService.ShowMessageBox("資訊", "沒有啟用的羊單或已停用啟動全部按鈕");
                    return;
                }
            }
            await _taskManager.AddOrUpdate<StartFarmListTask>(AccountId);
            _dialogService.ShowMessageBox("資訊", "已新增啟動羊單列表任務");
        }

        private async Task StopHandler()
        {
            var task = _taskManager.Get<StartFarmListTask>(AccountId);

            if (task is not null) await _taskManager.Remove(AccountId, task);

            _dialogService.ShowMessageBox("資訊", "已移除啟動羊單列表任務");
        }

        private async Task SaveHandler()
        {
            var result = await _farmListSettingInputValidator.ValidateAsync(FarmListSettingInput);
            if (!result.IsValid)
            {
                _dialogService.ShowMessageBox("錯誤", result.ToString());
                return;
            }

            var settings = FarmListSettingInput.Get();
            new SetSettingCommand().Execute(AccountId, settings);
            await _mediator.Publish(new AccountSettingUpdated(AccountId));

            _dialogService.ShowMessageBox("資訊", "設定已儲存");
        }

        private async Task ActiveFarmListHandler()
        {
            var selectedFarmList = FarmLists.SelectedItem;
            if (selectedFarmList is null)
            {
                _dialogService.ShowMessageBox("警告", "未選擇羊單列表");
                return;
            }

            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                // sqlite no async
#pragma warning disable S6966 // Awaitable method should be used
                context.FarmLists
                   .Where(x => x.Id == selectedFarmList.Id)
                   .ExecuteUpdate(x => x.SetProperty(x => x.IsActive, x => !x.IsActive));
#pragma warning restore S6966 // Awaitable method should be used
            }

            await _mediator.Publish(new FarmListUpdated(AccountId));
        }

        private Dictionary<AccountSettingEnums, int> LoadSettingHandler(AccountId accountId)
        {
            var items = new GetSetting().Get(accountId);
            return items;
        }

        private List<ListBoxItem> LoadFarmListHandler(AccountId accountId)
        {
            using var context = _contextFactory.CreateDbContext();

            var items = context.FarmLists
                .Where(x => x.AccountId == accountId.Value)
                .Select(x => new ListBoxItem()
                {
                    Id = x.Id,
                    Color = x.IsActive ? Color.Green : Color.Red,
                    Content = x.Name,
                })
                .ToList();

            return items;
        }

        private string _activeText = "未選擇羊單列表";

        public string ActiveText
        {
            get => _activeText;
            set => this.RaiseAndSetIfChanged(ref _activeText, value);
        }

        private int CountActive(AccountId accountId)
        {
            using var context = _contextFactory.CreateDbContext();

            var count = context.FarmLists
                .Where(x => x.AccountId == accountId.Value)
                .Where(x => x.IsActive)
                .Count();
            return count;
        }
    }
}