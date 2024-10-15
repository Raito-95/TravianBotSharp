using MainCore.Commands.UI.Villages;
using MainCore.UI.Models.Input;
using MainCore.UI.Models.Output;
using MainCore.UI.ViewModels.Abstract;
using MainCore.UI.ViewModels.UserControls;
using ReactiveUI;
using System.Reactive.Linq;
using System.Text.Json;

namespace MainCore.UI.ViewModels.Tabs.Villages
{
    [RegisterSingleton<BuildViewModel>]
    public class BuildViewModel : VillageTabViewModelBase
    {
        private readonly IMediator _mediator;
        private readonly IDialogService _dialogService;
        private readonly ITaskManager _taskManager;

        private ReactiveCommand<ListBoxItem, List<BuildingEnums>> LoadBuildNormal { get; }

        public ReactiveCommand<Unit, Unit> BuildNormal { get; }
        public ReactiveCommand<Unit, Unit> BuildResource { get; }
        public ReactiveCommand<Unit, Unit> UpgradeOneLevel { get; }
        public ReactiveCommand<Unit, Unit> UpgradeMaxLevel { get; }

        public ReactiveCommand<Unit, Unit> Up { get; }
        public ReactiveCommand<Unit, Unit> Down { get; }
        public ReactiveCommand<Unit, Unit> Top { get; }
        public ReactiveCommand<Unit, Unit> Bottom { get; }
        public ReactiveCommand<Unit, Unit> Delete { get; }
        public ReactiveCommand<Unit, Unit> DeleteAll { get; }
        public ReactiveCommand<Unit, Unit> Import { get; }
        public ReactiveCommand<Unit, Unit> Export { get; }
        public ReactiveCommand<VillageId, List<ListBoxItem>> LoadBuilding { get; }
        public ReactiveCommand<VillageId, List<ListBoxItem>> LoadJob { get; }
        public ReactiveCommand<VillageId, List<ListBoxItem>> LoadQueue { get; }

        public NormalBuildInput NormalBuildInput { get; } = new();

        public ResourceBuildInput ResourceBuildInput { get; } = new();

        public ListBoxItemViewModel Buildings { get; } = new();
        public ListBoxItemViewModel Queue { get; } = new();
        public ListBoxItemViewModel Jobs { get; } = new();

        public BuildViewModel(IMediator mediator, IDialogService dialogService, ITaskManager taskManager)
        {
            _mediator = mediator;
            _dialogService = dialogService;
            _taskManager = taskManager;

            BuildNormal = ReactiveCommand.CreateFromTask(BuildNormalHandler);
            BuildResource = ReactiveCommand.CreateFromTask(ResourceNormalHandler);
            UpgradeOneLevel = ReactiveCommand.CreateFromTask(UpgradeOneLevelHandler);
            UpgradeMaxLevel = ReactiveCommand.CreateFromTask(UpgradeMaxLevelHandler);

            Up = ReactiveCommand.CreateFromTask(UpHandler);
            Down = ReactiveCommand.CreateFromTask(DownHandler);
            Top = ReactiveCommand.CreateFromTask(TopHandler);
            Bottom = ReactiveCommand.CreateFromTask(BottomHandler);
            Delete = ReactiveCommand.CreateFromTask(DeleteHandler);
            DeleteAll = ReactiveCommand.CreateFromTask(DeleteAllHandler);
            Import = ReactiveCommand.CreateFromTask(ImportHandler);
            Export = ReactiveCommand.CreateFromTask(ExportHandler);

            LoadBuilding = ReactiveCommand.Create<VillageId, List<ListBoxItem>>(LoadBuildingHandler);
            LoadJob = ReactiveCommand.Create<VillageId, List<ListBoxItem>>(LoadJobHandler);
            LoadQueue = ReactiveCommand.Create<VillageId, List<ListBoxItem>>(LoadQueueHandler);
            LoadBuildNormal = ReactiveCommand.Create<ListBoxItem, List<BuildingEnums>>(LoadBuildNormalHandler);

            this.WhenAnyValue(vm => vm.Buildings.SelectedItem)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .InvokeCommand(LoadBuildNormal);

            LoadBuilding.Subscribe(Buildings.Load);
            LoadJob.Subscribe(Jobs.Load);
            LoadQueue.Subscribe(Queue.Load);

            LoadBuildNormal.Subscribe(buildings =>
            {
                switch (buildings.Count)
                {
                    case 0:
                        NormalBuildInput.Clear();
                        break;

                    default:
                        NormalBuildInput.Set(buildings, -1);
                        break;
                }
            });
        }

        public async Task QueueRefresh(VillageId villageId)
        {
            if (!IsActive) return;
            if (villageId != VillageId) return;
            await LoadQueue.Execute(villageId);
        }

        public async Task BuildingListRefresh(VillageId villageId)
        {
            if (!IsActive) return;
            if (villageId != VillageId) return;
            await LoadBuilding.Execute(villageId);
        }

        public async Task JobListRefresh(VillageId villageId)
        {
            if (!IsActive) return;
            if (villageId != VillageId) return;
            await LoadJob.Execute(villageId);
            await LoadBuilding.Execute(villageId);
        }

        protected override async Task Load(VillageId villageId)
        {
            await LoadJob.Execute(villageId);
            await LoadBuilding.Execute(villageId);
            await LoadQueue.Execute(villageId);
        }

        private static List<ListBoxItem> LoadBuildingHandler(VillageId villageId)
        {
            var getBuildings = Locator.Current.GetService<GetBuildings>();
            var items = getBuildings.LayoutItems(villageId);
            return items;
        }

        private static List<ListBoxItem> LoadQueueHandler(VillageId villageId)
        {
            var getBuildings = Locator.Current.GetService<GetBuildings>();
            var items = getBuildings.QueueItems(villageId);
            return items;
        }

        private static List<ListBoxItem> LoadJobHandler(VillageId villageId)
        {
            var getJobs = Locator.Current.GetService<GetJobs>();
            var jobs = getJobs.Items(villageId);
            return jobs;
        }

        private List<BuildingEnums> LoadBuildNormalHandler(ListBoxItem item)
        {
            if (item is null) return [];
            var getBuildings = Locator.Current.GetService<GetBuildings>();
            var buildings = getBuildings.NormalBuilds(VillageId, new BuildingId(item.Id));
            return buildings;
        }

        private async Task BuildNormalHandler()
        {
            if (!IsAccountPaused(AccountId)) return;

            var buildNormalCommand = Locator.Current.GetService<BuildNormalCommand>();
            var location = Buildings.SelectedIndex + 1;
            await buildNormalCommand.Execute(AccountId, VillageId, NormalBuildInput, location);
        }

        private async Task UpgradeOneLevelHandler()
        {
            if (!IsAccountPaused(AccountId)) return;

            var upgradeLevel = Locator.Current.GetService<UpgradeLevel>();
            var location = Buildings.SelectedIndex + 1;
            await upgradeLevel.Execute(AccountId, VillageId, location, false);
        }

        private async Task UpgradeMaxLevelHandler()
        {
            if (!IsAccountPaused(AccountId)) return;

            var upgradeLevel = Locator.Current.GetService<UpgradeLevel>();
            var location = Buildings.SelectedIndex + 1;
            await upgradeLevel.Execute(AccountId, VillageId, location, true);
        }

        private async Task ResourceNormalHandler()
        {
<<<<<<< HEAD
            var status = _taskManager.GetStatus(AccountId);
            if (status == StatusEnums.Online)
            {
                _dialogService.ShowMessageBox("警告", "請暫停帳號後再修改建築佇列");
                return;
            }
            var result = await _resourceBuildInputValidator.ValidateAsync(ResourceBuildInput);
            if (!result.IsValid)
            {
                _dialogService.ShowMessageBox("錯誤", result.ToString());
                return;
            }
=======
            if (!IsAccountPaused(AccountId)) return;
>>>>>>> upstream/main

            var buildResourceCommand = Locator.Current.GetService<BuildResourceCommand>();
            await buildResourceCommand.Execute(AccountId, VillageId, ResourceBuildInput);
        }

        private async Task UpHandler()
        {
            if (!IsAccountPaused(AccountId)) return;
            var moveJobCommand = Locator.Current.GetService<MoveJobCommand>();
            await moveJobCommand.Execute(AccountId, VillageId, Jobs, MoveEnums.Up);
        }

        private async Task DownHandler()
        {
            if (!IsAccountPaused(AccountId)) return;
            var moveJobCommand = Locator.Current.GetService<MoveJobCommand>();
            await moveJobCommand.Execute(AccountId, VillageId, Jobs, MoveEnums.Down);
        }

        private async Task TopHandler()
        {
            if (!IsAccountPaused(AccountId)) return;
            var moveJobCommand = Locator.Current.GetService<MoveJobCommand>();
            await moveJobCommand.Execute(AccountId, VillageId, Jobs, MoveEnums.Top);
        }

        private async Task BottomHandler()
        {
            if (!IsAccountPaused(AccountId)) return;
            var moveJobCommand = Locator.Current.GetService<MoveJobCommand>();
            await moveJobCommand.Execute(AccountId, VillageId, Jobs, MoveEnums.Bottom);
        }

        private async Task DeleteHandler()
        {
<<<<<<< HEAD
            var status = _taskManager.GetStatus(AccountId);
            if (status == StatusEnums.Online)
            {
                _dialogService.ShowMessageBox("警告", "請暫停帳號後再修改建築佇列");
                return;
            }
=======
            if (!IsAccountPaused(AccountId)) return;
>>>>>>> upstream/main
            if (!Jobs.IsSelected) return;
            var jobId = Jobs.SelectedItemId;

            var deleteJobCommand = Locator.Current.GetService<DeleteJobCommand>();
            deleteJobCommand.ByJobId(new JobId(jobId));
            await _mediator.Publish(new JobUpdated(AccountId, VillageId));
        }

        private async Task DeleteAllHandler()
        {
<<<<<<< HEAD
            var status = _taskManager.GetStatus(AccountId);
            if (status == StatusEnums.Online)
            {
                _dialogService.ShowMessageBox("警告", "請暫停帳號後再修改建築佇列");
                return;
            }
            using var context = await _contextFactory.CreateDbContextAsync();

            //sqlite async dont work
#pragma warning disable S6966 // Awaitable method should be used
            context.Jobs
                .Where(x => x.VillageId == VillageId.Value)
                .ExecuteDelete();
#pragma warning restore S6966 // Awaitable method should be used

=======
            if (!IsAccountPaused(AccountId)) return;
            var deleteJobCommand = Locator.Current.GetService<DeleteJobCommand>();
            deleteJobCommand.ByVillageId(VillageId);
>>>>>>> upstream/main
            await _mediator.Publish(new JobUpdated(AccountId, VillageId));
        }

        private async Task ImportHandler()
        {
            if (!IsAccountPaused(AccountId)) return;
            var importCommand = Locator.Current.GetService<ImportCommand>();
            await importCommand.Execute(AccountId, VillageId);
        }

        private async Task ExportHandler()
        {
            var path = _dialogService.SaveFileDialog();
            if (string.IsNullOrEmpty(path)) return;
            var getJobs = Locator.Current.GetService<GetJobs>();
            var jobs = getJobs.Dtos(VillageId);
            jobs.ForEach(job => job.Id = JobId.Empty);
            var jsonString = JsonSerializer.Serialize(jobs);
            await File.WriteAllTextAsync(path, jsonString);
            _dialogService.ShowMessageBox("資訊", "工作清單已匯出");
        }

<<<<<<< HEAD
        private static List<ListBoxItem> GetBuildingItems(VillageId villageId)
        {
            var items = new GetBuildings().Execute(villageId).Select(x => ToListBoxItem(x)).ToList();
            return items;
        }

        private static ListBoxItem ToListBoxItem(BuildingItem building)
        {
            const string arrow = " -> ";
            var sb = new StringBuilder();
            sb.Append(building.CurrentLevel);
            if (building.QueueLevel != 0)
            {
                var content = $"{arrow}({building.QueueLevel})";
                sb.Append(content);
            }
            if (building.JobLevel != 0)
            {
                var content = $"{arrow}[{building.JobLevel}]";
                sb.Append(content);
            }

            var item = new ListBoxItem()
            {
                Id = building.Id.Value,
                Content = $"[{building.Location}] {GetNameInChinese(building.Type)} | lvl {sb}",
                Color = building.Type.GetColor(),
            };
            return item;
        }

        private List<BuildingEnums> GetNormalBuilding(VillageId villageId, BuildingId buildingId)
        {
            var buildingItems = new GetBuildings().Execute(villageId);
            var type = buildingItems
                .Where(x => x.Id == buildingId)
                .Select(x => x.Type)
                .FirstOrDefault();
            if (type != BuildingEnums.Site) return new() { type };
            using var context = _contextFactory.CreateDbContext();

            var buildings = buildingItems
                .Select(x => x.Type)
                .Where(x => !MultipleBuildings.Contains(x))
                .Distinct()
                .ToList();

            return AvailableBuildings.Where(x => !buildings.Contains(x)).ToList();
        }

        private static readonly List<BuildingEnums> MultipleBuildings = new()
        {
            BuildingEnums.Warehouse,
            BuildingEnums.Granary,
            BuildingEnums.Trapper,
            BuildingEnums.Cranny,
        };

        private static readonly List<BuildingEnums> IgnoreBuildings = new()
        {
            BuildingEnums.Site,
            BuildingEnums.Blacksmith,
            BuildingEnums.CityWall,
            BuildingEnums.EarthWall,
            BuildingEnums.Palisade,
            BuildingEnums.WW,
            BuildingEnums.StoneWall,
            BuildingEnums.MakeshiftWall,
        };

        private static readonly IEnumerable<BuildingEnums> AvailableBuildings = Enum.GetValues(typeof(BuildingEnums))
            .Cast<BuildingEnums>()
            .Where(x => !IgnoreBuildings.Contains(x));

        private List<ListBoxItem> GetQueueItems(VillageId villageId)
        {
            using var context = _contextFactory.CreateDbContext();

            var queue = context.QueueBuildings
                .Where(x => x.VillageId == villageId.Value)
                .Where(x => x.Type != BuildingEnums.Site)
                .AsEnumerable()
                .Select(x => new ListBoxItem()
                {
                    Id = x.Id,
                    Content = $"{GetNameInChinese(x.Type)} 到等級 {x.Level} 完成於 {x.CompleteTime}",
                })
                .ToList();

            var tribe = (TribeEnums)context.VillagesSetting
                .Where(x => x.VillageId == villageId.Value)
                .Where(x => x.Setting == VillageSettingEnums.Tribe)
                .Select(x => x.Value)
                .FirstOrDefault();

            var count = 2;
            if (tribe == TribeEnums.Romans) count = 3;
            queue.AddRange(Enumerable.Range(0, count - queue.Count).Select(x => new ListBoxItem()));

            return queue;
        }

        private List<ListBoxItem> GetJobItems(VillageId villageId)
        {
            using var context = _contextFactory.CreateDbContext();

            var items = context.Jobs
                .Where(x => x.VillageId == villageId.Value)
                .OrderBy(x => x.Position)
                .ToDto()
                .AsEnumerable()
                .Select(x => new ListBoxItem()
                {
                    Id = x.Id.Value,
                    Content = GetContent(x),
                })
                .ToList();

            return items;
        }

        private static string GetContent(JobDto job)
        {
            switch (job.Type)
            {
                case JobTypeEnums.NormalBuild:
                    {
                        var plan = JsonSerializer.Deserialize<NormalBuildPlan>(job.Content);
                        return $"建造 {GetNameInChinese(plan.Type)} 到等級 {plan.Level} 位置 {plan.Location}";
                    }
                case JobTypeEnums.ResourceBuild:
                    {
                        var plan = JsonSerializer.Deserialize<ResourceBuildPlan>(job.Content);
                        return $"建造 {plan.Plan.Humanize()} 到等級 {plan.Level}";
                    }
                default:
                    return job.Content;
            }
        }

        private List<JobDto> GetJobs(VillageId villageId)
        {
            using var context = _contextFactory.CreateDbContext();

            var jobs = context.Jobs
                .Where(x => x.VillageId == villageId.Value)
                .OrderBy(x => x.Position)
                .ToDto()
                .ToList();
            return jobs;
        }

        public async Task UpgradeLevel(AccountId accountId, VillageId villageId, int location, bool isMaxLevel)
=======
        private bool IsAccountPaused(AccountId accountId)
>>>>>>> upstream/main
        {
            var status = _taskManager.GetStatus(accountId);
            if (status == StatusEnums.Online)
            {
<<<<<<< HEAD
                _dialogService.ShowMessageBox("警告", "請暫停帳號後再修改建築佇列");
                return;
=======
                _dialogService.ShowMessageBox("Warning", "Please pause account before modifing building queue");
                return false;
>>>>>>> upstream/main
            }
            return true;
        }
        public static string GetNameInChinese(Enum enumValue)
        {
            switch (enumValue)
            {
                case BuildingEnums buildingEnum:
                    return ((BuildingEnumsChinese)(int)buildingEnum).ToString();
                case ResourcePlanEnums resourcePlanEnum:
                    return ((ResourcePlanEnumsChinese)(int)resourcePlanEnum).ToString();
                case TroopEnums troopEnum:
                    return ((TroopEnumsChinese)(int)troopEnum).ToString();
                case TribeEnums tribeEnum:
                    return ((TribeEnumsChinese)(int)tribeEnum).ToString();
                default:
                    return enumValue.ToString();
            }
        }
    }
}